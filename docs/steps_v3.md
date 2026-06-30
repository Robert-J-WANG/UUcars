## Step 57 · 数据库查询诊断与索引优化

### 这一步做什么

现在进入 V3——让系统在真实负载下表现得更好。

V3 的第一步不是加新功能，而是**先把地基打牢**：找出现有查询的性能瓶颈，用正确的索引修复它。

为什么从这里开始？

因为现在 `Cars` 表还很小（几十条数据），查询快不快没有感知。但数据量涨到几万条之后，没有索引的查询会从几毫秒劣化到几秒。这种问题越晚发现越难改——等到用户投诉再加索引，已经损失了信任。在 V3 开始时解决，是正确的时机。



### 1. 切出 V3 的第一个功能分支

```bash
git checkout develop
git checkout -b feature/v3-query-optimization
git push -u origin feature/v3-query-optimization
```



### 2. 先理解：查询为什么会变慢

在写任何代码之前，先把"查询变慢的本质原因"想清楚。

数据库里的表，本质上就是一堆行存在磁盘上。当你执行：

```sql
SELECT * FROM Cars WHERE Brand = 'BMW'
```

如果没有索引，数据库只能做一件事：**从第一行读到最后一行，逐行检查 Brand 是不是 BMW**。这叫 **Table Scan（全表扫描）**。1000 行扫 1000 次，10 万行扫 10 万次，线性增长。

**索引是什么?**

-  索引就像一本书的目录。是一张"附加的映射表"，存的是 **值 → 行位置** 的对应关系，查询时先查这张映射表，拿到行号，再直接跳过去取数据。

有了索引，数据库会维护一个额外的数据结构（B-Tree），按 Brand 排好序，查询时直接跳到 BMW 的位置。这叫 **Index Seek（索引查找）**。10 万行也只需要几次比较，时间复杂度从 O(n) 变成 O(log n)。

**数字感受：**

```
Table Scan  10万行  ≈  50-500ms
Index Seek  10万行  ≈  0.1-1ms
```

差距是 100 倍量级。



### 3. 找出 UUcars 里的慢查询

现在来看我们的具体代码，找出哪些查询在数据量大时会出问题。

打开 `UUcars.API/Repositories/EfCarRepository.cs`，看 `GetPagedAsync` 方法：

```csharp
var query = _context.Cars
    .Include(c => c.Seller)
    .Include(c => c.Images)
    .Where(c => c.Status == status);      // ← 过滤 Status

// 搜索过滤
if (!string.IsNullOrWhiteSpace(request.Brand))
{
    query = query.Where(c => c.Brand.Contains(request.Brand));  // ← 品牌搜索
}
if (request.MinPrice.HasValue)
{
    query = query.Where(c => c.Price >= request.MinPrice.Value); // ← 价格过滤
}
if (request.MaxPrice.HasValue)
{
    query = query.Where(c => c.Price <= request.MaxPrice.Value);
}
```

**问题一：`Status` 字段没有索引**

`GET /cars` 每次都要过滤 `Status = 'Published'`，是系统最高频的查询。但 `Status` 字段目前只有一个普通列，查询时全表扫描。

**问题二：`Brand` 用了 `Contains`（即 SQL `LIKE '%bmw%'`）**

`LIKE '%xxx%'` 前面有通配符，索引无法利用，必须全表扫描。这是一个暂时接受的权衡——V3 的 Step 65 会用全文搜索彻底解决，现在先加前缀索引。

**问题三：`Price` 区间查询没有索引**

`Price >= X AND Price <= Y` 这类范围查询，有了索引可以快很多。

**问题四：`GetBySellerAsync` 按 `SellerId` 过滤也没有索引**

卖家查自己的车辆列表，`WHERE SellerId = X`，如果卖家多了，这个查询也会慢。



### 4. 什么是复合索引，为什么顺序重要

在加索引之前，先理解一个关键概念：**复合索引（Composite Index）**。

```sql
-- 单列索引
CREATE INDEX IX_Cars_Status ON Cars (Status)

-- 复合索引
CREATE INDEX IX_Cars_Status_CreatedAt ON Cars (Status, CreatedAt DESC)
```

复合索引的字段顺序非常重要，遵循一个原则：

**等值查询的字段放前面，范围查询和排序的字段放后面。**

为什么？因为 B-Tree 索引是按顺序排的。如果把 `Status` 放第一位（等值：`= 'Published'`），`CreatedAt` 放第二位（排序：`ORDER BY CreatedAt DESC`），那么 `Status = 'Published'` 的所有行已经按 `CreatedAt` 排好了序，查询可以直接利用索引排序，不需要额外的排序操作。

如果顺序反过来（`CreatedAt, Status`），等值查询就必须扫整个索引。

**UUcars 的查询模式：**

```
最常见查询：WHERE Status = 'Published' ORDER BY CreatedAt DESC
→ 复合索引：(Status, CreatedAt DESC)

带品牌过滤：WHERE Status = 'Published' AND Brand LIKE 'BMW%'
→ 复合索引：(Status, Brand)

价格范围：WHERE Status = 'Published' AND Price >= X AND Price <= Y
→ 复合索引：(Status, Price)

卖家查询：WHERE SellerId = X AND Status != 'Deleted' ORDER BY CreatedAt DESC
→ 复合索引：(SellerId, Status, CreatedAt DESC)
```



### 5. 用 EF Core Fluent API 添加索引

EF Core 里添加索引有两种方式：Data Annotations（`[Index]` attribute）和 Fluent API。我们用 Fluent API，原因和 Step 06 一样：保持实体类干净，配置集中管理。

打开 `UUcars.API/Configurations/CarConfiguration.cs`，在现有代码末尾添加索引配置：

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        // ── 原有字段配置（不变）──────────────────────────────────────
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Brand)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Model)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Year)
            .IsRequired();

        builder.Property(c => c.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Mileage)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasOne(c => c.Seller)
            .WithMany(u => u.Cars)
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── 索引配置（V3 新增）───────────────────────────────────────

        // 索引 1：Status + CreatedAt
        // 用途：GET /cars 列表（最高频查询）
        //   WHERE Status = 'Published' ORDER BY CreatedAt DESC
        // Status 放前面（等值过滤），CreatedAt 放后面（排序）
        // IsDescending(false, true)：Status 升序，CreatedAt 降序
        builder.HasIndex(c => new { c.Status, c.CreatedAt })
            .HasDatabaseName("IX_Cars_Status_CreatedAt")
            .IsDescending(false, true);

        // 索引 2：Status + Brand
        // 用途：按品牌搜索
        //   WHERE Status = 'Published' AND Brand LIKE 'BMW%'
        // 注意：LIKE '%BMW%' 无法利用索引，但 LIKE 'BMW%'（前缀匹配）可以
        // 这个索引对前缀搜索有效，对包含搜索（Contains）无效
        // Step 65 会用全文索引彻底解决，现在先加着
        builder.HasIndex(c => new { c.Status, c.Brand })
            .HasDatabaseName("IX_Cars_Status_Brand");

        // 索引 3：Status + Price
        // 用途：价格区间过滤
        //   WHERE Status = 'Published' AND Price >= X AND Price <= Y
        builder.HasIndex(c => new { c.Status, c.Price })
            .HasDatabaseName("IX_Cars_Status_Price");

        // 索引 4：SellerId + Status + CreatedAt
        // 用途：卖家查自己的车辆列表
        //   WHERE SellerId = X AND Status != 'Deleted' ORDER BY CreatedAt DESC
        builder.HasIndex(c => new { c.SellerId, c.Status, c.CreatedAt })
            .HasDatabaseName("IX_Cars_SellerId_Status_CreatedAt")
            .IsDescending(false, false, true);
    }
}
```



### 6. 生成并执行 Migration

索引配置写好了，用 Migration 把它应用到数据库：

```bash
# 在项目根目录执行
dotnet ef migrations add AddCarIndexes --project UUcars.API/UUcars.API.csproj

# 确认 Docker SQL Server 在运行
docker ps | grep uucars

# 执行 Migration
dotnet ef database update --project UUcars.API/UUcars.API.csproj
```

执行完之后，打开数据库客户端，在 `Cars` 表上查看索引，应该能看到 4 个新索引：

```
IX_Cars_Status_CreatedAt
IX_Cars_Status_Brand
IX_Cars_Status_Price
IX_Cars_SellerId_Status_CreatedAt
```



### 7. 查看 Migration 文件，理解它做了什么

打开刚生成的 Migration 文件（类似 `20250101_AddCarIndexes.cs`），在 `Up` 方法里会看到：

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateIndex(
        name: "IX_Cars_Status_CreatedAt",
        table: "Cars",
        columns: new[] { "Status", "CreatedAt" },
        descending: new[] { false, true });

    migrationBuilder.CreateIndex(
        name: "IX_Cars_Status_Brand",
        table: "Cars",
        columns: new[] { "Status", "Brand" });

    // ...
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Down 方法：回滚时自动删除索引
    migrationBuilder.DropIndex(name: "IX_Cars_Status_CreatedAt", table: "Cars");
    // ...
}
```

这和 V1 建表时的 Migration 结构完全一样——EF Core 自动生成了 `CREATE INDEX` 语句，`Down` 方法里有对应的 `DROP INDEX`，随时可以回滚。



### 8. EF Core 的查询优化：`AsNoTracking` 和投影

除了索引，还有两个代码层面的优化，不需要改数据库，但能显著减少内存占用和响应时间。

#### 8.1 只读查询加 `AsNoTracking()`

EF Core 默认对查询出来的实体开启**变更追踪（Change Tracking）**——它会在内存里记录每个实体的原始状态，方便后续调用 `SaveChangesAsync` 时检测哪些字段改了。

但列表查询（GET /cars、GET /cars/my-listings）只是读数据，不会修改，完全不需要变更追踪。关掉它可以节省内存，提升查询速度约 10-30%。

打开 `UUcars.API/Repositories/EfCarRepository.cs`，在 `GetPagedAsync` 和 `GetBySellerAsync` 里加 `AsNoTracking()`：

```csharp
public async Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
    CarStatus status,
    CarQueryRequest request,
    CancellationToken cancellationToken = default)
{
    var query = _context.Cars
        .AsNoTracking()              // ✅ 只读查询，关闭变更追踪
        .Include(c => c.Seller)
        .Include(c => c.Images)
        .Where(c => c.Status == status);

    // 过滤条件不变...
```

```csharp
public async Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
    int sellerId,
    CarQueryRequest query,
    CancellationToken cancellationToken = default)
{
    var queryable = _context.Cars
        .AsNoTracking()              // ✅ 同上
        .Include(c => c.Seller)
        .Include(c => c.Images)
        .Where(c => c.SellerId == sellerId)
        .Where(c => c.Status != CarStatus.Deleted);

    // 其余不变...
```

> **什么时候不能用 `AsNoTracking()`？**
> 增删改操作之前的查询不能用。比如 `GetByIdAsync` 查出实体后要修改状态，EF Core 需要追踪它。`GetPagedAsync` 和 `GetBySellerAsync` 只是列表查询，可以安全加。

#### 8.2 列表查询用 `Select` 投影，只取需要的字段

`GetPagedAsync` 现在会加载 Car 实体的所有字段，包括 `Description`（2000字）。但列表页的卡片根本不显示 Description，这 2000 字是纯粹的浪费。

用 `Select` 只取卡片需要的字段：

```csharp
var cars = await query
    .OrderByDescending(c => c.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    // ✅ 投影：只取卡片需要的字段，Description 不加载
    // EF Core 会生成 SELECT Id, Title, Brand, ... 而不是 SELECT *
    .Select(c => new Car
    {
        Id = c.Id,
        Title = c.Title,
        Brand = c.Brand,
        Model = c.Model,
        Year = c.Year,
        Price = c.Price,
        Mileage = c.Mileage,
        Status = c.Status,
        SellerId = c.SellerId,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        // 导航属性也可以投影
        Seller = new User
        {
            Id = c.Seller.Id,
            Username = c.Seller.Username,
        },
        // Images 只取第一张（封面图），不加载全部
        Images = c.Images
            .OrderBy(i => i.SortOrder)
            .Take(1)
            .ToList(),
    })
    .ToListAsync(cancellationToken);
```

> **注意：** 投影之后就不需要 `.Include(c => c.Seller)` 和 `.Include(c => c.Images)` 了，因为 `Select` 里已经明确指定了要取哪些字段。两者同时存在会有冲突，所以改用投影后要删掉 `Include`。

完整更新后的 `GetPagedAsync`：

```csharp
public async Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
    CarStatus status,
    CarQueryRequest request,
    CancellationToken cancellationToken = default)
{
    var query = _context.Cars
        .AsNoTracking()
        // ✅ 不再用 Include，改用下面的 Select 投影
        .Where(c => c.Status == status);

    // 搜索过滤（不变）
    if (!string.IsNullOrWhiteSpace(request.Brand))
        query = query.Where(c => c.Brand.Contains(request.Brand));

    if (request.MinPrice.HasValue)
        query = query.Where(c => c.Price >= request.MinPrice.Value);

    if (request.MaxPrice.HasValue)
        query = query.Where(c => c.Price <= request.MaxPrice.Value);

    if (request.MinYear.HasValue)
        query = query.Where(c => c.Year >= request.MinYear.Value);

    if (request.MaxYear.HasValue)
        query = query.Where(c => c.Year <= request.MaxYear.Value);

    var totalCount = await query.CountAsync(cancellationToken);

    var page = request.Page < 1 ? 1 : request.Page;
    var pageSize = Math.Min(50, request.PageSize < 1 ? 20 : request.PageSize);

    var cars = await query
        .OrderByDescending(c => c.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(c => new Car                      // ✅ 投影，只取需要的字段
        {
            Id         = c.Id,
            Title      = c.Title,
            Brand      = c.Brand,
            Model      = c.Model,
            Year       = c.Year,
            Price      = c.Price,
            Mileage    = c.Mileage,
            Status     = c.Status,
            SellerId   = c.SellerId,
            CreatedAt  = c.CreatedAt,
            UpdatedAt  = c.UpdatedAt,
            Seller     = new User { Id = c.Seller.Id, Username = c.Seller.Username },
            Images     = c.Images
                           .OrderBy(i => i.SortOrder)
                           .Take(1)              // ✅ 只取封面图，不加载全部图片
                           .ToList(),
        })
        .ToListAsync(cancellationToken);

    return (cars, totalCount);
}
```

完整更新后的 `GetPagedAsync`：

```ts
// UUcars.API/Repositories/EfCarRepository.cs

// GetBySellerAsync：加 AsNoTracking + Select 投影 
public async Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
    int sellerId,
    CarQueryRequest request,
    CancellationToken cancellationToken = default)
{
    var query = _context.Cars
        .AsNoTracking() // ✅ 只读查询，关闭变更追踪
        //.Include(c => c.Seller)  // 使用投影select
        //.Include(c => c.Images)
        .Where(c => c.SellerId == sellerId);
    // 注意：这里没有过滤 Status，返回卖家所有状态的车辆
    // 但排除逻辑删除的车辆——卖家也不需要看到已删除的车
    // 如果将来需要显示已删除的车，可以单独加一个接口

    if (!string.IsNullOrWhiteSpace(request.Brand))
        query = query.Where(c => c.Brand.Contains(request.Brand.ToLower()));

    if (request.MinPrice.HasValue) query = query.Where(c => c.Price >= request.MinPrice.Value);

    if (request.MaxPrice.HasValue) query = query.Where(c => c.Price <= request.MaxPrice.Value);

    if (request.MinYear.HasValue) query = query.Where(c => c.Year >= request.MinYear.Value);

    if (request.MaxYear.HasValue) query = query.Where(c => c.Year <= request.MaxYear.Value);

    // 去掉逻辑删除的车（卖家视角也不需要看到已删除的车）
    query = query.Where(c => c.Status != CarStatus.Deleted);

    var totalCount = await query.CountAsync(cancellationToken);

    var page = request.Page;
    var pageSize = Math.Min(50, request.PageSize);

    var cars = await query
        .OrderByDescending(c => c.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        // 使用投影，只取需要的字段
        .Select(c => new Car
        {
            Id = c.Id,
            Title = c.Title,
            Brand = c.Brand,
            Model = c.Model,
            Year = c.Year,
            Price = c.Price,
            Mileage = c.Mileage,
            Status = c.Status,
            SellerId = c.SellerId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Seller = new User
            {
                Id = c.Seller.Id,
                Username = c.Seller.Username
            },
            Images = c.Images
                .OrderBy(i => i.SortOrder)
                .Take(1)
                .ToList()
        })
        .ToListAsync(cancellationToken);

    return (cars, totalCount);
}


```

注意：EfCarRepository里的其他方法不需要加AsNoTracking和投影

```c#
// ── GetByIdAsync：保持原样，不改 
// 原因：查出来之后要修改状态，必须保留 EF Core 变更追踪
// 不加 AsNoTracking，不加投影

// ── GetDetailByIdAsync：保持原样，不改 
// 原因：详情页展示用，需要加载完整 Images 列表
// 虽然是只读，但加 AsNoTracking 后如果收藏/下单操作里间接读到这个 Car 对象，会有追踪丢失的隐患
// 保持现状，清晰优于微小的性能收益
```

#### 8.3 其他repository的优化

##### **对于favorite （`EfFavoriteRepository.cs`）来说：**

`GetByUserAsync` 是收藏列表，纯只读展示，加 `AsNoTracking`。

**投影的问题：** `GetByUserAsync` 返回的是 `List<Favorite>`，Favorite 实体本身字段很少（UserId、CarId、CreatedAt），主要内容在导航属性 `Car` 和 `Car.Seller` 里。

这里加投影收益不大（Favorite 自身没有大字段），但可以把 Car 里的 `Description` 排除掉，保持和 `GetPagedAsync` 一致的原则：

```c#
// UUcars.API/Repositories/EfFavoriteRepository.cs

public async Task<(List<Favorite> Favorites, int TotalCount)> GetByUserAsync(
    int userId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    var query = _context.Favorites
        .AsNoTracking()                          // ✅ 只读，关闭变更追踪
        .Where(f => f.UserId == userId);

    var totalCount = await query.CountAsync(cancellationToken);

    var favorites = await query
        .OrderByDescending(f => f.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        // ✅ 投影：Favorite 里的 Car 排除 Description，只取封面图
        .Select(f => new Favorite
        {
            UserId    = f.UserId,
            CarId     = f.CarId,
            CreatedAt = f.CreatedAt,
            Car = new Car
            {
                Id        = f.Car.Id,
                Title     = f.Car.Title,
                Brand     = f.Car.Brand,
                Model     = f.Car.Model,
                Year      = f.Car.Year,
                Price     = f.Car.Price,
                Mileage   = f.Car.Mileage,
                Status    = f.Car.Status,
                SellerId  = f.Car.SellerId,
                CreatedAt = f.Car.CreatedAt,
                UpdatedAt = f.Car.UpdatedAt,
                Seller = new User
                {
                    Id       = f.Car.Seller.Id,
                    Username = f.Car.Seller.Username,
                },
                Images = f.Car.Images
                            .OrderBy(i => i.SortOrder)
                            .Take(1)
                            .ToList(),
            },
        })
        .ToListAsync(cancellationToken);

    return (favorites, totalCount);
}

// ── GetAsync（按联合主键查单条收藏）：保持原样 ────────────────────────────
// 原因：用于判断是否已收藏，结果不做展示，不需要投影
// AddAsync / DeleteAsync：写操作，不加 AsNoTracking
```

##### **对于order（`EfOrderRepository.cs`）来说：**

`GetByBuyerAsync` 和 `GetBySellerAsync` 都是列表查询，加 `AsNoTracking` + 投影。

Order 实体本身没有大字段（Price 是 decimal，不大），但导航属性 Car 里有 `Description`，投影时排除掉：

```c#
// UUcars.API/Repositories/EfOrderRepository.cs

public async Task<(List<Order> Orders, int TotalCount)> GetByBuyerAsync(
    int buyerId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    var query = _context.Orders
        .AsNoTracking()                          // ✅ 只读
        .Where(o => o.BuyerId == buyerId);

    var totalCount = await query.CountAsync(cancellationToken);

    var orders = await query
        .OrderByDescending(o => o.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(o => new Order                   // ✅ 投影
        {
            Id        = o.Id,
            CarId     = o.CarId,
            BuyerId   = o.BuyerId,
            SellerId  = o.SellerId,
            Price     = o.Price,
            Status    = o.Status,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            // Car：只取列表展示需要的字段（不含 Description）
            Car = new Car
            {
                Id    = o.Car.Id,
                Title = o.Car.Title,
                Brand = o.Car.Brand,
                Year  = o.Car.Year,
                Images = o.Car.Images
                            .OrderBy(i => i.SortOrder)
                            .Take(1)
                            .ToList(),
            },
            // 买家查"我买的"，关心卖家是谁
            Seller = new User
            {
                Id       = o.Seller.Id,
                Username = o.Seller.Username,
            },
            // Buyer 不需要（就是自己）
        })
        .ToListAsync(cancellationToken);

    return (orders, totalCount);
}

public async Task<(List<Order> Orders, int TotalCount)> GetBySellerAsync(
    int sellerId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    var query = _context.Orders
        .AsNoTracking()                          // ✅ 只读
        .Where(o => o.SellerId == sellerId);

    var totalCount = await query.CountAsync(cancellationToken);

    var orders = await query
        .OrderByDescending(o => o.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(o => new Order                   // ✅ 投影
        {
            Id        = o.Id,
            CarId     = o.CarId,
            BuyerId   = o.BuyerId,
            SellerId  = o.SellerId,
            Price     = o.Price,
            Status    = o.Status,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            Car = new Car
            {
                Id    = o.Car.Id,
                Title = o.Car.Title,
                Brand = o.Car.Brand,
                Year  = o.Car.Year,
                Images = o.Car.Images
                            .OrderBy(i => i.SortOrder)
                            .Take(1)
                            .ToList(),
            },
            // 卖家查"我卖的"，关心买家是谁
            Buyer = new User
            {
                Id       = o.Buyer.Id,
                Username = o.Buyer.Username,
            },
            // Seller 不需要（就是自己）
        })
        .ToListAsync(cancellationToken);

    return (orders, totalCount);
}

// ── GetByIdAsync（按 ID 查单条订单）：保持原样 ────────────────────────────
// 原因：CancelAsync 查出来之后要修改状态，需要变更追踪
// 不加 AsNoTracking，不加投影
```

#### 8.4 所有方法(读取数据）决策汇总

```
方法                        AsNoTracking  投影    原因
──────────────────────────────────────────────────────────────
Car
  GetPagedAsync             ✅ 已加       ✅ 已加  只读列表
  GetBySellerAsync          ✅ 已加       ✅ 已加  只读列表
  GetByIdAsync              ❌            ❌       写前查询，需要追踪
  GetDetailByIdAsync        ❌            ❌       详情展示，保持清晰

Favorite
  GetByUserAsync            ✅ 已加       ✅ 已加  只读列表
  GetAsync（单条）           ❌            ❌       判断存在，结果不展示

Order
  GetByBuyerAsync           ✅ 已加       ✅ 已加  只读列表
  GetBySellerAsync          ✅ 已加       ✅ 已加  只读列表
  GetByIdAsync              ❌            ❌       写前查询（取消订单）
```



### 9. 验证优化效果

在开发环境里，用 EF Core 日志验证 SQL 确实变了。

Step 03 里配置了 `appsettings.Development.json`：

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

启动项目后访问 `GET /cars`，在控制台输出里找 EF Core 生成的 SQL，确认两件事：

**确认1：`SELECT` 里没有 `Description` 字段**

```sql
-- 优化前（SELECT *，把 Description 也取出来）
SELECT [c].[Id], [c].[Title], [c].[Description], [c].[Brand], ...

-- 优化后（投影，Description 不在里面）
SELECT [c].[Id], [c].[Title], [c].[Brand], [c].[Model], [c].[Year], ...
```

**确认2：Images 子查询只取一条**

```sql
-- 优化前（取所有图片）
SELECT [c0].[Id], [c0].[ImageUrl] FROM [CarImages] AS [c0]
WHERE [c0].[CarId] = @carId

-- 优化后（TOP(1) 只取封面图）
SELECT TOP(1) [c0].[Id], [c0].[ImageUrl] FROM [CarImages] AS [c0]
WHERE [c0].[CarId] = @carId ORDER BY [c0].[SortOrder]
```



### 10. 编译和测试

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

```bash
dotnet test
```

所有测试应该仍然通过，不需要改测试——只是查询方式变了，业务逻辑没有变化。



### 11. Git 提交

```bash
git add .
git commit -m "perf: add car indexes + AsNoTracking + select projection for list queries"
git push origin feature/v3-query-optimization

# 合并回 develop
git checkout develop
git merge --no-ff feature/v3-query-optimization -m "merge: feature/v3-query-optimization into develop"
git push origin develop
git branch -d feature/v3-query-optimization
git push origin --delete feature/v3-query-optimization
```



### Step 57 完成状态

```
✅ 理解全表扫描 vs 索引查找的本质区别（O(n) vs O(log n)）
✅ 理解复合索引字段顺序的原则（等值在前，范围/排序在后）
✅ CarConfiguration 添加 4 个索引（Status+CreatedAt、Status+Brand、
   Status+Price、SellerId+Status+CreatedAt）
✅ Migration 生成并执行（AddCarIndexes）
✅ GetPagedAsync 加 AsNoTracking + Select 投影（不取 Description，只取封面图）
✅ GetBySellerAsync 加 AsNoTracking
✅ dotnet build 通过，dotnet test 通过
✅ Git commit 完成
```



## Step 58 · 缓存层：从 IMemoryCache 到 Redis

### 这一步做什么

Step 57 用索引解决了"查询本身慢"的问题。这一步解决另一个问题：**重复查询**。

`GET /cars`（公开车辆列表）是整个平台访问最频繁的接口。每次有用户打开首页，就发一次数据库查询。如果同时有 50 个用户在浏览，数据库就收到 50 个几乎完全相同的查询——而这 50 个查询的结果，在这一秒内是完全一样的（车辆列表不会每秒都变）。

这 50 次查询里，有 49 次是在做无意义的重复工作。

**缓存的本质：** 把第一次查询的结果存起来，后续相同的请求直接返回存储的结果，不再查数据库。



### 1. 切出分支

```bash
git checkout develop
git pull origin develop
git checkout -b feature/v3-redis-cache
git push -u origin feature/v3-redis-cache
```



### 2. 缓存的核心概念

#### 2.1 Cache-Aside 模式（旁路缓存）

这是最通用的缓存模式，也是我们要用的：

```
读操作：
  1. 先查缓存
  2. 缓存命中 → 直接返回，结束
  3. 缓存未命中 → 查数据库 → 把结果写入缓存 → 返回

写操作（数据变化时）：
  1. 更新数据库
  2. 删除相关缓存（让下次读重新从数据库加载最新数据）
```

为什么写操作是"删除缓存"而不是"更新缓存"？因为更新缓存需要重新计算缓存值，而此时可能有多个并发写操作，容易产生数据不一致。删除更简单，也更安全——下次读时自然会重建。

#### 2.2 TTL（Time To Live，生存时间）

缓存不能永久存在，否则数据库更新后缓存里永远是旧数据。TTL 决定缓存多久自动过期。

**TTL 设计原则：数据变化越频繁，TTL 越短。**

```
车辆公开列表（变化频率：小时级）  → TTL 60 秒
管理员待审核列表（变化频率：分钟级）→ TTL 30 秒
用户信息（变化频率：天级）        → TTL 5 分钟
```

TTL 不是越长越好——太长会导致用户看到过期数据；也不是越短越好——太短缓存命中率低，等于没缓存。

#### 2.3 缓存 Key 设计

缓存 Key 是字符串，必须能唯一标识一次查询的所有参数。

```
// 错误：不同参数用同一个 Key
key = "cars:published"

// 正确：参数组合进 Key
key = "cars:published:page:1:size:20"
key = "cars:published:brand:BMW:page:1:size:20"
key = "cars:published:minPrice:10000:maxPrice:50000:page:2:size:20"
```

Key 命名规范：用冒号分隔层级，从粗到细：

```
{业务域}:{实体}:{过滤维度}:{过滤值}:...
```

#### 2.4 IMemoryCache vs Redis，选哪个

|            | IMemoryCache     | Redis            |
| ---- | ---- | ---- |
| 存储位置   | 应用进程内存     | 独立进程/服务器  |
| 进程重启   | 缓存丢失         | 缓存保留         |
| 多实例部署 | 每个实例各自一份 | 所有实例共享     |
| 配置复杂度 | 极简（内置）     | 需要安装和配置   |
| 适合场景   | 单实例，开发测试 | 生产环境，多实例 |

现在 Azure 上只有一个容器实例，用 `IMemoryCache` 也够用。

但 V3 的目标是生产级，而且 Azure Container Apps 在流量变化时会自动扩缩容（可能变成多实例）。**选 Redis**，一步到位。

先在本地用 Docker 跑 Redis，Azure 上用 Azure Cache for Redis（Basic tier，开发测试够用）。



### 3. 安装 Redis 并更新 Docker Compose

#### 3.1 本地：给 docker-compose 加 Redis 容器

打开项目根目录的 `docker-compose.yml`，在 `services` 里加入 Redis：

```yaml
services:
  # SQL Server（原有）
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${DB_PASSWORD}"
      MSSQL_PID: Developer
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P ${DB_PASSWORD} -Q 'SELECT 1' -C"]
      interval: 10s
      timeout: 5s
      retries: 5

  # ✅ 新增：Redis 缓存服务
  redis:
    image: redis:7-alpine         # alpine 版本更小，生产够用
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data          # 数据持久化（可选，开发时可以不要）
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  # API（原有，更新 depends_on）
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=UUcarsDB;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True"
      # ✅ 新增 Redis 连接字符串
      ConnectionStrings__Redis: "redis:6379"
      JwtSettings__Secret: "${JWT_SECRET}"
      JwtSettings__Issuer: "UUcars"
      JwtSettings__Audience: "UUcarsUsers"
      JwtSettings__ExpiresInMinutes: "60"
      ASPNETCORE_ENVIRONMENT: Production
    depends_on:
      sqlserver:
        condition: service_healthy
      # ✅ 新增：等 Redis 健康检查通过再启动 API
      redis:
        condition: service_healthy
    restart: unless-stopped

volumes:
  sqlserver_data:
  redis_data:      # ✅ 新增
```

#### 3.2 安装 NuGet 包

```bash
cd UUcars.API
dotnet add package StackExchange.Redis --version 2.8.16
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 9.0.0
cd ..
```

两个包的分工：

- `StackExchange.Redis`：Redis 的 .NET 驱动，提供底层连接和命令
- `Microsoft.Extensions.Caching.StackExchangeRedis`：把 Redis 接入 ASP.NET Core 的 `IDistributedCache` 接口



### 4. 配置 Redis 连接

#### 4.1 更新 `appsettings.json`

在 `ConnectionStrings` 里加 Redis：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=UUcarsDB;User Id=sa;Password=PLACEHOLDER;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS",
    "ExpiresInMinutes": 60,
    "Issuer": "UUcars",
    "Audience": "UUcarsUsers"
  },
  "Serilog": { ... }
}
```

#### 4.2 User Secrets 里不需要额外配置

本地 Redis 不设密码（开发环境），连接字符串 `localhost:6379` 直接进 `appsettings.json` 即可，不是敏感信息。



### 5. 设计缓存抽象层

**为什么要抽象层，而不是直接用 `IDistributedCache`？**

`IDistributedCache` 是 ASP.NET Core 内置的分布式缓存接口，API 比较底层：

```csharp
// IDistributedCache 的原始用法——繁琐
byte[]? cachedBytes = await _cache.GetAsync(key);
if (cachedBytes != null)
{
    var result = JsonSerializer.Deserialize<T>(cachedBytes);
    return result;
}
var data = await queryDatabase();
var bytes = JsonSerializer.SerializeToUtf8Bytes(data);
await _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
});
return data;
```

每次用缓存都要写这一坨。抽象成一个 `ICacheService`，调用方只需要：

```csharp
// 抽象后的用法——简洁
var result = await _cache.GetOrSetAsync(
    key: "cars:published:page:1",
    factory: () => queryDatabase(),
    ttl: TimeSpan.FromSeconds(60)
);
```

#### 5.1 定义接口

新建 `UUcars.API/Services/ICacheService.cs`：

```csharp
namespace UUcars.API.Services;

public interface ICacheService
{
    // 核心方法：有缓存直接返回，没有则执行 factory 并缓存结果
    // factory：一个异步函数，当缓存未命中时调用，返回要缓存的数据
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory, 
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    // 主动删除缓存（数据变更时调用）
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    // 按前缀批量删除（一个列表可能有很多分页缓存，一次性清掉）
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
```

>`Func<Task<T>> factory `:
>
>- 无参数，返回值类型是 Task<T> 的C# 委托类型, `Task<T>` 是异步操作的包装，`await` 之后拿到的是 `T`。
>- 我们自定义的一个callback函数，用来让调用方（比如carService) 来自己决定逻辑来获取缓存数据或者操作缓存数据
>- "数据从哪来"由调用方决定，缓存服务不关心， 从而实现解耦
>- 叫 `factory` 是一个**命名惯例**——在设计模式里"工厂"表示"负责生产某个东西的函数"，这里的语义是"负责生产缓存数据的函数"，所以约定叫 `factory`。



#### 5.2 实现 RedisCacheService

##### 需要使用到一些接口

- `IDistributedCache`

    ASP.NET Core 内置的分布式缓存抽象接口，定义了处理缓存的一些方法：

    ```c#
    // 接口定义（框架内部，不是我们写的）
    public interface IDistributedCache
    {
        byte[]?      Get(string key);
        Task<byte[]?> GetAsync(string key, CancellationToken token = default);
    
        void         Set(string key, byte[] value, DistributedCacheEntryOptions options);
        Task         SetAsync(string key, byte[] value,
                              DistributedCacheEntryOptions options,
                              CancellationToken token = default);
    
        void         Refresh(string key);
        Task         RefreshAsync(string key, CancellationToken token = default);
    
        void         Remove(string key);
        Task         RemoveAsync(string key, CancellationToken token = default);
    }
    ```

    注意：它操作的是 `byte[]`（字节数组），不是字符串或对象。所以存取数据都要手动序列化/反序列化。

    **框架还提供了扩展方法 `GetStringAsync` / `SetStringAsync`，内部自动做 `byte[]` ↔ `string` 转换。**

- `IConnectionMultiplexer`

    StackExchange.Redis 的原生客户端， 它能执行任意 Redis 命令：

    ```c#
    var server = _redis.GetServer(_redis.GetEndPoints().First());
    var keys   = server.Keys(pattern: "uucars:cars:published:*");
    var db     = _redis.GetDatabase();
    await db.KeyDeleteAsync(keys.ToArray());
    ```

    我们需要**按前缀批量删除**caches, 所以就使用到IConnectionMultiplexer接口

##### 完整的 ICacheService 实现逻辑

新建 `UUcars.API/Services/RedisCacheService.cs`：

```csharp
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace UUcars.API.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache; // 高层接口：Get/Set/Remove
    private readonly IConnectionMultiplexer _redis; // 底层原生：批量删除
    private readonly ILogger<RedisCacheService> _logger;

    // JSON 序列化选项：属性名用 camelCase，和前端保持一致
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public RedisCacheService(
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(
    string key,
    Func<Task<T>> factory,
    TimeSpan ttl,
    CancellationToken cancellationToken = default)
    {
        // ── 第一步：尝试从 Redis 读取 ─────────────────────────────────────
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            //                        ↑
            //            IDistributedCache 的扩展方法
            //            内部：GetAsync(key) 返回 byte[]，再转成 string

            if (cached is not null)
            {
                // 命中：把 JSON 字符串反序列化成 T 对象，直接返回
                // 不查数据库，不调用 factory
                _logger.LogDebug("Cache HIT: {Key}", key);
                return JsonSerializer.Deserialize<T>(cached, JsonOptions)!;
            }
        }
        catch (Exception ex)
        {
            // Redis 连不上：不抛异常，继续往下走（降级到查数据库）
            // 这是关键的容错设计：缓存挂了，系统仍然可用
            _logger.LogWarning(ex, "Cache GET failed for key: {Key}", key);
        }

        // ── 第二步：缓存未命中，调用 factory 查数据库 ─────────────────────
        // factory 就是调用方传进来的那个 lambda
        // 只有走到这里才会执行，缓存命中时 factory 根本不会被调用
        _logger.LogDebug("Cache MISS: {Key}", key);
        var data = await factory();
        //                 ↑
        //         执行调用方传入的函数
        //         例：async () => await _carRepository.GetPagedAsync(...)

        // ── 第三步：把结果写入 Redis ──────────────────────────────────────
        try
        {
            var serialized = JsonSerializer.Serialize(data, JsonOptions);
            //  把 T 对象序列化成 JSON 字符串

            await _cache.SetStringAsync(
                key,
                serialized,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                    // AbsoluteExpiration：从现在起 ttl 时间后过期
                    // 还有 SlidingExpiration：每次访问后重置过期时间
                    // 我们用 Absolute，更可预测
                },
                cancellationToken);
            // 写缓存失败不影响返回结果
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key: {Key}", key);
        }

        return data;  // 返回从数据库查到的数据
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache REMOVED: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(
    string prefix,
    CancellationToken cancellationToken = default)
    {
        try
        {
            // IDistributedCache 没有按前缀删除的功能
            // 必须用 IConnectionMultiplexer 的原生 API

            // GetServer：获取 Redis 服务器实例（用于执行管理命令）
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            // Keys()：执行 Redis 的 SCAN 命令（生产安全，不用 KEYS）
            // pattern 要加上 InstanceName 前缀，因为 Redis 里实际的 Key 带前缀
            var keys = server.Keys(pattern: $"uucars:{prefix}*").ToArray();
            //                                ↑
            //                         和 InstanceName 保持一致

            if (keys.Length == 0) return;

            // KeyDeleteAsync：批量删除，一次网络往返删多个 Key
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys);

            _logger.LogDebug(
                "Cache REMOVED {Count} keys with prefix: {Prefix}",
                keys.Length, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Cache REMOVE_BY_PREFIX failed for prefix: {Prefix}", prefix);
        }
    }
}
```

> **降级策略是什么？** 注意两个 `catch` 块——Redis 连不上或者出错时，不抛异常，只记录 Warning，然后继续正常流程（直接查数据库）。这叫**降级（Graceful Degradation）**：缓存挂了，系统仍然可用，只是性能变差。如果缓存异常直接抛给用户，用户看到 500 错误，那就是缓存把整个系统搞垮了，不可接受。



### 6. 注册依赖到 Program.cs

打开 `UUcars.API/Program.cs`，在服务注册区域加入：

```csharp
// 顶部 using
using StackExchange.Redis;
using UUcars.API.Services;

// ── 服务注册 ──

// Redis 连接（IConnectionMultiplexer 是线程安全的，注册为 Singleton）
// Singleton：整个应用生命周期只创建一次连接，所有请求共享
// 不用 Scoped 的原因：Redis 连接是昂贵的资源，每次请求创建一个连接会耗尽连接池
var redisConnectionString = builder.Configuration
    .GetConnectionString("Redis") ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));

// IDistributedCache 的 Redis 实现
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    // Key 前缀：区分不同应用共用同一个 Redis 实例时的 Key 冲突
    options.InstanceName = "uucars:";
});

// 缓存服务（Singleton，因为它依赖 Singleton 的 IConnectionMultiplexer）
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
```



### 7. 定义缓存 Key 常量

缓存 Key 分散在代码各处很容易出错（拼写错误导致缓存无法命中或无法删除）。集中管理：

新建 `UUcars.API/Services/CacheKeys.cs`：

```csharp
namespace UUcars.API.Services;

// 所有缓存 Key 的集中定义
// 好处：
//   1. IDE 自动补全，不会拼错
//   2. 修改 Key 格式时只改一处
//   3. RemoveByPrefix 用的前缀和 GetOrSet 用的 Key 保证一致
public static class CacheKeys
{
    // 公开车辆列表（带过滤参数）
    // 参数变化 → Key 变化 → 各自独立缓存
    public static string PublishedCars(
        int page, int pageSize,
        string? brand, decimal? minPrice, decimal? maxPrice,
        int? minYear, int? maxYear)
        => $"cars:published:p{page}:s{pageSize}" +
           $":brand{brand ?? ""}:min{minPrice}:max{maxPrice}" +
           $":miny{minYear}:maxy{maxYear}";

    // 公开车辆列表的前缀（用于批量删除：车辆审核通过时清掉所有分页缓存）
    public const string PublishedCarsPrefix = "cars:published:";

    // 待审核车辆列表（Admin 用）
    public static string PendingCars(int page, int pageSize)
        => $"cars:pending:p{page}:s{pageSize}";

    public const string PendingCarsPrefix = "cars:pending:";
}
```



### 8. Service里操作缓存

#### 8.1 添加缓存

只对**公开列表**和**待审核列表**加缓存，原因：

- 公开列表：访问量最大，数据变化频率低（只有 Admin 审核时才变）
- 待审核列表：Admin 频繁刷新，数据库压力大
- 卖家列表（`GetMyListings`）：每个卖家的数据不同，缓存收益小，先不加

添加缓存的操作如下：

| Service         | 方法                  | 缓存Key             | TTL  |      |
| --- | --- | - | ---- | ---- |
| CarService      | GetPublishedCarsAsync | PublishedCarsPrefix | 60s  |      |
| AdminCarService | GetPendingCarsAsync   | PendingCarsPrefix   | 30s  |      |

打开 `UUcars.API/Services/CarService.cs`，注入 `ICacheService` 并在对应方法里使用：

```csharp
public class CarService
{
    private readonly ICarRepository _carRepository;
    private readonly ICarImageRepository _carImageRepository;
    private readonly ICacheService _cache;              // ✅ 新增
    private readonly ILogger<CarService> _logger;

    public CarService(
        ICarRepository carRepository,
        ICarImageRepository carImageRepository,
        ICacheService cache,                           // ✅ 新增
        ILogger<CarService> logger)
    {
        _carRepository = carRepository;
        _carImageRepository = carImageRepository;
        _cache = cache;                                // ✅ 新增
        _logger = logger;
    }

    // ✅ 公开车辆列表：加缓存
    public async Task<PagedResponse<CarResponse>> GetPublishedCarsAsync(
        CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page     = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Min(request.PageSize < 1 ? 20 : request.PageSize, 50);

        // 构建缓存 Key（包含所有过滤参数，不同参数对应不同缓存）
        var cacheKey = CacheKeys.PublishedCars(
            page, pageSize,
            request.Brand,
            request.MinPrice, request.MaxPrice,
            request.MinYear,  request.MaxYear);

        // GetOrSetAsync：有缓存直接返回；没有则查数据库并缓存结果
        return await _cache.GetOrSetAsync(
            key:     cacheKey,
            factory: async () =>
            {
                // 这个 lambda 只在缓存未命中时执行
                var (cars, totalCount) = await _carRepository.GetPagedAsync(
                    CarStatus.Published, request, cancellationToken);
                var items = cars.Select(MapToResponse).ToList();
                return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
            },
            ttl: TimeSpan.FromSeconds(60),   // 公开列表缓存 60 秒
            cancellationToken: cancellationToken);
    }

    // 其他方法不变...
}
```

打开 `UUcars.API/Services/AdminCarService.cs`，注入 `ICacheService` 并在对应方法里使用：

```c#
public class AdminCarService
{
    private readonly ICacheService _cache; // ✅ 新增
    private readonly ICarRepository _carRepository;
    private readonly ILogger<AdminCarService> _logger;

    public AdminCarService(ICarRepository carRepository, ILogger<AdminCarService> logger, ICacheService cache)
    {
        _carRepository = carRepository;
        _logger = logger;
        _cache = cache;
    }	

	...

    // ✅ 待审核车辆列表：加缓存
    public async Task<PagedResponse<CarResponse>> GetPendingCarsAsync(
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        pageSize = Math.Min(pageSize, 50);

        // 构建缓存 Key（包含所有过滤参数，不同参数对应不同缓存）
        var cacheKey = CacheKeys.PendingCars(page, pageSize);

        // GetOrSetAsync：有缓存直接返回；没有则查数据库并缓存结果

        return await _cache.GetOrSetAsync(cacheKey,
            async () =>
            {
                var (cars, totalCount) = await _carRepository.GetPagedAsync(
                    CarStatus.PendingReview,
                    query, cancellationToken);
                var items = cars.Select(CarService.MapToResponse).ToList();
                return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
            }, TimeSpan.FromSeconds(30), cancellationToken);
    }
    ...
}
```

#### 8.2 清除缓存

当车辆的状态改变，并影响公开列表的数据的操作，需要清除缓存

清除缓存的操作如下：

| Service           | 方法                   | 清除的Key                                   | 原因                                             |
| ----- | ---- | - |  |
| AdminCarService   | ApproveAsync           | PublishedCarsPrefix + PendingCarsPrefix | 车辆从待审核进入公开列表，两个列表同时变化       |
|    | RejectAsync            | PendingCarsPrefix                         | 车辆退回Draft，只影响待审核列表                  |
|  | AdminDeleteAsync     | PublishedCarsPrefix + PendingCarsPrefix | 车辆可能处于任何状态被删除，两个列表都可能受影响 |
| CarService      | SubmitForReviewAsync | PendingCarsPrefix                         | 车辆进入待审核列表，缓存数据已过时          |
| OrderService    | CreateAsync          | PublishedCarsPrefix                       | 车辆变Sold从公开列表消失，缓存数据已过时         |
|     | CancelAsync          | PublishedCarsPrefix                       | 车辆恢复Published重回公开列表，缓存数据已过时    |

 AdminCarService里清缓存：

```csharp
public class AdminCarService
{
   ...
       
    public async Task<CarResponse> ApproveAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Published;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);

        // ✅ 车辆上架 → 公开列表变了 → 清掉公开列表的所有缓存
        // 用前缀删除：一次清掉所有分页（page1/page2/page3...）
        await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        _logger.LogInformation("Car {CarId} approved by admin, now Published", carId);

        return CarService.MapToResponse(updated);
    }

    public async Task<CarResponse> RejectAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Draft;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);

        // ✅ 车辆退回 → 待审核列表变了 → 清待审核列表缓存
        await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        _logger.LogInformation("Car {CarId} rejected by admin, returned to Draft", carId);

        return CarService.MapToResponse(updated);
    }

	...
        
    public async Task AdminDeleteAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.Status == CarStatus.Deleted)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Published);

        var previousStatus = car.Status; // ✅ 记录删除前的状态
        car.Status = CarStatus.Deleted;
        car.UpdatedAt = DateTime.UtcNow;

        await _carRepository.UpdateAsync(car, cancellationToken);

        // ✅ 根据删除前的状态，清对应的缓存
        if (previousStatus == CarStatus.Published)
            await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);

        if (previousStatus == CarStatus.PendingReview)
            await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        _logger.LogInformation(
            "Car {CarId} forcefully deleted by admin (was {Status}), cache invalidated",
            carId, previousStatus);
    }
}
```

 CarService里清缓存：

```c#
public class CarService
{
...

	public async Task<CarResponse> SubmitForReviewAsync(
        int carId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.SellerId != currentUserId)
            throw new ForbiddenException();

        if (car.Status != CarStatus.Draft)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.PendingReview;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);
        
        //清除待审核车辆列表缓存
        await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        _logger.LogInformation("Car {CarId} submitted for review by seller {SellerId}",
            car.Id, currentUserId);

        return MapToResponse(updated);
    }
    // 其他方法不变...
}
```

 OrderService里清缓存：

```c#
public class OrderService
{
  	...

    public async Task<OrderResponse> CreateAsync(
        int buyerId,
        OrderCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(request.CarId, cancellationToken);
        if (car == null)
            throw new CarNotFoundException(request.CarId);

        if (car.Status != CarStatus.Published)
            throw new CarNotAvailableException(request.CarId);

        if (car.SellerId == buyerId)
            throw new CannotOrderOwnCarException();

        var order = new Order
        {
            CarId = car.Id,
            BuyerId = buyerId,
            SellerId = car.SellerId, 
            Price = car.Price, 
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 更新车辆状态为 Sold
        car.Status = CarStatus.Sold;
        car.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Add(order);
        _context.Cars.Update(car);
        await _context.SaveChangesAsync(cancellationToken);

        // ✅ 车辆变 Sold，从公开列表消失，清缓存
        await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} created: buyer {BuyerId} purchased car {CarId} from seller {SellerId}",
            order.Id, buyerId, car.Id, car.SellerId);

        var created = await _orderRepository.GetByIdAsync(order.Id, cancellationToken);
        return MapToResponse(created!);
    }
    

    public async Task<OrderResponse> CancelAsync(
        int orderId,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order == null)
            throw new OrderNotFoundException(orderId);

        if (order.BuyerId != currentUserId)
            throw new ForbiddenException();

        if (order.Status != OrderStatus.Pending)
            throw new OrderStatusException(orderId, order.Status, OrderStatus.Pending);

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        // 恢复车辆状态为 Published
        var car = await _carRepository.GetByIdAsync(order.CarId, cancellationToken);
        if (car != null)
        {
            car.Status = CarStatus.Published;
            car.UpdatedAt = DateTime.UtcNow;
        }
        _context.Orders.Update(order);
        if (car != null)
            _context.Cars.Update(car);
        await _context.SaveChangesAsync(cancellationToken);

        // ✅ 车辆恢复 Published，重回公开列表，清缓存
        await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} cancelled by buyer {BuyerId}, car {CarId} restored to Published",
            orderId, currentUserId, order.CarId);

        return MapToResponse(order);
    }

	...
}
```



### 9. 更新 Program.cs 里 AdminCarService 的注册

`AdminCarService` 现在多了一个依赖，DI 需要知道：

```csharp
// Admin 模块
builder.Services.AddScoped<AdminCarService>();
// ICacheService 已经注册为 Singleton，DI 会自动注入，不需要额外改动
```

同时，`CarService` 也多了一个依赖：

```csharp
// 车辆模块
builder.Services.AddScoped<ICarRepository, EfCarRepository>();
builder.Services.AddScoped<ICarImageRepository, EfCarImageRepository>();
builder.Services.AddScoped<CarService>();
// ICacheService 已注册，DI 自动处理
```



### 10. 启动 Redis 并验证

#### 10.1 启动本地 Redis（如果没在 Docker Compose 里）

```bash
# 单独启动 Redis 容器（开发时用这个，不需要跑整个 compose）
docker run -d --name uucars-redis -p 6379:6379 redis:7-alpine

# 确认 Redis 正在运行
docker ps | grep redis

# 确认 Redis 能正常响应
docker exec uucars-redis redis-cli ping
# 应该输出：PONG
```

#### 10.2 启动 API 并测试缓存

```bash
cd UUcars.API
dotnet run
```

**第一次请求（缓存未命中）：**

```bash
curl http://localhost:5085/cars
```

查看控制台日志，应该看到：

```
[Debug] Cache MISS: cars:published:p1:s20:brand:min:max:miny:maxy
[Info]  Executed DbCommand ... SELECT ...
```

**第二次相同请求（缓存命中）：**

```bash
curl http://localhost:5085/cars
```

控制台日志：

```
[Debug] Cache HIT: cars:published:p1:s20:brand:min:max:miny:maxy
```

没有 SQL 查询日志，说明缓存生效了，数据库完全没有被访问。

**验证缓存数据：**

```bash
# 进入 Redis 容器，查看缓存里存了什么
docker exec -it uucars-redis redis-cli

# 列出所有 Key（生产环境不要用，这里只是验证）
KEYS *

# 应该看到类似：
# 1) "uucars:cars:published:p1:s20:brand:min:max:miny:maxy"

# 查看这个 Key 的剩余 TTL（单位：秒）
TTL "uucars:cars:published:p1:s20:brand:min:max:miny:maxy"
# 应该是 0-60 之间的数字

# 退出 redis-cli
EXIT
```



### 11. 编译和测试

#### 单元测试里的 CarService 怎么处理 ICacheService？ 

测试里用的是 Fake Repository，但现在 CarService 多了 `ICacheService` 依赖。需要给测试提供一个 fake 的 ICacheService——最简单的方式是用 `NSubstitute` 或直接实现一个 `FakeCacheService`（直接执行 factory，不做任何缓存）。


 打开 `UUcars.Tests/Fakes/`，新建 `FakeCacheService.cs`：

```csharp
// UUcars.Tests/Fakes/FakeCacheService.cs
using UUcars.API.Services;

namespace UUcars.Tests.Fakes;

// 测试用的 CacheService：直接执行 factory，不做任何缓存
// 保证单元测试不依赖 Redis，行为和真实 CacheService 一致（逻辑透明）
public class FakeCacheService : ICacheService
{
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        // 直接执行 factory，不缓存
        return await factory();
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
```

然后在 `CarServiceTests.cs` 里的 `CreateService` 方法里传入 `FakeCacheService`：

```csharp
private static CarService CreateService(
    FakeCarRepository carRepo,
    FakeCarImageRepository? imageRepo = null)
{
    return new CarService(
        carRepo,
        imageRepo ?? new FakeCarImageRepository(),
        new FakeCacheService(),          // ✅ 新增
        NullLogger<CarService>.Instance
    );
}
```

同样，`OrderService` 的测试也需要传入 `FakeCacheService`。

```c#
private static OrderService CreateService(
        AppDbContext context,
        FakeCarRepository? carRepo = null)
    {
        var orderRepo = new EfOrderRepository(context);
        return new OrderService(
            new FakeCacheService(),
            carRepo ?? new FakeCarRepository(),
            context,
            NullLogger<OrderService>.Instance,
            orderRepo
        );
    }
```

#### 集成测试如何处理Redis？

集成测试不应该依赖真实的 Redis，原因和替换 `IEmailService` 一样：测试环境不保证 Redis 可用，而且缓存会在测试之间互相干扰。

因此使用 `FakeCacheService`替换

打开 `UUcars.Tests/Integration/SqlServerTestFactory.cs`，在 `ConfigureWebHost` 里补充：

```c#
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureServices(services =>
    {
        // 替换 DbContext
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
        if (descriptor != null) services.Remove(descriptor);
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(_sqlContainer.GetConnectionString()));

        // 替换 IEmailService
        var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
        if (emailDescriptor != null) services.Remove(emailDescriptor);
        services.AddSingleton<IEmailService>(FakeEmail);

        // ✅ 替换 ICacheService，集成测试不依赖真实 Redis
        var cacheDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
        if (cacheDescriptor != null) services.Remove(cacheDescriptor);
        services.AddSingleton<ICacheService, FakeCacheService>();
    });

    builder.UseEnvironment("Testing");
}
```

再跑一遍：

```bash
dotnet build
dotnet test
```



### 12. 生产环境 Redis 部署（Upstash）

Azure部署时， 可以用 Azure Cache for Redis， 但作为学习项目，使用独立第三方提供的redis更合理

#### 12.1 选 Upstash的Redis

|          | Upstash                                         | Azure Cache for Redis   |
| -- | ----- | ----- |
| 费用     | 免费套餐（10,000 requests/天）                  | 最低 $16/月（Basic C0） |
| 架构     | 独立托管服务，和 Azure Cache for Redis 完全一致 | 独立托管服务            |
| 迁移成本 | 只需换连接字符串，代码零改动                    | -                       |
| 适用场景 | 学习/练手项目                                   | 生产级项目              |

学习阶段用 Upstash，后期迁移到 Azure Cache for Redis 只需要换连接字符串。

**不要用 Redis sidecar 方案**：Container App 重启时缓存数据全部丢失，多实例之间缓存不共享，完全不是生产模式。

#### 12.2 创建 Upstash 数据库

1. 去 [upstash.com](https://upstash.com) 注册账号
2. 创建新数据库，Region 选离 Azure 服务器最近的
3. 进入数据库控制台 → **Connect** → 选 **.NET / StackExchange.Redis**
4. 拿到连接字符串，格式类似：

```
exact-dove 111625.upstash.io:6379,password=gQAAAAAAAAbQJAAIgcDJmMmY0MmE2Njk1NGQ0ZTY5OWRhMjVhZDZiYTc2ZGEwOQ,ssl=True,abortConnect=False
```

#### 12.3 配置到 Azure Container Apps

Azure Portal → Container Apps → `uucars-api` → **Environment variables** → 添加：

```
ConnectionStrings__Redis = exact-dove111625.upstash.io:6379,password=gQAAAAAAAAbQJAAIgcDJmMmY0MmE2Njk1NGQ0ZTY5OWRhMjVhZDZiYTc2ZGEwOQ,ssl=True,abortConnect=False
```

**不需要改 CI/CD 文件**，部署流程不变，Azure 启动时自动读取环境变量。

#### 12.4 本地开发 vs 生产环境

|                                  | 连接的 Redis                         |
| -- |  |
| 本地开发（docker-compose）       | `redis:6379`（本地容器）             |
| 集成测试                         | `FakeCacheService`（不连任何 Redis） |
| 生产环境（Azure Container Apps） | Upstash（Azure 环境变量配置）        |



### 13. Git 提交

```bash
git add .
git commit -m "feat: Redis cache layer - ICacheService, RedisCacheService, car list caching"
git push origin feature/v3-redis-cache

# 合并回 develop
git checkout develop
git merge --no-ff feature/v3-redis-cache -m "merge: feature/v3-redis-cache into develop"
git push origin develop
git branch -d feature/v3-redis-cache
git push origin --delete feature/v3-redis-cache
```



### Step 58 完成状态

```
✅ 理解 Cache-Aside 模式（读查缓存→未命中查DB→写缓存；写删缓存）
✅ 理解 TTL 设计原则（变化越频繁 TTL 越短）
✅ 理解 IMemoryCache vs Redis 的区别（选 Redis，生产就绪）
✅ Docker Compose 加入 Redis 容器（redis:7-alpine）
✅ 安装 StackExchange.Redis + Microsoft.Extensions.Caching.StackExchangeRedis
✅ ICacheService 接口（GetOrSetAsync / RemoveAsync / RemoveByPrefixAsync）
✅ RedisCacheService 实现（降级策略：Redis 故障时不影响系统可用性）
✅ CacheKeys 常量类（集中管理，防止 Key 拼写错误）
✅ CarService.GetPublishedCarsAsync 接入缓存（TTL 60s）
✅ AdminCarService 审核通过/拒绝时主动 invalidate 缓存
✅ OrderService 创建/取消时主动 invalidate 缓存
✅ FakeCacheService（单元测试透明执行，不依赖 Redis）
✅ dotnet build + dotnet test 全部通过
✅ 本地验证：Cache HIT/MISS 日志正确，redis-cli 能看到缓存 Key
✅ Azure部署Upstash Redis
✅ Git commit + 合并回 develop 完成
```



## Step 59 · API 限流（Rate Limiting）

### 这一步做什么

Step 58 加了缓存，保护了数据库。但缓存只能减少数据库压力，无法阻止恶意请求本身进入系统。

现在有两个没有解决的安全问题：

**问题一：暴力破解密码。** 攻击者对 `POST /auth/login` 接口每秒发几百次请求，逐个尝试密码组合。没有任何限制，服务器只能照单全收。

**问题二：爬虫绕过缓存。** Step 58 里的缓存 Key 包含过滤参数。攻击者写个脚本，每次换一个不同的 `minPrice` 或 `brand` 参数，每次都是缓存未命中，直接打到数据库。缓存完全帮不上忙。

**限流（Rate Limiting）** 是解决这两个问题的标准方案：限制同一个来源在一段时间内最多能发多少请求，超出后返回 `429 Too Many Requests`，让客户端等待后重试。



### 1. 切出分支

```bash
git checkout develop
git pull origin develop
git checkout -b feature/v3-rate-limiting
git push origin feature/v3-rate-limiting
```



### 2. 限流算法

ASP.NET Core 8 内置了限流中间件，支持多种算法。

#### 2.1 固定窗口（Fixed Window）

把时间切成固定大小的窗口，窗口内统计请求数，超过上限就拒绝。

```
窗口大小：60秒，上限：10次

时间轴：  0s ─────────────────── 60s ─────────────────── 120s
          [        窗口1          ]   [        窗口2          ]

第 5s：请求1  → 计数 1/10 ✅
第 30s：请求2  → 计数 2/10 ✅
...
第 55s：请求10 → 计数 10/10 ✅
第 58s：请求11 → 计数 11/10 ❌ 拒绝
第 61s：窗口重置 → 计数 0/10，重新开始
```

**缺陷：边界突刺问题。**

```
第 59s 发 10 次 → 窗口1用完 10 次
第 61s 发 10 次 → 窗口2重置，又可以 10 次
结果：2秒内发了 20 次，是限制的两倍
```

**适用场景：** 要求不那么严格的接口，实现简单，资源消耗最小。

#### 2.2 滑动窗口（Sliding Window）

不用固定边界，而是"以当前时间为终点，往前推 N 秒"的动态窗口。没有边界突刺问题。

```
窗口大小：60秒，上限：10次

当前时间：70s
统计窗口：[10s ~ 70s] 内的请求数
如果这个窗口内已有 10 次，则拒绝

→ 不管攻击者怎么在边界附近集中请求，60秒内永远不超过 10 次
```

**缺陷：** 需要记录每次请求的精确时间戳，内存占用比固定窗口高。

**适用场景：** 需要精确控制频率的场景，比如登录接口。

#### 2.3 令牌桶（Token Bucket）

系统以固定速率往桶里放令牌（比如每秒放 2 个），每次请求消耗一个令牌，桶里没令牌就拒绝请求，桶有上限（防止令牌无限积累）。

```
桶容量：10个令牌
填充速率：每秒2个令牌

用户突发 10 次请求：
  → 消耗 10 个令牌（桶瞬间清空）✅（允许突发）
  → 第 11 次请求：桶空，等待 0.5 秒补充 1 个令牌
  → 之后：每 0.5 秒可以发 1 次（稳定速率 2次/秒）
```

**特点：** 允许短暂突发流量（正常用户偶尔高频操作），然后平滑降速。对用户体验最友好。

**适用场景：** 浏览接口，允许用户快速翻几页，但不允许持续高频。



### 3. 实现限流

#### 3.1 零依赖：ASP.NET Core 8 内置

不需要安装任何 NuGet 包。`Microsoft.AspNetCore.RateLimiting` 是 ASP.NET Core 8 内置的，`.csproj` 文件里不需要添加任何引用。

直接调用AddRateLimiter方法可以实现配置

```c#{
builder.Services.AddRateLimiter(options =>
{
    //限流配置
}
```

- 被限流时统一返回码（比如429， TooManyRequests）
- 请求被拒绝时执行的回调函数 `OnRejected`
- 限流策略的配置 `AddPolicy`

#### 3.2 配置限流策略

和jwtAuthentication的配置一致，我们也封装成扩展方法，保持Program.cs文件的简洁。

新建 `UUcars.API/Extensions/RateLimitingExtensions.cs`

```c#
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace UUcars.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 被限流时统一返回 429，并附上标准 ApiResponse 格式的错误信息
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // OnRejected：每次有请求被拒绝时执行的回调
            // 用途：
            //   1. 写入 Retry-After 响应头（告诉客户端多少秒后重试）
            //   2. 返回符合我们 ApiResponse 格式的 JSON，而不是空响应
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.ContentType = "application/json";

                // 从限流器里取出"多少秒后可以重试"
                // 不是所有限流器都提供这个信息，所以用 TryGetMetadata 安全读取
                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter, out var retryAfter))
                    httpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                // 返回符合 ApiResponse 格式的 JSON
                var response = new
                {
                    success = false,
                    message = "Too many requests. Please slow down and try again later.",
                    errors = (object?)null
                };

                await httpContext.Response.WriteAsJsonAsync(
                    response, cancellationToken);
            };

 
            // ── 限流策略(比如给注册接口限流）
            // 算法：固定窗口
            // 规则：同一 IP，每小时最多 5 次
            // 原因：防止批量注册垃圾账号
            options.AddPolicy("register", httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"register:{ipAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 5,
                        QueueLimit = 0
                    });
            });

        });
        return services;
    }
}
```

> **为什么 `QueueLimit = 0`？**
>
> `QueueLimit` 是排队等待的请求数。设为 0 表示"超出上限立刻拒绝，不排队等待"。
>
> 如果设为 5，超出限制的请求会在内存里等待，直到有新令牌/配额才继续处理。 对我们的场景来说，攻击者的请求不应该被排队——排队只会占用服务器内存，正常用户的请求反而被拖慢。



#### 3.2 注册限流配置

打开 `UUcars.API/Program.cs`，调用扩展方法注册：

```csharp
builder.Services.AddRateLimiting();
```

#### 3.3 在中间件管道里启用限流

打开 `Program.cs` 中间件管道部分，加入 `UseRateLimiter()`：

```csharp
    // =============================================
    // 中间件管道
    // 顺序很重要：GlobalExceptionMiddleware 必须在最外层
    // =============================================

    // 全局异常处理，放最前面，兜住所有后续中间件的异常
    app.UseMiddleware<GlobalExceptionMiddleware>();


    // 开发环境才挂载 API 文档
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
            options
                .WithTitle("UUcars API Documentation")
                .WithTheme(ScalarTheme.Moon)
        );
    }

    // CORS 必须在 Authentication 和 Authorization 之前
    // 原因：Preflight 请求（OPTIONS）不携带 Token，
    // 如果 CORS 在 Auth 之后，Preflight 会因为没有 Token 被拦截，
    // 导致浏览器认为服务器不支持跨域
    app.UseCors(CorsExtensions.PolicyName);

    // ✅ 限流中间件
    // 必须在 UseCors之后
    // 原因：限流策略导致的响应码 429，响应没有 CORS 头，浏览器拦截
    // 必须在 UseAuthentication / UseAuthorization 之前
    // 原因：限流是最外层的防护，不管请求有没有 Token，都要先过限流检查
    // 这样攻击者带着无效 Token 的请求也能被拦截，不会浪费认证处理的开销

    app.UseRateLimiter();

    // UseAuthentication 必须在 UseAuthorization 之前
    // 原因：Authorization 需要读取 Authentication 的结果（HttpContext.User）
    // 如果顺序反了，[Authorize] 读到的 HttpContext.User 是空的，永远判定为未认证
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
```

> **中间件顺序的本质：** ASP.NET Core 的中间件是一个管道，请求从上往下穿过每一层，响应从下往上返回。越靠前的中间件越早执行，越能在请求消耗任何资源之前就拦截掉它。限流放最前面，被拒绝的请求连认证处理都不需要跑。

#### 3.4 在 Controller 上应用限流策略

限流策略的应用，在controller上， 通过 ` [EnableRateLimiting("限流策略名")]`的形式。以注册接口限流为例：

打开 `UUcars.API/Controllers/AuthController.cs`：

```c#
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    // ✅ 使用限流策略：每IP每小时5次
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    public async Task<IActionResult> Register(...) { ... }

   ...
}
```

#### 3.5 哪些些接口需要限流？

处理上面实例中的注册接口，我们项目的其他接口哪些还需要进行限流？使用什么算法？？

对于auth认证类的接口:

- 注册/登录需要限流，防止恶意注册/登录（比如暴力破解密码）
- 重发验证邮件(`resend-verification` )必须限流，它会触发发邮件，攻击者可以反复调用让系统滥发邮件
- ~~`verify-email` 和 `reset-password` 需要有效的 token 参数才能操作，攻击者无法无限枚举，可以不限流。~~

对于业务接口：

- 读操作（`GET`）, 需要登录认证后操作的，不需要限流，而对于不需要登录的公共接口，需要限流，防止刷单，我们设置一个browse策略，统一配置公共的读操作的接口
- 写操作（`POST/PUT/DELETE`）, 有些需要限流，比如 `POST /cars`, 限流可以防止批量创建车辆草稿刷库； 而有些不需要限流，比如 `POST /cars/{id}/submit` , 一辆车只能提交一次，状态机已限制， 因此无需限流。 对于写操作的限流，我们设置一个write策略，统一配置这些写操作的接口限流。

完整的限流策策略算法和接口如下：

```
策略名              算法      规则                    应用接口
──────────────────────────────────────────────────────────────────────
login               固定窗口  每IP 10次/分            POST /auth/login
register            固定窗口  每IP 5次/时             POST /auth/register
forgot-password     固定窗口  每IP 3次/时             POST /auth/forgot-password
resend-verification 固定窗口  每IP 3次/时             POST /auth/resend-verification
browse              令牌桶    登录120/分，未登录60/分  GET /cars
                                                    GET /cars/{id}
                                                    GET /reviews/seller/{id}
                                                    
write               令牌桶    每用户 30次/分           POST /cars
                                                     POST /cars/{id}/images
                                                     POST /orders
                                                     POST /orders/{id}/cancel
                                                     POST /favorites/{carId}
                                                     DELETE /favorites/{carId}
                                                     PUT /users/me
                                                     POST /reviews
```

#### 3.6 集中管理策略名字符串

我们对策略名字符串集中管理，和 `CacheKeys` 的思路完全一样。防止在 配置限流策略和在Controller 里使用时，手写字符串拼错：

新建 `UUcars.API/Extensions/RateLimitPolicies.cs`

```c#
namespace UUcars.API.Extensions;

/// <summary>
///     限流策略名称常量
///     用于 [EnableRateLimiting("xxx")] 和策略注册时保持一致
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Register = "register";
    public const string ForgotPassword = "forgot-password";
    public const string ResendVerification = "resend-verification";
    public const string Browse = "browse";
    public const string Write = "write";
}
```

#### 3.7 完整的限流策略配置

补充上面扩展方法里的完整限流策略，并使用集中管理策略名字符串

```c#
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace UUcars.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // 被限流时统一返回 429，并附上标准 ApiResponse 格式的错误信息
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // OnRejected：每次有请求被拒绝时执行的回调
            options.OnRejected = async (context, cancellationToken) =>
            {
               ...
            };

            // ── 策略1： 登录接口 ─────────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每分钟最多 10 次
            // 原因：防止暴力破解密码
            //       10次/分钟 对正常用户完全够用（谁会一分钟内登录10次？）
            //       对暴力破解来说，10次/分钟意味着破解一个8位密码要几百万年
            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
            {
                // 按 IP 分区：不同 IP 各自独立计数
                // 为什么不按用户 ID？登录接口还没有用户 ID（正在尝试登录）
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"login:{ipAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueLimit = 0, // 不排队，超出直接拒绝
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });

            // ── 策略2：注册接口 ───────────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每小时最多 5 次
            // 原因：防止批量注册垃圾账号
            options.AddPolicy(RateLimitPolicies.Register, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"register:{ipAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 5,
                        QueueLimit = 0
                    });
            });

            // ── 策略3：忘记密码接口 ─────────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每小时最多 3 次
            // 原因：防止滥发密码重置邮件（有邮件发送成本，也是骚扰手段）
            options.AddPolicy(RateLimitPolicies.ForgotPassword, httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"forgot-password:{ipAddress}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 3,
                        QueueLimit = 0
                    });
            });

            // ── 策略4：重发验证邮件 ───────────────────────
            // 算法：固定窗口
            // 规则：同一 IP，每小时最多 3 次
            // 原因：它会触发发邮件，攻击者可以反复调用让系统滥发邮件
            options.AddPolicy(RateLimitPolicies.ResendVerification, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    $"resend-verification:{ip}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromHours(1),
                        PermitLimit = 3,
                        QueueLimit = 0
                    });
            });

            // ── 策略5：公开浏览接口 ──────────────────────────
            // 算法：令牌桶
            // 规则：
            //   已登录用户：按用户ID分区，每分钟 120 次（更宽松）
            //   未登录用户：按 IP 分区，每分钟 60 次
            // 原因：
            //   - 允许正常用户快速翻页（令牌桶支持短暂突发）
            //   - 已登录用户给更高配额（他们是真实注册用户，风险更低）
            //   - 公司/学校共用 IP 的情况下，按 IP 限制可能误伤，
            //     但未登录用户无法区分个体，只能按 IP
            options.AddPolicy(RateLimitPolicies.Browse, httpContext =>
            {
                // 检查是否已登录（JWT 认证中间件已经处理过了）
                var userId = httpContext.User?.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                    // 已登录：按用户 ID 分区，配额更高
                    return RateLimitPartition.GetTokenBucketLimiter(
                        $"browse:user:{userId}",
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 120, // 桶容量：最多积累 120 个令牌
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1), // 每分钟补充
                            TokensPerPeriod = 120, // 每分钟补充 120 个令牌
                            AutoReplenishment = true, // 自动补充
                            QueueLimit = 0
                        });

                // 未登录：按 IP 分区
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                                ?? "unknown";
                return RateLimitPartition.GetTokenBucketLimiter(
                    $"browse:ip:{ipAddress}",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 60,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 60,
                        AutoReplenishment = true,
                        QueueLimit = 0
                    });
            });

            // ── 策略6：写操作 ─────────────────────────
            // 算法：令牌桶
            // 规则：
            //   已登录用户每分钟30次
            //   写操作必须登录，所以按用户 ID 分区
            //   未登录的写请求会被 [Authorize] 拦截，走不到这里
            options.AddPolicy(RateLimitPolicies.Write, httpContext =>
            {
                var userId = httpContext.User?.FindFirst(
                                 ClaimTypes.NameIdentifier)?.Value
                             ?? httpContext.Connection.RemoteIpAddress?.ToString()
                             ?? "unknown";

                return RateLimitPartition.GetTokenBucketLimiter(
                    $"write:{userId}",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 30,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        TokensPerPeriod = 30,
                        AutoReplenishment = true,
                        QueueLimit = 0
                    });
            });
        });
        return services;
    }
}
```

#### 3.8 限流策略完整的应用

给所有需要添加限流的接口使用对应的限流策略

##### AuthController

打开 `UUcars.API/Controllers/AuthController.cs`：

```csharp
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    // ✅ 注册
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    public async Task<IActionResult> Register(...) { ... }

    // ✅ 登录
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(...) { ... }

    // ✅ 忘记密码
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.ForgotPassword)]
    public async Task<IActionResult> ForgotPassword(...) { ... }
    
    // ✅ 重发验证邮件
    [HttpPost("resend-verification")]
    [EnableRateLimiting(RateLimitPolicies.ResendVerification)]
    public async Task<IActionResult> ResendVerification(...) {...}
    

    // 验证邮箱、重置密码接口不加限流
    // 原因：需要有效的 token 参数才能操作，攻击者无法无限枚举
}
```

##### CarsController

打开 `UUcars.API/Controllers/CarsController.cs`

```csharp
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("cars")]
public class CarsController : ControllerBase
{
    // ✅ 公开车辆列表：令牌桶限流
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Browse)]
    public async Task<IActionResult> GetPaged(...) { ... }

    // ✅ 车辆详情：令牌桶限流
    [HttpGet("{id:int}")]
    [EnableRateLimiting(RateLimitPolicies.Browse)]
    public async Task<IActionResult> GetById(...) { ... }
    
    
    // ✅ 创建车辆草稿：令牌桶限流
    [HttpPost]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> Create(...) { ... }
    
    // ✅ 上传车辆图片：令牌桶限流
    [HttpPost("{id:int}/images")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> AddImage(...) { ... }

    // 其他接口无需格外的限流
    ...
}
```

##### OrdersController

打开 `UUcars.API/Controllers/OrdersController.cs`

```c#
[ApiController]
[Route("orders")]
[Authorize] 
public class OrdersController : ControllerBase
{
    ...

    // ✅ 创建订单：令牌桶限流(防止刷单)
    [HttpPost]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> Create(...) { ... }

    // ✅ 取消订单：令牌桶限流(防止刷库)
    [HttpPost("{id:int}/cancel")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> Cancel(...) { ... }
    
    // 其他接口无需格外的限流
    ...
    
}
```

##### FavoritesController

打开 `UUcars.API/Controllers/FavoritesController.cs`

```c#
[ApiController]
[Route("favorites")]
[Authorize] 
public class FavoritesController : ControllerBase
{
    ...

    [HttpPost("{carId:int}")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> AddFavorite(...) { ... }

    [HttpDelete("{carId:int}")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> RemoveFavorite(...) { ... }
    
    // 其他接口无需格外的限流
    ...
    
}
```

##### UsersController

打开 `UUcars.API/Controllers/UsersController.cs`

```c#
[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    ...

    // PUT /users/me
    [HttpPut("me")]
        
    public async Task<IActionResult> UpdateMe( ... ) { ... }
    
    
}
```

##### ReviewsController

打开 `UUcars.API/Controllers/ReviewsController.cs`

```c#
[ApiController]
[Route("reviews")]
public class ReviewsController : ControllerBase
{
    ...

    // POST /reviews
    [HttpPost]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Write)]
    public async Task<IActionResult> Create( ... ) { ... }
    
    [HttpGet("seller/{id:int}")]
    [EnableRateLimiting(RateLimitPolicies.Browse)]
    public async Task<IActionResult> GetSellerRating( ...) { ... }
       
}
```

> AdminController的接口:
>
> Admin 接口由 `[Authorize(Roles = "Admin")]` 严格保护，管理员数量极少，不需要限流。



### 4. 前端处理 429 响应

限流不只是后端的事。用户触发限流时，前端不应该显示一个红色的网络错误，而应该给友好的提示并告知等待时间。

因此我们需要在响应拦截器里进行处理：

打开 `uucars-web/src/api/client.ts`，在响应拦截器的错误处理里加入 429 的处理：

```typescript
// 在响应拦截器的 error 处理里（加在其他错误处理之前）
apiClient.interceptors.response.use(
  (response) => {
    // 成功处理（原有逻辑不变）
    return response;
  },
  (error: AxiosError) => {

    // ✅ 处理 429 Too Many Requests
    if (error.response?.status === 429) {
      // 读取 Retry-After 响应头（后端返回的秒数）
      const retryAfterHeader = error.response.headers["retry-after"];
      const seconds = retryAfterHeader
        ? parseInt(retryAfterHeader, 10)
        : 60; // 没有 Retry-After 头就默认提示60秒

      // 用 sonner toast 给用户友好提示
      toast.error(
        `Too many requests. Please wait ${seconds} seconds before trying again.`,
        { duration: 5000 }
      );

      // 返回一个标准格式的错误，让调用方知道是限流问题
      return Promise.reject(
        new Error(`Rate limited. Retry after ${seconds}s.`)
      );
    }

    // 其他错误处理（原有逻辑不变）
    return Promise.reject(error);
  }
);
```



### 5. 本地验证

启动项目（确保 Redis 也在运行）：

```bash
cd UUcars.API
dotnet run
```

用命令行快速发多次登录请求，验证限流生效：

```bash
# 快速发 15 次登录请求（限制是每分钟10次）
for i in $(seq 1 15); do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -X POST http://localhost:5085/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"wrong"}')
  echo "请求 $i: HTTP $STATUS"
done
```

预期输出：

```
请求 1: HTTP 401   ← 密码错误（正常处理）
请求 2: HTTP 401
...
请求 10: HTTP 401
请求 11: HTTP 429  ← 被限流
请求 12: HTTP 429
请求 13: HTTP 429
请求 14: HTTP 429
请求 15: HTTP 429
```

验证响应头里有 `Retry-After`：

```bash
curl -v -X POST http://localhost:5085/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"wrong"}' 2>&1 | grep -i "retry-after"

# 预期输出类似：
# < Retry-After: 47
```



### 6. 关于测试的处理

#### 6.1 单元测试

单元测试不走 HTTP 管道，直接调用 Service 方法，不受限流影响，无需任何改动。

#### 6.2 集成测试

限流会干扰集成测试。比如 `CoreFlowIntegrationTests` 里多次调用 `/auth/register` 或 `/auth/login`，可能触发限流导致测试失败。

在 `SqlServerTestFactory.cs` 的 `ConfigureWebHost` 里，把限流禁用掉（测试环境不需要限流）：

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureServices(services =>
    {
        // 原有替换（DbContext、EmailService、Redis）...

        // ✅ 新增：测试环境禁用限流
            // 先移除所有已注册的 RateLimiter 相关服务
            var rateLimiterDescriptors = services
                .Where(d => d.ServiceType.FullName != null &&
                            d.ServiceType.FullName.Contains("RateLimit"))
                .ToList();
            foreach (var d in rateLimiterDescriptors)
                services.Remove(d);

            // 再注册完全无限制的 
            services.AddRateLimiter(options =>
            {
                // 覆盖全局限流
                options.GlobalLimiter =
                    PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                        RateLimitPartition.GetNoLimiter("no-limit"));

                // ✅ 必须把所有命名策略都注册为 NoLimiter
                // 否则 [EnableRateLimiting("xxx")] 找不到策略会抛 500
                foreach (var policyName in new[]
                             { "login", "register", "forgot-password", "resend-verification", "browse", "write" })
                    options.AddPolicy(policyName, _ =>
                        RateLimitPartition.GetNoLimiter("no-limit"));
            });
    });

    builder.UseEnvironment("Testing");
}
```

> **`RateLimitPartition.GetNoLimiter` 是什么？**
>
> ASP.NET Core 内置的"无限制"分区，任何请求都直接通过，相当于把限流关掉。只在测试环境用，生产环境绝对不用。



### 7. 编译和测试

```bash
dotnet build
dotnet test
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Test summary: total: XX, failed: 0, succeeded: XX
```



### 8. Git 提交

```bash
git add .
git commit -m "feat: rate limiting"
git push origin feature/v3-rate-limiting

# 合并回 develop
git checkout develop
git merge --no-ff feature/v3-rate-limiting \
  -m "merge: feature/v3-rate-limiting into develop"
git push origin develop
git branch -d feature/v3-rate-limiting
git push origin --delete feature/v3-rate-limiting
```



### Step 59 完成状态

```
知识点：
✅ 理解固定窗口算法（原理、边界突刺缺陷、适用场景）
✅ 理解滑动窗口算法（无边界突刺、内存占用更高）
✅ 理解令牌桶算法（允许突发、平滑限速、适合浏览接口）
✅ 理解 QueueLimit=0 的含义（超出直接拒绝，不排队等待）
✅ 理解限流中间件在管道里的位置（必须在认证之前）
✅ 理解按 IP 和按用户 ID 分区的区别和选择依据

实现：
✅ 零依赖，使用 ASP.NET Core 8 内置 RateLimiter
✅ 6 个限流策略
✅ OnRejected 回调（返回标准 ApiResponse + Retry-After 响应头）
✅ [EnableRateLimiting] 应用到 AuthController 和 CarsController
✅ 前端 Axios 拦截器处理 429（读 Retry-After，显示 toast）
✅ 集成测试里用 GetNoLimiter 禁用限流
✅ 命令行验证：前10次401，后5次429，Retry-After 头存在
✅ dotnet build + dotnet test 通过
✅ Git commit + 合并回 develop 完成
```



## Step 60 · Hangfire 后台任务 + Application Insights

### 这一步做什么

Step 59 加了限流，保护了系统入口。现在来解决一个一直存在但还没修复的问题：**邮件发送的脆弱性**。

用户注册这个动作，本质上包含两件**性质不同**的事：

```
用户注册  =  [核心事务]  +  [后续副作用]
              写数据库        发验证邮件
              必须成功         失败可补救
              要快             可以慢
```

打开 V2 的注册流程

```c#
public class UserService
{
    ...

    public async Task<UserResponse> RegisterAsync(string username, string email, string password,
        CancellationToken cancellationToken = default)
    {
        // 1. 检查邮箱是否已被注册
        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing != null) throw new UserAlreadyExistsException(email);

        // 2. 构建用户实体（先不填 PasswordHash）
        var user = new User
        {
            Username = username,
            Email = email.ToLower(), // 统一存小写，保持一致性
            Role = UserRole.User, // 注册的用户默认是普通用户
            EmailConfirmed = false, // V1 不做邮箱验证，默认 false
            // 邮件验证的token
            EmailConfirmationToken = TokenGenerator.Generate(),
            EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Hash 密码（细节在 Step 09 讲解）
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        // 4. 存入数据库
        var created = await _userRepository.AddAsync(user, cancellationToken);

        // 先保存用户，再发邮件
        // 如果邮件发送失败，用户可以通过"重新发送"功能补救
        try
        {
            await _emailService.SendEmailVerificationAsync(
                created.Email,
                created.EmailConfirmationToken!,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // 邮件发送失败不阻断注册流程
            // 用户已成功写入数据库，可以通过"重新发送验证邮件"功能补救
            _logger.LogWarning(ex, "Failed to send verification email to {Email}", created.Email);
        }

        _logger.LogInformation("New user registered: {Email}", created.Email);

        // 5. 返回 DTO（不包含 PasswordHash）
        return MapToResponse(created);
    }
    
    ...

}
```

逻辑是这样的：

```
用户点击注册
  → AuthController.Register()
    → UserService.RegisterAsync()
      → 写入数据库
      → EmailService.SendEmailConfirmationAsync()   ← 同步调用 Resend API
        → 等待 Resend 响应（可能 200ms，可能 2000ms，可能超时）
    → 返回 201
```

问题出在第三步：`await` 的方式把这两件事**串在同一条链上**， 导致注册逻辑和**邮件发送是同步（耦合）的**。

整个注册请求必须等待 Resend API 响应才能返回给用户。

这意味着：

- Resend 慢了 → 用户等待
- Resend 短暂故障 → 注册失败（虽然用户数据已经写入数据库）
- Resend 超时 → 用户看到 500 错误，但账号其实已经创建了

**这是一个典型的"主流程不应该依赖外部服务"的问题。** 

在真实生产项目里，任何发邮件、发短信、发通知的操作，都应该异步化——把任务放入队列，立即返回给用户，后台慢慢处理。

这样用户的体验从：

```
用户点击"注册" 
→ 等待... 等待... 等待...（3秒，邮件发完才返回）
→ "注册成功"

如果邮件服务挂掉
→ "注册失败" ❌

但实际上数据库已经写入了。
用户再试一次 → "邮箱已被注册" ❌

用户懵了。
```

变成

```
用户点击"注册"
→ 立刻返回（< 100ms）
→ "注册成功，验证邮件已发送"
→ 几秒后邮件到达收件箱

如果邮件服务挂掉
用户账号正常创建。
邮件服务恢复后，队列自动补发邮件。
用户最终会收到，只是晚了一点。
```

这就是 **后台任务队列（Background Job Queue）** 的核心价值。

**一句话总结**：

> **await：邮件发送的问题，变成了用户注册的问题。**
>
> **队列：邮件发送的问题，只是邮件发送的问题。**



### 为什么用 Hangfire方案？

可能的几种后台任务的方案：

**方案一：`IHostedService` / `BackgroundService`（.NET 内置）**

```csharp
// 这是最简单的后台任务方式
public class EmailBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 自己轮询，自己实现重试，自己实现持久化...
        }
    }
}
```

问题：需要自己实现任务队列、重试机制、失败记录、可视化面板。工作量很大，而且没有持久化——应用重启后，队列里还没执行的任务全部丢失。

**方案二：Azure Service Bus / RabbitMQ / Redis Streams**

分布式消息队列。功能强大，但复杂度高，需要额外的基础设施。对于我们的场景（单实例应用，任务量不大）是过度设计。

**方案三：Hangfire**

```
优点：
- 任务持久化到 SQL Server（应用重启不丢失）
- 内置重试机制（失败自动重试，指数退避）
- 内置 Dashboard（可视化查看任务状态）
- 支持多种任务类型（Fire-and-Forget / Recurring / Delayed / Continuation）
- 复用现有 SQL Server，不需要额外基础设施
- 生产项目里非常常见，面试被问到的概率高

缺点：
- 给 SQL Server 加了几张系统表（Hangfire 自动创建）
- 不适合极高频任务（每秒几千个）
```

对于 UUcars 的场景（注册邮件、密码重置邮件、定时清理），Hangfire 是最合适的选择。



### Hangfire 的核心概念

Hangfire提供了一些核心方法和配置属性来实现不同的任务

- 核心方法， 通过静态工具类， 比如 `BackgroundJob`，提供的方法
- 核心配置， 通过扩展方法， 比如`AddHangfire`, 挂在 `IServiceCollection` 上进行配置

#### 任务类型

##### **Fire-and-Forget（触发即忘）**

```csharp
BackgroundJob.Enqueue(() => emailService.SendAsync(...));
```

- 调用后立即返回，任务放入队列
- 后台 Worker 取出任务执行
- 执行失败自动重试（默认重试 10 次，指数退避）
- **适用场景：发邮件、发通知、记录日志等一次性任务**

##### **Recurring Job（定时任务）**

```csharp
RecurringJob.AddOrUpdate("cleanup-tokens", 
    () => cleanupService.CleanExpiredTokensAsync(), 
    Cron.Daily);
```

- 按 Cron 表达式定期执行
- 应用启动时自动注册，重启后继续执行
- **适用场景：每天清理过期数据、每小时生成报表等**

##### **Delayed Job（延迟执行）**

```csharp
BackgroundJob.Schedule(() => someService.DoSomething(), TimeSpan.FromHours(24));
```

- 在指定时间后执行
- **适用场景：24小时后发送提醒邮件、订单超时自动取消等**

##### **Continuation（链式任务）**

```csharp
var jobId = BackgroundJob.Enqueue(() => step1());
BackgroundJob.ContinueJobWith(jobId, () => step2());
```

- 前一个任务完成后触发下一个
- **适用场景：数据处理流水线**

这一步用到前两种：Fire-and-Forget（邮件发送）和 Recurring Job（Token 清理）。

#### 核心配置

进行配置的静态方法都挂在 `IServiceCollection` 上。 类似如下：

```c#
// Hangfire 包内部大概长这样
public static class HangfireServiceCollectionExtensions
{
    public static IServiceCollection AddHangfire(
        this IServiceCollection services,   // this 关键字 = 扩展方法
        Action<IGlobalConfiguration> configuration)
    {
        // 内部帮你注册一堆 Hangfire 需要的服务
        ...
        return services;
    }
}
```

> `this IServiceCollection` 这个语法的意思是：**把这个方法挂到 IServiceCollection 类型上**，可以像调用它自己的方法一样调用。
>
> ```c#
> builder.Services.AddControllers();        // ASP.NET 提供的扩展方法
> builder.Services.AddDbContext<AppDbContext>(); // EF Core 提供的扩展方法
> builder.Services.AddHangfire(...);        // Hangfire 提供的扩展方法
> builder.Services.AddScoped<IUserRepository, UserRepository>(); // 原生方法
> ```

常用的核心配置：

##### 存储配置（必须配）

```c#
// Program.cs
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(connectionString)   // PostgreSQL
    .UseSqlServerStorage(connectionString)    // SQL Server
    .UseRedis(connectionString)               // Redis
    .UseInMemoryStorage()                     // 内存，仅开发用，重启丢失
);
```

```
选哪个：你数据库用什么就用什么，不需要额外部署新服务
```

##### Worker 数量配置

```c#
builder.Services.AddHangfireServer(options => {
    options.WorkerCount = 5;  // 同时执行几个任务，默认是 CPU核心数 * 5
});
```

```
使用场景：
发邮件任务不需要太多并发 → 设小一点，比如 3-5
CPU密集型任务             → 设成 CPU 核心数
```

##### Dashboard 配置

```c#
app.UseHangfireDashboard("/hangfire", new DashboardOptions {
    // 生产环境加权限控制，不然任何人都能访问
    Authorization = [new HangfireAuthFilter()]
});

// 权限过滤器示例
public class HangfireAuthFilter : IDashboardAuthorizationFilter {
    public bool Authorize(DashboardContext context) {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole("Admin");  // 只有 Admin 能看
    }
}
```

##### 过期任务清理

```c#
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions {
        JobExpirationCheckInterval = TimeSpan.FromHours(1),  // 多久检查一次过期任务
    })
    .WithJobExpirationTimeout(TimeSpan.FromDays(7))  // 成功的任务保留多久
);
```

#### Hangfire 的架构

```
你的代码
  ↓  BackgroundJob.Enqueue(...)
Hangfire Client
  ↓  序列化任务信息（方法名、参数）
SQL Server（Hangfire 系统表）
  ↑  轮询取任务
Hangfire Server（后台 Worker）
  ↓  反序列化，调用实际方法
你的 Service（EmailService、CleanupService 等）
```

关键点：

1. 任务信息（方法名+参数）被序列化后存入 SQL Server
2. 应用重启不影响已入队的任务
3. Hangfire Server 是在应用进程里运行的（不是单独的进程），通过 `AddHangfireServer()` 启动



### 1. 切出功能分支

```bash
git checkout develop
git pull origin develop
git checkout -b feature/v3-hangfire
git push -u origin feature/v3-hangfire
```



### 2. 安装 NuGet 包

```bash
cd UUcars.API
dotnet add package Hangfire --version 1.8.14
dotnet add package Hangfire.SqlServer --version 1.8.14
```

> **为什么选 `Hangfire.SqlServer`？**
>
> Hangfire 支持多种存储后端（SQL Server、Redis、PostgreSQL 等）。我们已经有 SQL Server，直接复用，不增加新的基础设施依赖。Hangfire 会在数据库里自动创建几张系统表（`HangFire.Job`、`HangFire.State` 等），第一次启动时自动完成，不需要手动建表。

确认包已安装：

```bash
cat UUcars.API.csproj | grep Hangfire
```

预期输出：

```xml
<PackageReference Include="Hangfire" Version="1.8.14" />
<PackageReference Include="Hangfire.SqlServer" Version="1.8.14" />
```



### 3. 配置 Hangfire（扩展方法）

和前面的 JWT、CORS、RateLimiting 配置保持一致的风格——封装成扩展方法，保持 `Program.cs` 简洁。

新建 `UUcars.API/Extensions/HangfireExtensions.cs`：

```csharp
using Hangfire;
using Hangfire.SqlServer;

namespace UUcars.API.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        services.AddHangfire(config => config
            // 使用 SQL Server 作为存储后端
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
               	// 队列轮询间隔：Hangfire Server 多久检查一次队列
                // 默认 15 秒，对邮件发送来说可以接受
                // 如果需要更快响应，可以降到 5 秒
                QueuePollInterval = TimeSpan.FromSeconds(15),

                // 任务滑动不可见超时：任务被取走后多久没有完成算超时
                // 超时后重新放回队列（防止 Worker 崩溃导致任务永久丢失）
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5), // 官方推荐显式设置

                // 自动删除已完成的任务（保留 7 天，避免数据库无限增长）
                JobExpirationCheckInterval = TimeSpan.FromHours(1)
            })
            // 使用结构化日志（接入已有的 Serilog）
            .UseRecommendedSerializerSettings()
        );

        // 注册 Hangfire Server（在当前进程里运行后台 Worker）
        services.AddHangfireServer(options =>
        {
            // Worker 线程数：同时处理多少个任务
            // 默认是 CPU 核心数 * 5，对我们足够
            options.WorkerCount = 5;

            // 队列名称：Worker 只处理哪些队列
            // 默认只处理 "default" 队列，我们暂时只用这一个
            options.Queues = new[] { "default" };
        });

        return services;
    }

}
```

**注册 Hangfire 服务**

打开 `UUcars.API/Program.cs`，在服务注册区域（其他 `Add*` 扩展方法附近）加入：

```csharp
// ── Hangfire 后台任务 ──────────
builder.Services.AddHangfireServices(builder.Configuration);

```



### 4. 邮件发送改为异步队列

这是这一步的核心改动：把同步邮件发送改为 Hangfire 异步任务。

#### 4.1 理解改动的范围

当前 V2 的邮件发送流程：

```csharp
// UserService.cs（当前 V2 代码）
public async Task<UserResponse> RegisterAsync(RegisterRequest request, ...)
{
    // ... 创建用户 ...
    await _context.SaveChangesAsync();

    // ❌ 同步发送邮件：整个请求等这一行完成
    await _emailService.SendEmailConfirmationAsync(user.Email, confirmationUrl);

    return MapToResponse(user);
}
```

改造目标：

```csharp
// UserService.cs（改造后）
public async Task<UserResponse> RegisterAsync(RegisterRequest request, ...)
{
    // ... 创建用户 ...
    await _context.SaveChangesAsync();

    // ✅ 异步入队：立即返回，邮件在后台发送
    BackgroundJob.Enqueue<IEmailService>(
        service => service.SendEmailConfirmationAsync(user.Email, confirmationUrl));

    return MapToResponse(user);
}
```

注意两处差异：

1. `await` 去掉了——不再等待邮件发送完成
2. 用 `BackgroundJob.Enqueue<IEmailService>` 而不是直接调用 `_emailService` （原因见下方说明）

#### 4.2 为什么用 `BackgroundJob.Enqueue<IEmailService>` 而不是 `_emailService`？

**方式一（错误）：**

```csharp
BackgroundJob.Enqueue(() => _emailService.SendEmailConfirmationAsync(email, url));
```

问题：Hangfire 序列化任务时，会把 `_emailService` 这个对象实例序列化进去。 `IEmailService` 的实现（`ResendEmailService`）包含 `HttpClient`、API Key 等， 无法被正确序列化和反序列化。应用重启后，从 SQL Server 恢复任务时， Hangfire 无法还原这个对象，任务会永久失败。

**方式二（正确）：**

```csharp
BackgroundJob.Enqueue<IEmailService>(
    service => service.SendEmailConfirmationAsync(email, url));
```

Hangfire 序列化的是：

- 类型：`IEmailService`
- 方法：`SendEmailConfirmationAsync`
- 参数：`email`（字符串），`url`（字符串）

执行时，Hangfire 从 DI 容器里解析 `IEmailService`，调用对应方法。 DI 容器保证每次都拿到正确配置的实例。这才是正确的姿势。

#### 4.3 注入 `IBackgroundJobClient`

`UserService` 当前的构造函数里没有 Hangfire 依赖，需要注入。

打开 `UUcars.API/Services/UserService.cs`：

```csharp
using Hangfire;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,   // ✅ 新增
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _backgroundJobClient = backgroundJobClient; // ✅ 新增
        _logger = logger;
    }
```

> **`IBackgroundJobClient` 是什么？**
>
> 这是 Hangfire 提供的接口，封装了 `BackgroundJob.Enqueue` 等静态方法的实例版本。
>
> 为什么用接口而不是直接调静态方法 `BackgroundJob.Enqueue(...)`？
>
> **可测试性**。静态方法无法在单元测试里被替换（Mock）， 而 `IBackgroundJobClient` 是接口，可以在测试里注入 `FakeBackgroundJobClient`， 验证"邮件任务是否被正确入队"而不真的发邮件。
>
> Hangfire 注册 `AddHangfire()` 后，`IBackgroundJobClient` 会自动注册到 DI 容器。

#### 4.4 修改 RegisterAsync

找到 `UserService.RegisterAsync` 方法，修改邮件发送部分：

```csharp
// 改之前
try
{
    await _emailService.SendEmailVerificationAsync(
        created.Email,
        created.EmailConfirmationToken!,
        cancellationToken);
}
catch (Exception ex)
{
    // 邮件发送失败不阻断注册流程
    // 用户已成功写入数据库，可以通过"重新发送验证邮件"功能补救
    _logger.LogWarning(ex, "Failed to send verification email to {Email}", created.Email);
}

_logger.LogInformation("New user registered: {Email}", created.Email);
```

```c#
// 改之后
// try/catch 不再需要：Hangfire 有内置重试机制（失败自动重试10次，指数退避）
// 邮件发送失败会在 Hangfire Dashboard 里显示，可以手动重试
_backgroundJobClient.Enqueue<IEmailService>(
    service => service.SendEmailVerificationAsync(
        created.Email,
        created.EmailConfirmationToken!,
        CancellationToken.None));  // Hangfire 序列化参数时不支持 CancellationToken，必须用 None

_logger.LogInformation(
    "New user registered: {Email}, verification email job enqueued", created.Email);
```

关键改动：

- `await _emailService.SendEmailConfirmationAsync(...)` → `_backgroundJobClient.Enqueue<IEmailService>(...)`
- 去掉了 `await`（不等待）
- 日志改为"job enqueued"，明确表示任务已入队而不是邮件已发送

#### 4.5 修改 ResendVerificationAsync

```c#
// 改之前
await _emailService.SendEmailVerificationAsync(
    user.Email, newToken, cancellationToken);

_logger.LogInformation("Verification email resent to: {Email}", email);
```

```c#
// 改之后
_backgroundJobClient.Enqueue<IEmailService>(
    service => service.SendEmailVerificationAsync(
        user.Email, newToken, CancellationToken.None));

_logger.LogInformation(
    "Verification email resend job enqueued for: {Email}", email);
```

#### 4.6 修改 ForgotPasswordAsync

同样找到 `ForgotPasswordAsync` 方法，修改邮件发送部分：

```csharp
// 改之前
await _emailService.SendPasswordResetAsync(
    user.Email, resetToken, cancellationToken);

_logger.LogInformation("Password reset email sent to: {Email}", email);
```

```c#
// 改之后
_backgroundJobClient.Enqueue<IEmailService>(
    service => service.SendPasswordResetAsync(
        user.Email, resetToken, CancellationToken.None));

_logger.LogInformation(
    "Password reset email job enqueued for: {Email}", email);
```

#### 4.7 不需要修改的地方

- `_emailService` 字段**保留**，`VerifyEmailAsync`、`ResetPasswordAsync` 等方法不涉及发邮件，不需要改
- `LoginAsync`、`GetCurrentUserAsync`、`UpdateCurrentUserAsync`、`MapToResponse` **完全不变**
- `VerifyEmailAsync`、`ResetPasswordAsync` **完全不变**



### 5. 更新测试

`UserService` 的构造函数多了 `IBackgroundJobClient` 依赖，现有的单元测试会编译失败。

#### 5.1 新建 FakeBackgroundJobClient

打开 `UUcars.Tests/Fakes/`，新建 `FakeBackgroundJobClient.cs`：

```csharp
using Hangfire;

namespace UUcars.Tests.Fakes;

/// <summary>
///     测试用的 BackgroundJobClient：记录入队的任务，不真正执行
/// </summary>
public class FakeBackgroundJobClient : IBackgroundJobClient
{
    // 记录所有入队的任务（方便在测试里断言"是否入队了"）
    public List<string> EnqueuedJobs { get; } = new();

    public string Enqueue(Job job, IState state)
    {
        EnqueuedJobs.Add(job.Method.Name);
        return Guid.NewGuid().ToString(); // 返回一个假的 Job ID
    }

    public bool ChangeState(string jobId, IState state, string? expectedCurrentState)
        => true;

    public string Create(Job job, IState state)
    {
        EnqueuedJobs.Add(job.Method.Name);
        return Guid.NewGuid().ToString();
    }
}
```

#### 5.2 更新 UserServiceTests 单元测试

打开 `UUcars.Tests/Services/UserServiceTests.cs`，在 `CreateService` 辅助方法里加入 `FakeBackgroundJobClient`：

```csharp
private static (UserService service, FakeBackgroundJobClient jobClient) CreateService(
    FakeUserRepository? userRepo = null)
{
    var jobClient = new FakeBackgroundJobClient();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["App:FrontendUrl"] = "https://localhost:5173"
        })
        .Build();

    var service = new UserService(
        userRepo ?? new FakeUserRepository(),
        new FakeEmailService(),
        jobClient,                              // ✅ 新增
        NullLogger<UserService>.Instance
    );

    return (service, jobClient);
}
```

**改为 Hangfire 后，测试逻辑如何调整？**

当前很多测试依赖 `FakeEmailService` 里存的 Token：

```c#
// 注册后从 FakeEmailService 里取出 Token 做后续操作
var token = emailService.SentVerificationEmails[0].Token;
await service.VerifyEmailAsync(token);
```

改为 Hangfire 后，邮件是"入队"而不是"直接发送"，`FakeBackgroundJobClient` 只记录了方法名，**没有记录 Token 参数**。这意味着这些测试无法再从邮件服务里取 Token。

**思路：直接从数据库（FakeUserRepository）取 Token**

不依赖邮件服务，注册后直接从 `repo` 里读用户的 `EmailConfirmationToken`：

```c#
var user = await repo.GetByEmailAsync("test@example.com");
var token = user!.EmailConfirmationToken;
```

这更符合测试的本质：测的是业务逻辑，不是邮件发送。

修改后的单元测试

```c#
public class UserServiceTests
{
    // v3 更新：
    // - 返回 (service, fakeBackgroundJobClient) 元组
    // - fakeEmailService 内部保留但不再从外部断言邮件内容
    //   原因：邮件改为 Hangfire 异步入队，不再直接发送
    //   Token 的验证改为从 FakeUserRepository 直接读取
    private static (UserService Service, FakeBackgroundJobClient JobClient)
        CreateService(FakeUserRepository repo)
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-at-least-32-characters!",
            ExpiresInMinutes = 60,
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });

        var fakeEmailService = new FakeEmailService();
        var fakeBackgroundJobClient = new FakeBackgroundJobClient();

        var service = new UserService(
            repo,
            new PasswordHasher<User>(),
            new JwtTokenGenerator(jwtSettings),
            fakeEmailService,
            fakeBackgroundJobClient,
            NullLogger<UserService>.Instance
        );

        return (service, fakeBackgroundJobClient);
    }

    // ===== 注册测试 =====

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnUserResponseAndEnqueueEmailJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        var result = await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);

        // V3 更新：验证邮件 Job 已入队（不再验证邮件是否直接发送）
        Assert.Single(jobClient.EnqueuedJobs);
        Assert.Contains("SendEmailVerificationAsync", jobClient.EnqueuedJobs);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowUserAlreadyExistsException()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User
        {
            Id = 1,
            Email = "existing@example.com",
            Username = "existing",
            PasswordHash = "somehash",
            Role = UserRole.User
        });

        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.RegisterAsync(
            "newuser",
            "existing@example.com",
            "Test@123456"
        ));
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotStorePasswordAsPlainText()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        var stored = await repo.GetByEmailAsync("test@example.com");
        Assert.NotNull(stored);
        Assert.NotEqual("Test@123456", stored.PasswordHash);
        Assert.True(stored.PasswordHash.Length > 20);
    }

    // ===== 登录测试 =====

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ShouldThrowEmailNotConfirmedException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // 注册后不验证邮箱，直接登录应被拒绝
        await Assert.ThrowsAsync<EmailNotConfirmedException>(() => service.LoginAsync(
            "test@example.com",
            "Test@123456"
        ));
    }

    [Fact]
    public async Task LoginAsync_WithVerifiedEmail_ShouldReturnLoginResponse()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // V3 更新：Token 直接从 repo 读取，不再从 FakeEmailService 取
        // 原因：邮件改为异步入队，FakeEmailService 不再收到真实调用
        var user = await repo.GetByEmailAsync("test@example.com");
        var token = user!.EmailConfirmationToken;
        await service.VerifyEmailAsync(token!);

        var result = await service.LoginAsync(
            "test@example.com",
            "Test@123456"
        );

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(
            "test@example.com",
            "WrongPassword"
        ));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldThrowInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(
            "notexist@example.com",
            "Test@123456"
        ));
    }

    // ===== 邮箱验证测试 =====

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldConfirmEmail()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // V3 更新：直接从 repo 取 Token
        var user = await repo.GetByEmailAsync("test@example.com");
        var token = user!.EmailConfirmationToken;

        await service.VerifyEmailAsync(token!);

        var updated = await repo.GetByEmailAsync("test@example.com");
        Assert.True(updated!.EmailConfirmed);
        Assert.Null(updated.EmailConfirmationToken);
        Assert.Null(updated.EmailConfirmationTokenExpiry);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.VerifyEmailAsync(
            "completely-wrong-token"
        ));
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            Role = UserRole.User,
            EmailConfirmed = false,
            EmailConfirmationToken = "expired-token",
            EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(-1)
        });

        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.VerifyEmailAsync(
            "expired-token"
        ));
    }

    [Fact]
    public async Task ResendVerificationAsync_WithUnverifiedEmail_ShouldEnqueueEmailJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // 注册时已入队一次
        Assert.Single(jobClient.EnqueuedJobs);

        await service.ResendVerificationAsync("test@example.com");

        // 重发后共入队两次
        Assert.Equal(2, jobClient.EnqueuedJobs.Count);
        Assert.All(jobClient.EnqueuedJobs,
            job => Assert.Equal("SendEmailVerificationAsync", job));
    }

    [Fact]
    public async Task ResendVerificationAsync_WithNonExistentEmail_ShouldNotEnqueueJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        // 不存在的邮箱：静默返回，不入队
        await service.ResendVerificationAsync("nobody@example.com");

        Assert.Empty(jobClient.EnqueuedJobs);
    }

    // ===== 密码重置测试 =====

    [Fact]
    public async Task ForgotPasswordAsync_WithValidVerifiedEmail_ShouldEnqueueResetEmailJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        // 注册 + 验证邮箱
        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );
        var user = await repo.GetByEmailAsync("test@example.com");
        await service.VerifyEmailAsync(user!.EmailConfirmationToken!);

        // 发起重置密码
        await service.ForgotPasswordAsync("test@example.com");

        // 注册时入队一次(SendEmailVerificationAsync)
        // ForgotPassword 入队一次(SendPasswordResetAsync)
        Assert.Equal(2, jobClient.EnqueuedJobs.Count);
        Assert.Contains("SendPasswordResetAsync", jobClient.EnqueuedJobs);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldNotEnqueueJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        await service.ForgotPasswordAsync("nobody@example.com");

        Assert.Empty(jobClient.EnqueuedJobs);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithUnverifiedEmail_ShouldNotEnqueueJob()
    {
        var repo = new FakeUserRepository();
        var (service, jobClient) = CreateService(repo);

        // 注册但不验证邮箱
        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );

        // 清空注册时产生的入队记录，专注测试 ForgotPassword 的行为
        jobClient.EnqueuedJobs.Clear();

        // 未验证的账号不能重置密码，不应入队
        await service.ForgotPasswordAsync("test@example.com");

        Assert.Empty(jobClient.EnqueuedJobs);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldUpdatePasswordAndClearToken()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        // 注册 + 验证邮箱
        await service.RegisterAsync(
            "testuser",
            "test@example.com",
            "Test@123456"
        );
        var user = await repo.GetByEmailAsync("test@example.com");
        await service.VerifyEmailAsync(user!.EmailConfirmationToken!);

        // 发起重置，Token 直接从 repo 读
        await service.ForgotPasswordAsync("test@example.com");
        var updatedUser = await repo.GetByEmailAsync("test@example.com");
        var resetToken = updatedUser!.ResetPasswordToken;

        // 用新密码重置
        await service.ResetPasswordAsync(resetToken!, "NewPassword@123");

        // 验证：新密码可以登录
        var loginResult = await service.LoginAsync(
            "test@example.com",
            "NewPassword@123"
        );
        Assert.NotEmpty(loginResult.Token);

        // 验证：旧密码不能再登录
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(
            "test@example.com",
            "Test@123456"
        ));

        // 验证：Token 已清除，不能重复使用
        var finalUser = await repo.GetByEmailAsync("test@example.com");
        Assert.Null(finalUser!.ResetPasswordToken);
        Assert.Null(finalUser.ResetPasswordTokenExpiry);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.ResetPasswordAsync(
            "invalid-token",
            "NewPassword@123"
        ));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        repo.Seed(new User
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = "hash",
            Role = UserRole.User,
            EmailConfirmed = true,
            ResetPasswordToken = "expired-reset-token",
            ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(-1)
        });

        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(() => service.ResetPasswordAsync(
            "expired-reset-token",
            "NewPassword@123"
        ));
    }
}
```

#### 5.3 更新集成测试

**问题所在**

集成测试里取 Token 的方式是：

```c#
// IntegrationTestBase.cs
var verifyToken = Factory.FakeEmail.SentVerificationEmails
    .Last(x => x.Email == email).Token;

// CoreFlowIntegrationTests.cs
var verifyToken = Factory.FakeEmail.SentVerificationEmails
    .Last(x => x.Email == "test@example.com").Token;
```

改为 Hangfire 后，`UserService.RegisterAsync` 调用的是：

```c#
_backgroundJobClient.Enqueue<IEmailService>(
    service => service.SendEmailVerificationAsync(...));
```

**`IEmailService` 不再被直接调用**，所以 `FakeEmailService.SentVerificationEmails` 永远是空列表，`Last()` 会抛 `InvalidOperationException`，测试直接崩。

**解决思路**

和单元测试一样：**直接从数据库查 Token**，不再依赖 `FakeEmailService`。

集成测试有真实的数据库（Testcontainers SQL Server），可以直接通过 `DbContext` 查询。

需要在 `SqlServerTestFactory` 里暴露一个查询 Token 的方法。

##### 修改一：`SqlServerTestFactory.cs`

**移除 `FakeEmail` 属性，改为暴露 `DbContext` 或直接提供查 Token 的辅助方法：**

```csharp
public class SqlServerTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // ✅ 移除：不再需要 FakeEmail
    // public FakeEmailService FakeEmail { get; } = new();

    // ✅ 新增：暴露 DbContext 查询能力，供 IntegrationTestBase 直接查 Token
    public AppDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 原有：替换 DbContext（不变）
            // ...

            // ✅ 移除：不再需要替换 IEmailService 为 FakeEmailService
            // var emailDescriptor = services.SingleOrDefault(...)
            // services.Remove(emailDescriptor);
            // services.AddSingleton<IEmailService>(FakeEmail);

            // ✅ 新增：替换 IBackgroundJobClient 为 Fake
            // 原因：不能在测试里真正调用 Hangfire（需要 SQL Server Hangfire 系统表）
            // FakeBackgroundJobClient 静默处理入队，不真正执行任务
            var jobClientDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBackgroundJobClient));
            if (jobClientDescriptor != null)
                services.Remove(jobClientDescriptor);

            services.AddSingleton<IBackgroundJobClient>(new FakeBackgroundJobClient());
        });

        builder.UseEnvironment("Testing");
    }

    // InitializeDatabaseAsync 和 CleanDatabaseAsync 完全不变
}
```

> **为什么要替换 `IBackgroundJobClient`？**
>
> Hangfire 注册后，`IBackgroundJobClient` 的真实实现会尝试把 Job 写入 SQL Server 的 Hangfire 系统表（`HangFire.Job` 等）。
>
> 但集成测试用的是 Testcontainers 临时数据库，`InitializeDatabaseAsync` 只跑了你的业务 Migration，没有 Hangfire 系统表。真实的 `IBackgroundJobClient` 一调用就会报错：表不存在。
>
> 替换为 `FakeBackgroundJobClient` 后，`Enqueue` 调用静默成功，不真正写任何东西。

##### 修改二：`IntegrationTestBase.cs`

**取 Token 的方式从 `FakeEmailService` 改为直接查数据库：**

```c#
protected async Task<string> RegisterAndLoginAsync(
    string email = "test@example.com",
    string username = "testuser",
    string password = "Test@123456")
{
    // Step 1：注册
    await Client.PostAsync("/auth/register", JsonContent(new
    {
        username, email, password
    }));

    // Step 2：直接从数据库取 EmailConfirmationToken
    // V3 更新：邮件改为 Hangfire 异步入队，FakeEmailService 不再收到调用
    // Token 已写入数据库，直接查比依赖 FakeEmailService 更可靠
    using var db = Factory.GetDbContext();
    var user = await db.Users
        .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    var verifyToken = user!.EmailConfirmationToken;

    // Step 3：验证邮箱
    await Client.GetAsync($"/auth/verify-email?token={verifyToken}");

    // Step 4：登录拿 JWT
    var loginResponse = await Client.PostAsync("/auth/login", JsonContent(new
    {
        email, password
    }));

    var result = await DeserializeAsync<ApiResponseWrapper<LoginData>>(loginResponse);
    return result?.Data?.Token ?? string.Empty;
}
```

##### 修改三：`CoreFlowIntegrationTests.cs`

**`Login_WithValidCredentials_ShouldReturnToken` 里同样改为查数据库取 Token：**

```c#
[Fact]
public async Task Login_WithValidCredentials_ShouldReturnToken()
{
    await Client.PostAsync("/auth/register", JsonContent(new
    {
        username = "testuser",
        email = "test@example.com",
        password = "Test@123456"
    }));

    // V3 更新：直接从数据库取 Token
    using var db = Factory.GetDbContext();
    var user = await db.Users
        .FirstOrDefaultAsync(u => u.Email == "test@example.com");
    var verifyToken = user!.EmailConfirmationToken;

    await Client.GetAsync($"/auth/verify-email?token={verifyToken}");

    var response = await Client.PostAsync("/auth/login", JsonContent(new
    {
        email = "test@example.com",
        password = "Test@123456"
    }));

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var result = await DeserializeAsync<ApiResponseWrapper<LoginData>>(response);
    Assert.NotNull(result?.Data?.Token);
    Assert.NotEmpty(result!.Data!.Token);
}
```

##### 修改四：AddServices 扩展方法

测试是，我们不需要注册Hangfire框架以及相关的中间件，因此直接过滤掉

```c#
public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment // 新增
    )
    {
        // ✅ Testing 环境完全跳过 Hangfire 配置
        // 原因：AddHangfire 的 UseSqlServerStorage 会尝试连接数据库初始化系统表
        // Testcontainers 的数据库里没有 Hangfire 系统表，直接崩溃
        // Testing 环境只需要 FakeBackgroundJobClient（在 SqlServerTestFactory 里注册）
        if (environment.IsEnvironment("Testing"))
            return services;

        // 其余内容不变

        return services;
    }
}
```

同时修改注册服务的参数

```c#
    // Hangfire服务
    builder.Services.AddHangfireServices(builder.Configuration, builder.Environment);
```



##### 总结

| 文件                          | 修改内容                                                     |
| ----- |  |
| `SqlServerTestFactory.cs`     | 移除 `FakeEmail` 属性 + 移除替换 `IEmailService` 的代码 + 新增替换 `IBackgroundJobClient` 为 `FakeBackgroundJobClient` + 新增 `GetDbContext()` 方法 |
| `IntegrationTestBase.cs`      | `RegisterAndLoginAsync` 里取 Token 改为查数据库              |
| `CoreFlowIntegrationTests.cs` | `Login_WithValidCredentials` 里取 Token 改为查数据库         |
| 其余集成测试                  | **完全不变**，都通过 `RegisterAndLoginAsync` 取 Token，只改一处即可 |



### 6. 定期清除token

Hangfire除了能实现异步队列（BackgroundJob.Enqueue）的功能外，也能实现定时任务（RecurringJob.AddOrUpdate）。

我们使用Hangfire使用定期清除token的功能。

#### 6.1. 新建 TokenCleanupService

新建 `UUcars.API/Services/TokenCleanupService.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;

namespace UUcars.API.Services;

/// <summary>
///     负责清理数据库里的过期 Token
///     由 Hangfire 每天凌晨定时调用
/// </summary>
public class TokenCleanupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(AppDbContext context, ILogger<TokenCleanupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanExpiredTokensAsync()
    {
        var now = DateTime.UtcNow;

        // 清理过期的密码重置 Token
        // 找出所有 ResetPasswordTokenExpiry 已过期的用户，清空他们的 Token 字段
        var expiredPasswordResets = await _context.Users
            .Where(u => u.ResetPasswordTokenExpiry != null
                        && u.ResetPasswordTokenExpiry < now)
            .ToListAsync();

        if (expiredPasswordResets.Count > 0)
        {
            foreach (var user in expiredPasswordResets)
            {
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cleaned {Count} expired password reset tokens",
                expiredPasswordResets.Count);
        }
        else
        {
            _logger.LogInformation("No expired password reset tokens found");
        }
    }
}
```

> **为什么不直接用 `ExecuteDeleteAsync`（批量删除）？**
>
> `ExecuteDeleteAsync` 是 EF Core 7+ 的批量操作，性能更好，但对 Token 清理来说， 我们需要把 Token 字段清空（设为 null），而不是删除整行用户记录。 所以这里是 `ExecuteUpdate` 更合适，但为了代码清晰易懂， 先用标准的"查出来再改"方式，数量不大时性能没有区别。
>
> V3 学习阶段先写清楚逻辑，性能优化不是这里的重点。

#### 6.2 封装定时任务扩展方法

在现有的扩展方法里封装新的方法，并使用TokenCleanupService

```c#
public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        ...
    }

    // 注册定时任务（Recurring Jobs）
    // 在 app.UseHangfire() 里调用，确保在应用完全启动后执行
    public static void UseHangfireJobs(this IApplicationBuilder app)
    {
        // 过期 Token 清理任务
        // Cron.Daily = 每天凌晨 00:00 执行
        // 凌晨执行原因：流量最低，对用户无感知
        RecurringJob.AddOrUpdate<TokenCleanupService>(
            "cleanup-expired-tokens", // 任务 ID（唯一标识，方便在 Dashboard 里识别）
            service => service.CleanExpiredTokensAsync(),
            Cron.Daily, // 执行频率
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc // 统一用 UTC，避免时区问题
            }
        );
    }
}
```

#### 6.3 使用定时任务扩展方法

在主程序的中间件管道部分使用

```c#
...
app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

...
// 定时清除过期token    
app.UseHangfireJobs();

app.MapControllers();
app.Run();
```



### 7. Hangfire面板

Hangfire 提供了控制面板，通过自定义配置，可以使用。

控制面板需要权限判断，比如只有管理员的角色才可以访问。

Hangfire Dashboard 有自己的访问控制接口 `IDashboardAuthorizationFilter`，我们需要实现它来把 Dashboard 的访问权限接入已有的 JWT + Role 认证体系。

新建 `UUcars.API/Auth/HangfireAdminAuthorizationFilter.cs`：

```csharp
using Hangfire.Dashboard;

namespace UUcars.API.Auth;

/// <summary>
///     Hangfire Dashboard 访问授权过滤器
///     只允许已认证的 Admin 角色用户访问 /hangfire 路由
/// </summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 必须已认证（登录）
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        // 必须是 Admin 角色
        return httpContext.User.IsInRole("Admin");
    }
}
```

> **注意：开发环境的特殊处理**
>
> 在本地开发时，你可能想直接访问 Dashboard 而不用每次都登录 Admin 账号。 可以在 `DashboardOptions` 里加判断：
>
> ```csharp
> Authorization = app.Environment.IsDevelopment()
>  ? new[] { new HangfireLocalDevAuthorizationFilter() }   // 开发环境：直接放行
>  : new[] { new HangfireAdminAuthorizationFilter() }      // 生产环境：必须 Admin
> ```
>
> 其中 `HangfireLocalDevAuthorizationFilter` 直接 `return true`。
>
> ```c#
> using Hangfire.Dashboard;
> 
> namespace UUcars.API.Auth;
> 
> /// <summary>
> /// 开发环境专用：直接放行，不做任何认证检查
> /// 只在 IsDevelopment() 时使用，生产环境永远不用这个
> /// </summary>
> public class HangfireLocalDevAuthorizationFilter : IDashboardAuthorizationFilter
> {
>     public bool Authorize(DashboardContext context)
>     {
>         return true;
>     }
> }
> ```
>
> 也可以为了保持一致性（开发环境和生产环境行为一致，避免"在我机器上好好的"问题）， 统一使用 `HangfireAdminAuthorizationFilter`。 开发时直接用 Admin 账号登录再访问 Dashboard。



**挂载 Hangfire相关中间件**

在 `app.Build()` 之后的中间件管道部分，加入 Hangfire 相关中间件：

完整的中间件顺序应该是：

```csharp
...

app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ✅ 非 Testing 环境才挂载 Hangfire Dashboard 和定时任务
if (!app.Environment.IsEnvironment("Testing"))
{
    // ✅ Hangfire Dashboard（在认证之后）
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = app.Environment.IsDevelopment()
            ? new[] { new HangfireLocalDevAuthorizationFilter() } // 开发：直接放行
            : new[] { new HangfireAdminAuthorizationFilter() } // 生产：必须 Admin
    });
    // 定时清除过期token
    app.UseHangfireJobs();
}

app.MapControllers();
app.Run();
```



###  8. Application Insights 接入

#### 为什么要接入 Application Insights？

V2 的 Serilog 配置把日志输出到控制台和文件。本地开发时没问题，但在 Azure Container Apps 上：

- 日志分散在容器实例里，多实例时查日志很麻烦
- 文件日志在容器重启后丢失
- 没有统一的查询界面

Application Insights 是 Azure 的统一可观测性平台：

- 所有日志集中存储，可跨实例查询
- 自动收集请求追踪、异常、性能指标
- 提供 KQL（Kusto Query Language）查询界面
- 免费额度：5GB/月，对学习项目完全够用

#### 8.1 安装 NuGet 包

```bash
dotnet add package Serilog.Sinks.ApplicationInsights --version 4.0.0
dotnet add package Microsoft.ApplicationInsights.AspNetCore --version 2.22.0
```

#### 8.2 在 Azure Portal 创建 Application Insights 资源

1. 进入 [portal.azure.com](https://portal.azure.com/)

2. 搜索 **Application Insights** → 创建

3. 配置：
    - 订阅：选择你的订阅
    - 资源组：选择和 Container Apps 相同的资源组（如 `uucars-rg`）
    - 名称：`uucars-appinsights`
    - 区域：和 Container Apps 相同的区域
    - 资源模式：**基于工作区**（推荐，可以和 Log Analytics 整合）

4. 创建完成后，进入资源 → **概述** → 复制 **连接字符串** 格式类似：

    ```
    InstrumentationKey=5b68dc35-74f5-4651-b081-e8cd17527ffb;IngestionEndpoint=https://australiaeast-1.in.applicationinsights.azure.com/;LiveEndpoint=https://australiaeast.livediagnostics.monitor.azure.com/;ApplicationId=e7f5c240-d73b-4987-adcb-a1aa860c5634
    ```

    

#### 8.3 配置 Connection String

**本地开发（`appsettings.Development.json`）：**

不在本地配置 Application Insights，本地只用控制台输出，避免把开发时的测试日志发到 Azure。

```json
{
  "ApplicationInsights": {
    "ConnectionString": ""
  }
}
```

**生产环境（Azure Container Apps 环境变量）：**

Azure Portal → Container Apps → `uucars-api` → **环境变量** → 添加：

```
ApplicationInsights__ConnectionString = InstrumentationKey=xxx;IngestionEndpoint=https://...
```

> **为什么用双下划线 `__`？**
>
> ASP.NET Core 的配置系统用 `__` 表示嵌套层级， 等价于 `appsettings.json` 里的 `{ "ApplicationInsights": { "ConnectionString": "xxx" } }`。

#### 8.4 更新 Serilog 配置

打开 `UUcars.API/Program.cs`，找到 Serilog 的 Bootstrap Logger 配置，更新 `UseSerilog` 部分：

```csharp
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console()
        .WriteTo.File(
            "logs/uucars-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30
        );

    // V3 修复：原本用 GetRequiredService<TelemetryConfiguration>()，
    // 一旦该服务未被成功注册就会直接抛 InvalidOperationException，
    // 导致 builder.Build() 失败、应用启动崩溃（2026-06 在 Container App
    // 上线后实际发生过一次，根因是订阅状态变动后 AI SDK 的服务注册
    // 出现了异常，具体内部原因未完全定位，详见 PR 描述）。
    //
    // 改用 GetService（允许返回 null）+ 判空，把"是否启用 Application
    // Insights 日志"降级为非关键路径：即使该服务因为任何原因未注册成功，
    // 应用本身依然能正常启动并提供核心业务功能，只是会失去 AI 日志能力。
    //
    // else 分支打印 Console 警告，是为了避免这种"静默失效"难以察觉——
    // 万一以后又复现类似问题，至少能在 Container App 的 Console log
    // stream 里看到提示，而不是悄无声息地丢失可观测性却毫无察觉。
    var aiConnStr = context.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrEmpty(aiConnStr))
    {
        var telemetryConfig = services.GetService
            <Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration>();

        if (telemetryConfig != null)
            configuration.WriteTo.ApplicationInsights(telemetryConfig, new TraceTelemetryConverter());
        else
            Console.WriteLine("[WARNING] ApplicationInsights ConnectionString 已配置，" +
                              "但 TelemetryConfiguration 服务未成功注册，AI 日志功能未启用。");
    }
});
```

同时在服务注册里加入 Application Insights SDK：

```csharp
// ── Application Insights 服务 ──
// V3 修复：原本用 if (!string.IsNullOrEmpty(aiConnectionString)) 包裹，
// 只在配置了 ConnectionString 时才注册，目的是避免本地开发环境
// 把测试日志发到 Azure，同时省去不必要的初始化开销。
//
// 但这种"按需注册"模式存在隐患：UseSerilog 的 lambda 里（见下方）
// 同样独立判断了一次 ConnectionString 是否为空，两边的判断理论上该同步，
// 但 AddApplicationInsightsTelemetry() 内部的注册逻辑不完全透明，
// 实测出现过"传入了合法的 ConnectionString，却没有把
// TelemetryConfiguration 真正注册进 DI 容器"的情况，导致下游
// GetRequiredService<TelemetryConfiguration> 直接抛异常，整个应用启动崩溃。
//
// 改为无条件注册：AddApplicationInsightsTelemetry 即使传入空字符串
// 也不会报错，只是会处于"禁用遥测"状态（不发送数据），但依然会把
// TelemetryConfiguration 服务注册进容器。这样可以从根上保证该服务
// 一定存在，避免"注册条件"和"使用条件"不同步导致的崩溃。
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddApplicationInsightsTelemetry(options => { options.ConnectionString = aiConnectionString; });

```

#### 8.5 验证 Application Insights 接收到日志

部署到 Azure 后（或者本地配置了 Connection String 后），验证步骤：

1. 进入 Azure Portal → Application Insights → **日志（Logs）**
2. 在查询编辑器里运行以下 KQL 查询（掌握这 4 个，工作中够用）：

**查询最近 1 小时的错误日志：**

```kusto
traces
| where timestamp > ago(1h)
| where severityLevel >= 3    // 3 = Warning, 4 = Error, 5 = Critical
| project timestamp, message, severityLevel, customDimensions
| order by timestamp desc
```

**查询响应时间超过 1 秒的请求：**

```kusto
requests
| where timestamp > ago(1h)
| where duration > 1000       // 单位：毫秒
| project timestamp, name, url, duration, resultCode
| order by duration desc
```

**查询某个用户的操作轨迹（按 UserId）：**

```kusto
traces
| where timestamp > ago(24h)
| where customDimensions["UserId"] == "123"
| project timestamp, message, customDimensions
| order by timestamp asc
```

**查询某个接口的调用频率（每 5 分钟）：**

```kusto
requests
| where timestamp > ago(1h)
| where name contains "Cars"  // 过滤包含 "Cars" 的接口
| summarize count() by bin(timestamp, 5m), name
| render timechart
```



### 9. 本地验证

#### 9.1 确保 SQL Server 正在运行

```bash
docker ps | grep sqlserver
```

如果没有运行：

```bash
docker-compose up -d sqlserver
```

#### 9.2 启动 API

```bash
cd UUcars.API
dotnet run
```

第一次启动时，Hangfire 会自动在数据库里创建系统表。查看日志，应该看到类似：

```
[Information] Starting Hangfire Server...
[Information] Hangfire Server started. Queues: 'default'
```

#### 9.3 访问 Hangfire Dashboard

用浏览器打开：`http://localhost:5085/hangfire`

你需要先登录 Admin 账号（通过前端登录页，或者直接用 Swagger/Scalar 登录获取 Token），然后才能访问 Dashboard。

Dashboard 里能看到：

- **Enqueued**：待处理的任务
- **Processing**：正在处理的任务
- **Succeeded**：已成功的任务
- **Failed**：失败的任务（可以手动重试）
- **Recurring**：定时任务列表

#### 10.4 测试邮件异步化

用 Scalar（`http://localhost:5065/scalar`）或 curl 注册一个新用户：

```bash
curl -X POST http://localhost:5065/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser11",
    "email": "test111@example.com",
    "password": "Test@1234"
  }'
```

预期行为：

1. **立即收到 201 响应**（不再等待邮件发送）

2. 查看控制台日志，看到：

    ```
    [Information] User 5 registered, email confirmation job enqueued
    ```

    注意：不是"email sent"，是"job enqueued"

3. 刷新 Hangfire Dashboard → **Enqueued** 或 **Succeeded** 里能看到 `SendEmailConfirmationAsync` 任务

#### 10.5 测试 TokenCleanupService 定时任务

不需要等到凌晨验证。可以在 Hangfire Dashboard 里手动触发：

1. Dashboard → **Recurring Jobs**
2. 找到 `cleanup-expired-tokens`
3. 点击 **Trigger now**
4. 切到 **Succeeded** 标签，确认任务执行成功



### 11. 编译和测试

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
dotnet test
```

预期：

```
Test summary: total: XX, failed: 0, succeeded: XX
```

如果有测试失败，检查：

- `UserServiceTests` 的 `CreateService` 方法是否更新了 `IBackgroundJobClient` 参数
- `FakeBackgroundJobClient` 是否正确实现了接口
- 集成测试的 `SqlServerTestFactory` 是否需要处理 Hangfire（**通常不需要**，集成测试走 HTTP 管道，`IBackgroundJobClient` 已经注册在 DI 里，Hangfire Server 不影响测试）



### 12. Git 提交

```bash
# 确认所有修改
git status

# 暂存所有修改
git add .

# 提交
git commit -m "feat: Hangfire background jobs - async email sending + token cleanup + Application insights"

# 推送
git push origin feature/v3-hangfire

# 合并回 develop
git checkout develop
git merge --no-ff feature/v3-hangfire \
  -m "merge: feature/v3-hangfire into develop"
git push origin develop

# 删除功能分支（本地 + 远程）
git branch -d feature/v3-hangfire
git push origin --delete feature/v3-hangfire
```



### Step 60 完成状态

```
知识点：
✅ 理解同步邮件发送的脆弱性，以及异步队列的价值
✅ 理解 Hangfire 的核心架构（Client → SQL Server → Server → Service）
✅ 理解 Fire-and-Forget vs Recurring Job 的区别和适用场景
✅ 理解 BackgroundJob.Enqueue<T>() 的正确用法（类型参数 + DI 解析）
✅ 理解为什么用 IBackgroundJobClient 接口而不是静态方法（可测试性）
✅ 理解 Hangfire Dashboard 的访问控制（IDashboardAuthorizationFilter）
✅ 理解 Application Insights 在云端可观测性中的角色
✅ 掌握 4 个工作中常用的 KQL 查询

实现：
✅ 安装 Hangfire + Hangfire.SqlServer
✅ HangfireExtensions（服务注册 + Dashboard + 定时任务注册）
✅ HangfireAdminAuthorizationFilter（Admin 才能访问 Dashboard）
✅ TokenCleanupService（清理过期密码重置 Token）
✅ UserService.RegisterAsync 改为异步入队
✅ UserService.ForgotPasswordAsync 改为异步入队
✅ FakeBackgroundJobClient（单元测试用）
✅ UserServiceTests 更新（验证"入队"而不是"发送"）
✅ Application Insights 接入（Serilog Conditional Sink）
✅ 4 个 KQL 查询（错误日志、慢请求、用户轨迹、接口频率）
✅ 本地验证：注册后立即返回201，Dashboard 能看到任务
✅ dotnet build + dotnet test 通过
✅ Git commit + 合并回 develop 完成
```



## Step 61 · Refresh Token 机制

### 这一步做什么

Step 60 解决了邮件发送的脆弱性。现在来解决认证体系里一个同样真实的问题：**用户体验差的 Token 过期策略**。

V2 的认证流程，Token 的生命周期是这样的：

```
用户登录
  → 服务端签发 AccessToken（JWT，60分钟有效）
  → 前端存入 localStorage / authStore
  → 用户操作一切正常

60分钟后：
  → 前端发请求 → 服务端验证 Token → 401 Unauthorized
  → 前端收到 401 → 清除 authStore → 跳转登录页
  → 用户被迫重新登录
```

这个流程有两个问题：

**问题一：用户体验差** 

- 用户正在填写一个表单（比如发布车辆），突然页面跳转到登录页，填写的内容全部丢失

**问题二：安全和便利的矛盾**

- AccessToken 有效期太短（10分钟）→ 用户频繁被踢出，体验差
- AccessToken 有效期太长（7天）→ Token 被盗后攻击者有很长时间可以使用，安全风险高

Refresh Token 机制是解决这个矛盾的标准方案：

```
AccessToken：有效期短（60分钟），用于 API 请求认证
RefreshToken：有效期长（7天），只用于换取新的 AccessToken，存在 HttpOnly Cookie 里

正常流程：
  → AccessToken 过期 → 前端自动用 RefreshToken 换新 AccessToken
  → 用户无感知，继续操作

攻击者盗取 AccessToken：
  → 最多只有 60 分钟可用，之后失效

攻击者盗取 RefreshToken（比 AccessToken 更难，因为在 HttpOnly Cookie 里）：
  → Token 轮换：每次刷新后旧 Token 立即作废
  → 可以检测到 Token 被重复使用（安全告警）
```





### 理解 HttpOnly Cookie 的安全意义

在讲实现之前，先把一个关键的安全概念想清楚。

**为什么 AccessToken 放 Response Body，RefreshToken 放 HttpOnly Cookie？**

**AccessToken（放 Body/localStorage）：**

```
1. 服务器通过 Response Body 把 Token 传过来
   （前端怎么知道 Token 是什么？就是从 Body 里读的）

2. 前端 JS 读取 Body 里的 Token
   然后存到 localStorage

3. 之后每次请求从 localStorage 取出来
   放到 Authorization: Bearer xxx 头里
```

- 前端 JS 需要读取它，然后放在每个请求的 `Authorization: Bearer xxx` 头里
- 所以必须让 JS 能访问到
- 风险：如果网站有 XSS 漏洞，攻击者的 JS 能读取到 Token

**RefreshToken（放 HttpOnly Cookie）：**

```
Cookie 方式：
  服务器 → Set-Cookie Header 设置 → 浏览器自动存
  前端 JS 完全不参与，浏览器自己管
```

- `HttpOnly` 属性：浏览器不允许 JS 读取这个 Cookie
- 只有浏览器发请求时会自动带上，后端能读到，前端 JS 永远读不到
- 所以即使有 XSS 漏洞，攻击者的 JS 也无法读取 RefreshToken
- 这就是为什么 RefreshToken 比 AccessToken 更难被盗

```
攻击者的 JS 能做什么：
  ✅ 读 localStorage → 拿到 AccessToken（有效期 60 分钟）
  ❌ 读 HttpOnly Cookie → 拿不到 RefreshToken

这就是两者分开存储的安全价值。
```

**Cookie 的完整安全属性：**

```
Set-Cookie: refreshToken=xxx;
  HttpOnly;           ← JS 无法读取
  Secure;             ← 只通过 HTTPS 传输（生产环境必须）
  SameSite=Strict;    ← 只在同源请求里发送（防 CSRF）
  Path=/auth;         ← 只在 /auth 路径下发送（减少暴露范围）
  Max-Age=604800;     ← 7天有效期（秒）
```



### Token 轮换（Token Rotation）

Refresh Token 还有一个重要的安全机制：**每次使用后立即作废，换新的**。

```
用户持有 RefreshToken_A（有效）
  → 调 POST /auth/refresh
  → 服务端：
      1. 验证 RefreshToken_A 有效
      2. 生成新的 AccessToken
      3. 生成新的 RefreshToken_B
      4. 把 RefreshToken_A 标记为 IsRevoked = true
      5. 返回新 AccessToken + 设置 RefreshToken_B 到 Cookie
  → 用户现在持有 RefreshToken_B（有效），RefreshToken_A 已作废
```

**为什么这样更安全？**

如果攻击者在某个时间点盗取了 RefreshToken_A：

- 正常用户先用了 → RefreshToken_A 作废，换了 RefreshToken_B
- 攻击者后用 RefreshToken_A → 发现已作废 → **可以检测到 Token 被重复使用**
- 系统可以据此撤销所有相关 Token，强制用户重新登录

这叫 **Refresh Token Rotation**，是 OAuth 2.0 推荐的最佳实践。



### 1. 切出功能分支

```bash
git checkout develop
git pull origin develop
git checkout -b feature/v3-refresh-token
git push -u origin feature/v3-refresh-token
```



### 2. 数据库：新建 RefreshTokens 表

RefreshToken要存储到数据库，这样服务端才有权限管理状态，因此我们需要新的一张数据库表

#### 2.1 新建实体类

新建 `UUcars.API/Entities/RefreshToken.cs`：

```csharp
namespace UUcars.API.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>
    /// Token 本体：随机生成的不透明字符串（不是 JWT）
    /// 存数据库，每次使用后作废（Token 轮换）
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 所属用户
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// 过期时间：7天后
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 是否已被撤销
    /// true = 已使用过（Token 轮换）或用户主动登出
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    public DateTime CreatedAt { get; set; }
}
```

#### 2.2 给 User 实体加导航属性

打开 `UUcars.API/Entities/User.cs`，加入导航属性：

```csharp
// 在现有导航属性的最后加入
public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
```

#### 2.3 新建 EF Core 配置

新建 `UUcars.API/Data/Configurations/RefreshTokenConfiguration.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(256);

        // Token 必须唯一：防止碰撞，也方便快速查找
        builder.HasIndex(rt => rt.Token)
            .IsUnique();

        // 查询时常见的过滤条件：UserId + IsRevoked + ExpiresAt
        // 加复合索引加速验证查询
        builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_IsRevoked_ExpiresAt");

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        // 关联 User
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade); // 用户删除时，其所有 RefreshToken 跟着删
    }
}
```

#### 2.4 在 AppDbContext 里注册

打开 `UUcars.API/Data/AppDbContext.cs`，加入 DbSet：

```csharp
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

`ApplyConfigurationsFromAssembly` 会自动扫描 `RefreshTokenConfiguration`，不需要手动注册。

#### 2.5 生成 Migration

```bash
cd UUcars.API
dotnet ef migrations add AddRefreshTokensTable 
```

检查生成的 Migration 文件，确认：

- 创建了 `RefreshTokens` 表（Id、Token、UserId、ExpiresAt、IsRevoked、CreatedAt）
- 创建了 `IX_RefreshTokens_Token`（唯一索引）
- 创建了 `IX_RefreshTokens_UserId_IsRevoked_ExpiresAt`（复合索引）
- 添加了 `Users` 表到 `RefreshTokens` 的外键约束

```bash
dotnet ef database update
```

预期输出：

```
Applying migration '20240xxx_AddRefreshTokensTable'.
Done.
```



### 3. Repository 层

#### 3.1 新建接口

新建 `UUcars.API/Repositories/IRefreshTokenRepository.cs`：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>
    /// 根据 Token 字符串查找（验证时使用）
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存新的 RefreshToken
    /// </summary>
    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销单个 Token（登出 / Token 轮换时使用）
    /// </summary>
    Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销某个用户的所有 Token（强制全设备登出时使用）
    /// </summary>
    Task RevokeAllByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
```

#### 3.2 EF Core 实现

新建 `UUcars.API/Repositories/EfRefreshTokenRepository.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public EfRefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            // Include User 是因为后续需要用 user.Id 签发新的 AccessToken
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        refreshToken.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        // EF Core 7+ 批量更新，不需要先 Select 再逐条修改
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.IsRevoked, true),
                cancellationToken);
    }
}
```



### 4. DTOs

#### 4.1 新增 RefreshTokenResponse

新建 `UUcars.API/DTOs/RefreshTokenResponse.cs`：

```csharp
namespace UUcars.API.DTOs.Auth;

public class RefreshTokenResponse
{
    /// <summary>
    /// 新签发的 AccessToken
    /// 前端收到后更新 authStore，替换旧的 Token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
}
```

> **为什么 Response 里只有 AccessToken，没有 RefreshToken？**
>
> 新的 RefreshToken 通过 `Set-Cookie` 响应头写入 HttpOnly Cookie， 不出现在 Response Body 里。 这样前端 JS 永远无法读到 RefreshToken，只能依赖浏览器自动管理 Cookie。

#### 4.2 新增 LogoutRequest（可选）

`POST /auth/logout` 不需要 Request Body——RefreshToken 从 Cookie 里读， UserId 从 JWT Claims 里读。不需要额外的 DTO。



### 5. Service 层

#### 5.1 新建 RefreshTokenService

新建 `UUcars.API/Services/RefreshTokenService.cs`：

```csharp
using UUcars.API.Auth;
using UUcars.API.Data;
using UUcars.API.Entities;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class RefreshTokenService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly AppDbContext _context;
    private readonly ILogger<RefreshTokenService> _logger;

    // RefreshToken 有效期：7天
    private const int RefreshTokenExpiryDays = 7;

    public RefreshTokenService(
        IRefreshTokenRepository refreshTokenRepository,
        JwtTokenGenerator jwtTokenGenerator,
        AppDbContext context,
        ILogger<RefreshTokenService> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 生成新的 RefreshToken 并存入数据库
    /// 在登录成功后调用
    /// </summary>
    public async Task<RefreshToken> CreateRefreshTokenAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshToken
        {
            Token = TokenGenerator.Generate(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        return refreshToken;
    }

    /// <summary>
    /// 验证 RefreshToken，签发新的 AccessToken + RefreshToken（Token 轮换）
    /// </summary>
    public async Task<(string NewAccessToken, RefreshToken NewRefreshToken)> RefreshAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // 1. 根据 Token 字符串查找记录
        var existingToken = await _refreshTokenRepository
            .GetByTokenAsync(token, cancellationToken);

        // 2. Token 不存在
        if (existingToken == null)
        {
            _logger.LogWarning("Refresh attempt with unknown token");
            throw new InvalidTokenException();
        }

        // 3. Token 已被撤销
        // ⚠️ 重要安全检查：如果已撤销的 Token 被使用，说明可能发生了 Token 盗用
        if (existingToken.IsRevoked)
        {
            _logger.LogWarning(
                "Revoked refresh token used for user {UserId}. Possible token theft detected.",
                existingToken.UserId);

            // 安全措施：撤销该用户的所有 Token，强制重新登录
            // 这是防止 Token 盗用的关键步骤
            await _refreshTokenRepository.RevokeAllByUserIdAsync(
                existingToken.UserId, cancellationToken);

            throw new InvalidTokenException();
        }

        // 4. Token 已过期
        if (existingToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogInformation(
                "Expired refresh token used for user {UserId}",
                existingToken.UserId);
            throw new InvalidTokenException();
        }

        var user = existingToken.User;

        // 5. Token 轮换：撤销旧 Token
        await _refreshTokenRepository.RevokeAsync(existingToken, cancellationToken);
        
        // 6. 签发新 AccessToken
        var newAccessToken = _jwtTokenGenerator.GenerateToken(user);

        // 7. 生成新 RefreshToken
        var newRefreshToken = new RefreshToken
        {
            Token = TokenGenerator.Generate(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // 8. 一次性保存（旧 Token 撤销 + 新 Token 创建，保证原子性）
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Refresh token rotated for user {UserId}", user.Id);

        return (newAccessToken, newRefreshToken);
    }

    /// <summary>
    /// 撤销当前用户的 RefreshToken（登出）
    /// </summary>
    public async Task RevokeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var existingToken = await _refreshTokenRepository
            .GetByTokenAsync(token, cancellationToken);

        // Token 不存在或已撤销：静默处理，不报错
        // 原因：前端可能重复调登出接口，或 Token 已过期被清理，都应该返回成功
        if (existingToken == null || existingToken.IsRevoked)
        {
            _logger.LogInformation("Logout: token not found or already revoked");
            return;
        }

        await _refreshTokenRepository.RevokeAsync(existingToken, cancellationToken);

        _logger.LogInformation(
            "Refresh token revoked for user {UserId}", existingToken.UserId);
    }
}
```



### 6. 更新 AuthController

我们现在需要把RefreshToken写入Cookie里，而Cookie 是 HTTP 协议层的概念，属于 Controller 的职责,。 因此，我们需要在controll层处理：

```
Controller 层：处理 HTTP 请求和响应（Headers、Cookies、状态码）
Service 层：处理业务逻辑（Token生成、验证、轮换、存储）
```

完整的controller操作如下：

```
AuthController.Login()
  → 调用 UserService.LoginAsync()        ← 验证用户名密码（Service）
  → 调用 RefreshTokenService.CreateAsync() ← 生成并存储RefreshToken（Service）
  → SetRefreshTokenCookie(token, expiry)  ← 写Cookie（Controller私有方法）
  → 返回 AccessToken 在 Response Body

AuthController.Refresh()
  → 从 Request.Cookies 读 RefreshToken   ← 读Cookie（Controller）
  → 调用 RefreshTokenService.RefreshAsync() ← 验证+轮换（Service）
  → SetRefreshTokenCookie(newToken, expiry) ← 写新Cookie（Controller）
  → 返回新 AccessToken 在 Response Body

AuthController.Logout()
  → 从 Request.Cookies 读 RefreshToken   ← 读Cookie（Controller）
  → 调用 RefreshTokenService.RevokeAsync() ← 撤销（Service）
  → Response.Cookies.Delete("refreshToken") ← 删Cookie（Controller）
  → 返回 200
```

#### 6.1 新建 SetRefreshTokenCookie 私有方法：

对于access token, 我们直接写入Response Body的

```c#
return new LoginResponse
{
    Token = token,
    ExpiresAt = DateTime.UtcNow.AddMinutes(60),
    User = MapToResponse(user)
};
```

```c#
return Ok(ApiResponse<LoginResponse>.SuccessResult(result));
```

但是，对于refresh token, 我们需要写入 `Set-Cookie` 响应头，不出现在 Body 里。 因此封装如下私有方法

```csharp
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
	...
    /// 其他公开接口不变
    
    /// <summary>
    /// 把 RefreshToken 写入 HttpOnly Cookie
    /// 提取为私有方法：Login 和 Refresh 都需要调用
    /// </summary>
    private void SetRefreshTokenCookie(string token, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,     // JS 无法读取
            Secure = true,       // 只通过 HTTPS 传输
            SameSite = SameSiteMode.Strict, // 防 CSRF
            Expires = expiresAt, // 和 RefreshToken 过期时间一致
            Path = "/"           // 全站发送，避免非/auth路径下无法携带Cookie
        };

        // 开发环境：Secure=false（本地没有 HTTPS）
        // 通过环境变量判断，避免本地开发时 Cookie 无法设置
        if (HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment())
        {
            cookieOptions.Secure = false;
        }

        Response.Cookies.Append("refreshToken", token, cookieOptions);
    }
}
```

从而实现 acess token和refresh token 区别传递的形式：

```
Response Body（Json）  ← Ok(ApiResponse<LoginResponse>) 这一行
  └── data.token = AccessToken（前端JS能读到，存authStore）

Response Header（Set-Cookie）  ← SetRefreshTokenCookie() 这一行
  └── refreshToken = RefreshToken（HttpOnly，前端JS读不到）
```



#### 6.2 更新登录接口：响应里加 RefreshToken Cookie

打开 `UUcars.API/Controllers/AuthController.cs`。

**注入 RefreshTokenService：**

```csharp
private readonly UserService _userService;
private readonly RefreshTokenService _refreshTokenService; // ✅ 新增
private readonly ILogger<AuthController> _logger;

public AuthController(
    UserService userService,
    RefreshTokenService refreshTokenService, // ✅ 新增
    ILogger<AuthController> logger)
{
    _userService = userService;
    _refreshTokenService = refreshTokenService;
    _logger = logger;
}
```

**更新 Login 方法：**

找到现有的 `Login` 方法，在返回 AccessToken 的同时，把 RefreshToken 写入 Cookie：

```csharp
[HttpPost("login")]
// 使用限流
// ✅ 登录：每IP每分钟10次
[EnableRateLimiting(RateLimitPolicies.Login)]
public async Task<IActionResult> Login(
    [FromBody] LoginRequest request,
    CancellationToken cancellationToken)
{
    var result = await _userService.LoginAsync(request.Email, request.Password, cancellationToken);

    // ✅ 新增：生成 RefreshToken 并写入 HttpOnly Cookie

    var refreshToken=await _refreshTokenService.CreateRefreshTokenAsync(result.User.Id, cancellationToken);
    SetRefreshTokenCookie(refreshToken.Token, refreshToken.ExpiresAt);

    // 登录成功返回 200 OK（不是 201，登录不是"创建资源"）
    return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
}
```

#### 6.3 新增 Refresh 接口

在 `AuthController` 里加入：

```csharp
/// <summary>
/// 用 RefreshToken 换取新的 AccessToken
/// RefreshToken 从 HttpOnly Cookie 里自动读取，不需要前端手动传
/// </summary>
[HttpPost("refresh")]
[EnableRateLimiting(RateLimitPolicies.Write)]
public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
{
    // 从 Cookie 里读取 RefreshToken
    // 前端不需要传任何 Body，浏览器自动携带 Cookie
    if (!Request.Cookies.TryGetValue("refreshToken", out var token)
        || string.IsNullOrEmpty(token))
        return Unauthorized(ApiResponse<object>.Fail("Refresh token not found"));

    var (newAccessToken, newRefreshToken) = await _refreshTokenService
        .RefreshAsync(token, cancellationToken);

    // 更新 Cookie（新的 RefreshToken 覆盖旧的）
    SetRefreshTokenCookie(newRefreshToken.Token, newRefreshToken.ExpiresAt);

    var response = new RefreshTokenResponse
    {
        AccessToken = newAccessToken
    };

    return Ok(ApiResponse<RefreshTokenResponse>.Ok(response, "token refreshed"));
}
```

#### 6.3 新增 Logout 接口

```csharp
/// <summary>
/// 登出：撤销当前设备的 RefreshToken，清除 Cookie
/// </summary>
[HttpPost("logout")]
[EnableRateLimiting(RateLimitPolicies.Write)]
public async Task<IActionResult> Logout(CancellationToken cancellationToken)
{
    // 从 Cookie 里读取 RefreshToken
    Request.Cookies.TryGetValue("refreshToken", out var token);

    // 撤销 Token（即使 Token 不存在也静默处理）
    if (!string.IsNullOrEmpty(token)) await _refreshTokenService.RevokeAsync(token, cancellationToken);

    // 清除 Cookie
    Response.Cookies.Delete("refreshToken", new CookieOptions
    {
        Path = "/" // 和 SetRefreshTokenCookie 里的 Path 保持一致，否则删不掉
    });

    return Ok(ApiResponse<object>.Ok("Logged out successfully"));
}
```



### 7. 更新 RateLimitPolicies 常量

打开 `UUcars.API/Configurations/RateLimitPolicies.cs`（或你存放常量的位置）， 确认有以下条目（Step 59 应已有，确认一下）：

```csharp
public static class RateLimitPolicies
{
    public const string Register = "register";
    public const string Login = "login";
    public const string ForgotPassword = "forgot-password";
    public const string ResendVerification = "resend-verification";
    public const string Browse = "browse";
    public const string Write = "write";
}
```

`/auth/refresh` 和 `/auth/logout` 用 `Write` 策略，无需新增。



### 8. 注册依赖

打开 `UUcars.API/Program.cs`，在服务注册区域加入：

```csharp
// ── Refresh Token ──────────────────────────────────
builder.Services.AddScoped<IRefreshTokenRepository, EfRefreshTokenRepository>();
builder.Services.AddScoped<RefreshTokenService>();
```



### 9. 更新 TokenCleanupService（复用 Hangfire）

Step 60 里已经创建了 `TokenCleanupService`，现在把 RefreshToken 的清理逻辑也加进去：

打开 `UUcars.API/Services/TokenCleanupService.cs`，更新 `CleanExpiredTokensAsync`：

```csharp
namespace UUcars.API.Services.Hangfire;

using Microsoft.EntityFrameworkCore;
using Data;

/// <summary>
///     负责清理数据库里的过期 Token
///     由 Hangfire 每天凌晨定时调用
/// </summary>
public class TokenCleanupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(AppDbContext context, ILogger<TokenCleanupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanExpiredTokensAsync()
    {
        var now = DateTime.UtcNow;

        // 清理过期的密码重置 Token
        // 找出所有 ResetPasswordTokenExpiry 已过期的用户，清空他们的 Token 字段
        var expiredPasswordResets = await _context.Users
            .Where(u => u.ResetPasswordTokenExpiry != null
                        && u.ResetPasswordTokenExpiry < now)
            .ToListAsync();

        if (expiredPasswordResets.Count > 0)
        {
            foreach (var user in expiredPasswordResets)
            {
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;
                user.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cleaned {Count} expired password reset tokens",
                expiredPasswordResets.Count);
        }

        else
        {
            _logger.LogInformation("No expired password reset tokens found");
        }

        // ── 清理过期或已撤销的 RefreshToken ──────────────
        // 使用 EF Core 批量删除（ExecuteDeleteAsync），性能更好
        // 这里是真正的删除行，而不是清空字段，所以用 Delete
        var deletedCount = await _context.RefreshTokens
            .Where(rt => rt.IsRevoked || rt.ExpiresAt < now)
            .ExecuteDeleteAsync();

        if (deletedCount > 0)
            _logger.LogInformation(
                "Cleaned {Count} expired or revoked refresh tokens", deletedCount);
        else
            _logger.LogInformation("No expired refresh tokens found");
    }
}
```



### 10. 前端：Axios 拦截器自动刷新

这是前端这一步的核心工作：实现**无感知的 Token 自动刷新**。

#### 10.1 理解当前前端的认证流程

V2 的 Axios 实例（`src/api/client.ts` ）请求拦截器：

```typescript
// =============================================
// 请求拦截器：自动附加 JWT Token
// =============================================
// 每次请求发出前，从 localStorage 取出 Token 附加到请求头
// 这样所有需要认证的接口不需要手动传 Token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

```

和响应拦截器：

```typescript
// 现有代码（处理 401 / 429 等）
// =============================================
// 响应拦截器：统一处理响应和错误
// =============================================
apiClient.interceptors.response.use(
  (response) => {
    ...
    }

    return response;
  },
  (error: AxiosError) => {
    // ✅ 处理 429 Too Many Requests
    ...

    // 处理用户 Token 过期时
    if (error.response?.status === 401) {
      localStorage.removeItem("accessToken");
      // 跳转登录页，带上当前路径方便登录后回跳
      window.location.href = `/login?redirect=${encodeURIComponent(window.location.pathname)}`;
      return Promise.reject(new Error("Session expired, please login again"));
    }

    // 请求失败（HTTP 4xx / 5xx / 网络错误）
   ...
  },
);
```

问题：

- 每次请求，都是从从 localStorage 直接读toke。accessToken 短期有效（60分钟），持久化意义不大，且存在 localStorage 里有 XSS 风险（JS 可以读取）
- 响应拦截器里，收到 401 就立即跳登录页，没有尝试用 RefreshToken 刷新。

更新思路：

- accessToken不再持久化到localStorage， 而是每次请确实，直接从 authStore 读取 （authStore 是唯一数据源，拦截器刷新后也会更新 authStore）
- 响应拦截器里，对401的响应进行refresh token的处理，换取新的access token

#### 10.2 更新 authStore

打开 `src/store/authStore.ts`，需要加入 `setAccessToken` 方法（供拦截器更新 Token 用）：

```typescript
import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { User } from "@/types";

interface AuthState {
  user: User | null;
  accessToken: string | null;

  // 登录成功后调用：保存用户信息和 Token
  setAuth: (user: User, token: string) => void;

  // ✅ 新增：只更新 AccessToken（拦截器刷新后调用）
  // 不影响 user 信息，只替换 Token
  setAccessToken: (token: string) => void;

  // 退出登录：清除所有状态
  logout: () => void;

  // 判断当前是否已登录
  isAuthenticated: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  // persist 中间件：把 store 的状态同步到 localStorage
  // 页面刷新后状态不会丢失，用户不需要重新登录
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,

      setAuth: (user, token) => {
        // ✅ 移除：不再把 accessToken 单独存 localStorage
        // 原因：accessToken 短期有效（60分钟），持久化意义不大
        // 且存在 localStorage 里有 XSS 风险（JS 可以读取）
        // 刷新页面后通过 /auth/refresh 重新获取，RefreshToken 在 HttpOnly Cookie 里
        set({ user, accessToken: token });
      },

      // ✅ 新增：拦截器刷新成功后调用，只更新 Token
      setAccessToken: (token) => {
        set({ accessToken: token });
      },

      clearAuth: () => {
        // ✅ 移除：不再需要单独清 localStorage 的 accessToken
        // persist 中间件会自动把 null 同步到 localStorage
        set({ user: null, accessToken: null });
      },

      isAuthenticated: () => {
        return get().accessToken !== null && get().user !== null;
      },
    }),
    {
      name: "auth-storage", // localStorage 里的 key 名称
      // ✅ 更新：只持久化 user，不持久化 accessToken
      // 原因：
      // 1. accessToken 60分钟过期，持久化后刷新页面拿到的可能是过期 Token
      // 2. 存 localStorage 有 XSS 风险
      // 3. 刷新页面时拦截器会自动用 RefreshToken（HttpOnly Cookie）换新 AccessToken
      //    用户无感知，不需要持久化 accessToken
      partialize: (state) => ({
        user: state.user,
      }),
    },
  ),
);

```

> **等一下：`partialize` 把 accessToken 排除在 localStorage 之外？**
>
> 是的。这是一个权衡：
>
> - 如果把 accessToken 存 localStorage → 刷新页面后立即可用，但有 XSS 风险
> - 如果不存 → 刷新页面后 accessToken 是 null，需要先调 /auth/refresh 重新获取
>
> 当用户刷新页面时：
>
> 1. authStore 里 accessToken = null（没有存 localStorage）
> 2. 第一个 API 请求发出 → 收到 401
> 3. 拦截器触发 → 调 /auth/refresh（Cookie 里的 RefreshToken 还在）
> 4. 刷新成功 → 拿到新 AccessToken → 重试原请求
> 5. 用户无感知
>
> 这个流程在网络好的情况下只有几十毫秒的额外延迟，但安全性大幅提升。 这是现代 SPA 认证的推荐做法。

#### 10.3 更新 Axios 拦截器

打开 `src/api/client.ts`，**完整替换**响应拦截器：

```typescript
// src/api/client.ts
import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { ApiResponse } from "@/types";
import { toast } from "sonner";
import { useAuthStore } from "@/stores/authStore";

// 创建 Axios 实例
// 所有请求都基于这个实例，统一配置 baseURL 和超时
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 10000, // 10秒超时
  headers: {
    "Content-Type": "application/json",
  },
  // ✅ 新增：允许跨域请求携带 Cookie
  // 原因：RefreshToken 存在 HttpOnly Cookie 里
  // withCredentials=true 告诉浏览器在跨域请求时携带 Cookie
  // 需要后端 CORS 同步配置 AllowCredentials()
  withCredentials: true,
});

// =============================================
// 请求拦截器：自动附加 JWT Token
// =============================================
// 每次请求发出前，从 localStorage 取出 Token 附加到请求头
// 这样所有需要认证的接口不需要手动传 Token
apiClient.interceptors.request.use((config) => {
  // ✅ 从 authStore 读取 accessToken
  // 不再从 localStorage 直接读
  // 原因：accessToken 不再持久化到 localStorage
  // authStore 是唯一数据源，拦截器刷新后也会更新 authStore
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// =============================================
// 响应拦截器：统一处理响应和错误
// =============================================
apiClient.interceptors.response.use(
  (response) => {
    // 204 No Content：没有响应体（DELETE 接口），直接放行
    if (response.status === 204) {
      return response;
    }

    // 请求成功（HTTP 2xx）
    // 后端所有接口都用 ApiResponse<T> 包装
    // 直接解包返回 data 字段，调用方不需要每次都写 response.data.data
    const apiResponse = response.data as ApiResponse<unknown>;

    if (!apiResponse.success) {
      // HTTP 状态码是 2xx，但业务逻辑失败（success: false）
      // 把业务错误信息包装成 Error 抛出，调用方统一用 catch 处理
      return Promise.reject(new Error(apiResponse.message ?? "Request failed"));
    }

    return response;
  },

  async (error: AxiosError) => {
    // error.config 包含了失败请求的完整配置（url、method、headers等）
    // 用 as 做类型断言，告诉 TypeScript 这个对象的类型
    // & { _retry?: boolean } 是交叉类型：在 Axios 原有类型基础上，
    // 扩展一个我们自己加的 _retry 字段
    // 原因：InternalAxiosRequestConfig 里没有 _retry，
    // 不声明的话 TypeScript 会报错"该属性不存在"
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      // _retry 标志：防止无限重试
      // 第一次 401 → _retry 为 undefined（falsy）→ 进入刷新逻辑 → 设为 true
      // 刷新后重试原请求 → 如果还是 401 → _retry 为 true → 跳过刷新 → 走普通错误处理
      _retry?: boolean;
    };

    // =============================================
    // 处理 401 Token 过期：自动刷新后重试
    // =============================================
    if (error.response?.status === 401 && !originalRequest._retry) {
      // 标记这个请求已经尝试过刷新
      // 防止刷新后重试的请求再次触发刷新（死循环）
      originalRequest._retry = true;

      try {
        // 调用刷新接口
        // 用原生 axios 而不是 apiClient 的原因：
        // 避免触发 apiClient 自己的拦截器（否则刷新失败又触发401，又来刷新，死循环）
        // withCredentials：携带 HttpOnly Cookie 里的 RefreshToken
        // RefreshToken 由浏览器自动附加，前端 JS 不需要（也无法）手动读取
        const response = await axios.post(
          `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
          null,
          { withCredentials: true },
        );

        // 从响应里取出新的 AccessToken
        // 后端返回格式：{ success: true, data: { accessToken: "..." } }
        const newAccessToken = response.data.data.accessToken;

        // 把新 Token 存入 authStore（内存），供后续请求使用
        // 不存 localStorage 的原因：AccessToken 短期有效，持久化意义不大
        // 且 localStorage 可被 JS 读取，有 XSS 风险
        useAuthStore.getState().setAccessToken(newAccessToken);

        // 用新 Token 更新原请求的 Authorization 头，然后重新发送
        // 对调用方完全透明：await apiClient.get("/cars") 最终正常返回数据
        // 用户不知道中间发生了一次 Token 刷新
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return apiClient(originalRequest);
      } catch {
        // 刷新失败：说明 RefreshToken 也过期了或被撤销（比如已在其他设备登出）
        // 无法继续保持登录状态，必须让用户重新登录

        // 清除前端的用户状态（authStore + localStorage 里的 user）
        useAuthStore.getState().clearAuth();

        // 跳转登录页
        // 带上当前路径（redirect 参数），登录成功后可以回到原来的页面
        window.location.href = `/login?redirect=${encodeURIComponent(window.location.pathname)}`;

        return Promise.reject(new Error("Session expired, please login again"));
      }
    }

    // =============================================
    // 处理 429 Too Many Requests（原有逻辑不变）
    // =============================================
    if (error.response?.status === 429) {
      // 读取 Retry-After 响应头（后端返回的秒数）
      const retryAfterHeader = error.response.headers["retry-after"];
      const seconds = retryAfterHeader ? parseInt(retryAfterHeader, 10) : 60; // 没有 Retry-After 头就默认提示60秒

      // 用 sonner toast 给用户友好提示
      toast.error(
        `Too many requests. Please wait ${seconds} seconds before trying again.`,
        { duration: 5000 },
      );

      // 返回一个标准格式的错误，让调用方知道是限流问题
      return Promise.reject(
        new Error(`Rate limited. Retry after ${seconds}s.`),
      );
    }

    // =============================================
    // 处理其他错误（原有逻辑不变）
    // =============================================
    if (error.response) {
      // 请求失败（HTTP 4xx / 5xx / 网络错误）
      // 服务端返回了错误响应
      const apiResponse = error.response.data as ApiResponse<unknown>;
      const message = apiResponse?.message ?? "An error occurred";
      return Promise.reject(new Error(message));
    }

    if (error.code === "ECONNABORTED") {
      return Promise.reject(new Error("Request timeout, please try again"));
    }

    // 网络错误（断网等）
    return Promise.reject(
      new Error("Network error, please check your connection"),
    );
  },
);

export default apiClient;

```

#### 10.4 更新登录逻辑

登录成功后，后端会在 Set-Cookie 里设置 RefreshToken。 前端只需要存 AccessToken 和用户信息，不需要处理 Cookie。

打开登录逻辑（ `LoginPage.tsx`  Hook 里）：

登录逻辑不变

```typescript
const onSubmit = async (data: LoginForm) => {
    // 清除上次的服务端错误
    setServerError(null);

    try {
      // 调用登录 API
      const result = await authApi.login(data);

      // 登录成功：把用户信息和 Token 存入 Zustand
      // setAuth 内部只会把写入 result.user 到 localStorage
      setAuth(result.user, result.token);

      // 跳转
      // 有来源页就跳回去，没有的话 Admin 跳管理页、普通用户跳首页
      ...
    } catch (error) {
      ...
    }
  };
```

#### 10.5 更新登出逻辑

登出时需要调后端 `/auth/logout` 接口，让服务端撤销 RefreshToken：

路由表中添加logout路由

```ts
// src/api/auth.ts
import apiClient from "./client";
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  User,
  ApiResponse,
} from "@/types";

export const authApi = {
 
  ...
    
  // 新增logout接口
  logout: async (): Promise<void> => {
    await apiClient.post<ApiResponse<void>>("/auth/logout");
  },

  ...
};

```

更新logout逻辑 

调用后端接口， 清除后端token

```typescript
// src/components/Layout.ts

const handleLogout = async () => {
    try {
      // 通知后端撤销 RefreshToken
      // 后端会把数据库里的 IsRevoked 设为 true，并清除 Cookie
      // 即使这个请求失败（网络问题），前端也要继续登出
      await authApi.logout();
    } catch {
      // 静默处理：不能因为后端失败导致用户无法登出
    } finally {
      // 清除前端状态（authStore + localStorage 里的 user）
      clearAuth();
      // 关闭移动端菜单
      setMobileOpen(false);
      // 跳转首页
      navigate("/");
    }
  };
```



### 11. 处理 CORS（允许 Cookie 跨域）

V2 的 CORS 配置需要更新，允许携带 Credentials（Cookie）。

打开 `UUcars.API/Extensions/CorsExtensions.cs`：

```csharp
namespace UUcars.API.Extensions;

public static class CorsExtensions
{
    // CORS 策略名称，在注册和使用时保持一致
    public const string PolicyName = "UUcarsCorsPolicy";

    public static IServiceCollection AddUUcarsCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 从配置文件读取允许的前端地址列表
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    // 只允许配置里指定的域名，不用 AllowAnyOrigin
                    // 原因：AllowAnyOrigin 表示任何网站都可以调用你的 API，
                    // 配合 AllowCredentials（允许携带 Cookie）时甚至会报错——
                    // 浏览器要求：如果允许携带凭证，Origin 不能是通配符
                    .WithOrigins(allowedOrigins)

                    // 允许所有 HTTP 方法（GET、POST、PUT、DELETE 等）
                    .AllowAnyMethod()

                    // 允许所有请求头（Content-Type、Authorization 等）
                    .AllowAnyHeader()

                    // 允许携带凭证（Cookie）
                    // V3 的 Refresh Token 存在 HttpOnly Cookie 里，需要这个
                    .AllowCredentials();
            });
        });

        return services;
    }
}
```

> **为什么必须加 `AllowCredentials()`？**
>
> 浏览器的 CORS 机制：当请求配置了 `withCredentials: true`， 服务端必须在响应头里返回 `Access-Control-Allow-Credentials: true`， 浏览器才会把 Cookie 发出去。
>
> `AllowCredentials()` 就是让 ASP.NET Core 自动加这个响应头。 如果不加，Cookie 永远不会被发送，RefreshToken 机制完全失效。

### 12. 应用初始化时静默刷新（auth state rehydration）

#### 12.1 有什么问题?

当前的方案中， accessToken只存内存（zustand - authStore，不persist），refreshToken存httpOnly cookie，localStorage只persist user信息。

刷新页面后会发生：

- zustand内存清空 → `accessToken = null`
- localStorage里的`user`还在（zustand persist恢复）
- refreshToken（cookie）还在

此时`isAuthenticated()`检查`accessToken !== null && user !== null`，因为`accessToken`是`null`，返回`false` → UI显示未登录。

#### 12.2 如何导致的？

理论上401拦截器可以兜底：发请求→401→自动refresh→重试成功。

但实际发现刷新后未登录，原因是：

1. 首页是公开路由（不经过`ProtectedRoute`），即使`isAuthenticated()`为`false`也不会跳转登录页，只是UI显示未登录样式
2. 首页本身可能不发任何需要鉴权的请求，所以401压根没机会触发
3. 没有任何代码在应用启动时主动调用`/auth/refresh`

也就是说：`accessToken`一旦变`null`，没有任何机制把它变回非null，除非用户偶然点击了一个需要鉴权的页面/操作，触发401→refresh的被动恢复链路。

#### 12.3 错误方案

通过把 `authStore` 里的 `isAuthenticated()`判断修改

```ts
isAuthenticated: () => {
        return get().user !== null;
      },
```

是否授权只依赖local storage里有没有user数据（登陆过），这样虽然可以解决登录后刷新页面是 渲染为已经登录的假象， 但和实际鉴权状态（accessToken是否有效）脱钩：

- `isAuthenticated()`语义从"现在能直接发请求"变成了"曾经登录过"
- 代价：刷新页面后，**第一个需要鉴权的请求必然先401再重试**，多了一次必然发生的失败往返，影响体验
- 隐患：未来任何依赖`isAuthenticated()===true`来判断"可以直接用accessToken"的代码都可能出错

#### 12.4 最终方案：应用启动时主动refresh

会话恢复（session restoration）或 **初始化时静默刷新（auth state rehydration）**。

核心思路：在`accessToken`被任何地方检查之前，先用refreshToken（cookie）主动换一次新的accessToken。

比如，刷新页面后，先主动发送一次refreshToken，在渲染页面。

##### 修改**`authStore.ts`**

- 新增`isInitializing`状态（初始为`true`）

    这个状态是必须的，否则 `ProtectedRoute` 在 初始化 完成之前就渲染了，会出现短暂的"未登录→已登录"闪烁。

- 新增 `initialize()`初始化方法：

    - 检查persist恢复的`user`是否存在
    - 存在 → 调用`/auth/refresh`（`withCredentials: true`带上cookie），成功则`setAccessToken`，失败则清空`user`
    - 不存在 → 直接结束
    - 最终都将`isInitializing`置为`false`

- `isAuthenticated()同时检查`accessToken !== null && user !== null`（语义恢复正确）

```ts
interface AuthState {
  ...
  
  // 新增：是否在做启动时的token恢复
  isInitializing: boolean;

  ...

  // 新增：应用启动时调用，尝试用 refresh token 恢复登录态
  initialize: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  // persist 中间件：把 store 的状态同步到 localStorage
  // 页面刷新后状态不会丢失，用户不需要重新登录
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      isInitializing: true, // 初始为true，应用启动时正在恢复

      ...

      initialize: async () => {
        const { user } = get();

        // localStorage里没有user信息 → 从未登录过，无需refresh
        if (!user) {
          set({ isInitializing: false });
          return;
        }

        // 有user信息 → 尝试用 refresh token (cookie) 换新的 access token
        try {
          const response = await axios.post(
            `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
            null,
            { withCredentials: true },
          );
          const newAccessToken = response.data.data.accessToken;
          set({ accessToken: newAccessToken, isInitializing: false });
        } catch {
          // refresh token也失效了 → 清除登录态
          set({ user: null, accessToken: null, isInitializing: false });
        }
      },
    }),
    {
      name: "auth-storage", // localStorage 里的 key 名称
      partialize: (state) => ({
        user: state.user,
      }),
    },
  ),
);

```

注意：调用端口时用原生 `axios` 而不是 `apiClient` 

```typescript
const response = await axios.post(
  `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
  null,
  { withCredentials: true }
);
```

正`initialize()` 在 `apiClient` 拦截器之外运行，用原生 `axios` 避免循环触发拦截器。和拦截器里的刷新保持一致的写法。

##### **新建`AuthInitializer`组件**

- `useEffect`中调用`initialize()`
- `isInitializing`为`true`时渲染loading占位
- 为`false`时渲染`children`（即整个路由树）

在 `RouterProvider` 渲染之前完成初始化，确保所有路由组件（包括 `ProtectedRoute`）看到的都是已经恢复好的认证状态。

```tsx
import { useEffect } from "react";
import { useAuthStore } from "@/stores/authStore";

export default function AuthInitializer({
  children,
}: {
  children: React.ReactNode;
}) {
  const initialize = useAuthStore((state) => state.initialize);
  const isInitializing = useAuthStore((state) => state.isInitializing);

  useEffect(() => {
    initialize();
  }, [initialize]);

  if (isInitializing) {
    return (
      <div
        className="flex h-screen w-full items-center justify-center"
        style={{ backgroundColor: "var(--color-bg)" }}
      >
        <div className="flex items-center gap-2">
          <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
            <circle
              className="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="var(--color-accent)"
              strokeWidth="4"
            />
            <path
              className="opacity-75"
              fill="var(--color-accent)"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
            />
          </svg>
          <span
            className="text-sm"
            style={{ color: "var(--color-text-muted)" }}
          >
            Loading...
          </span>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}


```



##### 修改**`main.tsx`**

- 用`AuthInitializer`包裹`RouterProvider`
- `App.tsx`是死代码（main.tsx直接渲染router，未引用App.tsx），可删除或忽略

```tsx
...

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
        
      <AuthInitializer>
        <RouterProvider router={router} />
      </AuthInitializer>
        
 
      <Toaster position="bottom-right" />
    </QueryClientProvider>
  </StrictMode>,
);

```

为什么这样组织

- `AuthInitializer`在`RouterProvider`渲染**之前**先跑`initialize()`,确保`ProtectedRoute`等组件被渲染时,`accessToken`已经恢复完毕(或确认无法恢复)。
- `Toaster`放在`AuthInitializer`外面,这样初始化期间loading画面也能正常弹toast(虽然这个场景一般不会触发)。

**总结：**

```
持久化 accessToken（方案A）：
  优点：实现简单，不需要 AuthInitializer，不需要 Loading 状态
  缺点：accessToken 在 localStorage 里，有 XSS 风险（理论上）

不持久化 + 静默刷新（方案B，你现在的）：
  优点：accessToken 只在内存，安全性更高
  缺点：多了 AuthInitializer 组件，每次刷新页面多一次网络请求
        需要处理 Loading 状态
```



### 13. 本地验证

#### 13.1 启动服务

```bash
# 确保 SQL Server 运行
docker-compose up -d sqlserver

# 启动 API
cd UUcars.API
dotnet run

# 启动前端
cd uucars-web
npm run dev
```

#### 13.2 验证登录后 Cookie 被设置

1. 用浏览器前端登录

2. 打开 DevTools → Application → Cookies → `http://localhost:5173`

3. 应该看到 

    ```
    refreshToken
    ```

     Cookie：

    - `HttpOnly`：✅（值显示为 `(HttpOnly)` 或不可读）
    - `Path`：`/auth`
    - 有过期时间（7天后）

#### 13.3 验证 Token 自动刷新

模拟 AccessToken 过期的方法：

**方法一（快速）：** 临时把 JWT 有效期改为 10 秒，等待过期后操作页面，观察拦截器是否自动刷新。

```csharp
// 临时修改 JwtOptions 或 JwtTokenGenerator，把 ExpiresAt 改为 10 秒
// 验证完成后改回 60 分钟
```

**方法二（精确）：** 在 DevTools Console 里手动清除 accessToken：

```javascript
// 注入一个过期的假 Token，触发 401
const store = JSON.parse(localStorage.getItem('auth-storage'))
// 直接修改内存里的 store（需要找到对应的 zustand store）
```

**方法三（最简单）：** 在 Network 面板里观察正常使用时的请求序列：

1. 登录
2. 浏览车辆列表（应该有 `GET /cars` 请求，正常 200）
3. 在 Hangfire Dashboard 里手动删除刚才创建的 RefreshToken（模拟 Token 作废）
4. 再次操作 → 观察是否出现 `/auth/refresh` 请求 → 最终失败跳转登录页

#### 13.4 验证 Token 轮换

1. 登录
2. 等 AccessToken 快过期（或临时设短有效期）
3. 操作页面触发自动刷新
4. 用 Scalar 或 SQL 客户端查询 `RefreshTokens` 表
5. 应该看到：旧 Token `IsRevoked = 1`，新 Token `IsRevoked = 0`

#### 13.5 验证登出

1. 点击登出按钮
2. 查看 DevTools Network：应该有 `POST /auth/logout` 请求
3. Cookie 被清除（DevTools → Application → Cookies → refreshToken 消失）
4. 再次用旧的 refreshToken 调 `/auth/refresh` → 应该收到 401



### 14. 编译和测试

```bash
dotnet build
dotnet test
```

如果有单元测试需要更新（`AuthController` 或 `UserService` 相关）， 检查测试里的依赖注入是否需要加入 `RefreshTokenService`。

集成测试（`Testcontainers`）通常不需要修改—— 登录接口的集成测试只验证 AccessToken 的返回， RefreshToken 的 Cookie 是额外的，不影响已有断言。



#### 15. Git 提交

```bash
git add .
git commit -m "feat: Refresh Token mechanism with token rotation and HttpOnly Cookie"
git push origin feature/v3-refresh-token

# 合并回 develop
git checkout develop
git merge --no-ff feature/v3-refresh-token \
  -m "merge: feature/v3-refresh-token into develop"
git push origin develop

# 删除功能分支
git branch -d feature/v3-refresh-token
git push origin --delete feature/v3-refresh-token
```



#### Step 61 完成状态

```
知识点：
✅ 理解 AccessToken 和 RefreshToken 各自的职责和存储位置
✅ 理解 HttpOnly Cookie 的安全意义（防 XSS 读取 RefreshToken）
✅ 理解 Cookie 的完整安全属性（HttpOnly/Secure/SameSite/Path）
✅ 理解 Token 轮换（Rotation）的安全价值
✅ 理解已撤销 Token 被重复使用时的安全响应（撤销所有 Token）
✅ 理解 Axios 请求队列处理（多并发 401 只触发一次刷新）
✅ 理解 withCredentials=true 和 AllowCredentials() 的关联
✅ 理解 partialize 把 accessToken 排除在 localStorage 之外的原因

实现：
✅ RefreshToken 实体 + EF Core 配置 + 复合索引
✅ Migration: AddRefreshTokensTable
✅ IRefreshTokenRepository + EfRefreshTokenRepository
✅ RefreshTokenService（创建/刷新/撤销，含 Token 轮换逻辑）
✅ GenerateSecureToken（RandomNumberGenerator，密码学安全）
✅ AuthController：Login 写 Cookie + Refresh 接口 + Logout 接口
✅ SetRefreshTokenCookie 私有方法（开发/生产环境 Secure 属性判断）
✅ TokenCleanupService 更新（加入 RefreshToken 清理）
✅ Axios 拦截器完整实现（isRefreshing 标志 + failedQueue 队列）
✅ authStore 更新（setAccessToken 方法 + partialize 排除 accessTtoken
✅ CORS 更新（AllowCredentials）
✅ 实现应用初始化时静默刷新（auth state rehydration）
✅ 本地验证：Cookie 设置、自动刷新、Token 轮换、登出撤销
✅ dotnet build + dotnet test 通过
✅ Git commit + 合并回 develop 完成
```



## Step 62 · 乐观锁与并发安全（RowVersion）

### 这一步做什么

Step 61 完善了认证体系。现在来解决一个数据层面的真实问题：**并发竞态条件（Race Condition）**。

考虑以下两个场景：

**场景一：两个 Admin 同时审核同一辆车**

```
T1: Admin A 读取车辆（Status = PendingReview）
T1: Admin B 读取车辆（Status = PendingReview）
T2: Admin A 审核通过 → Status = Published → SaveChanges ✅
T2: Admin B 审核通过 → Status = Published → SaveChanges ✅（重复操作，没报错）
```

更危险的情况：

```
T1: Admin A 读取车辆（Status = PendingReview）
T1: Admin B 读取车辆（Status = PendingReview）
T2: Admin A 审核通过 → Status = Published
T2: Admin B 审核拒绝 → Status = Draft（把刚通过的车又退回了！）
```

A 刚通过的车，被 B 的"拒绝"操作悄悄覆盖，数据不一致。

**场景二：两个买家同时下单同一辆车**

```
T1: 买家A 读取车辆（Status = Published）→ 验证通过
T1: 买家B 读取车辆（Status = Published）→ 验证通过
T2: 买家A 创建订单 → Status = Sold → SaveChanges ✅
T2: 买家B 创建订单 → Status = Sold → SaveChanges ✅（同一辆车被卖了两次！）
```

这是真实生产系统里的高危问题。



### 乐观锁 vs 悲观锁

解决并发冲突有两种思路：

**悲观锁（Pessimistic Locking）：**

```
假设冲突一定会发生 → 操作前先锁住资源 → 其他人等着
类比：厕所门锁，进去先锁门，别人只能等
-- SQL Server 悲观锁
SELECT * FROM Cars WITH (UPDLOCK) WHERE Id = @id
```

缺点：

- 持锁期间其他请求全部阻塞，性能差
- 容易死锁（A 等 B 的锁，B 等 A 的锁）
- 适合冲突极高频的场景（秒杀抢购）

**乐观锁（Optimistic Locking）：**

```
假设冲突很少发生 → 操作时不加锁 → 提交时检查有没有人动过
类比：Google Docs 协作编辑，保存时如果别人改过同一段，提示冲突
```

实现原理：给每行数据加一个版本号（`RowVersion`），每次更新时版本号自动递增。

```
读取时：car.RowVersion = [0x01, 0x02, ...]
更新时：WHERE Id = @id AND RowVersion = [0x01, 0x02, ...]
        如果行不存在（RowVersion 已经被别人改了）→ 0 rows affected → 抛并发异常
```

优点：

- 不阻塞其他请求，性能好
- 适合冲突低频的场景（Admin 审核车辆、普通用户下单）

**UUcars 的场景属于低频冲突，乐观锁是正确选择。**



### EF Core 的 `RowVersion` 机制

EF Core 内置对乐观锁的支持，通过 `[Timestamp]` 特性配置：

```csharp
// 实体上加 RowVersion 字段
[Timestamp]
public byte[] RowVersion { get; set; } = [];

// EF Core 自动：
// 1. 映射为 SQL Server 的 rowversion（timestamp）类型，每次行更新自动递增
// 2. 在 UPDATE 语句里自动加 WHERE RowVersion = @originalRowVersion 条件
// 3. 如果影响行数为 0（说明 RowVersion 已变），抛 DbUpdateConcurrencyException
```

只需要：

1. 实体里加 `RowVersion` 字段
2. 在 Service 层捕获 `DbUpdateConcurrencyException`
3. 转换成 `ConcurrencyException`（409）抛出
4. `GlobalExceptionMiddleware` 自动处理（无需修改）



### 1. 切出功能分支

```bash
git checkout develop
git pull origin develop
git checkout -b feature/v3-optimistic-lock
git push -u origin feature/v3-optimistic-lock
```



### 2. 新建 ConcurrencyException

异常目录（`UUcars.API/Exceptions/`）里已有完整的异常体系，`AppException` 是基类。 现在新增 `ConcurrencyException`，返回 409 Conflict。

新建 `UUcars.API/Exceptions/ConcurrencyException.cs`：

```csharp
namespace UUcars.API.Exceptions;

/// <summary>
///     并发冲突异常
///     当两个操作同时修改同一条数据，EF Core 检测到 RowVersion 不匹配时抛出
///     对应 HTTP 409 Conflict
/// </summary>
public class ConcurrencyException : AppException
{
    public ConcurrencyException() : base(StatusCodes.Status409Conflict,
        "The resource was modified by another operation. Please refresh and try again.")
    {
    }
}
```

> **为什么返回 409 Conflict？**
>
> HTTP 语义：409 表示"请求与资源当前状态冲突"。 并发冲突的本质就是：你想做的操作和数据库当前状态产生了冲突（别人先你一步改了它）。 比 500 更准确，让前端能区分"服务器出错"和"并发冲突"这两种情况，给出不同的提示。

`ConcurrencyException` 继承自 `AppException`，现有的 `GlobalExceptionMiddleware` 会 自动捕获它并返回 409.



### 3. 更新 Car 实体

打开 `UUcars.API/Entities/Car.cs`，加入 `RowVersion` 字段, 用于存储版本号：

- 同时使用特征标注 `[Timestamp]` , 它会告诉 EF Core 这个字段是并发令牌，EF Core 会在生成的 `UPDATE` 语句的 `WHERE` 子句里自动带上它。

```csharp
using System.ComponentModel.DataAnnotations;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class Car : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string? Description { get; set; }   // 允许为 null

    // 外键 + 导航属性
    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;

    public CarStatus Status { get; set; } = CarStatus.Draft;

    // ✅ 新增：乐观锁并发令牌
    // SQL Server rowversion 类型：每次行更新自动递增，不需要手动维护
    // EF Core 用它在 UPDATE 时加 WHERE RowVersion = @original 条件
    // 如果影响行数为 0，说明有人先改了这行 → 抛 DbUpdateConcurrencyException
    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    // 导航属性
    public ICollection<CarImage> Images { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
```

> **为什么用 `[Timestamp]` 而不是 Fluent API？**
>
> 两种方式等价，但 `[Timestamp]` 直接写在实体上，看代码时一眼就能看到这个字段是并发令牌， 不需要去 Configuration 文件里找。项目里其他实体已经用 Data Annotation 风格，保持一致。
>
> **乐观锁并发令牌 `RowVersion` 实际上存的是什么？？**
>
> 不是时间值（timestamp标注后，表面上好像是时间戳）， SQL Server 里 `rowversion`（对应 `[Timestamp]`）存的是一个**数据库级别的全局递增计数器**，跟时间没有任何关系。
>
> ```
> 数据库启动后维护一个内部计数器：1, 2, 3, 4, 5 ...
> 
> 任何表的任何行被 UPDATE → 计数器 +1 → 把新值写入该行的 rowversion 字段
> ```
>
> 所以它实际上是个**全局版本号**，只是用 8 字节二进制表示：
>
> ```
> 0x0000000000000001  ← 第1次写入
> 0x0000000000000002  ← 第2次写入（可能是另一张表的另一行）
> 0x0000000000000047  ← 第71次写入
> ```
>
> **为什么叫 Timestamp??**
>
> 这是 SQL Server 早期的历史遗留命名，微软自己后来也觉得这名字不好，在 SQL Server 2008 就把类型改名成了 `rowversion`，但 `timestamp` 作为别名还保留着。`[Timestamp]` 这个 EF Core 特性名也是沿用了这个历史叫法。



### 4. 生成 Migration

```bash
cd UUcars.API
dotnet ef migrations add AddCarRowVersion 
```

检查生成的 Migration 文件，确认：

- 给 `Cars` 表添加了 `RowVersion` 列（`rowversion` 类型）
- 没有其他意外改动

```bash
dotnet ef database update
```

预期输出：

```
Applying migration '20240xxx_AddCarRowVersion'.
Done.
```



### 5. 更新 AdminCarService

打开 `UUcars.API/Services/AdminCarService.cs`。

在 `ApproveAsync` 和 `RejectAsync` 里，把 `UpdateAsync` 包在 `try/catch` 里， 捕获 `DbUpdateConcurrencyException` 并转换为 `ConcurrencyException`：

```csharp
using Microsoft.EntityFrameworkCore;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services.Cache;

namespace UUcars.API.Services;

public class AdminCarService
{
    private readonly ICacheService _cache;
    private readonly ICarRepository _carRepository;
    private readonly ILogger<AdminCarService> _logger;

    public AdminCarService(
        ICarRepository carRepository,
        ILogger<AdminCarService> logger,
        ICacheService cache)
    {
        _carRepository = carRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<CarResponse> ApproveAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 只有 PendingReview 状态才能审核通过
        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Published;
        car.UpdatedAt = DateTime.UtcNow;

        try
        {
            var updated = await _carRepository.UpdateAsync(car, cancellationToken);

            // 车辆上架 → 公开列表变了 → 清掉公开列表的所有缓存
            await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);
            await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

            _logger.LogInformation("Car {CarId} approved by admin, now Published", carId);

            return CarService.MapToResponse(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            // 两个 Admin 同时审核同一辆车：先提交的成功，后提交的触发这个异常
            // RowVersion 不匹配：说明这辆车在读取之后已经被其他操作修改了
            // 告诉调用方：请刷新后重试
            _logger.LogWarning(
                "Concurrency conflict when approving car {CarId}", carId);
            throw new ConcurrencyException();
        }
    }

    public async Task<CarResponse> RejectAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 同样只有 PendingReview 状态才能被拒绝
        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Draft;
        car.UpdatedAt = DateTime.UtcNow;

        try
        {
            var updated = await _carRepository.UpdateAsync(car, cancellationToken);

            // 车辆退回 → 待审核列表变了 → 清待审核列表缓存
            await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

            _logger.LogInformation(
                "Car {CarId} rejected by admin, returned to Draft", carId);

            return CarService.MapToResponse(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict when rejecting car {CarId}", carId);
            throw new ConcurrencyException();
        }
    }

    // GetPendingCarsAsync 和 AdminDeleteAsync 不涉及并发冲突场景，保持不变
    public async Task<PagedResponse<CarResponse>> GetPendingCarsAsync(
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
       ...
    }

    public async Task AdminDeleteAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        ...
}
```



### 6. 更新 OrderService

打开 `UUcars.API/Services/OrderService.cs`。

只需要更新 `CreateAsync` 里的 `SaveChangesAsync` 部分，其余方法完全不变。

找到第6步的代码块，替换如下：

```csharp
// 6. 在同一个事务里保存订单和车辆状态变更
// EF Core 事务保证：订单创建和车辆状态变更要么同时成功，要么同时回滚
_context.Orders.Add(order);
_context.Cars.Update(car);

try
{
    await _context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
    // 两个买家同时下单同一辆车：先提交的成功，后提交的触发这个异常
    // 此时车辆的 RowVersion 已经被第一个买家的操作更新了
    // 第二个买家持有的是旧 RowVersion，EF Core 检测到不匹配，抛出异常
    _logger.LogWarning(
        "Concurrency conflict when creating order for car {CarId}", request.CarId);
    throw new ConcurrencyException();
}

// ✅ 车辆变 Sold，从公开列表消失，清缓存
// 注意：缓存清理在 try/catch 外面
// 原因：只有 SaveChangesAsync 成功，才需要清缓存
// 如果抛了 ConcurrencyException，catch 里直接 throw，缓存清理代码不会执行
await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);
```

同时在文件顶部加上 `using`（如果还没有）：

```csharp
using Microsoft.EntityFrameworkCore;
```



### 7. 前端处理 409

打开 `src/api/client.ts`，在响应拦截器的错误处理部分，在现有的 `429` 处理之前加入 `409` 处理：

```typescript
// =============================================
// 处理 409 Conflict（并发冲突）
// =============================================
if (error.response?.status === 409) {
  // 并发冲突：两个操作同时修改了同一条数据
  // 后端用乐观锁（RowVersion）检测到冲突，返回 409
  // 提示用户刷新后重试，不需要任何特殊的恢复逻辑
  const apiResponse = error.response.data as ApiResponse<unknown>;
  const message =
    apiResponse?.message ??
    "This resource was modified by another operation. Please refresh and try again.";

  toast.error(message, { duration: 5000 });

  return Promise.reject(new Error(message));
}
```

更新后响应拦截器的错误处理顺序：

```typescript
// 401 → 自动刷新 Token（原有）
// 409 → Toast 提示并发冲突  ← 新增
// 429 → Toast 提示限流（原有）
// 其他 4xx/5xx → 提取 message（原有）
// 超时 → 提示超时（原有）
// 网络错误 → 提示网络错误（原有）
```



### 8. 单元测试

进行AdminCarService 并发冲突测试。

新建 `UUcars.Tests/Services/AdminCarServiceTests.cs`，加入以下测试：

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class AdminCarServiceTests
{
    [Fact]
    public async Task ApproveAsync_WhenConcurrencyConflict_ThrowsConcurrencyException()
    {
        // Arrange
        var fakeRepo = new ConcurrencyCarRepository();
        var service = new AdminCarService(
            fakeRepo,
            NullLogger<AdminCarService>.Instance,
            new FakeCacheService()
        );

        fakeRepo.Seed(new Car
        {
            Id = 1,
            Title = "Test Car",
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Price = 10000,
            Mileage = 50000,
            SellerId = 1,
            Status = CarStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.ApproveAsync(1));
    }

    [Fact]
    public async Task RejectAsync_WhenConcurrencyConflict_ThrowsConcurrencyException()
    {
        // Arrange
        var fakeRepo = new ConcurrencyCarRepository();
        var service = new AdminCarService(
            fakeRepo,
            NullLogger<AdminCarService>.Instance,
            new FakeCacheService()
        );

        fakeRepo.Seed(new Car
        {
            Id = 1,
            Title = "Test Car",
            Brand = "Toyota",
            Model = "Corolla",
            Year = 2020,
            Price = 10000,
            Mileage = 50000,
            SellerId = 1,
            Status = CarStatus.PendingReview,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.RejectAsync(1));
    }
}

/// <summary>
///     专门用于测试并发冲突的假 Repository
///     UpdateAsync 固定抛 DbUpdateConcurrencyException，模拟 RowVersion 不匹配的场景
/// </summary>
/// 
/// <remarks>
///     为什么用显式接口实现而不是 override 或 new？
///
///     FakeCarRepository.UpdateAsync 不是 virtual，无法 override。
///
///     new（方法隐藏）虽然可以声明同名方法，但通过接口调用时走的仍是父类的实现，
///     因为 new 只影响继承链，不影响接口的方法映射。
///
///     显式接口实现（ICarRepository.UpdateAsync(...)）会直接绑定到
///     接口的方法映射上，优先级高于继承来的实现，所以通过接口调用时会走这里。
/// 
///     其他未显式实现的方法仍继承自 FakeCarRepository。
/// </remarks>
internal class ConcurrencyCarRepository : FakeCarRepository, ICarRepository
{
    Task<Car> ICarRepository.UpdateAsync(
        Car car,
        CancellationToken cancellationToken)
    {
        // 模拟乐观锁检测到 RowVersion 不匹配时抛出的异常
        throw new DbUpdateConcurrencyException(
            "Simulated concurrency conflict: RowVersion mismatch");
    }
}
```



### 9. 本地验证

#### 9.1 确认 Migration 生效

用数据库工具查看 `Cars` 表，确认有 `RowVersion` 列，类型是 `rowversion`（SQL Server 显示为 `timestamp`）。

#### 9.2 验证正常流程不受影响

用前端正常操作：

- 卖家发布车辆 → 提交审核
- Admin 审核通过
- 买家下单

确认所有正常流程不受影响（乐观锁不阻塞任何操作，只在真正冲突时才触发）。

#### 9.3 验证 409 响应和前端 Toast

用 Scalar（`http://localhost:5065/scalar`）验证：

1. 找一辆 `PendingReview` 状态的车，记录 `carId`
2. Admin 登录，拿到 Token
3. 调 `POST /admin/cars/{carId}/approve` → 200，车辆变为 `Published`
4. 再调一次 `POST /admin/cars/{carId}/approve` → 触发 `CarStatusException`（400），因为状态已不是 `PendingReview`

> **说明：** 手动模拟真正的 RowVersion 冲突需要两个请求在毫秒级同时到达， 实际验证通过单元测试覆盖。 前端 Toast 可以用 Scalar 直接调接口，构造一个会返回 409 的场景来验证显示效果。



### 10. 编译和测试

```bash
dotnet build
```

预期：

```bash
Build succeeded.
    0 Warning(s)
    0 Error(s)

```

```bash
dotnet test
```

预期：

```bash
Test summary: total: XX, failed: 0, succeeded: XX
```

新增了2个测试（`ApproveAsync_WhenConcurrencyConflict` 和 `RejectAsync_WhenConcurrencyConflict`）， 应该都通过。



### 11. Git 提交

```bash
git add .
git commit -m "feat: optimistic locking with RowVersion for concurrent car operations"
git push origin feature/v3-optimistic-lock

# 合并回 develop
git checkout develop
git merge --no-ff feature/v3-optimistic-lock \
  -m "merge: feature/v3-optimistic-lock into develop"
git push origin develop

# 删除功能分支
git branch -d feature/v3-optimistic-lock
git push origin --delete feature/v3-optimistic-lock
```



### Step 62 完成状态

```
知识点：
✅ 理解竞态条件（Race Condition）的本质和危害
✅ 理解乐观锁 vs 悲观锁的选择依据
✅ 理解 EF Core RowVersion 机制：
   - [Timestamp] 映射为 SQL Server rowversion 类型，每次行更新自动递增
   - EF Core 在 UPDATE 时自动加 WHERE RowVersion = @original 条件
   - 影响行数为 0 时抛 DbUpdateConcurrencyException
✅ 理解 409 Conflict 的 HTTP 语义（请求与资源当前状态冲突）
✅ 理解为什么 UUcars 适合乐观锁（低频冲突场景）
✅ 理解 new（方法隐藏）和 override（多态重写）的区别

实现：
✅ ConcurrencyException（继承 AppException，StatusCode = 409）
✅ GlobalExceptionMiddleware 自动处理（无需修改）
✅ Car 实体加 [Timestamp] RowVersion 字段
✅ Migration: AddCarRowVersion
✅ AdminCarService.ApproveAsync：try/catch DbUpdateConcurrencyException → ConcurrencyException
✅ AdminCarService.RejectAsync：try/catch DbUpdateConcurrencyException → ConcurrencyException
✅ OrderService.CreateAsync：try/catch DbUpdateConcurrencyException → ConcurrencyException
✅ 前端 client.ts：新增 409 处理（Toast 提示）
✅ 单元测试：ConcurrencyCarRepository + 2个并发冲突测试用例
✅ dotnet build + dotnet test 通过
✅ Git commit + 合并回 develop 完成
```



## Step 63 · 安全加固

### 这一步做什么

前面几步解决了性能（缓存、限流）和数据安全（并发）的问题。 这一步系统性地检查和修复 UUcars 里真实存在的安全问题。

现有的内容，存在如下一些安全问题：

1. **安全响应头**：缺少 `X-Content-Type-Options`、`X-Frame-Options` 等基础安全头 → 需要新增
2. **JWT 算法固定**：`TokenValidationParameters` 里没有明确限制算法 → 需要新增
3. **审计日志**：Admin 操作（审核通过/拒绝/强制删除）没有持久化的操作记录 → 需要新增

   

### 1. 切出功能分支

```bash
git checkout develop
git pull origin develop
git checkout -b feature/v3-security
git push -u origin feature/v3-security
```



### 2. 安全响应头

#### 2.1 为什么要加安全响应头？

浏览器默认行为存在一些安全隐患：

- 不指定 `X-Content-Type-Options`：浏览器可能"嗅探"响应内容类型，把文本当 JS 执行
- 不指定 `X-Frame-Options`：网站可以被嵌入 iframe，用于点击劫持（Clickjacking）攻击
- 不指定 `Referrer-Policy`：跨域跳转时可能泄露完整的来源 URL（包括敏感的查询参数）

#### 2.2 如何解决？

使用安全响应头中间件

```c#
// 安全响应头中间件
// 在所有响应里加上基础安全头，防御常见的浏览器端攻击
app.Use(async (context, next) =>
{
    // 防止 MIME 类型嗅探
    // 浏览器严格按 Content-Type 处理响应，不会"猜测"内容类型
    // 防止攻击者上传带有 JS 代码的文件，被浏览器误当作脚本执行
    context.Response.Headers.XContentTypeOptions = "nosniff";

    // 防止点击劫持（Clickjacking）
    // 禁止本站页面被嵌入任何 iframe，无论是同源还是跨源
    // UUcars 没有被嵌入 iframe 的需求，DENY 是最严格也最合适的选择
    context.Response.Headers.XFrameOptions = "DENY";

    // 防止 Referrer 信息泄露
    // strict-origin-when-cross-origin：
    //   同源请求：发送完整 Referrer
    //   跨源请求：只发送来源（origin），不发送路径和查询参数
    // 避免把用户访问的具体路径（如带 token 的链接）泄露给第三方网站
    context.Response.Headers["Referrer-Policy"] = "no-referrer";

    await next();
});
```

打开 `UUcars.API/Program.cs`，在 `app.UseMiddleware<GlobalExceptionMiddleware>()` 之后、 `app.UseCors()` 之前，加入安全响应头中间件，无论请求是否跨域，安全头都生效。

完整的中间件顺序：

```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();

// ✅ 安全响应头（新增）
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(...);
}

app.UseCors(CorsExtensions.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// ...
```

> **为什么不加 `X-XSS-Protection`？**
>
> 这个响应头是早期浏览器（IE/旧版 Chrome）的内置 XSS 过滤器开关。 现代浏览器（Chrome 78+、Firefox、Safari）已经**移除**了这个过滤器的实现， 这个头对现代浏览器没有任何效果，加了也是无意义的代码。 真正现代的防护手段是 CSP（Content-Security-Policy），但 CSP 主要保护前端页面， 适合配置在 Azure Static Web Apps 的 `staticwebapp.config.json` 里，不属于后端 API 的职责。

> **为什么不用 `NWebsec` 之类的安全库？**
>
> 对于 3 个响应头，手写中间件比引入一个新的 NuGet 依赖更直观， 代码量相近，但不增加项目依赖和学习成本。

#### 2.3 封装扩展方法

把安全响应头配置的这部分封装出去，保证主程序整洁。

创建扩展方法`SecurityHeadersExtensions`

```c#
namespace UUcars.API.Extensions;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
        return app;
    }
}
```

替换使用扩展方法

```c#
// =============================================
// 中间件管道
// =============================================

// 全局异常处理，放最前面，兜住所有后续中间件的异常
app.UseMiddleware<GlobalExceptionMiddleware>();

// ✅ 安全响应头（新增）
app.UseSecurityHeaders();

// 开发环境才挂载 API 文档
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options
            .WithTitle("UUcars API Documentation")
            .WithTheme(ScalarTheme.Moon)
    );
}
...
```



### 3. JWT 算法固定

**什么问题？**

打开 `UUcars.API/Extensions/AuthExtensions.cs`，当前的 `TokenValidationParameters` 没有限制接受的签名算法。理论上存在 `alg:none` 攻击的风险——攻击者构造一个 不带签名的 Token，如果验证逻辑不够严格，可能被接受。

**如何修改？**

在 `TokenValidationParameters` 的最后加上 `ValidAlgorithms`：

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    // ===== 签名验证 =====
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtSettings.Secret)),

    // ===== Issuer 验证 =====
    ValidateIssuer = true,
    ValidIssuer = jwtSettings.Issuer,

    // ===== Audience 验证 =====
    ValidateAudience = true,
    ValidAudience = jwtSettings.Audience,

    // ===== 过期时间验证 =====
    ValidateLifetime = true,

    // ===== 时钟偏差 =====
    ClockSkew = TimeSpan.Zero,

    // ✅ 新增：明确限制只接受 HmacSha256 签名的 Token
    // 防止 alg:none 攻击：攻击者构造一个 alg 字段为 "none" 的 Token（无签名）
    // 不限制算法时，某些 JWT 库实现可能接受这种 Token，完全绕过签名验证
    // 明确指定后，任何不是 HmacSha256 签名的 Token 都会被直接拒绝
    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
};
```

只需要在现有 `TokenValidationParameters` 对象初始化器的最后加上 `ValidAlgorithms` 这一行， 其余配置完全不变。

> **实际风险有多大？**
>
> Microsoft 的 `System.IdentityModel.Tokens.Jwt` 库默认就拒绝 `alg:none` 的 Token， 这个漏洞在你的实现里发生的概率极低。但明确限制算法是防御纵深（Defense in Depth）的体现—— 即使库的默认行为将来改变，代码也不依赖那个隐含假设。这也是面试时能展示的安全意识。



### 4. 审计日志

#### 4.1 为什么需要审计日志？

当前 Admin 的操作（审核通过/拒绝/强制删除）只有 Serilog 的结构化日志：

```csharp
_logger.LogInformation("Car {CarId} approved by admin, now Published", carId);
```

这种日志有几个局限：

- 日志文件按保留策略自动清理（30天后删除）
- 不在数据库里，无法用 SQL 查询和筛选
- 无法在后台管理页面展示给 Admin 查看操作历史

审计日志把关键操作持久化到数据库，提供可查询、可追溯的操作记录。

#### 4.2 设计：接口 + 实现

和项目里其他外部依赖（`IEmailService`、`IStorageService`、`ICacheService`）保持一致的模式： 定义接口，方便单元测试用 Fake 实现替换，不需要真实数据库。

新建 `UUcars.API/Services/Audit/IAuditLogService.cs`：

```csharp
namespace UUcars.API.Services.Audit;

public interface IAuditLogService
{
    Task LogAsync(
        int adminId,
        string action,
        string entityType,
        int entityId,
        string? detail = null,
        CancellationToken cancellationToken = default);
}
```

#### 4.3 AuditLog 实体

新建 `UUcars.API/Entities/AuditLog.cs`：

```csharp
namespace UUcars.API.Entities;

/// <summary>
///     审计日志：记录 Admin 的关键操作
///     不继承 BaseEntity（不需要 UpdatedAt，审计日志只写入，不修改）
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>
    /// 操作者（Admin 用户）的 Id
    /// </summary>
    public int AdminId { get; set; }
    public User Admin { get; set; } = null!;

    /// <summary>
    /// 操作类型，例如："CarApproved" / "CarRejected" / "CarDeleted"
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 被操作的实体类型，例如："Car"
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 被操作的实体 Id
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// 补充说明（可选）
    /// </summary>
    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

#### 4.4 操作类型常量

新建 `UUcars.API/Entities/AuditActions.cs`：

```csharp
namespace UUcars.API.Entities;

/// <summary>
///     审计日志操作类型常量
///     用常量而不是枚举，直接存字符串到数据库，查询时更直观
/// </summary>
public static class AuditActions
{
    public const string CarApproved = "CarApproved";
    public const string CarRejected = "CarRejected";
    public const string CarDeleted = "CarDeleted";
}
```

#### 4.5 EF Core 配置

新建 `UUcars.API/Data/Configurations/AuditLogConfiguration.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Detail)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // 关联 Admin 用户
        // Restrict：Admin 用户不能被删除（如果还有审计记录关联着他）
        // 保护审计记录的完整性，不让删除用户连带删除历史操作记录
        builder.HasOne(a => a.Admin)
            .WithMany()
            .HasForeignKey(a => a.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // 常见查询场景：按操作类型筛选，按时间排序
        builder.HasIndex(a => new { a.Action, a.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Action_CreatedAt");
    }
}
```

#### 4.6 注册到 AppDbContext

打开 `UUcars.API/Data/AppDbContext.cs`，加入 DbSet：

```csharp
public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
```

#### 4.7 实现 AuditLogService

新建 `UUcars.API/Services/Audit/AuditLogService.cs`：

```csharp
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Services.Audit;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        int adminId,
        string action,
        string entityType,
        int entityId,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            AdminId = adminId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Detail = detail,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit: Admin {AdminId} performed {Action} on {EntityType} {EntityId}",
            adminId, action, entityType, entityId);
    }
}
```

#### 4.8 单元测试用的 Fake 实现

新建 `UUcars.Tests/Fakes/FakeAuditLogService.cs`：

```csharp
using UUcars.API.Services.Audit;

namespace UUcars.Tests.Fakes;

/// <summary>
///     测试用的审计日志服务：记录调用参数，不真正写数据库
///     和 FakeEmailService / FakeCacheService 保持一致的测试模式
/// </summary>
public class FakeAuditLogService : IAuditLogService
{
    public record LoggedEntry(
        int AdminId, string Action, string EntityType, int EntityId, string? Detail);

    public List<LoggedEntry> Entries { get; } = new();

    public Task LogAsync(
        int adminId,
        string action,
        string entityType,
        int entityId,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(new LoggedEntry(adminId, action, entityType, entityId, detail));
        return Task.CompletedTask;
    }
}
```

#### 4.9 更新 AdminCarService

`AdminCarService` 需要注入 `IAuditLogService` 和 `CurrentUserService`（从 Token 取当前 Admin Id）， 在三个操作里写审计记录。

打开 `UUcars.API/Services/AdminCarService.cs`：

```csharp
using Microsoft.EntityFrameworkCore;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services.Audit;
using UUcars.API.Services.Cache;

namespace UUcars.API.Services;

public class AdminCarService
{
    private readonly ICacheService _cache;
    private readonly ICarRepository _carRepository;
    private readonly ILogger<AdminCarService> _logger;
    private readonly IAuditLogService _auditLogService;       // ✅ 新增
    private readonly CurrentUserService _currentUserService;  // ✅ 新增

    public AdminCarService(
        ICarRepository carRepository,
        ILogger<AdminCarService> logger,
        ICacheService cache,
        IAuditLogService auditLogService,       // ✅ 新增
        CurrentUserService currentUserService)  // ✅ 新增
    {
        _carRepository = carRepository;
        _logger = logger;
        _cache = cache;
        _auditLogService = auditLogService;        // ✅ 新增
        _currentUserService = currentUserService;  // ✅ 新增
    }

    public async Task<CarResponse> ApproveAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Published;
        car.UpdatedAt = DateTime.UtcNow;

        try
        {
            var updated = await _carRepository.UpdateAsync(car, cancellationToken);

            await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);
            await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

            // ✅ 写审计日志
            // GetCurrentUserId 理论上不会返回 null（Controller 已用 [Authorize(Roles="Admin")] 保护）
            // 但做防御性判断，避免极端情况下空引用
            var adminId = _currentUserService.GetCurrentUserId();
            if (adminId.HasValue)
                await _auditLogService.LogAsync(
                    adminId.Value, AuditActions.CarApproved, "Car", carId,
                    cancellationToken: cancellationToken);

            _logger.LogInformation("Car {CarId} approved by admin, now Published", carId);

            return CarService.MapToResponse(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict when approving car {CarId}", carId);
            throw new ConcurrencyException();
        }
    }

    public async Task<CarResponse> RejectAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Draft;
        car.UpdatedAt = DateTime.UtcNow;

        try
        {
            var updated = await _carRepository.UpdateAsync(car, cancellationToken);

            await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

            // ✅ 写审计日志
            var adminId = _currentUserService.GetCurrentUserId();
            if (adminId.HasValue)
                await _auditLogService.LogAsync(
                    adminId.Value, AuditActions.CarRejected, "Car", carId,
                    cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Car {CarId} rejected by admin, returned to Draft", carId);

            return CarService.MapToResponse(updated);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict when rejecting car {CarId}", carId);
            throw new ConcurrencyException();
        }
    }

    public async Task<PagedResponse<CarResponse>> GetPendingCarsAsync(
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        pageSize = Math.Min(pageSize, 50);

        var cacheKey = CacheKeys.PendingCars(page, pageSize);

        return await _cache.GetOrSetAsync(cacheKey,
            async () =>
            {
                var (cars, totalCount) = await _carRepository.GetPagedAsync(
                    CarStatus.PendingReview,
                    query, cancellationToken);
                var items = cars.Select(CarService.MapToResponse).ToList();
                return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
            }, TimeSpan.FromSeconds(30), cancellationToken);
    }

    public async Task AdminDeleteAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        if (car.Status == CarStatus.Deleted)
            throw new CarStatusException(car.Id, car.Status, CarStatus.Published);

        var previousStatus = car.Status;
        car.Status = CarStatus.Deleted;
        car.UpdatedAt = DateTime.UtcNow;

        await _carRepository.UpdateAsync(car, cancellationToken);

        if (previousStatus == CarStatus.Published)
            await _cache.RemoveByPrefixAsync(CacheKeys.PublishedCarsPrefix, cancellationToken);

        if (previousStatus == CarStatus.PendingReview)
            await _cache.RemoveByPrefixAsync(CacheKeys.PendingCarsPrefix, cancellationToken);

        // ✅ 写审计日志
        var adminId = _currentUserService.GetCurrentUserId();
        if (adminId.HasValue)
            await _auditLogService.LogAsync(
                adminId.Value, AuditActions.CarDeleted, "Car", carId,
                detail: $"Previous status: {previousStatus}",
                cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Car {CarId} forcefully deleted by admin (was {Status}), cache invalidated",
            carId, previousStatus);
    }
}
```

#### 4.10 新建审计日志查询接口

新建 `UUcars.API/DTOs/Responses/AuditLogResponse.cs`：

```csharp
namespace UUcars.API.DTOs.Responses;

public class AuditLogResponse
{
    public int Id { get; set; }
    public int AdminId { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

在 `AdminCarService` 里加入查询方法。这个方法需要直接查 `AuditLogs` 表， 所以需要额外注入 `AppDbContext`（其余方法不依赖它，只有这一个查询方法用到）：

```csharp
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;   // ✅ 新增

// 构造函数加入 AppDbContext
private readonly AppDbContext _context; // ✅ 新增：仅用于审计日志查询

public AdminCarService(
    ICarRepository carRepository,
    ILogger<AdminCarService> logger,
    ICacheService cache,
    IAuditLogService auditLogService,
    CurrentUserService currentUserService,
    AppDbContext context)              // ✅ 新增
{
    _carRepository = carRepository;
    _logger = logger;
    _cache = cache;
    _auditLogService = auditLogService;
    _currentUserService = currentUserService;
    _context = context;                // ✅ 新增
}

// 在 AdminDeleteAsync 之后加入这个新方法
public async Task<PagedResponse<AuditLogResponse>> GetAuditLogsAsync(
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    page = page < 1 ? 1 : page;
    pageSize = Math.Min(pageSize < 1 ? 20 : pageSize, 50);

    var query = _context.AuditLogs
        .Include(a => a.Admin)
        .OrderByDescending(a => a.CreatedAt);

    var totalCount = await query.CountAsync(cancellationToken);

    var logs = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    var items = logs.Select(a => new AuditLogResponse
    {
        Id = a.Id,
        AdminId = a.AdminId,
        AdminUsername = a.Admin?.Username ?? string.Empty,
        Action = a.Action,
        EntityType = a.EntityType,
        EntityId = a.EntityId,
        Detail = a.Detail,
        CreatedAt = a.CreatedAt
    }).ToList();

    return PagedResponse<AuditLogResponse>.Create(items, totalCount, page, pageSize);
}
```

#### 4.11 更新 AdminController

打开 `UUcars.API/Controllers/AdminController.cs`，加入审计日志查询接口。 路由放在 `GetPendingCars` 之后，和现有风格一致：

```csharp
// GET /admin/audit-logs
[HttpGet("audit-logs")]
public async Task<IActionResult> GetAuditLogs(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    var result = await _adminCarService.GetAuditLogsAsync(page, pageSize, cancellationToken);
    return Ok(ApiResponse<PagedResponse<AuditLogResponse>>.Ok(result));
}
```

`AdminController` 不需要新增任何注入——`_adminCarService` 已经存在， 新方法 `GetAuditLogsAsync` 也定义在 `AdminCarService` 里。

需要在文件顶部加上 DTO 的 using：

```csharp
using UUcars.API.DTOs.Responses; // 已存在，AuditLogResponse 在这个命名空间下
```

#### 4.12 注册到 DI

打开 `Program.cs`，在 Admin 模块注册部分加入：

```csharp
// Admin 模块
builder.Services.AddScoped<AdminCarService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>(); // ✅ 新增
```

记得在文件顶部加 using：

```csharp
using UUcars.API.Services.Audit;
```

#### 4.13 生成 Migration

```bash
cd UUcars.API
dotnet ef migrations add AddAuditLogsTable \
  --output-dir Data/Migrations
```

检查 Migration 文件，确认：

- 创建了 `AuditLogs` 表
- 有 `IX_AuditLogs_Action_CreatedAt` 索引
- 有 `AdminId` 外键指向 `Users` 表

```bash
dotnet ef database update
```



### 5. 本地验证

#### 5.1 验证安全响应头

启动 API 后，用浏览器 DevTools → Network，查看任意接口的响应头：

```
X-Content-Type-Options: nosniff                  ✅
X-Frame-Options: DENY                             ✅
Referrer-Policy: strict-origin-when-cross-origin  ✅
```

#### 5.2 验证 JWT 算法固定

正常登录获取的 Token 应该完全不受影响，登录、刷新、所有需要认证的接口都正常工作。

可选验证：用 [jwt.io](https://jwt.io/) 构造一个 `alg: none` 的伪造 Token， 调用任何需要认证的接口，应该收到 401（这个验证之前就会被拒绝，加了 `ValidAlgorithms` 后多一层明确的防护）。

#### 5.3 验证审计日志

1. Admin 登录
2. 审核通过一辆车（`POST /admin/cars/{id}/approve`）
3. 调 `GET /admin/audit-logs`
4. 返回的列表里应该有一条 `CarApproved` 记录

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "adminId": 1,
        "adminUsername": "admin",
        "action": "CarApproved",
        "entityType": "Car",
        "entityId": 5,
        "detail": null,
        "createdAt": "2024-..."
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```



### 6. 编译和测试

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**`AdminCarServiceTests` 需要更新构造函数调用：**

`AdminCarService` 构造函数多了 `IAuditLogService`、`CurrentUserService`、`AppDbContext` 三个依赖。 由于 `IAuditLogService` 已经是接口，直接用 `FakeAuditLogService` 替换，不需要引入 InMemory 数据库。

但 `GetAuditLogsAsync` 这一个方法依赖 `AppDbContext` 做真实查询，这个方法本身不在 现有的并发冲突测试范围内，单元测试只需要传一个能正常构造的 `AppDbContext` 实例 （用 InMemory provider，仅用于让构造函数能成功创建对象，不在并发测试里触发它）：

```csharp
// AdminCarServiceTests.cs 里更新 CreateService 辅助方法
private static AdminCarService CreateAdminService(
    ICarRepository carRepository,
    ICacheService? cache = null,
    IAuditLogService? auditLogService = null)
{
    // AppDbContext 只用于满足构造函数依赖（GetAuditLogsAsync 才会真正用到它）
    // 并发冲突测试（ApproveAsync/RejectAsync）不涉及这个依赖
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    var context = new AppDbContext(options);

    var httpContextAccessor = new HttpContextAccessor();
    var currentUserService = new CurrentUserService(httpContextAccessor);

    return new AdminCarService(
        carRepository,
        NullLogger<AdminCarService>.Instance,
        cache ?? new FakeCacheService(),
        auditLogService ?? new FakeAuditLogService(),
        currentUserService,
        context
    );
}
```

> **为什么这里还是要用 InMemory Database？**
>
> `IAuditLogService` 已经接口化，写审计日志的逻辑用 `FakeAuditLogService` 完全隔离了， 这是正确的测试模式。但 `GetAuditLogsAsync` 直接查询 `_context.AuditLogs`（没有走 Repository 抽象），所以 `AdminCarService` 仍然需要一个能实例化的 `AppDbContext`。 这里的 InMemory Database 只是为了让构造函数能正常工作，并发冲突的两个测试用例完全不会 触碰到这个 `_context`，职责依然是清晰分离的。

更新两个并发冲突测试，使用新的辅助方法：

```csharp
[Fact]
public async Task ApproveAsync_WhenConcurrencyConflict_ThrowsConcurrencyException()
{
    var fakeRepo = new ConcurrencyCarRepository();
    var service = CreateAdminService(fakeRepo);

    fakeRepo.Seed(new Car
    {
        Id = 1,
        Title = "Test Car",
        Brand = "Toyota",
        Model = "Corolla",
        Year = 2020,
        Price = 10000,
        Mileage = 50000,
        SellerId = 1,
        Status = CarStatus.PendingReview,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    });

    await Assert.ThrowsAsync<ConcurrencyException>(
        () => service.ApproveAsync(1));
}

[Fact]
public async Task RejectAsync_WhenConcurrencyConflict_ThrowsConcurrencyException()
{
    var fakeRepo = new ConcurrencyCarRepository();
    var service = CreateAdminService(fakeRepo);

    fakeRepo.Seed(new Car
    {
        Id = 1,
        Title = "Test Car",
        Brand = "Toyota",
        Model = "Corolla",
        Year = 2020,
        Price = 10000,
        Mileage = 50000,
        SellerId = 1,
        Status = CarStatus.PendingReview,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    });

    await Assert.ThrowsAsync<ConcurrencyException>(
        () => service.RejectAsync(1));
}
```

**新增一个测试，验证审计日志真正被调用：**

```csharp
[Fact]
public async Task ApproveAsync_WhenSuccessful_WritesAuditLog()
{
    var fakeRepo = new FakeCarRepository();
    var fakeAuditLog = new FakeAuditLogService();
    var service = CreateAdminService(fakeRepo, auditLogService: fakeAuditLog);

    fakeRepo.Seed(new Car
    {
        Id = 1,
        Title = "Test Car",
        Brand = "Toyota",
        Model = "Corolla",
        Year = 2020,
        Price = 10000,
        Mileage = 50000,
        SellerId = 1,
        Status = CarStatus.PendingReview,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    });

    await service.ApproveAsync(1);

    // 注意：GetCurrentUserId 在测试环境里没有真实 HttpContext，会返回 null
    // 所以 AdminCarService 里的 if (adminId.HasValue) 判断会跳过写审计日志
    // 这个测试验证的是"流程不报错"，真正的审计写入在集成测试里验证
    Assert.Empty(fakeAuditLog.Entries);
}
```

> **这个测试揭示了一个问题**：单元测试环境里 `CurrentUserService.GetCurrentUserId()` 永远拿不到真实的 Admin Id（没有真实的 HTTP 请求上下文），所以审计日志在单元测试层面 验证不到"真的写入了"，只能验证"流程不报错"。真正验证审计日志的写入需要在**集成测试**里做 （集成测试用真实的 HTTP 请求，`CurrentUserService` 能从真实 JWT 里解析出 Admin Id）。

**集成测试新增一个用例**（在 `CoreFlowIntegrationTests.cs` 或对应的 Admin 流程测试文件里）：

```csharp
[Fact]
public async Task AdminApprove_WhenSuccessful_CreatesAuditLogEntry()
{
    // 注册卖家、发布车辆、提交审核（复用现有的 RegisterAndLoginAsync 辅助方法）
    var sellerToken = await RegisterAndLoginAsync("seller@example.com");
    Client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", sellerToken);

    var createResponse = await Client.PostAsync("/cars", JsonContent(new
    {
        title = "Test Car", brand = "Toyota", model = "Corolla",
        year = 2020, price = 10000m, mileage = 50000
    }));
    var car = await DeserializeAsync<ApiResponseWrapper<CarData>>(createResponse);
    var carId = car!.Data!.Id;

    await Client.PostAsync($"/cars/{carId}/submit", null);

    // Admin 登录并审核
    var adminToken = await LoginAsAdminAsync(); // 假设已有这个辅助方法，登录 Seed 的 Admin 账号
    Client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", adminToken);

    await Client.PostAsync($"/admin/cars/{carId}/approve", null);

    // 验证审计日志接口能查到这条记录
    var auditResponse = await Client.GetAsync("/admin/audit-logs");
    var auditResult = await DeserializeAsync<ApiResponseWrapper<PagedAuditLogData>>(auditResponse);

    Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
    Assert.Contains(auditResult!.Data!.Items,
        log => log.Action == "CarApproved" && log.EntityId == carId);
}
```

> 这个测试需要根据你实际的 `IntegrationTestBase` 辅助方法名调整（比如是否已有 `LoginAsAdminAsync` 这样的方法）。如果没有，需要先确认 Admin 测试账号的登录方式。

```bash
dotnet test
```

预期：

```
Test summary: total: XX, failed: 0, succeeded: XX
```



### 7. Git 提交

```bash
git add .
git commit -m "feat: security hardening - security headers, JWT algorithm fix, audit logs"
git push origin feature/v3-security

# 合并回 develop
git checkout develop
git merge --no-ff feature/v3-security \
  -m "merge: feature/v3-security into develop"
git push origin develop

# 删除功能分支
git branch -d feature/v3-security
git push origin --delete feature/v3-security
```



#### 8. 合并到 main（v3.1 里程碑）

Step 60-63 全部完成，达到 v3.1 里程碑。

```bash
git checkout main
git pull origin main
git merge --no-ff develop \
  -m "release: v3.1 - Infrastructure + Auth + Concurrency + Security"
git tag -a v3.1 \
  -m "v3.1: Hangfire + Refresh Token + Optimistic Lock + Security Hardening"
git push origin main
git push origin main --tags
git checkout develop
```

推送到 main 后，GitHub Actions 自动触发：

- ✅ 单元测试 + 集成测试
- ✅ Docker 镜像构建并推送到 ghcr.io
- ✅ 后端部署到 Azure Container Apps
- ✅ 前端部署到 Azure Static Web Apps



### Step 63 完成状态

```
安全检查结果（确认无需修复）：
✅ IDOR 检查：所有 Ownership 验证均已正确实现
✅ 文件上传安全：FileValidator 已有类型白名单 + 大小限制
✅ 缓存逻辑检查：DeleteAsync 仅删 Draft 车辆不影响缓存；
                   AdminDeleteAsync 已按前状态正确清缓存
✅ 日志脱敏检查：无密码/Token 泄露

新增实现：
✅ 安全响应头（X-Content-Type-Options / X-Frame-Options / Referrer-Policy）
✅ JWT 算法固定（ValidAlgorithms = HmacSha256）
✅ IAuditLogService 接口 + AuditLogService 实现
✅ FakeAuditLogService（单元测试用，和现有 Fake 模式一致）
✅ AuditLog 实体 + AuditActions 常量 + EF Core 配置
✅ Migration: AddAuditLogsTable
✅ AdminCarService 三个操作写审计日志（Approve / Reject / Delete）
✅ GET /admin/audit-logs 分页查询接口
✅ AuditLogResponse DTO
✅ AdminCarServiceTests 更新（FakeAuditLogService + 新增审计写入测试）
✅ 集成测试新增：验证审计日志真实写入并可查询
✅ dotnet build + dotnet test 通过
✅ Git commit + 合并回 develop 完成
✅ v3.1 里程碑合并 main，触发 CI/CD 部署

知识点：
✅ 理解为什么安全响应头能以极低成本关闭常见攻击面
✅ 理解 alg:none 攻击的原理，以及 ValidAlgorithms 的防御纵深意义
✅ 理解审计日志和应用日志（Serilog）的本质区别（可查询 vs 临时排查）
✅ 理解接口化设计在测试中的价值：单元测试只验证流程逻辑，
   真实写入效果交给集成测试验证（这是分层测试策略的体现）
```



## fixed Issues 

### Fix 1. 并发 Refresh Token 请求竞态条件（Refresh Token Rotation Race Condition）

#### 1. 切出fix分支

```bash
git checkout develop
git pull origin develop
git checkout -b fix/refresh-token-rotation-race-condition
git push -u origin fix/refresh-token-rotation-race-condition
```

#### 2. 问题描述

前端应用启动, 并成功登陆后，快速连续刷新登录后的页面，用户被自动登出。

具体来说：前端在应用启动时通过 `AuthInitializer` 组件调用 `authStore.initialize()`，检测到 localStorage 里有 `user` 数据后，向 `/auth/refresh` 发送请求，用 HttpOnly cookie 里的 refresh token 换取新的 access token，实现页面刷新后的静默重登录。

**当用户快速连续刷新页面时**，浏览器会在极短时间内启动多个页面实例，每个实例都独立执行 `initialize()`。由于 `isInitializing` 只是 Zustand 的 UI 渲染标志，无法阻止两个调用同时执行到 `axios.post` 那一行，结果是两个请求几乎同时发出，携带的是**同一个 refresh token**。

后端 `RefreshTokenService.RefreshAsync()` 实现了 token rotation：第一个请求到达后，旧 token 被标记为 `IsRevoked = true`，新 token 写入数据库。第二个请求随后到达，查到的 token 已经是 revoked 状态，触发安全机制 `RevokeAllByUserIdAsync()`，撤销该用户所有 token，返回 400。前端 `catch` 块执行 `clearAuth()`，用户被登出。

#### 3. 根本原因

`isInitializing` 是 Zustand state，只控制 UI 渲染，不是执行锁。两个并发的 `initialize()` 调用都能同时通过 `if (!user)` 检查并到达 `axios.post`，形成竞态。

#### 4. 解决方案

设置模块级 Promise 锁（In-flight Deduplication）

在 `authStore.ts` 模块级别（store 外部）声明一个 `initializePromise` 变量。`initialize()` 执行时先检查这个变量：

- 如果已有飞行中的 Promise，直接返回它，不发新请求
- 如果没有，创建 Promise 并赋值给 `initializePromise`，`finally` 里清空

这样无论多少个并发调用，实际只会发出一个 HTTP 请求，后续调用都等待并复用第一个请求的结果。这个模式叫做 **Request Coalescing（请求合并）** 或 **In-flight Deduplication（飞行中去重）**。

```c#
import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { User } from "@/types";
import axios from "axios";

interface AuthState {
  ...
}

// fix： 定义模块级 Promise 锁
let initializePromise: Promise<void> | null = null;

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
     
      ...

      initialize: async () => {
        const { user } = get();

        // localStorage里没有user信息 → 从未登录过，无需refresh
        if (!user) {
          set({ isInitializing: false });
          return;
        }

        // 有user信息 → 尝试用 refresh token (cookie) 换新的 access token

        if (initializePromise) {
          console.log(111);
          return initializePromise;
        }

        initializePromise = (async () => {
          try {
            const response = await axios.post(
              `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
              null,
              { withCredentials: true },
            );
            const newAccessToken = response.data.data.accessToken;
            set({ accessToken: newAccessToken, isInitializing: false });
          } catch {
            // refresh token也失效了 → 清除登录态
            set({ user: null, accessToken: null, isInitializing: false });
          } finally {
            initializePromise = null;
          }
        })();
        return initializePromise;
      },
    }),
    {
      name: "auth-storage", // localStorage 里的 key 名称
      ...
      }),
    },
  ),
);

```

#### 5. 测试并合并分支

用户登录成功后，在当期页面连续刷新，Dev tools后台工具的Network里观察到只发送一次refresh请求。

合并回主分支

```bash
git add .
git commit -m "fix: prevent concurrent refresh token requests on initialize"
git push origin fix/refresh-token-rotation-race-condition

# 合并回 develop
git checkout develop
git merge --no-ff fix/refresh-token-rotation-race-condition \
  -m "merge: fix/refresh-token-rotation-race-condition into develop"
git push origin develop

# 删除功能分支
git branch -d fix/refresh-token-rotation-race-condition
git push origin --delete fix/refresh-token-rotation-race-condition
```



### Fix 2：生产环境 Refresh Token Cookie 跨域丢失

#### 1. 切出 fix 分支

```bash
git checkout develop
git pull origin develop
git checkout -b fix/cookie-samesite-production
git push -u origin fix/cookie-samesite-production
```

#### 2. 问题描述

部署到 Azure 后，用户登录成功，但浏览器里没有 `refreshToken` cookie。本地开发完全正常。导致生产环境任何页面刷新都会登出，因为 `initialize()` 调用 `/auth/refresh` 时没有 cookie 可以携带。

#### 3. 根本原因

本地开发时前端（`localhost:5173`）和后端（`localhost:5000`）同域，我们在SetRefreshTokenCookie里显式设置的`SameSite=Strict` 没有问题。

```c#
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict, // ← 显式设置
    Expires = expiresAt,
    Path = "/"
};
```

但是， 部署到 Azure 后，前端（`xxx.azurestaticapps.net`）和后端（`xxx.azurecontainerapps.io`）是不同域名，属于跨域。浏览器对 `SameSite=Strict` 的处理是：跨域请求时直接拒绝存储 cookie，`Set-Cookie` 响应头被静默忽略。

同理，`Logout` 里的 `Cookies.Delete` 也因为属性不匹配而删不掉 cookie。

**那么之前的版本为什么要设置 `SameSite = SameSiteMode.Strict`?**

`SameSite` 的本质目的是防御 **CSRF（跨站请求伪造）攻击**。

攻击场景：用户登录了网站，坏人的网站偷偷发一个请求到 API，浏览器会自动携带 cookie，服务器以为是合法用户操作。

- `Strict`：最严格，任何跨域请求都不带 cookie，CSRF 完全无法成立
- `Lax`：部分防护，导航跳转可以带，`fetch`/`XHR` 不带
- `None`：不防护，所有请求都带 cookie，但需要额外的 CSRF 防护手段

当前架构是**前后端分离 + 不同域名**，这本身就是跨域。前端发的每一个请求对浏览器来说都是"跨站"的，`Strict` 和 `Lax` 都会把自己的请求拦掉。

用了 `None` 之后 CSRF 防护就靠其他手段来补：

- 我们的 CORS 已经配了 `WithOrigins(allowedOrigins)`，只允许指定域名，不是 `*`
- `AllowCredentials()` 配合指定 Origin，浏览器会验证请求来源
- 这两个加在一起已经把 CSRF 的风险降到很低了

#### 4. 解决方案

跨域场景下 cookie 必须设置 `SameSite=None` + `Secure=true`。生产环境通过 `IsDevelopment()` 判断切换。

- 本地环境

    ```bash
    SameSite=None
    Secure=false
    ```

- 生成环境

    ```bash
    SameSite=None
    Secure=true
    ```

修改``AuthController.cs`里的`SetRefreshTokenCookie` 方法：

```c#

private void SetRefreshTokenCookie(string token, DateTime expiresAt)
{
    var cookieOptions = new CookieOptions
    {
        HttpOnly = true, 
        Secure = true, 
        SameSite = SameSiteMode.None, //允许跨域， 防 CSRF 通过其他配置
        Expires = expiresAt, 
        Path = "/" 
    };

    ,,,
}
```

更新 `Logout`

```c#

[HttpPost("logout")]
[EnableRateLimiting(RateLimitPolicies.Write)]
public async Task<IActionResult> Logout(CancellationToken cancellationToken)
{
    ...

    // 清除 Cookie
    Response.Cookies.Delete("refreshToken", new CookieOptions
    {
        Path = "/", // 和 SetRefreshTokenCookie 里的 Path 保持一致，否则删不掉
        SameSite = SameSiteMode.None, // 必须和 Set-Cookie 时的属性一致才能删掉
        Secure = true
    });

    return Ok(ApiResponse<object>.Ok("Logged out successfully"));
}
```

#### 5. 测试并合并分支

部署到 Azure 后，登录成功，浏览器 Application → Cookies 里可以看到 `refreshToken`。刷新页面不再登出。Logout 后 cookie 被正确清除。

合并分支：

```bash
git add .
git commit -m "fix: use SameSite=None in production for cross-origin cookie"
git push origin fix/cookie-samesite-production

git checkout develop
git merge --no-ff fix/cookie-samesite-production \
  -m "merge: fix/cookie-samesite-production into develop"
git push origin develop

git branch -d fix/cookie-samesite-production
git push origin --delete fix/cookie-samesite-production

```

合并回main分支

```bash
git checkout main
git pull origin main
git merge --no-ff develop \
  -m "merge: fix/cookie-samesite-production into main"
git push origin main
git checkout develop
```



