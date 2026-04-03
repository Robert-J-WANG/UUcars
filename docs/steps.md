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


