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

