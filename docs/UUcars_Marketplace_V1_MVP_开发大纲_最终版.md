# UUcars Marketplace V1（MVP）开发大纲 · 最终版

> **目标：构建一个可上线运行的二手车 C2C Marketplace API**
>
> 原则：**功能简单 · 架构完整 · 数据结构可扩展 · Git 流程规范**

---

## 技术栈

**后端：**

- ASP.NET Core (.NET 9)
- Entity Framework Core
- SQL Server
- JWT Authentication

**前端（V2 开始）：**

- React

**开发环境：**

- JetBrains Rider
- Docker SQL Server

---

## Git 分支策略

### 分支结构

```
main      ← 仅存放可部署的里程碑版本，打 tag，触发部署
develop   ← 日常开发基准线，所有 feature 从这里切出、合并回来
feature/* ← 单个模块/功能分支，完成后合并进 develop 后删除
```

### 里程碑合并计划（develop → main）

```
v0.1   Step 07 + 工程基础设施完成（数据库 + 中间件 + 日志）
v0.5   Step 26 完成（用户 + 车辆 + 收藏 + 订单核心业务可运行）
v1.0   Step 36 完成（全部功能 + 测试 + Docker 部署）
```

### 标准工作流

```bash
# 1. 切出 feature 分支
git checkout develop
git checkout -b feature/xxx

# 2. 开发过程中，每完成一个有意义的小单元 commit 一次
git add .
git commit -m "feat: ..."

# 3. 完成后合并回 develop（--no-ff 保留合并记录）
git checkout develop
git merge --no-ff feature/xxx -m "merge: feature/xxx into develop"
git push origin develop
git branch -d feature/xxx
git push origin --delete feature/xxx

# 4. 里程碑节点合并 main + 打 tag
git checkout main
git merge --no-ff develop -m "release: vX.X"
git tag -a vX.X -m "描述"
git push origin main
git push origin vX.X
```

### Commit Message 约定

```
feat:     新功能
fix:      修复 bug
test:     添加/修改测试
refactor: 重构（不改功能）
chore:    配置、依赖、脚本等
docs:     文档、注释
init:     项目初始化
merge:    分支合并
release:  版本发布
```

---

# 第一阶段：项目初始化（Project Setup）

> **Git 操作：直接在 `main` 完成初始 commit，然后切出 `develop`**

## Step 01 · 创建 Solution

创建：

```
UUcars.sln
UUcars.API
UUcars.Tests
```

学习点：

- Solution 结构
- Web API 模板

**Git 操作：**

```bash
# 第一个 commit 必须先建 .gitignore，防止敏感信息进入 Git
git init
git branch -M main

# .gitignore 内容（见下方）
git add .gitignore
git commit -m "init: add .gitignore"

git add .
git commit -m "init: create solution structure (UUcars.API + UUcars.Tests)"

# 连接远程 GitHub 仓库
git remote add origin https://github.com/你的账号/UUcars.git
git push -u origin main

# 从 main 切出 develop，所有后续开发在 develop 上进行
git checkout -b develop
git push -u origin develop
```

**.gitignore 内容：**

```gitignore
# .NET 构建产物
bin/
obj/

# IDE
.idea/
.vs/
*.user

# 环境配置（绝对不能进 Git）
appsettings.Production.json
secrets.json

# User Secrets（本地开发的 JWT Secret / 连接字符串）
# .NET User Secrets 实际存储在系统目录，但以防万一
**/secrets.json

# Docker
.dockerignore

# 日志文件（Serilog 输出）
logs/
*.log
```

> **说明：**
> - `appsettings.Development.json` 可以进 Git（只放非敏感的本地配置）
> - `appsettings.Production.json` 和 JWT Secret **绝不能进 Git**

笔记：`01_项目初始化`

---

## Step 02 · 项目架构设计

目录结构：

```
UUcars.API
│
├── Controllers
├── Services
├── Repositories
├── Entities
├── DTOs
│   ├── Requests
│   └── Responses
├── Data
├── Auth
├── Middleware
├── Extensions
├── Configurations
└── Program.cs
```

说明：

```
Controller  → 处理 HTTP 请求与响应
Service     → 业务逻辑
Repository  → 数据访问
DTO         → 接口输入输出结构
```

**Git 操作：**

```bash
git add .
git commit -m "init: scaffold folder structure"
```

笔记：`02_项目架构`

---

# 第二阶段：配置管理与工程基础设施

> **说明：这一阶段包含两类内容**
> - **配置管理**（Step 03）：真实项目第一天必须做对，后面补代价极高
> - **工程基础设施**（Step 03b-03c）：全局异常处理和日志也属于"地基"，必须在所有业务 API 开发之前搭好
>
> **Git 操作：本阶段在 `develop` 上直接 commit，无需 feature branch（属于项目骨架搭建）**

## Step 03 · 配置管理

### 连接字符串

放入 `appsettings.json`，禁止硬编码：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=UUcarsDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-at-least-32-characters",
    "ExpiresInMinutes": 60,
    "Issuer": "UUcars",
    "Audience": "UUcarsUsers"
  }
}
```

### 敏感信息保护（开发环境）

使用 .NET User Secrets，避免 Secret 进入 Git：

```bash
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:Secret" "your-secret-key"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
```

### 环境区分

```
appsettings.json              ← 通用配置（进 Git）
appsettings.Development.json  ← 开发环境（进 Git，不放敏感信息）
appsettings.Production.json   ← 生产环境（不提交 Git）
```

**Git 操作：**

```bash
git add .
git commit -m "chore: add configuration management & user secrets"
```

笔记：`03_配置管理`

---

## Step 03b · 全局异常处理 Middleware

> **为什么在业务开发前做？**
> 所有后续业务 Controller 的错误响应都依赖这个中间件。先建好再写业务，后面无需大规模回头改。

统一捕获未处理异常，返回标准错误格式：

```csharp
public class GlobalExceptionMiddleware
{
    // 捕获业务异常（NotFound / Conflict）→ 4xx
    // 捕获未知异常 → 500
}
```

---

## Step 03c · API 统一返回结构

> **同理，先定好格式，所有 Controller 从第一天起就用统一结构返回。**

定义：

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
```

分页列表专用结构：

```csharp
public class PagedResponse<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
```

---

## Step 03d · 日志系统（Serilog）

> **日志也是基础设施，配置一次全局生效，不需要等到功能做完再加。**

使用 Serilog：

```
结构化日志
输出到控制台 + 文件
记录请求/响应信息
记录异常堆栈
```

**Git 操作（Steps 03b-03d 一起 commit）：**

```bash
git add .
git commit -m "feat: global exception middleware + ApiResponse wrapper + Serilog"
```

笔记：`03b_工程基础设施`

---

# 第三阶段：数据库设计（核心）

> 这一阶段最重要，设计一个可升级的数据库模型。
>
> **Git 操作：切出 `feature/db-setup` 分支**

```bash
git checkout develop
git checkout -b feature/db-setup
```

## Step 04 · 数据库建模

### V1 表

```
Users
Cars
CarImages
Orders
Favorites
```

### V2 预留（不建表，只预留字段位置）

```
Reviews
Messages
```

---

### Users 表

```
Id                        int, PK, Identity
Username                  nvarchar(50), NOT NULL
Email                     nvarchar(100), NOT NULL, UNIQUE
PasswordHash              nvarchar(256), NOT NULL
Role                      nvarchar(20), NOT NULL  -- User / Admin
EmailConfirmed            bit, NOT NULL, DEFAULT 0
CreatedAt                 datetime2, NOT NULL
UpdatedAt                 datetime2, NOT NULL
```

V2 预留字段（可在 V2 Migration 添加）：

```
EmailConfirmationToken
ResetPasswordToken
ResetPasswordTokenExpiry
```

---

### Cars 表

```
Id                        int, PK, Identity
Title                     nvarchar(100), NOT NULL
Brand                     nvarchar(50), NOT NULL
Model                     nvarchar(50), NOT NULL
Year                      int, NOT NULL
Price                     decimal(18,2), NOT NULL
Mileage                   int, NOT NULL
Description               nvarchar(2000), NULL
SellerId                  int, FK → Users.Id, NOT NULL
Status                    nvarchar(20), NOT NULL
CreatedAt                 datetime2, NOT NULL
UpdatedAt                 datetime2, NOT NULL
```

**Status 完整枚举：**

```
Draft          卖家创建，草稿状态
PendingReview  卖家提交审核，等待 Admin 审核
Published      Admin 审核通过，公开可见
Sold           交易完成
Deleted        逻辑删除
```

**完整状态流转：**

```
卖家创建 → Draft
     ↓
卖家提交审核 → PendingReview
     ↓
Admin 审核通过 → Published
     ↓
买家下单成功 → Sold
     ↓
（违规/逻辑删除）→ Deleted
```

---

### CarImages 表

V1 只上传 URL，V2 做真实图片上传，表结构提前设计好：

```
Id                        int, PK, Identity
CarId                     int, FK → Cars.Id, NOT NULL
ImageUrl                  nvarchar(500), NOT NULL
SortOrder                 int, NOT NULL, DEFAULT 0
```

---

### Orders 表

```
Id                        int, PK, Identity
CarId                     int, FK → Cars.Id, NOT NULL
BuyerId                   int, FK → Users.Id, NOT NULL
SellerId                  int, FK → Users.Id, NOT NULL
Price                     decimal(18,2), NOT NULL
Status                    nvarchar(20), NOT NULL
CreatedAt                 datetime2, NOT NULL
UpdatedAt                 datetime2, NOT NULL
```

**说明：**

- `SellerId`：创建订单时从 Car 锁定，用于"我卖出的订单"查询
- `UpdatedAt`：订单状态变更时更新

Status：

```
Pending       待处理
Completed     交易完成
Cancelled     已取消
```

---

### Favorites 表

```
UserId                    int, FK → Users.Id, NOT NULL
CarId                     int, FK → Cars.Id, NOT NULL
CreatedAt                 datetime2, NOT NULL

PRIMARY KEY (UserId, CarId)   ← 联合主键，防重复收藏
```

**Git 操作：**

```bash
git add .
git commit -m "docs: database schema design"
```

笔记：`04_数据库建模`

---

## Step 05 · EF Core 实体设计

创建实体：

```
User
Car
CarImage
Order
Favorite
```

学习点：

- Navigation Property（导航属性）
- Foreign Key 配置
- Data Annotations vs Fluent API

**DTO 映射策略（统一约定）：**

V1 使用手动映射，在每个 Service 中明确写出 Entity → Response DTO 的转换：

```csharp
// 示例：Car → CarResponse
private static CarResponse MapToResponse(Car car) => new()
{
    Id = car.Id,
    Title = car.Title,
    Brand = car.Brand,
    Price = car.Price,
    // ...
};
```

优点：代码透明，学习阶段能看清每一步。V2 可视情况引入 AutoMapper。

**Git 操作：**

```bash
git add .
git commit -m "feat: EF Core entities (User, Car, CarImage, Order, Favorite)"
```

笔记：`05_实体设计`

---

## Step 06 · DbContext

创建 `AppDbContext`，配置：

```csharp
DbSet<User>
DbSet<Car>
DbSet<CarImage>
DbSet<Order>
DbSet<Favorite>
```

**Git 操作：**

```bash
git add .
git commit -m "feat: AppDbContext with all DbSets"
```

笔记：`06_DbContext`

---

## Step 07 · EF Core Migration + Admin Seed 数据

连接 Docker SQL Server，执行：

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

学习：

- `Add-Migration` / `Update-Database`
- Migration 文件结构

### Admin 初始用户的产生

> **注意：** `POST /auth/register` 只创建 `Role = "User"` 的普通用户。Admin 不通过注册接口产生，需要通过以下方式初始化：

在 `AppDbContext.OnModelCreating` 中使用 EF Core Seed Data：

```csharp
modelBuilder.Entity<User>().HasData(new User
{
    Id = 1,
    Username = "admin",
    Email = "admin@uucars.com",
    PasswordHash = "...", // 用 PasswordHasher 预先生成
    Role = "Admin",
    EmailConfirmed = true,
    CreatedAt = new DateTime(2024, 1, 1),
    UpdatedAt = new DateTime(2024, 1, 1)
});
```

执行完 migration 后，Admin 账号即存在于数据库中，可用于测试后续的 `/admin/*` 接口。

**Git 操作：**

```bash
git add .
git commit -m "feat: initial migration + admin seed data"

# 合并 feature/db-setup 回 develop
git checkout develop
git merge --no-ff feature/db-setup -m "merge: feature/db-setup into develop"
git push origin develop
git branch -d feature/db-setup
git push origin --delete feature/db-setup
```

> **里程碑 v0.1：** 数据库 + 配置 + 工程基础设施全部就绪，合并到 `main`：
>
> ```bash
> git checkout main
> git merge --no-ff develop -m "release: v0.1 - infrastructure ready"
> git tag -a v0.1 -m "DB + config + middleware + logging setup complete"
> git push origin main && git push origin v0.1
> git checkout develop
> ```

笔记：`07_EF迁移`

---

# 第四阶段：用户系统

> 目标：实现完整认证体系
>
> **Git 操作：切出 `feature/auth` 分支**

```bash
git checkout develop
git checkout -b feature/auth
```

## Step 08 · 用户注册

API：

```
POST /auth/register
```

流程：

```
验证 DTO（用户名/邮箱/密码格式）
↓
检查邮箱是否已被注册
↓
Hash 密码
↓
创建用户（Role = User，EmailConfirmed = false）
↓
返回注册成功
```

**Git 操作：**

```bash
git add .
git commit -m "feat: user registration endpoint"
```

笔记：`08_用户注册`

---

## Step 09 · 密码安全

使用 ASP.NET Core 内置：

```csharp
PasswordHasher<User>
```

- `HashPassword()`
- `VerifyHashedPassword()`

**Git 操作：**

```bash
git add .
git commit -m "feat: password hashing with PasswordHasher"
git commit -m "test: unit tests for UserService registration logic"
```

> 完成本步骤后，编写 `UserService` 注册逻辑的单元测试（Fake Repository）。

笔记：`09_密码Hash`

---

## Step 10 · 用户登录

API：

```
POST /auth/login
```

流程：

```
查询用户（by Email）
↓
验证密码 Hash
↓
生成 JWT Token
↓
返回 Token + 用户基本信息
```

**Git 操作：**

```bash
git add .
git commit -m "feat: user login endpoint + JWT token generation"
```

笔记：`10_用户登录`

---

## Step 11 · JWT 认证配置

配置：

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });
```

读取 `JwtSettings` 配置（来自 appsettings.json / User Secrets）。

**Git 操作：**

```bash
git add .
git commit -m "feat: JWT authentication middleware configuration"
```

笔记：`11_JWT认证`

---

## Step 12 · 当前用户信息

API：

```
GET /users/me       ← 需要登录
```

通过 JWT Claims 获取当前登录用户 Id，返回用户信息。

**Git 操作：**

```bash
git add .
git commit -m "feat: GET /users/me endpoint"
```

笔记：`12_当前用户`

---

## Step 13 · 修改用户信息

API：

```
PUT /users/me       ← 需要登录
```

支持修改：

```
Username
Email
```

**Git 操作：**

```bash
git add .
git commit -m "feat: PUT /users/me - update user profile"
git commit -m "test: unit tests for login / token generation"

# 合并 feature/auth 回 develop
git checkout develop
git merge --no-ff feature/auth -m "merge: feature/auth into develop"
git push origin develop
git branch -d feature/auth
git push origin --delete feature/auth
```

笔记：`13_修改用户`

---

# 第五阶段：车辆模块

> 项目核心模块
>
> **Git 操作：切出 `feature/cars` 分支**

```bash
git checkout develop
git checkout -b feature/cars
```

## Step 14 · 发布车辆（创建草稿）

API：

```
POST /cars          ← 需要登录
```

规则：

```
需要登录（JWT）
Status 默认为 Draft
SellerId = 当前登录用户 Id
```

**Git 操作：**

```bash
git add .
git commit -m "feat: POST /cars - create car draft"
```

笔记：`14_发布车辆`

---

## Step 15 · 提交审核

API：

```
POST /cars/{id}/submit    ← 需要登录
```

规则：

```
只有车主可以操作
只有 Draft 状态可以提交
Status: Draft → PendingReview
```

**Git 操作：**

```bash
git add .
git commit -m "feat: POST /cars/{id}/submit - submit for review"
```

笔记：`15_提交审核`

---

## Step 16 · 修改车辆

API：

```
PUT /cars/{id}      ← 需要登录
```

规则：

```
只有车主可以操作
只有 Draft 状态可以修改（PendingReview / Published 不可修改）
```

**Git 操作：**

```bash
git add .
git commit -m "feat: PUT /cars/{id} - edit car draft"
```

笔记：`16_修改车辆`

---

## Step 17 · 删除车辆（逻辑删除）

API：

```
DELETE /cars/{id}   ← 需要登录
```

规则：

```
只有车主可以操作
逻辑删除：Status = Deleted
已 Published / Sold 的车辆不允许删除
```

**Git 操作：**

```bash
git add .
git commit -m "feat: DELETE /cars/{id} - soft delete car"
```

笔记：`17_删除车辆`

---

## Step 18 · 车辆图片管理

> **说明：** CarImages 表在数据库已建好，这里补充对应的 API。V1 只支持 URL，不做真实文件上传（V2 扩展）。

API：

```
POST   /cars/{id}/images              ← 需要登录（车主）
DELETE /cars/{id}/images/{imageId}    ← 需要登录（车主）
```

规则：

```
只有车主可以添加/删除图片
只有 Draft 状态的车辆可以修改图片
ImageUrl 由客户端提交（V1 手动填写 URL）
```

**Git 操作：**

```bash
git add .
git commit -m "feat: car image management endpoints (add/delete)"
```

笔记：`18_车辆图片`

---

## Step 19 · 车辆列表（公开）

API：

```
GET /cars           ← 无需登录
```

**过滤规则（重要）：**

```
默认只返回 Status = Published 的车辆
Draft / PendingReview / Deleted / Sold 不出现在公开列表
```

支持分页，返回结构：

```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

**Git 操作：**

```bash
git add .
git commit -m "feat: GET /cars - public car listing with pagination"
```

笔记：`19_车辆列表`

---

## Step 20 · 我的车辆列表（卖家视角）

> **说明：** 公开列表只返回 Published 的车辆，卖家还需要看自己 Draft / PendingReview 状态的车辆。

API：

```
GET /cars/my-listings   ← 需要登录
```

返回当前登录用户作为卖家发布的所有车辆（含所有 Status，支持分页）。

**Git 操作：**

```bash
git add .
git commit -m "feat: GET /cars/my-listings - seller's own car list"
```

笔记：`20_我的车辆`

---

## Step 21 · 车辆搜索

在 `GET /cars` 基础上扩展 Query 参数：

```
GET /cars?brand=bmw&maxPrice=20000&minYear=2020&page=1&pageSize=20
```

过滤字段：

```
brand
minPrice / maxPrice
minYear / maxYear
```

学习点：

- `[FromQuery]` 参数模型绑定
- IQueryable 延迟查询（在数据库层过滤，不是拿到内存再过滤）

**Git 操作：**

```bash
git add .
git commit -m "feat: car search with query filters (brand/price/year)"
```

笔记：`21_车辆搜索`

---

## Step 22 · 车辆详情

API：

```
GET /cars/{id}
```

**访问权限规则：**

```
未登录用户：只能查看 Status = Published 的车辆
已登录的车主：可以查看自己的 Draft / PendingReview 状态车辆
Admin：可以查看所有状态
```

返回：

```
车辆信息（含 Status）
车辆图片列表
卖家基本信息（Id / Username）
```

**Git 操作：**

```bash
git add .
git commit -m "feat: GET /cars/{id} - car detail with permission rules"
git commit -m "test: CarService unit tests (status transitions, permissions)"

# 合并 feature/cars 回 develop
git checkout develop
git merge --no-ff feature/cars -m "merge: feature/cars into develop"
git push origin develop
git branch -d feature/cars
git push origin --delete feature/cars
```

笔记：`22_车辆详情`

---

# 第六阶段：收藏系统

> **Git 操作：切出 `feature/favorites` 分支**

```bash
git checkout develop
git checkout -b feature/favorites
```

## Step 23 · 收藏车辆

API：

```
POST /favorites/{carId}     ← 需要登录
```

规则：

```
只能收藏 Published 状态的车辆
同一用户不能重复收藏同一辆车（联合主键保证，返回 409）
```

---

## Step 24 · 取消收藏

API：

```
DELETE /favorites/{carId}   ← 需要登录
```

---

## Step 25 · 我的收藏列表

API：

```
GET /favorites              ← 需要登录
```

返回当前登录用户的收藏车辆列表（含分页）。

**Git 操作：**

```bash
git add .
git commit -m "feat: favorites system (add/remove/list)"

# 合并 feature/favorites 回 develop
git checkout develop
git merge --no-ff feature/favorites -m "merge: feature/favorites into develop"
git push origin develop
git branch -d feature/favorites
git push origin --delete feature/favorites
```

笔记：`23_收藏系统`

---

# 第七阶段：订单系统

> **Git 操作：切出 `feature/orders` 分支**

```bash
git checkout develop
git checkout -b feature/orders
```

## Step 26 · 创建订单

API：

```
POST /orders                ← 需要登录
```

流程：

```
验证车辆存在且 Status = Published
验证买家不是卖家本人
创建订单（BuyerId = 当前用户，SellerId 从 Car 获取，锁定 Price）
更新车辆状态：Published → Sold
```

**Git 操作：**

```bash
git add .
git commit -m "feat: POST /orders - create order with car status update"
```

笔记：`24_创建订单`

---

## Step 27 · 取消订单

API：

```
POST /orders/{id}/cancel    ← 需要登录
```

规则：

```
只有买家可以取消
只有 Pending 状态可以取消
取消后车辆状态恢复：Sold → Published
```

**Git 操作：**

```bash
git add .
git commit -m "feat: POST /orders/{id}/cancel - cancel order"
```

笔记：`25_取消订单`

---

## Step 28 · 我的订单

**买家视角：**

```
GET /orders/my-purchases    ← 需要登录
```

**卖家视角：**

```
GET /orders/my-sales        ← 需要登录
```

两个接口分别用 `BuyerId` 和 `SellerId` 查询（这就是为什么 Orders 表要存 SellerId）。

**Git 操作：**

```bash
git add .
git commit -m "feat: GET /orders/my-purchases + my-sales"
git commit -m "test: OrderService unit tests (create/cancel/edge cases)"

# 合并 feature/orders 回 develop
git checkout develop
git merge --no-ff feature/orders -m "merge: feature/orders into develop"
git push origin develop
git branch -d feature/orders
git push origin --delete feature/orders
```

> **里程碑 v0.5：** 用户 + 车辆 + 收藏 + 订单核心业务全部完成，合并到 `main`：
>
> ```bash
> git checkout main
> git merge --no-ff develop -m "release: v0.5 - core business modules complete"
> git tag -a v0.5 -m "Auth + Cars + Favorites + Orders all working"
> git push origin main && git push origin v0.5
> git checkout develop
> ```

笔记：`26_订单查询`

---

# 第八阶段：管理员系统

> **Git 操作：切出 `feature/admin` 分支**

```bash
git checkout develop
git checkout -b feature/admin
```

## Step 29 · 审核车辆（通过）

API：

```
POST /admin/cars/{id}/approve   ← [Authorize(Roles = "Admin")]
```

流程：

```
验证车辆 Status = PendingReview
Status → Published
```

---

## Step 30 · 审核车辆（拒绝）

API：

```
POST /admin/cars/{id}/reject    ← [Authorize(Roles = "Admin")]
```

流程：

```
验证车辆 Status = PendingReview
Status → Draft（退回卖家修改）
可选：记录拒绝原因（RejectReason 字段，V2 扩展）
```

---

## Step 31 · 待审核车辆列表

API：

```
GET /admin/cars/pending         ← [Authorize(Roles = "Admin")]
```

返回所有 `Status = PendingReview` 的车辆列表。

---

## Step 32 · 删除违规车辆

API：

```
DELETE /admin/cars/{id}         ← [Authorize(Roles = "Admin")]
```

逻辑删除：`Status = Deleted`

**Git 操作：**

```bash
git add .
git commit -m "feat: admin car approval/rejection flow"
git commit -m "feat: admin pending list + delete violation car"

# 合并 feature/admin 回 develop
git checkout develop
git merge --no-ff feature/admin -m "merge: feature/admin into develop"
git push origin develop
git branch -d feature/admin
git push origin --delete feature/admin
```

笔记：`27_管理员系统`

---

# 第九阶段：测试完善

> 注意：单元测试不集中在本阶段写，而是每完成一个 Service 就同步编写。
> 本阶段是系统性补全和回顾。
>
> **Git 操作：切出 `feature/tests` 分支**

```bash
git checkout develop
git checkout -b feature/tests
```

## Step 33 · 完善单元测试

补全以下 Service 的测试覆盖：

```
UserService   - 注册验证、密码 Hash
CarService    - 状态流转（Draft → PendingReview）、权限检查
OrderService  - 创建订单边界、取消订单、买家/卖家查询
```

使用：

```
xUnit
Fake Repository（不连真实数据库）
NullLogger
```

**Git 操作：**

```bash
git add .
git commit -m "test: complete unit test coverage for all Services"
```

笔记：`28_单元测试`

---

## Step 34 · 集成测试（可选进阶）

使用 `WebApplicationFactory` 测试完整 HTTP 请求链路：

```
注册 → 登录 → 发布车辆 → 创建订单
```

不依赖真实数据库（使用 InMemory DB 或 SQLite）。

**Git 操作：**

```bash
git add .
git commit -m "test: integration tests with WebApplicationFactory"

# 合并 feature/tests 回 develop
git checkout develop
git merge --no-ff feature/tests -m "merge: feature/tests into develop"
git push origin develop
git branch -d feature/tests
git push origin --delete feature/tests
```

笔记：`29_集成测试`

---

# 第十阶段：部署

> **Git 操作：切出 `feature/docker` 分支**

```bash
git checkout develop
git checkout -b feature/docker
```

## Step 35 · Docker 部署 API

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UUcars.API.dll"]
```

`docker-compose.yml` 同时启动 API + SQL Server。

**Git 操作：**

```bash
git add .
git commit -m "feat: Dockerfile + docker-compose for API + SQL Server"

# 合并 feature/docker 回 develop
git checkout develop
git merge --no-ff feature/docker -m "merge: feature/docker into develop"
git push origin develop
git branch -d feature/docker
git push origin --delete feature/docker
```

```bash
# 里程碑 v1.0：全部完成，合并到 main 打 tag
git checkout main
git merge --no-ff develop -m "release: v1.0 MVP complete"
git tag -a v1.0 -m "UUcars V1 MVP - full API + auth + tests + Docker"
git push origin main
git push origin v1.0
```

笔记：`30_Docker部署`

---

# V1 完成后你会得到

**完整系统功能：**

```
用户注册 / 登录 / JWT 认证
车辆发布 → 提交审核 → Admin 审核 → 上架
车辆搜索 / 分页 / 详情（含权限控制）
卖家管理自己的车辆（含图片）
收藏系统
订单系统（创建 / 取消 / 查询）
管理员审核体系
```

**技术能力：**

```
ASP.NET Core (.NET 9)
Entity Framework Core + Migration
SQL Server（Docker）
JWT Authentication + Role-based Authorization
REST API 设计规范
Repository / Service 分层架构
DTO 映射（手动）
全局异常处理 Middleware
统一 ApiResponse 结构
分页与搜索（IQueryable 延迟查询）
单元测试（xUnit + Fake Repository）
集成测试（WebApplicationFactory）
Docker 部署
Git 多分支工作流（main / develop / feature）
```

---

# 附录一：完整 API 清单

## 认证

```
POST   /auth/register             注册
POST   /auth/login                登录
```

## 用户

```
GET    /users/me                  当前用户信息（需登录）
PUT    /users/me                  修改用户信息（需登录）
```

## 车辆（公开）

```
GET    /cars                      公开车辆列表（仅 Published）
GET    /cars/{id}                 车辆详情（权限见 Step 22）
```

## 车辆（卖家）

```
POST   /cars                      创建草稿（需登录）
PUT    /cars/{id}                 修改草稿（需登录 + 车主）
DELETE /cars/{id}                 逻辑删除（需登录 + 车主）
POST   /cars/{id}/submit          提交审核（需登录 + 车主）
GET    /cars/my-listings          我发布的车辆列表（需登录）
POST   /cars/{id}/images          添加图片（需登录 + 车主）
DELETE /cars/{id}/images/{imgId}  删除图片（需登录 + 车主）
```

## 收藏

```
POST   /favorites/{carId}         收藏（需登录）
DELETE /favorites/{carId}         取消收藏（需登录）
GET    /favorites                 我的收藏列表（需登录）
```

## 订单

```
POST   /orders                    创建订单（需登录）
POST   /orders/{id}/cancel        取消订单（需登录 + 买家）
GET    /orders/my-purchases       我买的（需登录）
GET    /orders/my-sales           我卖的（需登录）
```

## 管理员

```
GET    /admin/cars/pending        待审核列表（Admin）
POST   /admin/cars/{id}/approve   审核通过（Admin）
POST   /admin/cars/{id}/reject    审核拒绝（Admin）
DELETE /admin/cars/{id}           删除违规车辆（Admin）
```

---

# 附录二：Git 操作速查

## 本地 + 远程初始化

```bash
git init && git branch -M main
git add .gitignore && git commit -m "init: add .gitignore"
git add . && git commit -m "init: create solution structure"
git remote add origin https://github.com/你的账号/UUcars.git
git push -u origin main
git checkout -b develop && git push -u origin develop
```

## 日常 feature 分支工作流

```bash
git checkout develop && git pull origin develop
git checkout -b feature/xxx
# ... 开发 + commit ...
git checkout develop
git merge --no-ff feature/xxx -m "merge: feature/xxx into develop"
git push origin develop
git branch -d feature/xxx && git push origin --delete feature/xxx
```

## 里程碑发布

```bash
git checkout main
git merge --no-ff develop -m "release: vX.X - 描述"
git tag -a vX.X -m "描述"
git push origin main && git push origin vX.X
git checkout develop
```

## 各模块对应分支汇总

| 分支 | 对应步骤 | 合并时机 |
|---|---|---|
| `main` 直接 | Step 01-02 | 项目骨架 commit |
| `develop` 直接 | Step 03-03d | 工程基础设施 |
| `feature/db-setup` | Step 04-07 | Step 07 完成 → v0.1 |
| `feature/auth` | Step 08-13 | Step 13 完成 |
| `feature/cars` | Step 14-22 | Step 22 完成 |
| `feature/favorites` | Step 23-25 | Step 25 完成 |
| `feature/orders` | Step 26-28 | Step 28 完成 → v0.5 |
| `feature/admin` | Step 29-32 | Step 32 完成 |
| `feature/tests` | Step 33-34 | Step 34 完成 |
| `feature/docker` | Step 35 | Step 35 完成 → v1.0 |

---

# 附录三：文件命名约定

```
Controller:    CarsController、UsersController、AdminController
Service:       CarService、UserService、OrderService、FavoriteService
Repository:    ICarRepository、EfCarRepository
Request DTO:   CarCreateRequest、CarQueryRequest、RegisterRequest
Response DTO:  CarResponse、CarDetailResponse、UserResponse
Entities:      Car、User、Order（不加后缀）
Exceptions:    CarNotFoundException、OrderConflictException
```

---

# 附录四：HTTP 状态码语义

```
200 OK           查询成功 / 操作成功
201 Created      创建成功
204 No Content   删除成功（无返回体）
400 Bad Request  请求参数错误 / 业务规则不满足
401 Unauthorized 未登录 / Token 无效
403 Forbidden    无权限（已登录但不是 Admin / 不是车主）
404 Not Found    资源不存在
409 Conflict     状态冲突（如重复收藏、订单状态已变更）
500 Internal     服务器错误
```

---

# V2：真实项目增强

> 功能更接近真实平台，前端 React 从此阶段开始。

## 1 · 邮箱验证注册

注册流程：

```
注册 → 发送验证邮件（SendGrid / Mailgun）→ 用户点击链接 → EmailConfirmed = true
```

数据库字段（已在 V1 预留）：

```
EmailConfirmed
EmailConfirmationToken
```

## 2 · 重置密码

```
POST /auth/forgot-password
POST /auth/reset-password
```

## 3 · 车辆图片上传（真实文件）

V1 存 ImageUrl（手动填写）。V2 实现真实文件上传：

- 本地存储（开发环境）
- 云存储：Amazon S3 / Azure Blob Storage（生产环境）

## 4 · React 前端

技术：React + TypeScript + Axios + React Router

页面：首页、车辆详情、用户中心、发布车辆、登录/注册

## 5 · 用户评价系统

买家可评价卖家：Reviews 表（Rating 1-5 星 + Comment）

## 6 · 私信系统（可选）

Messages 表 + Conversations 表

---

# V3：高级功能（加分项）

## OAuth 登录

Google / Facebook 登录（OAuth2 + OpenID Connect）

## 搜索优化

使用 Elasticsearch 替代 SQL LIKE 查询，支持全文搜索、拼写容错、模糊匹配。

## 推荐算法

猜你喜欢（基于收藏历史）、相似车辆推荐

## 实时聊天

使用 SignalR 实现买卖双方实时通讯
