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
| --------------- | --------------------- | ------------------- | ---- | ---- |
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
| ----------------- | ---------------------- | ------------------------------------------- | ------------------------------------------------ |
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

**单元测试里的 CarService 怎么处理 ICacheService？** 

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

再跑一遍：

```bash
dotnet build
dotnet test
```



### 12. Git 提交

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
✅ Git commit + 合并回 develop 完成
```
