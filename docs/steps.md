## Step 01 · 创建 Solution

### 这一步做什么

开始一个新项目，第一件事是把脚手架搭好——创建 Solution、建好项目文件、配置 Git。这些工作做一次就定下来了，后续所有开发都在这个基础上进行。


### 1. Solution 和 Project 的关系

在 .NET 里，**Solution（.sln）** 是一个容器，它本身不包含代码，只是把多个 **Project（.csproj）** 组织在一起管理。

UUcars 这个系统需要两个项目：

```
UUcars.sln
├── UUcars.API      ← 主项目，Web API 服务，所有业务代码在这里
└── UUcars.Tests    ← 测试项目，独立存在，依赖主项目但不污染它的依赖
```

测试项目从一开始就建好，是因为测试代码和业务代码是同步进行的——功能写完立刻写测试，而不是等项目快做完了再补。如果测试项目是后加的，还要处理项目引用、依赖等问题，不如一开始就建好。



### 2. 创建项目

打开终端，进入想存放项目的目录，依次执行：

```bash
# 创建 Solution，会生成一个 UUcars 文件夹
dotnet new sln -n UUcars

# 进入这个目录
cd UUcars

# 创建 Web API 主项目
# --use-controllers：指定使用 Controller 模式（而不是 Minimal API）
dotnet new webapi -n UUcars.API --use-controllers

# 创建测试项目（xUnit 是 .NET 最主流的测试框架）
dotnet new xunit -n UUcars.Tests

# 把两个项目注册进 Solution
dotnet sln add UUcars.API/UUcars.API.csproj
dotnet sln add UUcars.Tests/UUcars.Tests.csproj
```

> **为什么要加 `--use-controllers`？**
> .NET 6 之后，`webapi` 模板默认生成的是 Minimal API（直接在 `Program.cs` 里写路由处理逻辑）。我们要用的是 Controller 模式——每个业务模块有自己的 Controller 类，结构更清晰，也更适合这种有一定规模的项目。不加这个参数，模板生成的结构和我们要的不一样。

执行完之后，目录结构如下：

```
UUcars/
├── UUcars.sln
├── UUcars.API/
│   ├── Controllers/
│   │   └── WeatherForecastController.cs   ← 模板自带的示例
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── WeatherForecast.cs                 ← 模板自带的示例
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── UUcars.API.csproj
└── UUcars.Tests/
    ├── UnitTest1.cs                       ← 模板自带的示例
    └── UUcars.Tests.csproj
```

模板自带的 `WeatherForecast` 示例先不动，确认项目能正常运行之后再清理。



### 3. 验证项目能跑起来

```bash
cd UUcars.API
dotnet run
```

看到类似这样的输出说明启动成功：

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5085
```

打开浏览器访问：

```
http://localhost:5085/weatherforecast
```

返回了一串 JSON 天气数据，说明项目正常。按 `Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 4. 配置 Git

项目能跑起来之后，马上初始化 Git。**第一步必须先建 `.gitignore`**，这件事比写任何代码都要优先——一旦先 `git add .` 再补 `.gitignore`，构建产物或敏感配置可能已经被 Git 追踪了，事后处理很麻烦。

在项目根目录（`UUcars/`）创建 `.gitignore`：

```gitignore
# .NET 构建产物（每次 build 都会重新生成，不需要进 Git）
bin/
obj/

# JetBrains Rider IDE 文件
.idea/
*.user

# Visual Studio
.vs/

# 环境配置文件（包含数据库密码、JWT Secret 等敏感信息，绝不能进 Git）
appsettings.Production.json
secrets.json
**/secrets.json

# 日志文件（运行时生成）
logs/
*.log

# macOS
.DS_Store
```

> **`appsettings.Development.json` 不在排除列表里**，因为它只存放非敏感的本地开发配置（比如日志级别），可以进 Git。真正敏感的信息（数据库密码、JWT Secret）会用另一种机制处理，后面配置的时候会讲。

`.gitignore` 建好之后，初始化 Git：

```bash
# 初始化仓库，默认分支命名为 main
git init
git branch -M main

# 第一个 commit 只提交 .gitignore
# 原因：确保 Git 从一开始就知道哪些文件不该追踪
git add .gitignore
git commit -m "init: add .gitignore"

# 第二个 commit 提交整个项目骨架
git add .
git commit -m "init: create solution structure (UUcars.API + UUcars.Tests)"
```



### 5. 连接远程仓库

去 GitHub 新建一个空仓库，名字叫 `UUcars`。**不要勾选任何初始化选项**（不加 README、不加 .gitignore、不选 License），因为本地已经有了，远程必须是空的才能推上去。

建好之后：

```bash
# 关联远程仓库（替换成自己的地址）
git remote add origin https://github.com/的账号/UUcars.git

# 把 main 推到远程
git push -u origin main
```



### 6. 建立 develop 分支

`main` 分支的定位是**只存放可以部署的稳定版本**，不在上面直接写代码。日常开发在 `develop` 分支上进行：

```bash
# 从 main 切出 develop
git checkout -b develop

# 推到远程
git push -u origin develop
```

现在本地和远程都有了两个分支：

```
main     ← 只在发布里程碑版本时才动
develop  ← 日常开发的基准线，所有后续工作从这里开始
```

**后续所有开发都在 `develop` 或从 `develop` 切出的功能分支上进行，不直接动 `main`。**



### Step 01 完成状态

```
✅ Solution 结构建立（UUcars.API + UUcars.Tests）
✅ 项目可以正常运行
✅ .gitignore 已配置（第一个 commit）
✅ Git 本地 + 远程初始化完成
✅ main / develop 双分支建立
```



## Step 02 · 项目架构设计

### 这一步做什么

Step 01 把项目骨架建好了，但现在有两个问题：

1. 模板自带的示例代码（`WeatherForecast`）还留着，需要清理
2. 项目目录是空的，后续所有代码往哪里放还没有规划

这一步结束后，项目应该是一个**干净的、有明确结构的空壳**——没有多余代码，所有目录各就各位，后续每一步只需要往对应位置填内容，不用再想"这个文件该放哪"。



### 1. 确认当前分支

```bash
git branch
```

确认显示 `* develop`，在正确的分支上再继续。



### 2. 清理模板文件

模板生成的示例代码对我们没有用，先删掉：

```bash
# 删除 WeatherForecast 相关文件
rm UUcars.API/Controllers/WeatherForecastController.cs
rm UUcars.API/WeatherForecast.cs

# 删除测试项目里的模板测试
rm UUcars.Tests/UnitTest1.cs
```



### 3. 规划目录结构

UUcars 是一个有用户系统、车辆模块、订单系统等多个业务模块的项目。随着代码量增长，如果没有清晰的目录规划，文件会越来越难找，不同职责的代码也容易混在一起。

现在就把目录结构定下来。每个目录对应一种明确的职责：

```
UUcars.API/
├── Controllers/      HTTP 入口，处理请求和响应，调用 Service
├── Services/         业务逻辑，编排业务流程，判断业务规则
├── Repositories/     数据访问，所有和数据库打交道的代码
├── Entities/         EF Core 实体类，对应数据库里的表
├── DTOs/
│   ├── Requests/     客户端传进来的数据结构
│   └── Responses/    接口返回给客户端的数据结构
├── Data/             AppDbContext，EF Core 的数据库上下文
├── Auth/             JWT 生成、Token 解析等认证相关
├── Middleware/       自定义中间件（如全局异常处理）
├── Extensions/       Program.cs 服务注册的扩展方法
├── Configurations/   EF Core Fluent API 配置类
└── Program.cs
```

> **为什么要把 Requests 和 Responses 分开放？** 因为两者的职责完全不同。`Requests` 是约束"客户端能传什么"，`Responses` 是约束"接口返回什么"。同一个业务资源（比如 Car）的创建请求、查询请求、详情响应、列表响应字段都不一样，分开放不会混淆。

> **Controller 和 Service 为什么要分开？** Controller 只负责 HTTP 层面的事——解析请求参数、调用 Service、把结果包装成响应返回。它不应该包含任何业务判断逻辑。Service 只负责业务逻辑——"这辆车能不能被删除"、"买家不能购买自己的车"这类规则在 Service 里判断，Service 不关心 HTTP 状态码。
>
> 这样分层的最直接好处：Service 可以单独做单元测试，不依赖 HTTP 请求，测试又快又稳定。



### 4. 创建目录

```bash
cd UUcars.API

mkdir -p Controllers
mkdir -p Services
mkdir -p Repositories
mkdir -p Entities
mkdir -p DTOs/Requests
mkdir -p DTOs/Responses
mkdir -p Data
mkdir -p Auth
mkdir -p Middleware
mkdir -p Extensions
mkdir -p Configurations

cd ..
```

> **Git 不追踪空目录。** 纯空目录不会出现在 `git status` 里，也不会被提交。为了把目录结构保存进 Git，每个目录里放一个 `.gitkeep` 占位文件——这是业界通用的约定，文件名表示"保留这个目录"。等后续步骤往目录里加了真实文件，`.gitkeep` 就可以删掉了。

```bash
touch UUcars.API/Controllers/.gitkeep
touch UUcars.API/Services/.gitkeep
touch UUcars.API/Repositories/.gitkeep
touch UUcars.API/Entities/.gitkeep
touch UUcars.API/DTOs/Requests/.gitkeep
touch UUcars.API/DTOs/Responses/.gitkeep
touch UUcars.API/Data/.gitkeep
touch UUcars.API/Auth/.gitkeep
touch UUcars.API/Middleware/.gitkeep
touch UUcars.API/Extensions/.gitkeep
touch UUcars.API/Configurations/.gitkeep
```



### 5. 让测试项目引用主项目

测试项目需要能访问主项目里的类（Service、Repository 等），靠 **project reference** 实现：

```bash
dotnet add UUcars.Tests/UUcars.Tests.csproj reference UUcars.API/UUcars.API.csproj
```

执行完之后，打开 `UUcars.Tests/UUcars.Tests.csproj` 确认引用已加上：

```xml
<ItemGroup>
  <ProjectReference Include="..\UUcars.API\UUcars.API.csproj" />
</ItemGroup>
```



### 6. 整理 Program.cs

现在 `Program.cs` 里还有跟 `WeatherForecast` 相关的东西，文件已经删了但 `Program.cs` 里的引用还在。趁这一步一起清理干净，改成一个最简洁的起点：

```csharp
var builder = WebApplication.CreateBuilder(args);

// =============================================
// 服务注册
// =============================================
builder.Services.AddControllers();

// =============================================
// 构建应用
// =============================================
var app = builder.Build();

// =============================================
// 中间件管道
// =============================================
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

注释只是占位提示，后续每个步骤会在对应位置逐步填充内容，不会一下子把所有东西堆进来。

> **模板里有 `AddOpenApi()` 和 `MapOpenApi()`，为什么现在移除？** 模板默认用的是 .NET 9 官方的 OpenAPI（只生成 JSON 文档，没有 UI 界面）。我们后面会用 Scalar 作为 API 文档工具，它提供更好的界面，配置也和 .NET 9 的官方 OpenAPI 库直接集成。现在先把模板的清掉，等配置 Scalar 的时候一步到位，不留冗余配置。



### 7. 验证编译

```bash
dotnet build
```

预期输出：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 8. 此时的目录结构

```
UUcars/
├── UUcars.sln
├── .gitignore
├── UUcars.API/
│   ├── Controllers/
│   │   └── .gitkeep
│   ├── Services/
│   │   └── .gitkeep
│   ├── Repositories/
│   │   └── .gitkeep
│   ├── Entities/
│   │   └── .gitkeep
│   ├── DTOs/
│   │   ├── Requests/
│   │   │   └── .gitkeep
│   │   └── Responses/
│   │       └── .gitkeep
│   ├── Data/
│   │   └── .gitkeep
│   ├── Auth/
│   │   └── .gitkeep
│   ├── Middleware/
│   │   └── .gitkeep
│   ├── Extensions/
│   │   └── .gitkeep
│   ├── Configurations/
│   │   └── .gitkeep
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── UUcars.API.csproj
└── UUcars.Tests/
    └── UUcars.Tests.csproj
```



### 9. Git 提交

```bash
git add .
git commit -m "init: scaffold folder structure & clean up templates"
```



### Step 02 完成状态

```
✅ 模板示例代码清理完毕
✅ 目录结构按规划建立，职责划分清晰
✅ Tests 项目已引用 API 项目
✅ Program.cs 清理为干净起点
✅ dotnet build 通过，无报错
✅ Git commit 完成
```



## Step 03 · 配置管理与工程基础设施

### 这一步做什么

Step 02 把目录结构定好了。在开始写任何业务代码之前，有几件基础设施的事必须先做好——配置管理、统一的错误处理、统一的响应格式、日志。

为什么要在业务代码之前做这些？

以错误处理为例：如果先写了十几个 Controller，再来补统一的错误处理，就要回头逐个检查和修改。日志也一样，项目跑起来的第一天就应该有日志，不是最后才加的装饰品。这类东西越晚加，改动成本越高，所以现在一次性打好地基。



### 1. 确认当前分支

```bash
git branch   # 确认显示 * develop
```

这一阶段直接在 `develop` 上工作，不需要切功能分支——这些是项目骨架，不是某个具体功能。



### 2. 安装 NuGet 包

先把这一阶段需要的包装好：

```bash
# Scalar：API 文档 UI
# .NET 9 官方的 OpenAPI 库负责生成文档 JSON，Scalar 负责把它渲染成好看的界面
dotnet add UUcars.API/UUcars.API.csproj package Scalar.AspNetCore

# Serilog：结构化日志
# Serilog.AspNetCore 是主包，内置了 Console 输出和配置文件读取
# Serilog.Sinks.File 是额外的文件输出
dotnet add UUcars.API/UUcars.API.csproj package Serilog.AspNetCore
dotnet add UUcars.API/UUcars.API.csproj package Serilog.Sinks.File
```

> **`Microsoft.AspNetCore.OpenApi` 模板已经装好了，不用重复安装。** Scalar 依赖它生成 `/openapi/v1.json` 文档 JSON，两者配合使用。

装完之后确认 `UUcars.API.csproj` 里的包引用：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.x" />
  <PackageReference Include="Scalar.AspNetCore" Version="x.x.x" />
  <PackageReference Include="Serilog.AspNetCore" Version="x.x.x" />
  <PackageReference Include="Serilog.Sinks.File" Version="x.x.x" />
</ItemGroup>
```



### 3. 配置管理

#### 3.1 更新 `appsettings.json`

项目会用到两块外部配置：数据库连接字符串和 JWT 设置。现在把结构定好，同时加上 Serilog 的日志级别配置：

打开 `UUcars.API/appsettings.json`，替换为：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=UUcarsDB;User Id=sa;Password=PLACEHOLDER;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS",
    "ExpiresInMinutes": 60,
    "Issuer": "UUcars",
    "Audience": "UUcarsUsers"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  },
  "AllowedHosts": "*"
}
```

> **密码和 Secret 为什么写占位符？** `appsettings.json` 会进入 Git。真实的数据库密码和 JWT Secret 绝不能出现在这个文件里。这里写的占位符只是占个位置，真实值通过 User Secrets 在本地覆盖（下面会配置）。
>
> **`Microsoft.AspNetCore` 和 `Microsoft.EntityFrameworkCore` 设为 `Warning`：** 这两个命名空间下框架自身的日志非常多，在生产环境只需要看 Warning 以上的。开发环境另外配置。



#### 3.2 更新 `appsettings.Development.json`

开发环境需要更详细的日志，特别是能看到 EF Core 生成的 SQL 语句，方便调试：

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

> **`EntityFrameworkCore.Database.Command: Information`** 这一行会让开发时控制台输出 EF Core 执行的每一条 SQL 语句。能直接看到 ORM 在后台做什么，对调试和学习都很有帮助。生产环境不需要这个。



#### 3.3 建立 JwtSettings 配置绑定类

JWT 相关的配置有四个字段（Secret、过期时间、Issuer、Audience），如果每次都用 `Configuration["JwtSettings:Secret"]` 这样的字符串来读，散落在代码各处很难维护。

更好的做法是建一个和配置结构对应的 C# 类，通过 ASP.NET Core 的 Options 模式注入——强类型访问，IDE 有补全，字段改名时编译器会提醒。

```bash
touch UUcars.API/Auth/JwtSettings.cs
rm UUcars.API/Auth/.gitkeep
namespace UUcars.API.Auth;

// 对应 appsettings.json 中的 "JwtSettings" 配置节
// 通过 Options 模式注入到需要的地方（Step 11 配置 JWT 认证时使用）
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 60;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
```



#### 3.4 配置 User Secrets

User Secrets 是 .NET 开发环境专门用来保护敏感信息的机制。配置值存储在系统用户目录下，**不在项目文件夹里**，所以永远不会进入 Git：

```bash
cd UUcars.API

# 初始化 User Secrets（在 .csproj 里添加一个唯一的 UserSecretsId）
dotnet user-secrets init

# 设置本地开发用的数据库连接字符串（覆盖 appsettings.json 里的占位符）
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=UUcarsDB;User Id=sa;Password=YourStrongPassw0rd;TrustServerCertificate=True"

# 设置 JWT Secret（必须足够长，至少 32 个字符，否则 JWT 签名会报错）
dotnet user-secrets set "JwtSettings:Secret" "uucars-dev-secret-key-at-least-32-chars"

cd ..
```

验证设置是否生效：

```bash
cd UUcars.API && dotnet user-secrets list && cd ..
```

应该看到：

```
ConnectionStrings:DefaultConnection = Server=localhost,1433;...
JwtSettings:Secret = uucars-dev-secret-key-at-least-32-chars
```

> **User Secrets 存在哪里？**
>
> - macOS / Linux：`~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
> - Windows：`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
>
> ASP.NET Core 在开发环境启动时会自动读取这个文件，用里面的值覆盖 `appsettings.json` 里对应的键。生产环境不走这个机制，生产环境的密钥通过环境变量或密钥管理服务注入。



### 4. 统一响应结构

现在来想一个问题：API 返回的数据格式应该是什么样的？

如果不约定，不同接口可能长这样：

```json
// 接口 A 成功时
{ "id": 1, "title": "宝马3系" }

// 接口 B 成功时
{ "data": { "id": 1 }, "status": "ok" }

// 接口 C 失败时
{ "error": "Not found" }

// 接口 D 失败时
{ "message": "Car not found", "code": 404 }
```

前端要对接这四种格式，每个接口都要单独处理，非常痛苦。

统一成一种格式，所有接口都长一样：

```json
// 成功
{ "success": true, "data": { ... }, "message": null }

// 失败
{ "success": false, "data": null, "message": "Car not found" }
```

前端只需要判断 `success` 字段，就知道怎么处理。

新建两个文件。先建通用响应结构：

```bash
touch UUcars.API/DTOs/ApiResponse.cs
namespace UUcars.API.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    // 静态工厂方法：让 Controller 里的写法更简洁
    // 用法：return Ok(ApiResponse<CarResponse>.Ok(car));
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors
    };
}
```

再建分页专用结构：

```bash
touch UUcars.API/DTOs/PagedResponse.cs
namespace UUcars.API.DTOs;

// 专门用于列表接口的分页响应
// 使用时嵌套在 ApiResponse 里：ApiResponse<PagedResponse<CarResponse>>
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    // TotalPages 根据 TotalCount 和 PageSize 自动计算，不需要调用方传入
    public static PagedResponse<T> Create(List<T> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}
```



### 5. 业务异常基类

当业务规则不满足时，比如"要购买的车辆不存在"或"订单已经被取消了不能再取消"，代码需要一种方式来表达这些错误。

最常见的做法是抛异常，但直接抛 `Exception` 太粗糙，没办法区分是业务错误（应该返回 4xx）还是真正的程序 bug（应该返回 500）。

建一个业务异常基类，所有业务错误都继承它：

```bash
mkdir -p UUcars.API/Exceptions
touch UUcars.API/Exceptions/AppException.cs
namespace UUcars.API.Exceptions;

// 业务异常基类
// Service 层抛出继承自这个类的异常，
// 中间件统一捕获并转换成对应的 HTTP 响应
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
```

> **为什么 Service 层要用异常而不是返回错误码？** Service 层的方法如果要同时返回"结果"和"是否出错"，签名会变得很复杂（比如返回 `(Car? result, string? error)`）。用异常更自然——正常路径就是正常返回，出错路径直接抛出，调用方代码干净清晰。中间件在最外层统一兜住，不需要每个 Controller 都写 try-catch。



### 6. 全局异常处理中间件

有了异常基类，现在建中间件来统一处理它：

```bash
touch UUcars.API/Middleware/GlobalExceptionMiddleware.cs
rm UUcars.API/Middleware/.gitkeep
using System.Text.Json;
using UUcars.API.DTOs;
using UUcars.API.Exceptions;

namespace UUcars.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // 业务异常：预期内的情况，记录 Warning 即可
            _logger.LogWarning("Business exception [{StatusCode}]: {Message}",
                ex.StatusCode, ex.Message);
            await WriteErrorResponseAsync(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            // 未预期的异常：记录完整堆栈，返回 500
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message);

        // 使用 camelCase，和 Controller 正常返回的 JSON 格式保持一致
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
```

> **中间件的套娃结构：** ASP.NET Core 的中间件管道像俄罗斯套娃，请求进来时从外层依次往内走，响应时再从内层往外返回。`GlobalExceptionMiddleware` 必须注册在最外层，才能兜住所有内层中间件和 Controller 抛出的异常。



### 7. 把所有东西串进 Program.cs

现在把前面建好的所有基础设施都接进来，更新 `Program.cs`：

```csharp
using Scalar.AspNetCore;
using Serilog;
using UUcars.API.Auth;
using UUcars.API.Middleware;

// =============================================
// Bootstrap Logger
// 在 builder 构建之前就启动一个临时 Logger
// 用途：如果 builder 阶段本身出错（比如配置文件格式错误），
//       也能把错误信息记录下来
// =============================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // =============================================
    // 替换默认日志系统为 Serilog
    // ReadFrom.Configuration：从 appsettings.json 读取日志级别等配置
    // WriteTo.Console / File：输出目标
    // =============================================
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/uucars-.log",
                rollingInterval: RollingInterval.Day,  // 每天滚动一个新文件
                retainedFileCountLimit: 30             // 最多保留 30 天
            )
    );

    // =============================================
    // 服务注册
    // =============================================
    builder.Services.AddControllers();

    // OpenAPI + Scalar（API 文档）
    builder.Services.AddOpenApi();

    // 将 appsettings.json 中的 JwtSettings 节绑定到 JwtSettings 类
    // 后续需要 JWT 配置的地方通过 IOptions<JwtSettings> 注入
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings")
    );

    // =============================================
    // 构建应用
    // =============================================
    var app = builder.Build();

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

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // 捕获启动阶段的致命错误（配置错误、数据库连不上等）
    Log.Fatal(ex, "Application terminated unexpectedly during startup");
}
finally
{
    // 确保程序退出前把缓冲区里的日志全部写出去
    Log.CloseAndFlush();
}
```



### 8. 验证

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

启动项目，确认 Scalar 文档页面能打开：

```bash
cd UUcars.API && dotnet run
```

打开浏览器访问（端口号看启动输出里的地址）：

```
https://localhost:{端口}/scalar/v1
```

能看到 Scalar 的 Moon 主题深色界面，说明配置正确。目前没有任何 API，界面是空的，这是正常的。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 9. 此时的目录变化

```
UUcars.API/
├── Auth/
│   └── JwtSettings.cs                    ← 新增
├── DTOs/
│   ├── ApiResponse.cs                    ← 新增
│   ├── PagedResponse.cs                  ← 新增
│   ├── Requests/
│   │   └── .gitkeep
│   └── Responses/
│       └── .gitkeep
├── Exceptions/                           ← 新增目录
│   └── AppException.cs                   ← 新增
├── Middleware/
│   └── GlobalExceptionMiddleware.cs      ← 新增
├── Program.cs                            ← 已更新
├── appsettings.json                      ← 已更新
└── appsettings.Development.json          ← 已更新
```



### 10. Git 提交

```bash
git add .
git commit -m "feat: config management + ApiResponse + exception middleware + Serilog"
```



### Step 03 完成状态

```
✅ appsettings.json 配置结构建好（连接字符串、JWT、Serilog 级别）
✅ appsettings.Development.json 开发环境日志更详细
✅ User Secrets 初始化，敏感信息不进 Git
✅ JwtSettings 配置绑定类建立
✅ ApiResponse<T> 和 PagedResponse<T> 统一响应格式就绪
✅ AppException 业务异常基类建立
✅ GlobalExceptionMiddleware 挂载在管道最外层
✅ Serilog 配置完成（控制台 + 文件按天滚动）
✅ Scalar API 文档可访问
✅ dotnet build 通过，无报错
✅ Git commit 完成
```



## Step 04 · 数据库建模

### 这一步做什么
工程基础设施搭好了，现在进入数据库设计阶段。

在动手写任何实体代码之前，先要把整个数据库的结构想清楚：

- 需要哪些表？
- 每张表有哪些字段？
- 表与表之间什么关系？

设计想清楚了，后面写代码才不会反复改。



### 1. 切出 feature/db-setup 分支

数据库相关的工作（建模、实体、DbContext、Migration）是一个完整的功能块，单独开一个分支来做：

```bash
git checkout develop
git checkout -b feature/db-setup
git push -u origin feature/db-setup
```



### 2. 这个系统需要哪些表

UUcars 是一个二手车 C2C 平台，核心业务流程是：

```
卖家注册账号
    ↓
发布车辆（先是草稿）→ 提交审核 → Admin 审核通过 → 公开上架
    ↓
买家浏览 / 搜索 / 收藏车辆
    ↓
买家下单 → 交易完成
```

从这个流程出发，自然推导出需要五张表：

```
Users      所有用户（买家、卖家、Admin 都是用户，用 Role 字段区分）
Cars       车辆信息（每辆车归属于一个卖家）
CarImages  车辆图片（一辆车可以有多张图片，单独一张表）
Orders     订单（买家对某辆车下单产生）
Favorites  收藏关系（某用户收藏了某辆车）
```

表与表之间的关系：

```
Users  ──<  Cars         一个用户可以发布多辆车（一对多）
Cars   ──<  CarImages    一辆车可以有多张图片（一对多）
Users  ──<  Orders       一个用户可以有多个订单（一对多）
Cars   ──<  Orders       一辆车可以有多条订单记录（一对多）
Users  >──< Cars         用户和车辆之间有收藏关系（多对多，通过 Favorites 实现）
```



### 3. 逐表设计字段

#### Users 表

```
Id                int,           PK, Identity（自增主键）
Username          nvarchar(50),  NOT NULL
Email             nvarchar(100), NOT NULL, UNIQUE（登录凭证，不能重复）
PasswordHash      nvarchar(256), NOT NULL（存 Hash 后的密码，绝不存明文）
Role              nvarchar(20),  NOT NULL（User / Admin）
EmailConfirmed    bit,           NOT NULL, DEFAULT 0
CreatedAt         datetime2,     NOT NULL
UpdatedAt         datetime2,     NOT NULL
```

`Role` 决定了这个用户是普通用户还是管理员。买家和卖家都是 `User`——在这个平台上，同一个人既可以卖车也可以买车，不需要区分角色。

`EmailConfirmed` 是一个**状态标记字段**，只是一个 `bool`。V1 注册时直接写入 `false`，一行代码的事，不依赖任何额外功能。它存在的价值是：

- V1 的数据从第一天起就有这个标记，所有用户都是 `false`
- V2 做邮箱验证时，功能做完直接把对应用户的这个字段改成 `true` 就行
- 如果 V1 不加，V2 Migration 加字段时还需要考虑给存量用户设默认值，多一个麻烦



#### Cars 表

```
Id            int,             PK, Identity
Title         nvarchar(100),   NOT NULL（车辆标题，如"2020款宝马3系 里程少无事故"）
Brand         nvarchar(50),    NOT NULL（品牌，如 BMW、Toyota）
Model         nvarchar(50),    NOT NULL（车型，如 3 Series、Camry）
Year          int,             NOT NULL（出厂年份）
Price         decimal(18,2),   NOT NULL（售价）
Mileage       int,             NOT NULL（里程，单位：公里）
Description   nvarchar(2000),  NULL（详细描述，允许为空）
SellerId      int,             FK → Users.Id, NOT NULL
Status        nvarchar(20),    NOT NULL
CreatedAt     datetime2,       NOT NULL
UpdatedAt     datetime2,       NOT NULL
```

`Status` 是这张表里最关键的字段，贯穿整个业务流程：

```
卖家创建          → Draft          （草稿，只有卖家自己能看到）
卖家提交审核      → PendingReview  （等待 Admin 处理）
Admin 审核通过    → Published      （公开上架，买家可以下单）
Admin 审核拒绝    → Draft          （退回卖家修改，重新走流程）
买家下单成功      → Sold           （已售出，不再接受新订单）
违规或主动删除    → Deleted        （逻辑删除，数据还在但不可见）
```

注意这个流转是有约束的——不是任意两个状态之间都能转换。比如 `Sold` 不能退回 `Published`（除非买家取消订单才恢复），`Published` 不能直接改成 `Draft`。这些业务规则后面在 Service 层用代码来保证。



#### CarImages 表

```
Id          int,            PK, Identity
CarId       int,            FK → Cars.Id, NOT NULL
ImageUrl    nvarchar(500),  NOT NULL（V1 直接存 URL）
SortOrder   int,            NOT NULL, DEFAULT 0（图片排列顺序）
```

图片和车辆分开存的原因：一辆车通常有多张图片，如果把图片 URL 存在 Cars 表里（比如用逗号分隔），查询和修改都很麻烦，也没法控制顺序。单独一张表，每张图片一行，`SortOrder` 控制显示顺序，第一张作为封面图。

这张表没有 `CreatedAt` / `UpdatedAt`，图片是车辆的附属资源，不需要时间追踪。



#### Orders 表

```
Id          int,            PK, Identity
CarId       int,            FK → Cars.Id, NOT NULL
BuyerId     int,            FK → Users.Id, NOT NULL
SellerId    int,            FK → Users.Id, NOT NULL
Price       decimal(18,2),  NOT NULL
Status      nvarchar(20),   NOT NULL
CreatedAt   datetime2,      NOT NULL
UpdatedAt   datetime2,      NOT NULL
```

**`SellerId` 为什么要单独存？**

`SellerId` 信息本来可以通过 `CarId` 关联 `Cars` 表查到，但我们选择在 Orders 里冗余存一份。原因是：买家查"我买的订单"和卖家查"我卖的订单"是高频操作，如果 `SellerId` 不在 Orders 表里，每次查卖家视角的订单都要先 JOIN Cars 表，多一次关联。冗余存一份，直接按 `SellerId` 过滤，查询简单高效。

**`Price` 为什么要单独存？**

订单创建时，把当时的成交价格锁定在订单里。如果只记录 `CarId`，卖家后来修改了车辆价格，历史订单的价格就会跟着变，这显然是错的。

`Status` 的流转：

```
买家下单      → Pending    （待处理）
交易完成      → Completed  
买家取消      → Cancelled  （同时车辆状态恢复 Published）
```



#### Favorites 表

```
UserId       int,        FK → Users.Id, NOT NULL
CarId        int,        FK → Cars.Id, NOT NULL
CreatedAt    datetime2,  NOT NULL

PRIMARY KEY (UserId, CarId)
```

这张表实现的是用户和车辆之间的多对多收藏关系。用 `(UserId, CarId)` 联合主键，而不是单独设一个 `Id` 字段——联合主键本身就保证了同一个用户不能重复收藏同一辆车，数据库层面天然防重。



### 4. 用枚举表达 Status 字段

设计里有三个字段是有限的字符串集合：`Cars.Status`、`Orders.Status`、`Users.Role`。

在 C# 里用字符串常量来表达这些值，有一个根本性的问题：

```csharp
// 这样写编译器不会报错，但数据库里存进去的是一个无效值
car.Status = "published";   // 大小写错了
car.Status = "Pubished";    // 拼写错了
car.Status = "Online";      // 根本不在枚举里
```

用 C# 枚举就不会有这个问题——只能传枚举里定义的值，传错了编译器直接报错。

EF Core 支持把枚举以字符串形式存进数据库（存 `"Draft"` 这样可读的字符串，而不是数字 `0`），后面在 DbContext 配置时处理。

先建枚举目录：

```bash
mkdir -p UUcars.API/Entities/Enums
```

#### `Entities/Enums/CarStatus.cs`

```bash
touch UUcars.API/Entities/Enums/CarStatus.cs
namespace UUcars.API.Entities.Enums;

public enum CarStatus
{
    Draft,          // 草稿，卖家刚创建
    PendingReview,  // 已提交，等待 Admin 审核
    Published,      // 审核通过，公开可见
    Sold,           // 已售出
    Deleted         // 逻辑删除
}
```

#### `Entities/Enums/OrderStatus.cs`

```bash
touch UUcars.API/Entities/Enums/OrderStatus.cs
namespace UUcars.API.Entities.Enums;

public enum OrderStatus
{
    Pending,    // 待处理
    Completed,  // 交易完成
    Cancelled   // 已取消
}
```

#### `Entities/Enums/UserRole.cs`

```bash
touch UUcars.API/Entities/Enums/UserRole.cs
namespace UUcars.API.Entities.Enums;

public enum UserRole
{
    User,   // 普通用户（买家 / 卖家）
    Admin   // 管理员
}
```



### 5. BaseEntity：提取公共字段

回头看五张表，`Users`、`Cars`、`Orders` 都有相同的三个字段：

```
Id          int, PK, Identity
CreatedAt   datetime2, NOT NULL
UpdatedAt   datetime2, NOT NULL
```

不需要在每个实体类里重复写这三行。提取一个基类，有这三个字段的实体继承它：

```bash
touch UUcars.API/Entities/BaseEntity.cs
rm UUcars.API/Entities/.gitkeep
namespace UUcars.API.Entities;

// 所有实体的公共基类
// abstract：这个类本身不对应任何数据库表，
// 不应该被直接实例化，只能被继承
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

哪些实体会继承这个基类？

```
✅ User     有 Id / CreatedAt / UpdatedAt → 继承
✅ Car      有 Id / CreatedAt / UpdatedAt → 继承
✅ Order    有 Id / CreatedAt / UpdatedAt → 继承
❌ CarImage 只有 Id，没有时间字段        → 不继承，单独写
❌ Favorite 联合主键，没有独立 Id        → 不继承，单独写
```



### 6. 保存数据库设计文档

实体类和后面的 Migration 文件是数据库结构最权威的记录，但它们分散在各个文件里，不够直观。

建一份人类可读的 Schema 文档，方便随时查阅整体结构和字段的业务含义：

```bash
mkdir -p docs
touch docs/database-schema.md
```

`docs/database-schema.md` 内容：

```markdown
# UUcars 数据库 Schema

## 表关系

Users  ──<  Cars         一个用户可以发布多辆车（一对多）
Cars   ──<  CarImages    一辆车可以有多张图片（一对多）
Users  ──<  Orders       一个用户可以有多个订单（一对多）
Cars   ──<  Orders       一辆车可以有多条订单记录（一对多）
Users  >──< Cars         用户和车辆之间有收藏关系（多对多，通过 Favorites 实现）

## Users

| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK, Identity | 自增主键 |
| Username | nvarchar(50) | NOT NULL | |
| Email | nvarchar(100) | NOT NULL, UNIQUE | 登录凭证，不能重复 |
| PasswordHash | nvarchar(256) | NOT NULL | Hash 后的密码，不存明文 |
| Role | nvarchar(20) | NOT NULL | User / Admin |
| EmailConfirmed | bit | NOT NULL, DEFAULT 0 | V2 邮箱验证时使用 |
| CreatedAt | datetime2 | NOT NULL | |
| UpdatedAt | datetime2 | NOT NULL | |

V2 预留字段（届时通过 Migration 添加）：
EmailConfirmationToken / ResetPasswordToken / ResetPasswordTokenExpiry

## Cars

| 字段         | 类型           | 约束          | 说明                            |
|---|---|---|---|
| Id           | int            | PK, Identity  |                                 |
| Title        | nvarchar(100)  | NOT NULL      | 车辆标题                        |
| Brand        | nvarchar(50)   | NOT NULL      | 品牌，如 BMW                    |
| Model        | nvarchar(50)   | NOT NULL      | 车型，如 3 Series               |
| Year         | int            | NOT NULL      | 出厂年份                        |
| Price        | decimal(18,2)  | NOT NULL      | 售价                            |
| Mileage      | int            | NOT NULL      | 里程（公里）                    |
| Description  | nvarchar(2000) | NULL          | 详细描述，允许为空              |
| SellerId     | int            | FK → Users.Id | 卖家                            |
| Status       | nvarchar(20)   | NOT NULL      | 见下方状态流转                  |
| CreatedAt    | datetime2      | NOT NULL      |                                 |
| UpdatedAt    | datetime2      | NOT NULL      |                                 |

Status 流转：
Draft → PendingReview → Published → Sold / Deleted
PendingReview 被拒绝 → Draft（退回修改）

## CarImages

| 字段      | 类型          | 约束          | 说明                        |
|---|---|---|---|
| Id        | int           | PK, Identity  |                             |
| CarId     | int           | FK → Cars.Id  |                             |
| ImageUrl  | nvarchar(500) | NOT NULL      | V1 存 URL，V2 做真实上传    |
| SortOrder | int           | NOT NULL, DEFAULT 0 | 显示顺序，第一张为封面 |

## Orders

| 字段      | 类型          | 约束           | 说明                              |
|---|---|---|---|
| Id        | int           | PK, Identity   |                                   |
| CarId     | int           | FK → Cars.Id   |                                   |
| BuyerId   | int           | FK → Users.Id  |                                   |
| SellerId  | int           | FK → Users.Id  | 从 Car 冗余存储，方便卖家查询订单 |
| Price     | decimal(18,2) | NOT NULL       | 下单时锁定，不随车辆价格变动      |
| Status    | nvarchar(20)  | NOT NULL       | Pending / Completed / Cancelled   |
| CreatedAt | datetime2     | NOT NULL       |                                   |
| UpdatedAt | datetime2     | NOT NULL       |                                   |

## Favorites

| 字段      | 类型      | 约束          | 说明                    |
|---|---|---|---|
| UserId    | int       | FK → Users.Id |                         |
| CarId     | int       | FK → Cars.Id  |                         |
| CreatedAt | datetime2 | NOT NULL      |                         |

PRIMARY KEY (UserId, CarId)  — 联合主键，天然防重复收藏
```

> **这份文档后续怎么维护？** 每次表结构有变化（新增字段、加新表），在修改实体类的同一个 commit 里同步更新这个文档。Migration 文件是机器读的，这个文档是人读的，两者互补。



### 7. 此时的目录结构

```
UUcars.API/Entities/
├── BaseEntity.cs       ← 新增
└── Enums/
    ├── CarStatus.cs    ← 新增
    ├── OrderStatus.cs  ← 新增
    └── UserRole.cs     ← 新增
```



### 8. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 9. Git 提交

```bash
git add .
git commit -m "feat: database schema design - base entity + status enums + schema docs"
```



### Step 04 完成状态

```
✅ 切出 feature/db-setup 分支
✅ 五张表的字段设计全部明确
   - Users（8 个字段）
   - Cars（12 个字段，含完整状态流转）
   - CarImages（4 个字段，SortOrder 预留排序）
   - Orders（8 个字段，SellerId 冗余 + Price 锁定）
   - Favorites（联合主键，天然防重复收藏）
✅ CarStatus / OrderStatus / UserRole 三个枚举建立
✅ BaseEntity 基类建立（Id / CreatedAt / UpdatedAt）
✅ database-schema.md 文档建立
✅ dotnet build 通过
✅ Git commit 完成
```



## Step 05 · EF Core 实体设计

### 这一步做什么

Step 04 把数据库结构设计清楚了，现在把它翻译成 C# 代码。

EF Core 通过读取这些实体类来理解数据库结构，所以实体类的设计直接决定了最终建出来的表长什么样。



### 1. 两种配置方式：Data Annotations vs Fluent API

在告诉 EF Core"这个字段最大长度是多少"、"这个字段不能为空"这类信息时，有两种写法：

**Data Annotations**——直接在属性上加特性标签：

```csharp
[Required]
[MaxLength(50)]
public string Username { get; set; }
```

**Fluent API**——在单独的配置类里用代码描述：

```csharp
builder.Property(u => u.Username)
    .IsRequired()
    .HasMaxLength(50);
```

两种方式功能上等价，但有明显区别：

Data Annotations 写起来直接，但会把"业务属性"和"数据库配置"混在同一个类里——实体类本来只是描述业务概念的，混入数据库细节会让它越来越臃肿。而且 Data Annotations 能表达的配置有限，遇到复杂场景（比如枚举转字符串存储、联合主键、复合索引）就做不到了。

Fluent API 把数据库配置单独放在 `Configurations/` 目录下，实体类保持干净，只描述业务属性和关系。我们用这种方式。

所以实体类里**只写属性和导航属性**，EF Core 相关的配置（字段长度、索引、枚举转换等）统一放到 `Configurations/` 里，在注册 DbContext 时加载。



### 2. 什么是导航属性

在关系型数据库里，表和表之间通过外键关联。在 EF Core 里，这种关联用**导航属性**来表达：

```csharp
// Car 表里有 SellerId 外键
public int SellerId { get; set; }

// 对应的导航属性，让代码里可以直接访问 car.Seller.Username
// 而不用手动 JOIN
public User Seller { get; set; } = null!;
```

`null!` 是 .NET 的空值抑制符号。EF Core 在查询时会自动填充这些导航属性，但 C# 编译器不知道这件事，会警告说"这个属性可能是 null"。`null!` 告诉编译器"我知道这里不会是 null，不用警告"。



### 3. 创建实体类

#### `Entities/User.cs`

```bash
touch UUcars.API/Entities/User.cs
using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool EmailConfirmed { get; set; } = false;

    // 导航属性
    // 一个用户可以发布多辆车
    public ICollection<Car> Cars { get; set; } = [];

    // 一个用户可以有多个"买家身份"的订单
    public ICollection<Order> BuyerOrders { get; set; } = [];

    // 一个用户可以有多个"卖家身份"的订单
    public ICollection<Order> SellerOrders { get; set; } = [];

    // 一个用户可以收藏多辆车
    public ICollection<Favorite> Favorites { get; set; } = [];
}
```

> **为什么订单要分 `BuyerOrders` 和 `SellerOrders` 两个导航属性？** Orders 表里有两个外键都指向 Users 表（`BuyerId` 和 `SellerId`），EF Core 需要两个独立的导航属性来区分这两种关系。如果只写一个 `Orders`，EF Core 不知道它对应的是哪个外键。



#### `Entities/Car.cs`

```bash
touch UUcars.API/Entities/Car.cs
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

    // 导航属性
    public ICollection<CarImage> Images { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
```

> **`string?` 和 `string` 的区别：** `Description` 字段在 Step 04 设计时是 `NULL`（允许为空）。在 C# 里用 `string?` 表达这个含义，EF Core 看到 `?` 会在数据库里生成可为 NULL 的列。其他字符串字段没有 `?`，EF Core 会生成 NOT NULL 的列。



#### `Entities/CarImage.cs`

```bash
touch UUcars.API/Entities/CarImage.cs
namespace UUcars.API.Entities;

// CarImage 不继承 BaseEntity
// 因为它没有 CreatedAt / UpdatedAt，只有自己的 Id
public class CarImage
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    // 外键 + 导航属性
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;
}
```



#### `Entities/Order.cs`

```bash
touch UUcars.API/Entities/Order.cs
using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class Order : BaseEntity
{
    public decimal Price { get; set; }   // 下单时锁定的价格
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // 外键 + 导航属性（车辆）
    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    // 外键 + 导航属性（买家）
    public int BuyerId { get; set; }
    public User Buyer { get; set; } = null!;

    // 外键 + 导航属性（卖家，从 Car 冗余存储）
    public int SellerId { get; set; }
    public User Seller { get; set; } = null!;
}
```



#### `Entities/Favorite.cs`

```bash
touch UUcars.API/Entities/Favorite.cs
namespace UUcars.API.Entities;

// Favorite 不继承 BaseEntity
// 因为它用 (UserId, CarId) 联合主键，没有独立的 Id 字段
// 联合主键需要在 DbContext 里用 Fluent API 配置，
// 仅靠 Data Annotations 无法完整表达
public class Favorite
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CarId { get; set; }
    public Car Car { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
```



### 4. 此时的目录结构

```
UUcars.API/Entities/
├── BaseEntity.cs
├── Car.cs          ← 新增
├── CarImage.cs     ← 新增
├── Favorite.cs     ← 新增
├── Order.cs        ← 新增
├── User.cs         ← 新增
└── Enums/
    ├── CarStatus.cs
    ├── OrderStatus.cs
    └── UserRole.cs
```



### 5. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 6. Git 提交

```bash
git add .
git commit -m "feat: EF Core entities (User, Car, CarImage, Order, Favorite)"
```



### Step 05 完成状态

```
✅ User 实体（继承 BaseEntity，含 UserRole 枚举和四个导航属性）
✅ Car 实体（继承 BaseEntity，含 CarStatus 枚举和三个导航属性）
✅ CarImage 实体（不继承 BaseEntity，只有 Id + 两个字段）
✅ Order 实体（继承 BaseEntity，含 OrderStatus 枚举，三个外键）
✅ Favorite 实体（不继承 BaseEntity，联合主键待 DbContext 配置）
✅ dotnet build 通过
✅ Git commit 完成
```



## Step 06 · DbContext

### 这一步做什么

Step 05 把五个实体类写好了，但 EF Core 现在还不知道它们的存在。这一步要做两件事：

1. 创建 `AppDbContext`——把所有实体注册进去，EF Core 才知道要管理哪些表
2. 写 Fluent API 配置——把 Step 04 设计里的字段约束、索引、枚举转换、关系配置等细节告诉 EF Core



### 1. 为什么需要单独的配置类

Step 05 提到，实体类里只写属性和导航属性，数据库配置统一放在 `Configurations/` 目录下。现在来实现这些配置类。

#### 如何配置？

每个实体对应一个配置类，并实现 `IEntityTypeConfiguration<T>` 接口

```c#
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // 所有针对 User 实体的配置都在这里
    }
}
```

- 这个接口是由EntityFrameworkCore提供的，专门用来把实体配置从 `AppDbContext` 里拆出去单独管理。

- 这个接口只有一个方法 `Configure`，所有针对这个实体的 Fluent API 配置都在里面写。
- 配置方法传入 `EntityTypeBuilder<T>` 配置构建器 `builder`， `builder` 上面挂了很多方法，专门用来描述实体的数据库映射规则。在这里写的每一行配置，最终都会反映在 Migration 文件和数据库表结构里。

#### 配置类创建后，如何加载使用？

在创建 `AppDbContext` 时，需要让它知道所有配置类的存在。最直接的写法是手动一个个添加：

```c#
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfiguration(new UserConfiguration());
    modelBuilder.ApplyConfiguration(new CarConfiguration());
    modelBuilder.ApplyConfiguration(new CarImageConfiguration());
    modelBuilder.ApplyConfiguration(new OrderConfiguration());
    modelBuilder.ApplyConfiguration(new FavoriteConfiguration());
}
```

现在只有五个实体，还好。但随着项目增长，每次新增实体都要回来改 `AppDbContext`，很容易漏掉。

因此，替代 `ApplyConfiguration`单个加载的方法，EFcore提供了**统一自动加载的方法 `ApplyConfigurationsFromAssembly`。** 

它会自动扫描指定程序集里所有实现了 `IEntityTypeConfiguration<T>` 的类，并全部应用：

```c#
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

参数`typeof(AppDbContext).Assembly` 的意思是"AppDbContext 这个类所在的程序集"，也就是 `UUcars.API` 这个项目。以后新增实体时，只需要新建一个配置类，`AppDbContext` 完全不需要改动。



### 2. 安装 EF Core 相关包

EF Core 本体和 SQL Server 驱动需要单独安装：

```bash
dotnet add UUcars.API/UUcars.API.csproj package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add UUcars.API/UUcars.API.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
dotnet add UUcars.API/UUcars.API.csproj package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
```

**三个包的分工：**

`Microsoft.EntityFrameworkCore` 是 EF Core 的核心，提供 DbContext、DbSet、LINQ 查询等所有基础能力，和具体数据库无关。

`Microsoft.EntityFrameworkCore.SqlServer` 是 SQL Server 专属驱动，负责把 EF Core 生成的通用数据库操作翻译成 SQL Server 能执行的 T-SQL。EF Core 的设计是数据库无关的——如果将来要换成 PostgreSQL 或 SQLite，只需要换掉这个驱动包，其他业务代码一行不用改。

`Microsoft.EntityFrameworkCore.Tools` 提供 `dotnet ef` 命令行工具。下一步要用它来生成 Migration 文件和更新数据库，没有这个包，`dotnet ef` 命令会报错找不到。



### 3. 创建各实体的配置类

EF Core 理解实体结构的方式，除了step 05里提到的特性标签（Data Annotations）和Fluent API之外，还有EF Core 内置了一套默认规则，即：约定（Conventions）。

当没有显式配置时，EF Core 按约定自动处理。比如常见约定有：

```
属性名叫 Id 或 类名Id（int 类型）→ 自动识别为自增主键
属性类型是 string（无 ?）         → 数据库生成 NOT NULL
属性类型是 string?（有 ?）        → 数据库生成 NULL
```

因此，配置类里从来不出现 `Id` 字段 （不对Id字段进行显示配置）。

EF Core 看到 `int Id` 就自动把它处理成自增主键，完全不需要任何配置。

**约定能自动处理的，就不写；约定处理不了或需要覆盖默认行为的，才显式配置。**



先删掉占位文件：

```bash
rm UUcars.API/Configurations/.gitkeep
```

#### `Configurations/UserConfiguration.cs`

```bash
touch UUcars.API/Configurations/UserConfiguration.cs
```

```c#
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        // 唯一索引：数据库层面保证同一邮箱不能注册两次
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        // 枚举存为字符串，见下方解释
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();
    }
}
```

`HasConversion<string>()` 是什么 ？

这一行配置决定了枚举在数据库里以什么形式存储。 如果不加这行，EF Core 默认把枚举存成**整数**： ``` UserRole.User  → 存 0 UserRole.Admin → 存 1 ``` 。直接打开数据库看到的是一堆数字，完全不知道代表什么含义，调试起来很痛苦。 

加了 `HasConversion<string>()` 之后： ``` UserRole.User  → 存 "User" UserRole.Admin → 存 "Admin" ``` 。EF Core 在写入数据库时自动把枚举转成字符串，读取时再把字符串转回枚举。



#### `Configurations/CarConfiguration.cs`

```bash
touch UUcars.API/Configurations/CarConfiguration.cs
```

```c#
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
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

        // decimal 类型必须显式指定精度，约定不会自动处理
        // decimal(18,2)：最多 18 位数字，其中 2 位小数，适合存金额
        builder.Property(c => c.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.Mileage)
            .IsRequired();

        // Description 没有 IsRequired()，对应数据库的 NULL（允许为空）
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

        // 一辆车属于一个卖家（Car → User 是多对一）
        // Restrict：用户有车辆时不能被直接删除（安全防线）
        builder.HasOne(c => c.Seller)
            .WithMany(u => u.Cars)
            .HasForeignKey(c => c.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**`OnDelete` 的两种行为：**

配置外键关系时，`OnDelete` 决定"当父记录被删除时，子记录怎么处理"：

- **`DeleteBehavior.Cascade`（级联删除）**：父记录删了，子记录跟着自动删除。适合子记录完全依附于父记录、没有独立存在意义的情况。比如图片依附于车辆，车辆删了图片跟着删合理。
- **`DeleteBehavior.Restrict`（限制删除）**：父记录有子记录时，拒绝删除父记录，直接报错。适合子记录是重要业务数据、不能随便消失的情况。

项目里的选择逻辑：

```
CarImage → Car     Cascade   车删了图片跟着删，图片没有独立意义
Favorite → User    Cascade   用户删了收藏记录跟着删
Favorite → Car     Cascade   车删了收藏记录跟着删

Car      → User    Restrict  用户有车辆时不能被直接删除
Order    → Car     Restrict  订单是重要记录，不因车辆删除而消失
Order    → User    Restrict  订单是重要记录，不因用户删除而消失
```



#### `Configurations/CarImageConfiguration.cs`

```bash
touch UUcars.API/Configurations/CarImageConfiguration.cs
```

```c#
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class CarImageConfiguration : IEntityTypeConfiguration<CarImage>
{
    public void Configure(EntityTypeBuilder<CarImage> builder)
    {
        builder.Property(ci => ci.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ci => ci.SortOrder)
            .IsRequired()
            .HasDefaultValue(0);

        // Cascade：车辆删除时图片跟着删，图片依附于车辆没有独立意义
        builder.HasOne(ci => ci.Car)
            .WithMany(c => c.Images)
            .HasForeignKey(ci => ci.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```



#### `Configurations/OrderConfiguration.cs`

```bash 
touch UUcars.API/Configurations/OrderConfiguration.cs
```

```c#
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // 枚举存为字符串
        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        // 外键关系：订单关联车辆
        builder.HasOne(o => o.Car)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CarId)
            .OnDelete(DeleteBehavior.Restrict);

        // 外键关系：买家
        // 这里必须显式指定导航属性名，因为 User 有两个导航属性指向 Order
        // （BuyerOrders 和 SellerOrders），EF Core 无法自动判断该用哪个
        builder.HasOne(o => o.Buyer)
            .WithMany(u => u.BuyerOrders)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 外键关系：卖家
        builder.HasOne(o => o.Seller)
            .WithMany(u => u.SellerOrders)
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

> **Orders 表为什么三个外键都用 `Restrict`？** 订单是重要的业务记录，不应该因为用户或车辆被删除而跟着消失。业务上"删除"用户或车辆都是逻辑删除（改 Status 字段），不会真正从数据库删行，所以实际上也不会触发这个约束。加上 `Restrict` 是一道安全防线。



#### `Configurations/FavoriteConfiguration.cs`

```bash
touch UUcars.API/Configurations/FavoriteConfiguration.cs
```

```c#
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UUcars.API.Entities;

namespace UUcars.API.Configurations;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        // 配置联合主键
        // new { f.UserId, f.CarId } 是 C# 的匿名对象写法，
        // 用来同时传入两个字段，告诉 EF Core"这两个字段一起构成主键"
        // Data Annotations 的 [Key] 只能标记单个字段，无法表达复合主键，
        // 这是必须用 Fluent API 的场景
        builder.HasKey(f => new { f.UserId, f.CarId });

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // 外键关系：收藏关联用户
        builder.HasOne(f => f.User)
            .WithMany(u => u.Favorites)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 外键关系：收藏关联车辆
        builder.HasOne(f => f.Car)
            .WithMany(c => c.Favorites)
            .HasForeignKey(f => f.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```



### 4. 创建 AppDbContext

```bash
touch UUcars.API/Data/AppDbContext.cs
rm UUcars.API/Data/.gitkeep
```

```c#
using Microsoft.EntityFrameworkCore;
using UUcars.API.Configurations;
using UUcars.API.Entities;

namespace UUcars.API.Data;

public class AppDbContext : DbContext
{
    // DbContextOptions 包含数据库连接字符串等配置
    // 通过构造函数注入，由 DI 容器在运行时传入
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSet<T> 对应数据库里的一张表
    // 通过它可以对这张表做查询、新增、修改、删除
    public DbSet<User> Users => Set<User>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarImage> CarImages => Set<CarImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 统一加载 Configurations/ 目录下的所有配置类
        // 每新增一个 IEntityTypeConfiguration 实现，这里自动识别，不需要手动添加
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

> **`ApplyConfigurationsFromAssembly` 的好处：** 它会自动扫描当前程序集里所有实现了 `IEntityTypeConfiguration<T>` 的类并应用。后续新增实体时，只需要新建一个配置类，`AppDbContext` 不需要改动。



### 5. 在 Program.cs 里注册 DbContext

打开 `Program.cs`，在服务注册区域加上 DbContext 的注册：

```csharp
// 在 builder.Services.AddControllers(); 下方添加

// 注册 AppDbContext
// 从配置文件读取连接字符串（开发环境从 User Secrets 读取）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
```

>`GetConnectionString("DefaultConnection")` 会从配置系统读取连接字符串。
>
>ASP.NET Core 的配置系统有优先级——开发环境会优先读 User Secrets，生产环境读环境变量。
>
>我们在 Step 03 里已经通过 User Secrets 设置了真实的连接字符串，这里的代码不需要关心具体从哪里读，框架自动处理。



同时在文件顶部补上 using：

```csharp
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
```

此时 `Program.cs` 服务注册部分完整如下：

```c#
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings")
);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
```



### 6. 此时的目录结构变化

```
UUcars.API/
├── Configurations/
│   ├── CarConfiguration.cs         ← 新增
│   ├── CarImageConfiguration.cs    ← 新增
│   ├── FavoriteConfiguration.cs    ← 新增
│   ├── OrderConfiguration.cs       ← 新增
│   └── UserConfiguration.cs        ← 新增
├── Data/
│   └── AppDbContext.cs             ← 新增
```



### 7. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 8. Git 提交

```bash
git add .
git commit -m "feat: AppDbContext + Fluent API configurations for all entities"
```



### Step 06 完成状态

```
✅ 五个 Fluent API 配置类（字段约束、索引、枚举转字符串、关系配置）
   - UserConfiguration（Email 唯一索引，UserRole 枚举转字符串）
   - CarConfiguration（CarStatus 枚举转字符串，Seller 外键 Restrict）
   - CarImageConfiguration（Car 外键 Cascade）
   - OrderConfiguration（三个外键全部 Restrict，买家/卖家关系明确）
   - FavoriteConfiguration（联合主键，两个外键 Cascade）
✅ AppDbContext 创建（五个 DbSet，ApplyConfigurationsFromAssembly 自动加载）
✅ EF Core + SQL Server 包安装
✅ DbContext 注册进 Program.cs
✅ dotnet build 通过
✅ Git commit 完成
```



## Step 07 · EF Core Migration

### 这一步做什么

Step 06 把 DbContext 和所有实体配置写好了，但数据库里现在还是空的——什么表都没有。这一步要做两件事：

1. 用 EF Core Migration 机制把 C# 实体类"翻译"成数据库表
2. 在 Migration 里写入初始 Admin 用户数据，让系统从第一天起就有可用的管理员账号



### 1. 先启动 Docker SQL Server

Migration 执行时需要连接真实数据库，所以先确认数据库容器在跑：

```bash
# 启动 SQL Server 容器
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrongPassw0rd" \
  -e "MSSQL_PID=Developer" \
  -p 1433:1433 \
  --name uucars-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

如果之前已经创建过这个容器，直接启动就好：

```bash
docker start uucars-sqlserver
```

确认容器正在运行：

```bash
docker ps
```

看到 `uucars-sqlserver` 在列表里且状态是 `Up`，说明数据库已经就绪。



### 2. 什么是 Migration

在解释命令之前，先理解 Migration 是什么。

我们用的是 Code-First 开发方式：C# 实体类是"真相的唯一来源"，数据库结构由代码决定。但代码写好了，数据库不会自动变化，需要一个"翻译和执行"的机制，这就是 Migration。

Migration 的工作流程分两步：

```
第一步：dotnet ef migrations add
   EF Core 读取当前所有实体类和配置，
   和上一次 Migration 的状态对比，
   把"需要做哪些变更"写成一个 C# 文件（Migration 文件）

第二步：dotnet ef database update
   读取 Migration 文件，
   连接数据库，
   把文件里描述的变更真正执行到数据库上
```

Migration 文件是纯代码文件，会进入 Git。这意味着每一次数据库结构变更都有完整的历史记录，可以回滚，可以在不同环境（开发机、测试服务器、生产服务器）上重放同样的变更。



### 3. Admin Seed 数据的问题

在生成 Migration 之前，先处理一个问题：系统需要一个 Admin 用户来测试后续的审核接口，但注册接口只创建普通 `User` 角色，无法注册 Admin。

解决方案是在 `AppDbContext` 里用 EF Core 的 **Seed Data** 机制写入初始数据：Seed Data 会被包含在 Migration 文件里，数据库初始化时自动插入。

Seed Data 里需要存入一个已经 Hash 过的密码。这里有一个重要的细节：

**不能在 Seed Data 里动态调用 `PasswordHasher.HashPassword()`。**

原因是 `PasswordHasher` 每次调用都会用随机 salt，生成的 hash 值每次都不同。EF Core 9 在执行 `database update` 时会重新扫描当前模型（包括 Seed Data），发现 hash 值和 Migration 快照里记录的不一样，就认为"有未提交的变更"，直接报错拒绝执行。

正确做法是**预先生成一次 hash，把结果硬编码成固定字符串**。

**第一步：临时生成 hash 值**

在 `Program.cs` 的 `app.Run()` 前临时加这几行，运行一次拿到 hash：

```c#
// 临时代码，获取 hash 后立刻删除
var tempHasher = new PasswordHasher<UUcars.API.Entities.User>();
var tempHash = tempHasher.HashPassword(new UUcars.API.Entities.User(), "Admin@123456");
Console.WriteLine("HASH: " + tempHash);
```

运行临时脚本，拿到hash 值

```bash
cd UUcars.API && dotnet run
```

控制台输出类似：

```bash
HASH: AQAAAAIAAYagAAAAE......（一长串 Base64 字符串）
```

复制这个 hash 值，**立刻删掉临时代码**，回到根目录：

```bash
cd ..
```

**第二步：更新 `AppDbContext.cs`，把 hash 硬编码进去**

实际上更简单的做法是直接在代码里用 `PasswordHasher` 生成，但我们的 `PasswordHasher` 包还没安装。

这里先用一个临时方式：直接在 `AppDbContext` 里用硬编码的方式生成一次，后面 的Step 会正式安装和使用 `PasswordHasher`。

暂时先用 ASP.NET Core 内置的 `PasswordHasher<T>` 来生成 Hash。在 `AppDbContext.cs` 里做 Seed 时调用它：

打开 `UUcars.API/Data/AppDbContext.cs`，更新内容：

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarImage> CarImages => Set<CarImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Seed Data：在数据库初始化时插入初始 Admin 用户
        // 为什么用固定时间（new DateTime(2025, 1, 1)）而不是 DateTime.UtcNow？
        // 因为 Seed Data 会被写入 Migration 文件，Migration 文件进入 Git。
        // 如果用 DateTime.UtcNow，每次生成 Migration 时时间都不一样，
        // 导致 Migration 文件内容每次都变，Git 历史会被污染。
        // 固定时间保证 Migration 文件的内容是稳定的、可重复的。
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // 使用预先计算好的固定 hash，而不是在这里动态生成
        // 原因：PasswordHasher 每次调用会用随机 salt，导致 EF Core 9 每次
        // 扫描模型时发现值不同，误判为"有未提交的变更"
        const string adminPasswordHash = "这里粘贴生成的 hash 字符串";

        var adminUser = new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@uucars.com",
            PasswordHash = adminPasswordHash,
            Role = UserRole.Admin,
            EmailConfirmed = true,
            CreatedAt = seedDate,
            UpdatedAt = seedDate
        };

        // 添加种子数据给User实体
        modelBuilder.Entity<User>().HasData(adminUser);
    }
}
```

> **为什么 Admin 的 Id 要固定写 `1`？** Seed Data 要求实体的主键必须是确定的固定值，不能用自增——EF Core 需要知道"这条 Seed 数据是不是已经在数据库里了"，如果 Id 不固定就无法判断。所以 Seed Data 的主键必须手动指定。



### 4. 安装 Migration 命令行工具

`dotnet ef` 命令需要全局工具支持，如果之前没装过：

```bash
dotnet tool install --global dotnet-ef
```

已经装过的话，确认版本和项目的 EF Core 版本匹配：

```bash
dotnet ef --version
```



### 5. 生成 Migration 文件

在根目录执行：

```bash
dotnet ef migrations add InitialCreate --project UUcars.API/UUcars.API.csproj
```

> **`--project` 参数是什么？** 告诉 `dotnet ef` 命令去哪个项目里找 `DbContext`。因为我们的 Solution 里有两个项目（API 和 Tests），不指定的话工具不知道该用哪个。

执行成功后，项目里会新增一个 `Migrations/` 目录：

```
UUcars.API/Migrations/
├── 20250101000000_InitialCreate.cs        ← Migration 逻辑文件
└── AppDbContextModelSnapshot.cs          ← 记录当前模型状态，供下次 Migration 对比用
```

打开 `InitialCreate.cs` 看一下里面的内容：

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Up 方法：执行变更（创建表、加字段等）
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Username = table.Column<string>(maxLength: 50, nullable: false),
                // ...
            });
        // ... 其他建表语句
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Down 方法：回滚变更（删表、删字段等）
        // 如果执行 dotnet ef database update [上一个版本]，就会执行这里的代码
        migrationBuilder.DropTable(name: "Users");
        // ...
    }
}
```

每个 Migration 文件都有 `Up` 和 `Down` 两个方法——`Up` 是正向变更，`Down` 是回滚。这就是 Migration 能回滚的原因。



### 6. 把 Migration 应用到数据库

```bash
dotnet ef database update --project UUcars.API/UUcars.API.csproj
```

执行成功后，数据库里会建好所有表，并且 Admin 用户已经被插入 `Users` 表。

可以用数据库客户端（比如 Rider 内置的 Database 工具，或 Azure Data Studio）连接到 `localhost:1433`，用 `sa` / `YourStrongPassw0rd` 登录，查看 `UUcarsDB` 数据库里的表结构，确认一切正常。

连接配置：

```
Server:   localhost, 1433
Username: sa
Password: YourStrongPassw0rd
Database: UUcarsDB
```

应该能看到五张表：`Users`、`Cars`、`CarImages`、`Orders`、`Favorites`，以及 EF Core 自动创建的 `__EFMigrationsHistory` 表（这张表记录了哪些 Migration 已经被执行过，EF Core 内部使用，不需要手动管理）。



### 7. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 8. 此时的目录结构变化

```
UUcars.API/
├── Data/
│   └── AppDbContext.cs     ← 已更新（加了 Seed Data）
└── Migrations/             ← 新增目录
    ├── 20250101000000_InitialCreate.cs
    └── AppDbContextModelSnapshot.cs
```



### 9. Git 提交 + 合并分支

```bash
git add .
git commit -m "feat: initial migration + admin seed data"
```

所有数据库相关工作（Step 04 到 Step 07）都完成了，把 `feature/db-setup` 合并回 `develop`：

```bash
git checkout develop
git merge --no-ff feature/db-setup -m "merge: feature/db-setup into develop"
git push origin develop
git branch -d feature/db-setup
git push origin --delete feature/db-setup
```

这是第一个里程碑节点——数据库 + 配置 + 工程基础设施全部就绪，合并到 `main` 并打 `v0.1` tag：

```bash
git checkout main
git merge --no-ff develop -m "release: v0.1 - infrastructure ready"
git tag -a v0.1 -m "DB + config + middleware + logging setup complete"
git push origin main
git push origin v0.1
git checkout develop
```



### Step 07 完成状态

```
✅ Docker SQL Server 启动
✅ 理解 Migration 机制（Up/Down，两步流程）
✅ Admin Seed Data 写入 AppDbContext（固定 Id、固定时间、PasswordHasher）
✅ dotnet ef migrations add 生成 InitialCreate
✅ dotnet ef database update 建表成功
✅ 数据库里五张表可见，Admin 用户已插入
✅ Git commit 完成
✅ feature/db-setup 合并回 develop
✅ v0.1 里程碑 tag 打完
```



已读完全部 7 个步骤，内容完全对齐。特别注意到：

- 工程基础设施（异常中间件、`ApiResponse<T>`、Serilog、Scalar）已就绪
- 五个实体类、Fluent API 配置、`AppDbContext` 已完成
- Migration 已执行，数据库有五张表，Admin Seed 数据已写入
- `v0.1` tag 已打，当前在 `develop` 分支

现在开始 Step 08。



## Step 08 · 用户注册

### 这一步做什么

数据库和工程基础设施都就绪了，从这一步开始正式写业务功能。

用户注册是整个系统的第一个业务接口：没有用户就没有后续的车辆发布、下单、收藏等一切操作，所以从这里开始是自然的起点。

这一步会建立起完整的分层结构：`Controller → Service → Repository`，后续所有模块都遵循同样的模式。



### 1. 切出 feature/auth 分支

用户系统（注册、登录、JWT、用户信息）是一个完整的功能块，单独开分支：

```bash
git checkout develop
git checkout -b feature/auth
git push -u origin feature/auth
```



### 2. 回顾一下分层职责

在写代码之前，先明确这一步要建的几个层各自负责什么：

```
AuthController          接收 HTTP 请求，验证格式，调用 Service，返回响应
    ↓
UserService             业务逻辑：检查邮箱是否已注册，Hash 密码，创建用户
    ↓
IUserRepository         数据访问接口（定义"能做什么"）
EfUserRepository        数据访问实现（用 EF Core 真正操作数据库）
```

Controller 不写业务判断，Service 不写 SQL，Repository 不写业务规则——三层各司其职，互不越界。



### 3. 创建业务异常

注册时如果邮箱已被使用，需要返回 `409 Conflict`。在 Step 03 里建的 `AppException` 基类就是为这种情况准备的，现在建第一个具体的业务异常：

```bash
touch UUcars.API/Exceptions/UserAlreadyExistsException.cs
namespace UUcars.API.Exceptions;

// 邮箱已被注册时抛出
// 继承 AppException，StatusCode 固定为 409
// GlobalExceptionMiddleware 会捕获它并返回对应的 HTTP 响应
public class UserAlreadyExistsException : AppException
{
    public UserAlreadyExistsException(string email)
        : base(StatusCodes.Status409Conflict, $"Email '{email}' is already registered.")
    {
    }
}
```



### 4. 创建 DTO

#### `DTOs/Requests/RegisterRequest.cs`

`RegisterRequest` 是客户端传进来的数据结构。

这里用 **Data Annotations** 做输入验证——ASP.NET Core 的 `[ApiController]` 特性会在进入 Action 之前自动检查这些规则，不满足就直接返回 400，不需要在 Controller 里手动检查。

>
>
>#### Data Annotations 是框架自带的
>
>来自 .NET 内置命名空间：
>
>```csharp
>using System.ComponentModel.DataAnnotations;  // 不需要安装任何包
>```
>
>在 ASP.NET Core Web API 中，当请求进来时，框架会**自动触发验证**，不需要手动调用。
>
>
>
>#### 常用 Annotations 分类一览
>
>##### 必填 / 存在性
>
>```csharp
>[Required]                          // 不能为 null 或空
>[Required(ErrorMessage = "自定义")]  // 带自定义错误信息
>```
>
>##### 字符串长度
>
>```csharp
>[MaxLength(50)]          // 最大长度
>[MinLength(6)]           // 最小长度
>[StringLength(50, MinimumLength = 6)]  // 同时限制最大和最小
>```
>
>##### 数值范围
>
>```csharp
>[Range(1, 100)]                     // 数字在 1~100 之间
>[Range(0.01, 9999.99)]             // 支持小数
>```
>
>##### 格式验证
>
>```csharp
>[EmailAddress]          // 必须是合法邮箱格式
>[Phone]                 // 必须是合法电话格式
>[Url]                   // 必须是合法 URL
>[RegularExpression("正则表达式")]   // 自定义正则
>```
>
>##### 比较
>
>```csharp
>[Compare("Password")]   // 必须和另一个字段值相同，常用于"确认密码"
>```
>
>##### 其他
>
>```csharp
>[CreditCard]            // 信用卡号格式
>[DataType(DataType.Date)]       // 标注数据类型（更多是语义提示）
>[DataType(DataType.Password)]
>```
>
>
>
>#### 验证失败后框架做什么
>
>```
>请求进来
>    ↓
>框架自动读取 Annotations 逐一验证
>    ↓
>验证失败 → 自动返回 400 Bad Request + 错误详情（不会进入方法）
>验证通过 → 进入 Controller / Service 正常执行
>```
>
>返回的错误大概长这样：
>
>```json
>{
>  "errors": {
>    "Email": ["Invalid email format."],
>    "Password": ["Password must be at least 8 characters..."]
>  },
>  "status": 400
>}
>```
>
>
>
>#### 和代码里异常机制的分工
>
>```
>Data Annotations  →  验证"格式是否合法"（输入层，进门前拦截）
>                      比如：邮箱格式、密码强度、字段必填
>
>AppException      →  验证"业务规则是否满足"（逻辑层，进门后判断）
>                      比如：邮箱已被注册、车辆不存在
>```
>
>两者配合，覆盖了从"格式"到"业务"的完整验证链。



创建RegisterRequestDTO

```bash
touch UUcars.API/DTOs/Requests/RegisterRequest.cs
```

代码实现如下：

```c#

using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(50, ErrorMessage = "Username must not exceed 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    // 密码最少 8 位，包含大小写字母和数字
    // 正则说明：
    //   (?=.*[a-z])  至少一个小写字母
    //   (?=.*[A-Z])  至少一个大写字母
    //   (?=.*\d)     至少一个数字
    //   .{8,}        最少 8 个字符
    [Required(ErrorMessage = "Password is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain uppercase, lowercase, and a number.")]
    public string Password { get; set; } = string.Empty;
}
```



#### `DTOs/Responses/UserResponse.cs`

接口返回给客户端的用户数据结构。注意这里**不包含 `PasswordHash`**——密码哈希绝不能出现在任何响应里，哪怕是加密过的。

```bash
touch UUcars.API/DTOs/Responses/UserResponse.cs
```

```c#
public class UserResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```



### 5. 创建 Repository

#### `Repositories/IUserRepository.cs`

接口定义"用户数据访问能做什么"，不涉及任何实现细节：

```bash
touch UUcars.API/Repositories/IUserRepository.cs
rm UUcars.API/Repositories/.gitkeep
```

```c#
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IUserRepository
{
    // 按 Email 查找用户（登录和注册校验时使用）
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    // 新增用户，返回创建后的用户（含数据库分配的 Id）
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
}
```



#### `Repositories/EfUserRepository.cs`

用 EF Core 实现接口：

```bash
touch UUcars.API/Repositories/EfUserRepository.cs
```

```c#
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public EfUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // FirstOrDefaultAsync：找到第一条匹配的记录，没有则返回 null
        // 用 ToLower() 做大小写不敏感的邮箱匹配
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // 把实体加入 DbContext 的追踪（此时还没写数据库）
        _context.Users.Add(user);

        // SaveChangesAsync：把所有待处理的变更一次性写入数据库
        // 执行完之后，user.Id 会被数据库分配的自增值填充
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }
}
```



### 6. 创建 UserService

```bash
touch UUcars.API/Services/UserService.cs
rm UUcars.API/Services/.gitkeep
```

`UserService` 是业务逻辑的核心。注册的流程是：检查邮箱 → Hash 密码 → 创建用户。

关于密码 Hash：我们这里用 `PasswordHasher<User>`，它是 ASP.NET Core Identity 内置的密码哈希工具，不需要额外安装包。它的使用和安全性细节在 Step 09 会专门讲解，这里先直接用。

```csharp
using Microsoft.AspNetCore.Identity;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = new PasswordHasher<User>();
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. 检查邮箱是否已被注册
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            throw new UserAlreadyExistsException(request.Email);
        }

        // 2. 构建用户实体（先不填 PasswordHash）
        var user = new User
        {
            Username = request.Username,
            Email = request.Email.ToLower(),    // 统一存小写，保持一致性
            Role = UserRole.User,               // 注册的用户默认是普通用户
            EmailConfirmed = false,             // V1 不做邮箱验证，默认 false
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Hash 密码（细节在 Step 09 讲解）
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        // 4. 存入数据库
        var created = await _userRepository.AddAsync(user, cancellationToken);

        _logger.LogInformation("New user registered: {Email}", created.Email);

        // 5. 返回 DTO（不包含 PasswordHash）
        return MapToResponse(created);
    }

    // 实体 → DTO 的映射方法
    // 写成 private static：不依赖实例状态，也不需要从外部调用
    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };
}
```



### 7. 创建 AuthController

```bash
touch UUcars.API/Controllers/AuthController.cs
rm UUcars.API/Controllers/.gitkeep
```

```c#
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;

    public AuthController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.RegisterAsync(request, cancellationToken);

        // 201 Created：表示成功创建了新资源
        // 用 ApiResponse<T>.Ok 包装成统一格式
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Ok(user, "Registration successful."));
    }
}
```

> #### 一些说明：
>
> ##### 1. 根路径
>
> **`[Route("auth")]` 而不是 `[Route("[controller]")]`：** `[controller]` 是占位符，会自动取类名去掉 "Controller" 后缀，即 `Auth`。两种写法结果一样，但显式写 `"auth"` 更清晰直接——路由是 API 的公开契约，建议明确写出来而不是依赖约定。
>
> ##### 2. `[FromBody] `是什么？
>
> 它告诉框架：**从请求体（Body）里读取数据，并反序列化成对应的对象**。
>
> ```
> POST /auth/register
> Content-Type: application/json
> 
> {                              ← 这就是 Body
> "username": "alice",
> "email": "alice@example.com",
> "password": "Abc12345"
> }
>       ↓ [FromBody] 自动把这段 JSON 转成 RegisterRequest 对象
> RegisterRequest request
> ├── Username = "alice"
> ├── Email    = "alice@example.com"
> └── Password = "Abc12345"
> ```
>
> 不加 `[FromBody]` 框架不知道去哪里找数据：
>
> ```csharp
> // 数据来源有多种，需要明确告诉框架
> [FromBody]   // 从请求体 JSON 读取   ← 最常用
> [FromQuery]  // 从 URL 参数读取  ?email=xxx
> [FromRoute]  // 从路由读取       /users/{id}
> [FromHeader] // 从请求头读取
> ```
>
> ##### 3. `StatusCode()` 是什么？
>
> 来自 `ControllerBase`，类继承了它所以直接能用：
>
> ```csharp
> public class AuthController : ControllerBase  // ← 继承自这里
> {
>  // ControllerBase 提供了很多内置方法
>  StatusCode(int code, object value)  // 自定义状态码
>  Ok(object value)                    // 200
>  Created(...)                        // 201
>  BadRequest(...)                     // 400
>  NotFound(...)                       // 404
>  Unauthorized()                      // 401
> }
> ```
>
> ##### 4. 三个 User 数据模型综合对比
>
> 定位一览
>
> ```
>前端/客户端
>  ↓ 发送 JSON
>RegisterRequest   ← 输入层：接收前端传来的数据
>  ↓ Service 处理
>User              ← 核心层：数据库实体，内部流转
>  ↓ 映射过滤
> UserResponse      ← 输出层：返回给前端的数据
>     ↓ 返回 JSON
> 前端/客户端
>    ```
> 
>    字段对比
> 
>    | 字段                 | RegisterRequest | User | UserResponse |
> | -- | --- | ---- |  |
> | Id                   | ❌               | ✅    | ✅            |
>| Username             | ✅               | ✅    | ✅            |
> | Email                | ✅               | ✅    | ✅            |
>| Password（明文）     | ✅               | ❌    | ❌            |
> | PasswordHash（密文） | ❌               | ✅    | ❌            |
>| Role                 | ❌               | ✅    | ✅            |
> | CreatedAt            | ❌               | ✅    | ✅            |
> 
> 各自职责
> 
> **RegisterRequest** — 我只管收数据
> 
> ```csharp
> // 前端传来什么，我就收什么
> // 带验证规则（Data Annotations）
>// 有 Password 明文，因为需要用来生成 Hash
> // 没有 Id / Role / CreatedAt，这些由服务器决定
>```
> 
>**User** — 我只管存数据
> 
>```csharp
> // 对应数据库表结构
> // 有 PasswordHash，密码必须加密存储
> // 没有 Password 明文，明文处理完就丢弃
> // 不直接对外暴露
> ```
> 
>**UserResponse** — 我只管返数据
> 
>```csharp
> // 只含可以公开的字段
> // 没有任何密码相关字段（明文/密文都没有）
> // 前端拿到什么，完全由这个类决定
> ```
> 
> 完整数据流
>
> ```
>① 前端发请求
> { username, email, password }
>          ↓
> ② RegisterRequest 接收
>    + Data Annotations 验证格式
>             ↓
>③ Service 处理
>    password → BCrypt → passwordHash
>   生成 Role = "User"
>    生成 CreatedAt = 现在时间
>            ↓
> ④ 构建 User 实体存入数据库
>    { id(自增), username, email, passwordHash, role, createdAt }
>                ↓
>    ⑤ 映射成 UserResponse（过滤掉 passwordHash）
>    { id, username, email, role, createdAt }
>             ↓
> ⑥ 返回给前端
>    ApiResponse<UserResponse>
> ```
> 



### 8. 注册依赖到 Program.cs

打开 `Program.cs`，在 `AddDbContext` 下方添加：

```csharp
// 用户模块
// AddScoped：每次 HTTP 请求创建一个新实例，请求结束后销毁
// Repository 和 Service 都用 Scoped，因为它们依赖 DbContext（也是 Scoped）
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<UserService>();
```

同时在顶部补上 using：

```csharp
using UUcars.API.Repositories;
using UUcars.API.Services;
```



### 9. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 10. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

打开 Scalar 文档页面，找到 `POST /auth/register`，测试以下几个场景：

**正常注册：**

```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "Test@123456"
}
```

预期响应（201）：

```json
{
  "success": true,
  "data": {
    "id": 2,
    "username": "testuser",
    "email": "test@example.com",
    "role": "User",
    "createdAt": "..."
  },
  "message": "Registration successful."
}
```

**重复邮箱（应返回 409）：**

再发一次同样的请求，预期：

```json
{
  "success": false,
  "message": "Email 'test@example.com' is already registered."
}
```

**密码格式不对（应返回 400）：**

```json
{
  "username": "testuser2",
  "email": "test2@example.com",
  "password": "123456"
}
```

预期返回 400，带上验证错误信息。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 11. 此时的目录变化

```
UUcars.API/
├── Controllers/
│   └── AuthController.cs               ← 新增
├── DTOs/
│   ├── Requests/
│   │   └── RegisterRequest.cs          ← 新增
│   └── Responses/
│       └── UserResponse.cs             ← 新增
├── Exceptions/
│   └── UserAlreadyExistsException.cs   ← 新增
├── Repositories/
│   ├── IUserRepository.cs              ← 新增
│   └── EfUserRepository.cs             ← 新增
└── Services/
    └── UserService.cs                  ← 新增
```



### 12. Git 提交

```bash
git add .
git commit -m "feat: user registration endpoint"
```



### Step 08 完成状态

```
✅ 切出 feature/auth 分支
✅ UserAlreadyExistsException（409）
✅ RegisterRequest DTO（含 DataAnnotations 输入验证）
✅ UserResponse DTO（不含 PasswordHash）
✅ IUserRepository 接口 + EfUserRepository 实现
✅ UserService.RegisterAsync（检查邮箱 → Hash 密码 → 创建用户）
✅ AuthController POST /auth/register（返回 201）
✅ DI 注册完成
✅ Scalar 测试通过（201 注册成功，409 邮箱重复，400 格式错误）
✅ Git commit 完成
```



## Step 09 · 密码安全

### 这一步做什么

Step 08 在 `UserService` 里用了 `PasswordHasher<User>` 来 Hash 密码，但没有深入解释它是什么、为什么要这样用。

这一步补上这块知识，同时做两件事：

1. 把 `PasswordHasher` 改成通过 DI 注入（更符合架构规范，也方便测试）
2. 为 `UserService` 的注册逻辑写单元测试



### 1. 为什么不能直接存明文密码

先从根本问题说起。如果数据库被拖库，存明文密码意味着所有用户的密码直接暴露。即使存 MD5 或 SHA256 这类普通 Hash，攻击者也可以用"彩虹表"（预先计算好的哈希表）快速反查出原始密码。

安全的密码存储需要满足两个条件：

```
1. 不可逆：从 Hash 值无法反推出原始密码
2. 抗彩虹表：每次 Hash 结果不同，即使两个用户密码相同，存储的值也不一样
```

第二个条件靠的是 **Salt**——每次 Hash 时随机生成一段数据混入密码一起计算。这样即使两个用户都用了 `"123456"`，因为 Salt 不同，数据库里存的 Hash 值也完全不一样，彩虹表失效。



### 2. PasswordHasher 的两个核心方法

`PasswordHasher<User>` 是 ASP.NET Core 内置的密码哈希工具，底层使用 **PBKDF2** 算法（业界标准，被 NIST 推荐）。它提供两个方法：

**`HashPassword(user, plainPassword)`**

输入明文密码，输出包含 Salt 和 Hash 的字符串。每次调用都会生成新的随机 Salt，所以同一个密码每次的输出都不同——这正是我们在 Step 07 里不能在 Seed Data 里动态调用它的原因。

```csharp
var hasher = new PasswordHasher<User>();
string hash1 = hasher.HashPassword(user, "Test@123456");
string hash2 = hasher.HashPassword(user, "Test@123456");
// hash1 != hash2，每次 Salt 不同，输出不同
// 但两个都是合法的哈希，都能验证通过
```

**`VerifyHashedPassword(user, hashedPassword, plainPassword)`**

输入存储的 Hash 字符串和用户提供的明文密码，验证是否匹配。它能从 Hash 字符串里提取出当时用的 Salt，重新计算后比对。返回值是 `PasswordVerificationResult` 枚举：

```
Success          → 验证通过
Failed           → 验证失败（密码错误）
SuccessRehashNeeded → 验证通过，但算法版本较旧，建议重新 Hash
```

`VerifyHashedPassword` 在登录时会用到，Step 10 实现登录时再具体写。



### 3. 把 PasswordHasher 改为 DI 注入

Step 08 在 `UserService` 构造函数里直接 `new PasswordHasher<User>()`，这有两个问题：

一是不符合 DI 原则——依赖应该从外部注入，而不是在类内部自己创建。二是给单元测试带来麻烦——如果将来需要替换 Hash 算法，要改 Service 内部代码，而不是在 DI 注册处换一个实现。

`PasswordHasher<User>` 实现了 `IPasswordHasher<User>` 接口，改成注入接口：

打开 `UUcars.API/Services/UserService.cs`，更新构造函数：

```csharp
using Microsoft.AspNetCore.Identity;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;  // 改为接口
    private readonly ILogger<UserService> _logger;

    // 通过构造函数注入，由 DI 容器提供
    public UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,   // 改为接口注入
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            throw new UserAlreadyExistsException(request.Email);
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email.ToLower(),
            Role = UserRole.User,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var created = await _userRepository.AddAsync(user, cancellationToken);

        _logger.LogInformation("New user registered: {Email}", created.Email);

        return MapToResponse(created);
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };
}
```

打开 `Program.cs`，在用户模块注册那里补上 `IPasswordHasher<User>`：

```csharp
using Microsoft.AspNetCore.Identity;
using UUcars.API.Entities;
// 用户模块
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<UserService>();
```



### 4. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 5. 单元测试

#### 5.1 测什么，怎么测

单元测试测的是**业务逻辑**，对应的就是 `UserService`。它承载了所有注册规则：邮箱不能重复、密码要 Hash、用户角色默认是 User 等。

单元测试的核心要求是**快速、稳定、不依赖外部资源**（不连数据库、不发网络请求）。但 `UserService` 依赖 `IUserRepository`，Repository 是要操作数据库的。解决方案是用 **Fake Repository**——一个用内存模拟数据库的假实现，测试时替换真实的 Repository。

#### 5.2 创建 Fake Repository

```c#
mkdir -p UUcars.Tests/Fakes
touch UUcars.Tests/Fakes/FakeUserRepository.cs
```

```c#
using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

// 用内存字典模拟数据库，实现 IUserRepository 接口
// 只用于测试，不会真正操作数据库
public class FakeUserRepository : IUserRepository
{
    // 模拟数据库表，key = Email（方便按邮箱查找）
    private readonly Dictionary<string, User> _store = new();

    // 供测试用：预先插入数据，模拟"数据库里已有这条记录"
    public void Seed(User user) => _store[user.Email.ToLower()] = user;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(email.ToLower(), out var user);
        return Task.FromResult(user);
    }

    public Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        // 模拟数据库自增 Id
        user.Id = _store.Count + 1;
        _store[user.Email.ToLower()] = user;
        return Task.FromResult(user);
    }
}
```

#### 5.3 编写测试用例

```bash
mkdir -p UUcars.Tests/Services
touch UUcars.Tests/Services/UserServiceTests.cs
```

```c#
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class UserServiceTests
{
    // 测试用的辅助方法：创建一个标准的 UserService 实例
    // NullLogger：空日志，测试里不关心日志输出
    // PasswordHasher<User>：用真实实现，它是纯计算，不依赖外部资源
    private static UserService CreateService(FakeUserRepository repo) =>
        new(repo, new PasswordHasher<User>(), NullLogger<UserService>.Instance);

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnUserResponse()
    {
        // Arrange：准备一个空的 Repository（没有任何用户）
        var repo = new FakeUserRepository();
        var service = CreateService(repo);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        };

        // Act：执行注册
        var result = await service.RegisterAsync(request);

        // Assert：验证返回的数据是否符合预期
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);   // 默认角色是 User
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowUserAlreadyExistsException()
    {
        // Arrange：预先插入一个用户，模拟"邮箱已被注册"
        var repo = new FakeUserRepository();
        repo.Seed(new User
        {
            Id = 1,
            Email = "existing@example.com",
            Username = "existing",
            PasswordHash = "somehash",
            Role = UserRole.User
        });

        var service = CreateService(repo);

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",  // 用同一个邮箱
            Password = "Test@123456"
        };

        // Act + Assert：必须抛出 UserAlreadyExistsException
        await Assert.ThrowsAsync<UserAlreadyExistsException>(
            () => service.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotStorePasswordAsPlainText()
    {
        // Arrange
        var repo = new FakeUserRepository();
        var service = CreateService(repo);

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        };

        // Act
        await service.RegisterAsync(request);

        // Assert：数据库里存的不应该是明文密码
        // 通过 Fake Repo 直接读取"数据库"里的记录
        var stored = await repo.GetByEmailAsync("test@example.com");
        Assert.NotNull(stored);
        Assert.NotEqual("Test@123456", stored.PasswordHash);  // 不等于明文
        Assert.True(stored.PasswordHash.Length > 20);          // Hash 字符串很长
    }
}
```

#### 5.4 运行测试

```bash
dotnet test
```

预期输出：

```
Test summary: total: 3, failed: 0, succeeded: 3, skipped: 0
```



### 6. 此时的目录变化

```
UUcars.API/
├── Services/
│   └── UserService.cs      ← 已更新（PasswordHasher 改为接口注入）
└── Program.cs              ← 已更新（注册 IPasswordHasher<User>）

UUcars.Tests/
├── Fakes/
│   └── FakeUserRepository.cs   ← 新增
└── Services/
    └── UserServiceTests.cs     ← 新增
```



### 7. Git 提交

```bash
git add .
git commit -m "feat: password hashing with IPasswordHasher<User> DI injection"
git commit -m "test: unit tests for UserService registration logic"
```



### Step 09 完成状态

```
✅ 理解 PasswordHasher：PBKDF2、随机 Salt、HashPassword / VerifyHashedPassword
✅ UserService 改为注入 IPasswordHasher<User>（符合 DI 原则，方便测试）
✅ Program.cs 注册 IPasswordHasher<User>
✅ FakeUserRepository 实现（内存模拟数据库）
✅ 三个单元测试通过
   - 新邮箱注册成功，返回正确的 UserResponse
   - 已有邮箱注册，抛出 UserAlreadyExistsException
   - 密码不以明文存储
✅ dotnet test 全部通过
✅ Git commit 完成
```



## Step 10 · 用户登录

### 这一步做什么

注册功能写好了，现在实现登录。

登录的结果是返回一个 **JWT Token**，客户端拿到这个 Token 之后，后续请求需要登录才能访问的接口时，把 Token 放在请求头里带过去。

这一步要做：

1. 验证邮箱和密码
2. 生成 JWT Token
3. 返回 Token 给客户端



### 1. 什么是 JWT

JWT（JSON Web Token）是目前最主流的 API 认证方式。它是一个字符串，分三段，用 `.` 分隔：

```
eyJhbGciOiJIUzI1NiJ9         ← Header：声明算法类型
.eyJ1c2VySWQiOjF9            ← Payload：存放用户信息（Claims）
.SflKxwRJSMeKKF2QT4fwpMeJf   ← Signature：用 Secret Key 对前两段签名
```

每一段都是 Base64 编码的，不是加密的：Payload 里的内容任何人都可以解码看到。

JWT 的安全性靠的是第三段 Signature：它是用服务器的 `Secret Key` 对前两段内容签名的，客户端拿不到 `Secret Key`，所以无法伪造或篡改 Token。

一旦 Payload 被改动，Signature 验证就会失败，服务器会拒绝这个 Token。

**JWT 和传统 Session 的区别：**

```
Session：服务器存状态，每次请求查数据库验证身份
JWT：服务器不存状态，直接验证 Token 签名，无需查数据库
```

JWT 是"无状态"的，适合前后端分离的 API 场景。



### 2. 什么是 Claims

Claims（声明）是存放在 JWT Payload 里的键值对，代表"关于这个用户的一些事实"。

客户端登录成功后拿到 Token，后续每次请求都带上这个 Token，服务器从 Token 里解析出 Claims，就知道是谁在发这个请求、有什么权限，不需要每次都查数据库。

Claims 分两类：

**标准 Claims（Registered Claims）**——JWT 规范预定义的字段，有固定的简短名字：

```
sub（Subject）    主体，通常存用户 Id
email            用户邮箱
exp（Expiration） 过期时间（Unix 时间戳）
iat（Issued At）  签发时间
jti（JWT ID）     这个 Token 的唯一标识
iss（Issuer）     签发方（谁签发的这个 Token）
aud（Audience）   受众（这个 Token 给谁用的）
```

这些简短名字是 JWT 规范定义的，目的是节省 Token 体积（Token 越小，每次请求携带的开销越小）。

而 `System.IdentityModel.Tokens.Jwt`包已经提供了 一个静态类 `JwtRegisteredClaimNames` , 里面定义了所有 JWT 标准字段的名字, 如下：

| 常量名                               | 实际值          | 存什么                |
| ------------------------------------ | --------------- | --------------------- |
| `JwtRegisteredClaimNames.Sub`        | `"sub"`         | 用户唯一ID            |
| `JwtRegisteredClaimNames.Email`      | `"email"`       | 邮箱                  |
| `JwtRegisteredClaimNames.Jti`        | `"jti"`         | Token唯一标识，防重放 |
| `JwtRegisteredClaimNames.Name`       | `"name"`        | 用户显示名称          |
| `JwtRegisteredClaimNames.GivenName`  | `"given_name"`  | 名字 First Name       |
| `JwtRegisteredClaimNames.FamilyName` | `"family_name"` | 姓氏 Last Name        |

**自定义 Claims**——应用程序自己定义的字段，比如 `role`（角色），或者`my_role`，而ASP.NET Core 内置有一套自己的 Claim 类型常量（`ClaimTypes.Role`、`ClaimTypes.Email` 、`ClaimTypes.DateOfBirth`、`ClaimTypes.Country`等），它们的实际值是很长的 URI 字符串，但框架内部能识别，`[Authorize(Roles = "Admin")]` 就是靠 `ClaimTypes.Role` 来判断角色的。因此，尽量这些内置的常量，避免手写错误的同时，也方便框架内部组件之间的识别。

程序中Claims的设计逻辑：

- 先使用标准 Claims：JWT 规范预定义的字段
- 如果不够，再使用自定义 Claims ：优先使用框架内置的Claim 类型常量



### 3. 安装 JWT 包

```bash
dotnet add UUcars.API/UUcars.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add UUcars.API/UUcars.API.csproj package System.IdentityModel.Tokens.Jwt
```

两个包的分工：`Microsoft.AspNetCore.Authentication.JwtBearer` 是中间件，负责在请求进来时自动解析和验证 Token（Step 11 会用到）。`System.IdentityModel.Tokens.Jwt` 提供生成 Token 的工具类，这一步用它来签发 Token。



### 4. 创建 JWT Token 生成器

JWT 生成逻辑是独立的功能，单独放在 `Auth/` 目录里，不混入 Service：

```bash
touch UUcars.API/Auth/JwtTokenGenerator.cs
```

```c#
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UUcars.API.Entities;

namespace UUcars.API.Auth;

public class JwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    // IOptions<JwtSettings>：从 DI 容器中读取配置
    // Step 03 里已经在 Program.cs 用 Configure<JwtSettings> 绑定好了
    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(User user)
    {
        // =============================================
        // 第一步：定义 Claims（Token Payload 的内容）
        // =============================================
        // new Claim(key, value)：每一个 Claim 就是一个键值对
        //
        // JwtRegisteredClaimNames 是一个静态类，由`System.IdentityModel.Tokens.Jwt`包提供
        // 里面定义了所有 JWT 标准字段的名字
        // 比如 JwtRegisteredClaimNames.Sub 的值就是字符串 "sub"
        // 用这个类是为了避免手写字符串出错（写错了编译器不报错，运行时才发现）
        var claims = new[]
        {
            // sub：存用户 Id（转成字符串，因为 Claim 的 value 必须是字符串）
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            // email：存用户邮箱
            new Claim(JwtRegisteredClaimNames.Email, user.Email),

            // ClaimTypes.Role 是 ASP.NET Core 定义的角色 Claim Key
            // 它的实际值是一个很长的 URI：
            // "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            // 必须用这个 Key（而不是随便写个 "role"），
            // 因为 [Authorize(Roles = "Admin")] 就是靠识别这个 Key 来判断角色的
            new Claim(ClaimTypes.Role, user.Role.ToString()),

            // jti（JWT ID）：给这个 Token 一个唯一标识
            // 用 Guid 生成，保证每个 Token 都不一样
            // 作用：如果以后要实现"注销 Token"功能，可以把 jti 存进黑名单
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // =============================================
        // 第二步：创建签名凭证
        // =============================================
        // SymmetricSecurityKey：对称加密的 Key 对象
        // "对称"的意思是：签名和验证用同一个 Key（就是 appsettings 里的 JwtSettings:Secret）
        // Encoding.UTF8.GetBytes：把字符串 Secret 转成字节数组，Key 对象需要字节数组
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

        // SigningCredentials：把 Key 和加密算法打包在一起
        // SecurityAlgorithms.HmacSha256 是 HMAC-SHA256 算法，目前最常用的 JWT 签名算法
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // =============================================
        // 第三步：组装 Token
        // =============================================
        // JwtSecurityToken 是表示一个 JWT 的对象，把所有参数组装进去：
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,              // 签发方（谁签发的）
            audience: _jwtSettings.Audience,          // 受众（给谁用的）
            claims: claims,                           // Payload 内容
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes), // 过期时间
            signingCredentials: credentials           // 签名凭证
        );

        // =============================================
        // 第四步：序列化成字符串
        // =============================================
        // JwtSecurityTokenHandler 是处理 JWT 的工具类
        // WriteToken：把 JwtSecurityToken 对象序列化成 "xxxxx.yyyyy.zzzzz" 格式的字符串
        // 这个字符串就是最终返回给客户端的 Token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

把整个生成过程串起来理解：

```
用户 Id / 邮箱 / 角色
        ↓
    打包成 Claims 数组
        ↓
    用 Secret Key 创建签名凭证
        ↓
    组装成 JwtSecurityToken 对象（含过期时间、Issuer、Audience）
        ↓
    WriteToken 序列化成字符串
        ↓
    返回给客户端
```



### 5. 创建 DTO

#### `DTOs/Requests/LoginRequest.cs`

```bash
touch UUcars.API/DTOs/Requests/LoginRequest.cs
```



```c#
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
```



#### `DTOs/Responses/LoginResponse.cs`

```bash
touch UUcars.API/DTOs/Responses/LoginResponse.cs
```

```c#
namespace UUcars.API.DTOs.Responses;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = null!;
}
```



### 6. 创建登录相关的业务异常

登录失败时（邮箱不存在或密码错误），统一返回同一个错误信息——**不能区分"邮箱不存在"和"密码错误"**，否则攻击者可以通过错误信息枚举出系统里存在哪些邮箱账号。这是安全设计的基本原则。

```bash
touch UUcars.API/Exceptions/InvalidCredentialsException.cs
```

```c#
namespace UUcars.API.Exceptions;

public class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException()
        : base(StatusCodes.Status401Unauthorized, "Invalid email or password.")
    {
    }
}
```



### 7. 在 UserService 里添加登录逻辑

打开 `UUcars.API/Services/UserService.cs`，完整更新如下：

```csharp
namespace UUcars.API.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        JwtTokenGenerator jwtTokenGenerator,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
        {
            throw new UserAlreadyExistsException(request.Email);
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email.ToLower(),
            Role = UserRole.User,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var created = await _userRepository.AddAsync(user, cancellationToken);

        _logger.LogInformation("New user registered: {Email}", created.Email);

        return MapToResponse(created);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. 按邮箱查找用户
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // 2. 用户不存在：抛出和密码错误一样的异常，不透露"邮箱不存在"
        if (user == null)
        {
            throw new InvalidCredentialsException();
        }

        // 3. 验证密码
        // VerifyHashedPassword 做的事：
        //   从 user.PasswordHash 字符串里提取出当初 HashPassword 时用的 Salt
        //   用这个 Salt 对 request.Password（用户输入的明文）重新计算 Hash
        //   把计算结果和 user.PasswordHash 比对
        // 返回值是 PasswordVerificationResult 枚举：
        //   Success             → 匹配，验证通过
        //   Failed              → 不匹配，密码错误
        //   SuccessRehashNeeded → 匹配，但 Hash 算法版本较旧，建议更新
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        // 4. 验证通过，生成 JWT Token
        var token = _jwtTokenGenerator.GenerateToken(user);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new LoginResponse
        {
            Token = token,
            // ExpiresAt 和 JwtTokenGenerator 里的 expires 保持一致
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToResponse(user)
        };
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };
}
```



### 8. 在 AuthController 里添加登录端点

打开 `UUcars.API/Controllers/AuthController.cs`，添加 `Login` Action：

```csharp
namespace UUcars.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;

    public AuthController(UserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Ok(user, "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);

        // 登录成功返回 200 OK（不是 201，登录不是"创建资源"）
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
    }
}
```



### 9. 注册依赖到 Program.cs

打开 `Program.cs`，在用户模块注册区域补上 `JwtTokenGenerator`：

```csharp
// 用户模块
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<UserService>();
```

顶部补上 using：

```csharp
using UUcars.API.Auth;
```



### 10. 更新测试用例逻辑

`UserService` 的构造函数加了 `JwtTokenGenerator` 参数，但测试里的 `CreateService` 方法还是旧的签名，没有传这个参数，编译就报错了。

打开 `UUcars.Tests/Services/UserServiceTests.cs`，更新 `CreateService` 方法：

```c#
...

namespace UUcars.Tests.Services;

public class UserServiceTests
{
    private static UserService CreateService(FakeUserRepository repo)
    {
        // 给测试用的 JwtSettings，值随便填——注册测试根本不会走到生成 Token 的逻辑
        // 但 UserService 构造函数需要这个依赖，所以必须传进去
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test-secret-key-at-least-32-characters!",
            ExpiresInMinutes = 60,
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });

        return new UserService(
            repo,
            new PasswordHasher<User>(),
            new JwtTokenGenerator(jwtSettings),
            NullLogger<UserService>.Instance
        );
    }

    // 下面三个测试用例不需要改，逻辑不变
    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnUserResponse() { ... }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowUserAlreadyExistsException() { ... }

    [Fact]
    public async Task RegisterAsync_ShouldNotStorePasswordAsPlainText() { ... }
}
```

核心思路是：**测试注册逻辑，不需要真实的 JWT 配置，但构造函数必须满足**。所以用 `Options.Create()` 包一个假的 `JwtSettings` 传进去，`JwtTokenGenerator` 能正常实例化就行，注册测试里根本不会调用到它。



### 11. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 12. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常登录（用 Step 08 注册的账号）：**

```json
POST /auth/login
{
  "email": "test@example.com",
  "password": "Test@123456"
}
```

预期响应（200）：

```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiJ9......",
    "expiresAt": "2025-xx-xx...",
    "user": {
      "id": 2,
      "username": "testuser",
      "email": "test@example.com",
      "role": "User"
    }
  },
  "message": "Login successful."
}
```

把返回的 Token 复制到 [jwt.io](https://jwt.io/) 粘贴进去，可以直观地看到 Payload 里的 Claims 内容：

```json
{
  "sub": "2",
  "email": "test@example.com",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "User",
  "jti": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "exp": 1234567890,
  "iss": "UUcars",
  "aud": "UUcarsUsers"
}
```

注意 `role` 对应的 Key 是那个很长的 URI，这就是 `ClaimTypes.Role` 的实际值。

**密码错误（应返回 401）：**

```json
{
  "email": "test@example.com",
  "password": "wrongpassword"
}
```

预期：

```json
{
  "success": false,
  "message": "Invalid email or password."
}
```

**邮箱不存在（也应返回 401，信息和密码错误完全一样）：**

```json
{
  "email": "notexist@example.com",
  "password": "Test@123456"
}
```

两种失败情况返回的信息完全一样，攻击者无法通过错误信息判断邮箱是否存在。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 12. 此时的目录变化

```
UUcars.API/
├── Auth/
│   ├── JwtSettings.cs              （已有）
│   └── JwtTokenGenerator.cs        ← 新增
├── DTOs/
│   ├── Requests/
│   │   └── LoginRequest.cs         ← 新增
│   └── Responses/
│       └── LoginResponse.cs        ← 新增
├── Exceptions/
│   └── InvalidCredentialsException.cs  ← 新增
└── Services/
    └── UserService.cs              ← 已更新（新增 LoginAsync）
    
UUcars.Tests/
├── Services/
    └── UserServiceTests.cs              ← 已更新
```



### 13. Git 提交

```bash
git add .
git commit -m "feat: login endpoint + JWT token generation"
```



### Step 10 完成状态

```
✅ 理解 JWT 三段结构（Header / Payload / Signature）
✅ 理解 Claims（标准 Claims 和自定义 Claims，JwtRegisteredClaimNames 和 ClaimTypes）
✅ 理解 Token 生成的完整流程（Claims → 签名凭证 → JwtSecurityToken → 字符串）
✅ 安装 JWT 相关包
✅ JwtTokenGenerator（读取 JwtSettings，生成带 Claims 的 Token）
✅ LoginRequest / LoginResponse DTO
✅ InvalidCredentialsException（401，邮箱不存在和密码错误返回相同信息）
✅ UserService.LoginAsync（验证密码 → 生成 Token）
✅ AuthController POST /auth/login（返回 200 + Token）
✅ DI 注册完成
✅ Scalar 测试通过（200 登录成功，401 密码错误/邮箱不存在）
✅ jwt.io 验证 Token 结构和 Claims 内容正确
✅ Git commit 完成
```

