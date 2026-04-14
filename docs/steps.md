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
```

```c#
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
```

```c#
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

```b
namespace UUcars.API.DTOs;
```

```c#
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
```

```c#
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
```

```c#
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
|  | --- | --- |
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



## Step 11 · JWT 认证配置

### 这一步做什么

Step 10 实现了登录并生成了 JWT Token，客户端现在可以拿到 Token 了。

但服务器这边还没有配置"如何验证 Token"——目前所有接口都是公开的，就算带了 Token 也没有任何效果。

这一步配置 JWT 认证中间件，让服务器能够：

1. 自动解析请求头里的 Token
2. 验证 Token 的签名和有效期
3. 把 Token 里的 Claims 注入到请求上下文里，供后续接口使用



### 1. 认证（Authentication）和授权（Authorization）的区别

这两个概念经常被混淆，先说清楚：

**认证（Authentication）**：你是谁？验证 Token 是否合法、是否过期，从 Token 里提取出用户身份信息。对应中间件 `UseAuthentication()`。

**授权（Authorization）**：你能做什么？在认证通过的基础上，检查用户是否有权限访问这个接口。比如 `[Authorize(Roles = "Admin")]` 只允许 Admin 角色访问。对应中间件 `UseAuthorization()`。

两者的关系是：**先认证，再授权**——必须先知道"你是谁"，才能判断"你能不能做这件事"。所以中间件管道里 `UseAuthentication()` 必须在 `UseAuthorization()` 之前。



### 2. JWT 认证的完整流程

在配置代码之前，先把整个认证流程在脑子里过一遍，这样看代码时更容易理解每一行的作用：

```c#
客户端发请求
    ↓
请求头携带 Token：Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
    ↓
UseAuthentication() 中间件拦截请求
    ↓
JwtBearerHandler（框架内置）提取 Token 字符串
    ↓
用配置的 TokenValidationParameters 验证：
    - 签名是否合法（用 Secret Key 重新计算签名比对）
    - Issuer 是否匹配（是不是我们自己签发的）
    - Audience 是否匹配（是不是给我们用的）
    - 是否已过期（检查 exp 字段）
    ↓
验证通过 → 把 Token Payload 里的 Claims 解析出来
         → 注入到 HttpContext.User（ClaimsPrincipal）里
         → 请求继续往下走
    ↓
UseAuthorization() 中间件读取 HttpContext.User
    ↓
检查接口上的 [Authorize] 特性，判断是否有权限访问
    ↓
有权限 → 进入 Controller Action
无权限 → 返回 401 / 403
```

这个流程完全是**自动的**——我们只需要配置好参数，框架内置的 `JwtBearerHandler` 会处理所有细节。我们自己不需要写任何 Token 解析代码。



### 3. 配置 JWT 认证

打开 `Program.cs`，顶部补上 using：

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
```

在服务注册区域，`AddDbContext` 下方添加：

```csharp
// 先从配置系统读取 JwtSettings
// 开发环境自动从 User Secrets 读取真实值（Step 03 已配置）
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

builder.Services.AddAuthentication(options =>
    {
        // DefaultAuthenticateScheme：指定默认用哪种方式来"认证"请求
        // DefaultChallengeScheme：当认证失败时，用哪种方式来"挑战"客户端
        //   （对 JWT 来说，挑战的结果就是返回 401 Unauthorized）
        // 两个都设为 JwtBearerDefaults.AuthenticationScheme（值是字符串 "Bearer"），
        // 表示默认用 JWT Bearer 方式处理认证，这样 [Authorize] 特性不需要额外指定方案
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // TokenValidationParameters：告诉框架"验证 Token 时要检查哪些东西"
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ===== 签名验证 =====
            // ValidateIssuerSigningKey = true：必须验证签名
            // IssuerSigningKey：用来验证签名的 Key，必须和生成 Token 时用的 Key 一样
            // 原理：框架用这个 Key 对 Token 的 Header+Payload 重新计算签名，
            //       和 Token 第三段比对，不一样就拒绝
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),

            // ===== Issuer 验证 =====
            // 验证 Token 的 iss 字段是否匹配，防止接受其他系统签发的 Token
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,       // 值："UUcars"

            // ===== Audience 验证 =====
            // 验证 Token 的 aud 字段是否匹配，确保 Token 是给我们的系统用的
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,   // 值："UUcarsUsers"

            // ===== 过期时间验证 =====
            // 验证 Token 的 exp 字段，过期的 Token 直接拒绝
            ValidateLifetime = true,

            // ===== 时钟偏差 =====
            // 默认值是 5 分钟：即使 Token 过期了，还有 5 分钟的宽限期
            // 设为 0 更严格：过期就是过期，没有宽限期
            // 宽限期的存在是为了应对不同服务器之间时钟不完全同步的情况，
            // 单机部署时设为 0 没问题
            ClockSkew = TimeSpan.Zero
        };
    });
```



### 4. 在中间件管道里启用认证

打开 `Program.cs` 的中间件管道部分：

```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();

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

// UseAuthentication 必须在 UseAuthorization 之前
// 原因：Authorization 需要读取 Authentication 的结果（HttpContext.User）
// 如果顺序反了，[Authorize] 读到的 HttpContext.User 是空的，永远判定为未认证
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```



### 5. [Authorize] 特性是怎么工作的

配置好认证中间件之后，保护接口只需要加一个特性标签：

```csharp
[Authorize]                         // 必须登录（任何角色都可以）
[Authorize(Roles = "Admin")]        // 必须是 Admin 角色
[Authorize(Roles = "Admin,User")]   // Admin 或 User 角色都可以
[AllowAnonymous]                    // 明确允许匿名访问（即使 Controller 上有 [Authorize]）
```

`[Authorize]` 加在 Controller 类上，表示这个 Controller 里的所有接口都需要认证。加在单个 Action 方法上，只有这个方法需要认证。后面的车辆、订单等模块会大量用到这个模式——Controller 级别加 `[Authorize]`，少数公开接口再单独加 `[AllowAnonymous]`。

`[Authorize(Roles = "Admin")]` 能生效，是因为 Step 10 里我们生成 Token 时用了 `ClaimTypes.Role` 这个 Key 存角色——框架认识这个 Key，`UseAuthorization()` 中间件会自动读取它来判断角色。



### 6. 验证认证是否生效：临时测试接口

在 `AuthController` 里加一个临时接口，验证配置是否正确：

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
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
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserResponse>.Ok(user, "Registration successful."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
    }

    // 临时测试接口：验证 JWT 认证配置是否生效
    // 测试完成后会删掉，Step 12 会建正式的 GET /users/me
    [HttpGet("test-auth")]
    [Authorize]
    public IActionResult TestAuth()
    {
        // User 是 ControllerBase 提供的属性，类型是 ClaimsPrincipal
        // 认证中间件在验证 Token 后，会把 Token Payload 里的所有 Claims
        // 解析出来注入到这个对象里
        //
        // FindFirst(key)：在 Claims 集合里找第一个匹配 key 的 Claim，返回 Claim 对象
        // ?.Value：取这个 Claim 的值（字符串），用 ?. 是因为找不到时返回 null
        //
        // 为什么要同时找 ClaimTypes.NameIdentifier 和 "sub"？
        // 不同版本的 JWT 库对 "sub" 的处理不一样：
        //   有些版本会把 "sub" 映射成 ClaimTypes.NameIdentifier（一个很长的 URI）
        //   有些版本直接保留 "sub" 这个简短名字
        // 两个都找，确保能取到值
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(ApiResponse<object>.Ok(new { userId, email, role }, "Token is valid."));
    }
}
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



### 8. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**场景一：不带 Token 访问受保护接口（应返回 401）**

直接请求 `GET /auth/test-auth`，不带任何 Token。

预期：返回 401。注意这个 401 不是走 `GlobalExceptionMiddleware` 返回的，而是 ASP.NET Core 的认证中间件直接处理的，格式和我们的 `ApiResponse` 不同，这是正常现象。

**场景二：带有效 Token 访问（应返回 200）**

先调用 `POST /auth/login` 拿到 Token

```json
POST /auth/login
{
  "email": "test@example.com",
  "password": "Test@123456"
}
```

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

然后在请求头里带上：

```
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

Scalar 里可以在请求设置里填入 Bearer Token，不需要手动拼请求头。

预期响应（200）：

```json
{
  "success": true,
  "data": {
    "userId": "1001",
    "email": "test@example.com",
    "role": "User"
  },
  "message": "Token is valid.",
  "errors": null
}
```

能从 Token 里正确解析出用户信息，说明认证配置完全生效。

**场景三：用 Admin Token 验证角色**

用 Admin 账号登录（`admin@uucars.com` / `Admin@123456`）拿到 Token，同样访问 `GET /auth/test-auth`，确认返回的 `role` 字段是 `"Admin"`。

测试完成后 `Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 9. 删除临时测试接口

测试通过后，把 `AuthController` 里的 `TestAuth` 方法删掉。Step 12 会建正式的 `GET /users/me` 接口来取代它：

```csharp
// 整个删掉
[HttpGet("test-auth")]
[Authorize]
public IActionResult TestAuth() { ... }
```



### 10. 此时 Program.cs 服务注册部分完整如下

```csharp
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings")
);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// JWT 认证
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 用户模块
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<UserService>();
```



### 11. Git 提交

```bash
git add .
git commit -m "feat: JWT authentication middleware configuration"
```



### Step 11 完成状态

```
✅ 理解认证和授权的区别及顺序（Authentication 先于 Authorization）
✅ 理解 JWT 认证的完整自动流程（Token 提取 → 验证 → Claims 注入）
✅ JWT Bearer 认证配置（验证签名、Issuer、Audience、过期时间、ClockSkew）
✅ UseAuthentication() 加入中间件管道，位置在 UseAuthorization() 之前
✅ 理解 [Authorize] 特性的工作原理和用法
✅ 理解 HttpContext.User（ClaimsPrincipal）和 FindFirst 的用法
✅ 临时测试接口验证：无 Token 返回 401，有效 Token 正确解析 Claims
✅ 临时测试接口删除，代码保持干净
✅ dotnet build 通过
✅ Git commit 完成
```



## Step 12 · 当前用户信息

### 这一步做什么

Step 11 完成了 JWT 认证配置，但有一块遗留问题：JWT 认证那段代码直接堆在 `Program.cs` 里，将近 20 行，读起来费力。因此先把入口程序整理干净。

用户登录之后，客户端（前端或 App）拿到了一个 JWT Token。Token 里确实包含了用户 Id、邮箱、角色这些基本信息，客户端理论上可以自己解码 Token 拿到这些数据。但我们需要服务端更完整的提供当前登录用户的信息。

因此，本节主要完成如下功能：

- 优化重构功能性配置逻辑
- 创建获取当前登录用户信息的接口



### 1. 重构：把 JWT 配置抽成扩展方法

#### 什么是扩展方法

扩展方法（Extension Methods）是 C# 的一个特性，允许你在不修改原有类的情况下，给它"添加"新方法。语法上只有一个要求：方法必须是静态的，第一个参数前面加 `this` 关键字：

```csharp
// 给 IServiceCollection 扩展一个新方法
public static IServiceCollection AddJwtAuthentication(
    this IServiceCollection services,   // this 关键字：表示这是 IServiceCollection 的扩展方法
    IConfiguration configuration)
{
    // ...
}
```

加了 `this` 之后，这个方法就可以像 `IServiceCollection` 的原生方法一样调用：

```csharp
// 调用时不需要传第一个参数，C# 自动把调用对象传进去
builder.Services.AddJwtAuthentication(builder.Configuration);

// 等价于：
ServiceCollectionExtensions.AddJwtAuthentication(builder.Services, builder.Configuration);
```

ASP.NET Core 里大量用了这个模式——`AddControllers()`、`AddDbContext()`、`AddAuthentication()` 这些方法本质上都是扩展方法，只不过是框架写好的。我们现在做的是同样的事情，只不过是为自己的项目写。

#### 为什么只抽 JWT，不抽其他的

`Program.cs` 里目前有三块功能性配置：

```
UseSerilog(...)                          ← 日志，一段代码，不复杂，留着
AddDbContext(...)                        ← 数据库，一行，留着
AddAuthentication(...).AddJwtBearer(...) ← JWT，将近 20 行，值得抽出去
```

业务模块的 Service 和 Repository 注册继续直接写在 `Program.cs` 里，这是 .NET 社区的普遍习惯，清晰直观，不需要过度封装。

#### 创建扩展方法文件

`Extensions/` 目录在 Step 02 就建好了，专门用来放这类扩展方法：

```bash
touch UUcars.API/Extensions/AuthExtensions.cs
rm UUcars.API/Extensions/.gitkeep
```

```c#
public static class AuthExtensions
{
    // 不能通过 IOptions<JwtSettings> 注入的方法使用jwtSettings配置
    // 因为AddJwtAuthentication的功能还在注册阶段，不是在运行阶段，无法通过DI创建JwtSettings对象实例
    /*
     public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IOptions<JwtSettings> jwtSettings)
    {
        return services;
    }
    */

    // 先从配置系统读取 JwtSettings
    // // 开发环境自动从 User Secrets 读取真实值（Step 03 已配置）
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
        services.AddAuthentication(options =>
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // TokenValidationParameters：告诉框架"验证 Token 时要检查哪些东西"
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // ===== 签名验证 =====
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret)),

                // ===== Issuer 验证 =====
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer, // 值："UUcars"

                // ===== Audience 验证 =====
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience, // 值："UUcarsUsers"

                // ===== 过期时间验证 =====
                ValidateLifetime = true,
                // ===== 时钟偏差 =====
                ClockSkew = TimeSpan.Zero
            };
        });
        
        return services;
    }
}
```

> **为什么 `return services`？** 返回 `IServiceCollection` 本身是为了支持**链式调用**。ASP.NET Core 里经常看到 `builder.Services.AddX().AddY().AddZ()` 这种写法，就是因为每个扩展方法都返回了 `IServiceCollection`，让下一个方法可以继续调用。我们保持这个约定。

#### 更新 Program.cs

顶部 using 补上：

```csharp
using UUcars.API.Extensions;
```

把原来那一大段 JWT 配置替换成一行调用，服务注册区域现在变成：

```csharp
...
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
...

    // =============================================
    // 服务注册
    // =============================================
    builder.Services.AddControllers();
...
    // 配置 JWT 认证
    // JWT 认证（细节在 Extensions/AuthExtensions.cs）
    builder.Services.AddJwtAuthentication(builder.Configuration);
    
    // 用户模块
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<JwtTokenGenerator>();
    builder.Services.AddScoped<UserService>();

    // =============================================
    // 构建应用
    // =============================================
    var app = builder.Build();

...
    app.Run();
}
catch (Exception ex)
{
   ...
}
finally
{
 ...
}
```

**干净了很多——`Program.cs` 只看得到"做了什么"，看不到"怎么做"，细节藏在扩展方法里。**



### 2. 为什么需要新接口 `GET /users/me` ？

用户登录之后，客户端（前端或 App）拿到了一个 JWT Token。Token 里确实包含了用户 Id、邮箱、角色这些基本信息，客户端理论上可以自己解码 Token 拿到这些数据。但这带来几个问题：

- **Token 里的信息可能已经过时**

    Token 是登录时签发的，有效期内不会自动更新。如果用户在另一个设备上修改了用户名，当前设备的 Token 里存的还是旧用户名。客户端如果只靠解码 Token 来展示用户信息，就会显示过时的数据。

- **Token 里存的信息是有限的**

    为了控制 Token 体积，里面只存了几个关键字段（Id、邮箱、角色）。但展示用户资料时可能还需要头像、注册时间、手机号等字段——这些信息不在 Token 里，必须从数据库里查。

- **不应该让客户端自己解码 Token**

    解码 Token 是客户端能做的事，但不代表应该这样做。服务器提供一个专门的接口返回当前用户信息，是更标准、更清晰的做法——客户端不需要了解 Token 的内部结构，只需要调接口。

所以， 需要新接口 `GET /users/me` ， 其作用是：**客户端登录后，通过这个接口从服务器获取最新、最完整的当前用户信息**。这是几乎所有需要登录的系统都会提供的标准接口。 如果不实现它，客户端只能靠解码 Token 来获取用户信息，展示的数据可能过时，且无法获取 Token 里没有的字段——用户中心页、个人资料页这些功能就没办法正常做。



### 3. 封装 `CurrentUserService`服务

Step 11 里提到，JWT 认证中间件会把 Token Payload 里的 Claims 注入到 `HttpContext.User`（`ClaimsPrincipal`）里。

在 Controller 里可以直接通过 `this.User` 访问，但如果把读取 Claims 的逻辑直接写在 Action 里，将来每个需要当前用户的接口都要重复这段代码。更好的做法是封装成一个独立的服务：

但是，不能直接在非 Controller 的地方访问当前 HTTP 请求的上下文，比如Service层。因此ASP.NET Core 提供的一个接口 `IHttpContextAccessor ` 可以间接访问。

注意：这个接口默认不自动注册，需要在 Program.cs 里显式调用 `AddHttpContextAccessor()`

```bash
touch UUcars.API/Services/CurrentUserService.cs
```

```c#
using System.Security.Claims;
namespace UUcars.API.Services;

// 封装"从当前请求的 JWT Token 里提取用户信息"的逻辑
public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // 需要通过 IHttpContextAccessor 间接访问当前 HTTP 请求的上下文。
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return null;

        // ClaimTypes.NameIdentifier 是 ASP.NET Core 对 JWT "sub" Claim 的映射名
        // JWT 库在解析 Token 时，会把 "sub" 这个标准 Claim
        // 自动映射成 ClaimTypes.NameIdentifier（一个很长的 URI 字符串）
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Claim 的 value 是字符串，需要转成 int
        // int.TryParse：转换失败时返回 false，不会抛异常（比 int.Parse 更安全）
        if (int.TryParse(value, out var userId))
            return userId;

        return null;
    }
}
```



### 4. 给 IUserRepository 增加按 Id 查询的方法

`GET /users/me` 拿到用户 Id 之后，需要从数据库里查出用户信息。目前 `IUserRepository` 只有按 Email 查询的方法，补上按 Id 查询：

打开 `UUcars.API/Repositories/IUserRepository.cs`：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    // 新增：按 Id 查询用户
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfUserRepository.cs`，补充实现：

```csharp
public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}
```



### 5. 创建业务异常

用户 Id 不存在时需要返回 404：

```bash
touch UUcars.API/Exceptions/UserNotFoundException.cs
namespace UUcars.API.Exceptions;

public class UserNotFoundException : AppException
{
    public UserNotFoundException(int id)
        : base(StatusCodes.Status404NotFound, $"User with id '{id}' was not found.")
    {
    }
}
```



### 6. 在 UserService 里添加获取当前用户的方法

打开 `UUcars.API/Services/UserService.cs`，添加 `GetCurrentUserAsync`：

```csharp
public async Task<UserResponse> GetCurrentUserAsync(
    int userId,
    CancellationToken cancellationToken = default)
{
    var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
    if (user == null)
        throw new UserNotFoundException(userId);

    return MapToResponse(user);
}
```



### 7. 创建 UsersController

```bash
touch UUcars.API/Controllers/UsersController.cs
```

```c#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("users")]
[Authorize]     // Controller 级别的 [Authorize]：这个 Controller 里所有接口都需要登录
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly CurrentUserService _currentUserService;

    public UsersController(UserService userService, CurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    // GET /users/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();

        // 理论上不会发生（[Authorize] 已经保证了 Token 合法），
        // 但做防御性编程，Token 里没有 sub 时返回 401
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var user = await _userService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(user));
    }
}
```



### 8. 注册新依赖到 Program.cs

```csharp
	// 用户模块
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<JwtTokenGenerator>();
    builder.Services.AddScoped<UserService>();
    
    // AddHttpContextAccessor：IHttpContextAccessor 默认不自动注册，需要显式加上
    // 它内部用 AsyncLocal<T> 保证线程安全，每个请求有自己独立的 HttpContext
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<CurrentUserService>();	
```



### 9. 同步更新 FakeUserRepository

`IUserRepository` 新增了 `GetByIdAsync`，`FakeUserRepository` 也要补上，否则编译报错：

打开 `UUcars.Tests/Fakes/FakeUserRepository.cs`，补充：

```csharp
public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    var user = _store.Values.FirstOrDefault(u => u.Id == id);
    return Task.FromResult(user);
}
```



### 10. 验证编译和测试

```bash
dotnet build
dotnet test
```

两个都应该通过。



### 11. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**GET /users/me（需要带 Token）：**

先登录拿到 Token，然后带上 Authorization 头：

```
GET /users/me
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "id": 2,
    "username": "testuser",
    "email": "test@example.com",
    "role": "User",
    "createdAt": "..."
  }
}
```

**不带 Token 访问（应返回 401）：**

不带 Authorization 头直接访问，预期返回 401。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 12. 此时的目录变化

```
UUcars.API/
├── Controllers/
│   └── UsersController.cs          ← 新增
├── Exceptions/
│   └── UserNotFoundException.cs    ← 新增
├── Extensions/
│   └── AuthExtensions.cs          ← 新增
└── Services/
    ├── CurrentUserService.cs       ← 新增
    └── UserService.cs             ← 已更新

UUcars.Tests/
└── Fakes/
    └── FakeUserRepository.cs      ← 已更新
```



### 13. Git 提交

```bash
git add .
git commit -m "feat: JWT config refactor + GET /users/me + CurrentUserService"
```



### Step 12 完成状态

```
✅ JWT 配置抽成扩展方法（Extensions/AuthExtensions.cs）
✅ 理解扩展方法（this 关键字、静态类、链式调用、return services）
✅ CurrentUserService（封装从 Token Claims 里提取用户 Id 的逻辑）
✅ 理解 IHttpContextAccessor 的作用和注册方式
✅ IUserRepository 新增 GetByIdAsync
✅ UserNotFoundException（404）
✅ UserService 新增 GetCurrentUserAsync
✅ UsersController（GET /users/me，Controller 级别 [Authorize]）
✅ FakeUserRepository 同步更新
✅ dotnet build + dotnet test 全部通过
✅ Scalar 测试通过
✅ Git commit 完成
```



## Step 13 · 修改用户信息

### 这一步做什么

Step 12 实现了查看当前用户信息，这一步实现修改——`PUT /users/me`。完成后补充登录和 Token 生成相关的单元测试，用户系统的所有功能就全部完成了。

目前当前用户信息只包含 `Username` 和 `Email`，没有密码修改。

这是有意为之的设计决策——**修改密码是一个独立的功能，安全要求更高**，和修改用户名/邮箱不应该放在同一个接口里。

原因是：修改密码通常需要先验证旧密码（防止别人拿到你的登录态后悄悄改密码），而修改用户名/邮箱不需要这个步骤。

如果把三个字段放在一个接口里，逻辑会变得混乱：旧密码填了但想改用户名怎么处理？不填旧密码但想改密码怎么处理？

真实项目里通常会有一个单独的接口：

```http
PUT /users/me/password
{
  "currentPassword": "旧密码",
  "newPassword": "新密码"
}
```

我们会在之后的版本中进行密码重置的功能时完成。



### 1. 创建 DTO

#### `DTOs/Requests/UpdateUserRequest.cs`

```bash
touch UUcars.API/DTOs/Requests/UpdateUserRequest.cs
```

```c#
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [MaxLength(50, ErrorMessage = "Username must not exceed 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;
}
```



### 2. 扩展 IUserRepository 和 EfUserRepository

`PUT /users/me` 需要更新用户信息，`IUserRepository` 目前没有更新方法，补上：

打开 `UUcars.API/Repositories/IUserRepository.cs`：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // 新增
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfUserRepository.cs`，补充实现：

```csharp
public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        /*
        EF Core 的变更追踪机制：
        从 DbContext 查出来的实体会被 EF Core "追踪"。
        修改它的属性后调用 SaveChangesAsync，EF Core 自动检测哪些字段变了，
        只对变化的字段生成 UPDATE 语句，不需要手动写 SQL。
        这里的 user 对象是从 GetByIdAsync 查出来的，已经在追踪中，
        直接改属性再 Update + SaveChanges 就行
        */
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
}
```

### 3. 在 UserService 里添加修改用户的方法

打开 `UUcars.API/Services/UserService.cs`，在 `GetCurrentUserAsync` 下方添加：

```csharp
public async Task<UserResponse> UpdateCurrentUserAsync(
    int userId,
    UpdateUserRequest request,
    CancellationToken cancellationToken = default)
{
    var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
    if (user == null)
        throw new UserNotFoundException(userId);

    // 如果邮箱发生了变化，检查新邮箱是否已被其他人使用
    // 这里用 OrdinalIgnoreCase 做大小写不敏感的比较
    // 原因：用户输入的邮箱大小写可能和数据库里存的不一致（数据库存的是小写）
    // 不能直接用 == 比较，否则 "Test@example.com" 和 "test@example.com" 会被判断为不同
    if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // existing.Id != userId：排除自己
        // 场景：用户只改了用户名，邮箱没变，或者邮箱大小写变了但本质一样
        // 如果不排除自己，用原邮箱提交也会触发"邮箱已被注册"的错误
        if (existing != null && existing.Id != userId)
            throw new UserAlreadyExistsException(request.Email);
    }

    user.Username = request.Username;
    user.Email = request.Email.ToLower();
    user.UpdatedAt = DateTime.UtcNow;

    var updated = await _userRepository.UpdateAsync(user, cancellationToken);

    _logger.LogInformation("User updated: {Email}", updated.Email);

    return MapToResponse(updated);
}
```

同时确认 `UserService.cs` 顶部的 using 包含：

```csharp
using UUcars.API.DTOs.Requests;
```



### 4. 在 UsersController 里添加 PUT /users/me

打开 `UUcars.API/Controllers/UsersController.cs`，完整更新如下：

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly CurrentUserService _currentUserService;

    public UsersController(UserService userService, CurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var user = await _userService.GetCurrentUserAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(user));
    }

    // PUT /users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var user = await _userService.UpdateCurrentUserAsync(userId.Value, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(user, "Profile updated successfully."));
    }
}
```



### 5. 同步更新 FakeUserRepository

`IUserRepository` 新增了 `UpdateAsync`，`FakeUserRepository` 也要补上，否则编译报错。

打开 `UUcars.Tests/Fakes/FakeUserRepository.cs`：

```csharp
public Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
{
    // 用更新后的对象覆盖字典里的旧数据
    // 注意：如果邮箱改变了，旧邮箱的 key 仍然存在于字典里
    // 在测试场景下这不影响正确性，因为 GetByEmailAsync 按新邮箱查
    _store[user.Email.ToLower()] = user;
    return Task.FromResult(user);
}
```



### 6. 补充单元测试

这一步要补充登录和 Token 生成相关的单元测试。

判断依据是：`LoginAsync` 里有三条明确的业务规则:

- 用户不存在时抛异常
- 密码错误时抛异常
- 成功时返回含 Token 的响应。

这三条规则出错会有安全隐患，值得用测试锁定。

登录测试里有一个和注册测试不同的地方：测试前需要先有一个密码被正确 Hash 过的用户。如果直接用 `repo.Seed()` 插入一个 `PasswordHash = "somehash"` 的假用户，`VerifyHashedPassword` 会验证失败——因为 `"somehash"` 不是合法的 PBKDF2 格式。

所以登录测试里先调用 `RegisterAsync` 走一遍完整注册流程，让 `PasswordHasher` 生成真实的 Hash，再用同样的密码登录——这样测试最贴近真实场景。

打开 `UUcars.Tests/Services/UserServiceTests.cs`，在已有的三个测试下方添加：

```csharp
[Fact]
public async Task LoginAsync_WithValidCredentials_ShouldReturnLoginResponse()
{
    // Arrange：先注册一个用户，确保数据库里有正确 Hash 过的密码
    var repo = new FakeUserRepository();
    var service = CreateService(repo);

    await service.RegisterAsync(new RegisterRequest
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "Test@123456"
    });

    // Act：用同样的密码登录
    var result = await service.LoginAsync(new LoginRequest
    {
        Email = "test@example.com",
        Password = "Test@123456"
    });

    // Assert
    Assert.NotNull(result);
    // Token 不为空字符串，说明 JWT 生成成功
    Assert.NotEmpty(result.Token);
    Assert.NotNull(result.User);
    Assert.Equal("test@example.com", result.User.Email);
}

[Fact]
public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
{
    // Arrange：先注册用户
    var repo = new FakeUserRepository();
    var service = CreateService(repo);

    await service.RegisterAsync(new RegisterRequest
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "Test@123456"
    });

    // Act + Assert：用错误密码登录，必须抛出 InvalidCredentialsException
    // 注意：不是 "密码错误异常"，而是 InvalidCredentialsException——
    // 邮箱不存在和密码错误返回同一个异常，不透露具体原因
    await Assert.ThrowsAsync<InvalidCredentialsException>(
        () => service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        }));
}

[Fact]
public async Task LoginAsync_WithNonExistentEmail_ShouldThrowInvalidCredentialsException()
{
    // Arrange：空 repo，没有任何用户
    var repo = new FakeUserRepository();
    var service = CreateService(repo);

    // Act + Assert：邮箱不存在时抛出的异常和密码错误完全一样
    // 这是有意为之的安全设计：攻击者无法通过错误信息判断邮箱是否存在
    await Assert.ThrowsAsync<InvalidCredentialsException>(
        () => service.LoginAsync(new LoginRequest
        {
            Email = "notexist@example.com",
            Password = "Test@123456"
        }));
}
```



### 7. 验证编译和测试

```bash
dotnet build
dotnet test
```

预期：

```
Test summary: total: 6, failed: 0, succeeded: 6, skipped: 0
```

原来的 3 个注册测试 + 新增的 3 个登录测试，全部通过。



### 8. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**PUT /users/me（正常修改用户名）：**

先登录拿到 Token，然后：

```json
PUT /users/me
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......

{
  "username": "newname",
  "email": "test@example.com"
}
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "id": 2,
    "username": "newname",
    "email": "test@example.com",
    "role": "User",
    "createdAt": "..."
  },
  "message": "Profile updated successfully."
}
```

**修改成已被占用的邮箱（应返回 409）：**

先注册另一个账号 `other@example.com`，再用当前用户尝试把邮箱改成它：

```json
{
  "username": "newname",
  "email": "other@example.com"
}
```

预期（409）：

```json
{
  "success": false,
  "message": "Email 'other@example.com' is already registered."
}
```

**用同一个邮箱更新自己（应返回 200）：**

邮箱不变只改用户名，不应该触发邮箱冲突检查，应该正常返回 200。这个场景专门验证前面 `existing.Id != userId` 那个判断的正确性。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 9. 此时的目录变化

```
UUcars.API/
├── Controllers/
│   └── UsersController.cs          ← 已更新（新增 PUT /users/me）
├── DTOs/
│   └── Requests/
│       └── UpdateUserRequest.cs    ← 新增
└── Repositories/
    ├── IUserRepository.cs          ← 已更新（新增 UpdateAsync）
    └── EfUserRepository.cs         ← 已更新（新增 UpdateAsync 实现）

UUcars.Tests/
├── Fakes/
│   └── FakeUserRepository.cs      ← 已更新（新增 UpdateAsync）
└── Services/
    └── UserServiceTests.cs        ← 已更新（新增 3 个登录测试）
```



### 10. Git 提交 + 合并分支

```bash
git add .
git commit -m "feat: PUT /users/me - update user profile"
git commit -m "test: unit tests for login / token generation"
```

用户系统全部完成，合并回 `develop`：

```bash
git checkout develop
git merge --no-ff feature/auth -m "merge: feature/auth into develop"
git push origin develop
git branch -D feature/auth
git push origin --delete feature/auth
```



### Step 13 完成状态

```
✅ UpdateUserRequest DTO
✅ IUserRepository 新增 UpdateAsync
✅ EfUserRepository 实现 UpdateAsync（EF Core 变更追踪）
✅ UserService.UpdateCurrentUserAsync（邮箱冲突检查 + 更新）
✅ UsersController 新增 PUT /users/me
✅ FakeUserRepository 同步更新
✅ 理解登录测试为什么要先走注册流程而不是直接 Seed 假数据
✅ 6 个单元测试全部通过（3 个注册 + 3 个登录）
✅ Scalar 测试通过（修改成功 200、邮箱冲突 409、用同邮箱更新自己 200）
✅ feature/auth 合并回 develop
```



## Step 14 · 发布车辆（创建草稿）

### 这一步做什么

用户系统完成了，现在进入车辆模块——整个项目的核心业务。

卖家发布车辆的第一步不是直接上架，而是创建一个草稿（`Draft`）。

草稿只有卖家自己能看到，需要经过提交审核、Admin 审核通过后才会公开。

这一步实现 `POST /cars`，让登录用户能创建一辆车的草稿。



### 1. 切出 feature/cars 分支

```bash
git checkout develop
git checkout -b feature/cars
git push -u origin feature/cars
```



### 2. 为什么 Status 默认是 Draft 而不是直接 Published

这是一个常见的设计问题，直接上架看起来更简单，为什么要多一个草稿状态？

原因有两个：

**第一，给卖家修改的机会。** 车辆信息（标题、价格、描述）填完之后，卖家可能还想调整。草稿状态下可以反复修改，确认没问题再提交审核。如果创建就直接上架，买家看到的可能是还没填完整的信息。

**第二，平台需要审核。** UUcars 是 C2C 平台，任何人都可以注册发车，如果创建就直接公开，平台无法控制内容质量（比如虚假信息、违规车辆）。Draft → PendingReview → Published 的流程让 Admin 有机会审核，这是平台运营的基本需求。



### 3. 创建业务异常

后续车辆模块会用到的业务异常，现在一起建好：

```bash
touch UUcars.API/Exceptions/CarNotFoundException.cs
touch UUcars.API/Exceptions/ForbiddenException.cs
```

```c#
namespace UUcars.API.Exceptions;

public class CarNotFoundException : AppException
{
    public CarNotFoundException(int id)
        : base(StatusCodes.Status404NotFound, $"Car with id '{id}' was not found.")
    {
    }
}
```

```c#
namespace UUcars.API.Exceptions;

// 已登录但无权限操作时抛出（403 Forbidden）
// 和 401 Unauthorized 的区别：
//   401：没有身份（没登录或 Token 无效）→ 去登录
//   403：有身份但没权限（登录了但不是车主）→ 你没有权限做这件事
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(StatusCodes.Status403Forbidden, message)
    {
    }
}
```



### 4. 创建 DTO

#### `DTOs/Requests/CarCreateRequest.cs`

```bash
touch UUcars.API/DTOs/Requests/CarCreateRequest.cs
```

```c#
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CarCreateRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(100, ErrorMessage = "Title must not exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Brand is required.")]
    [MaxLength(50, ErrorMessage = "Brand must not exceed 50 characters.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [MaxLength(50, ErrorMessage = "Model must not exceed 50 characters.")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year is required.")]
    [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Mileage is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Mileage must be 0 or greater.")]
    public int Mileage { get; set; }

    [MaxLength(2000, ErrorMessage = "Description must not exceed 2000 characters.")]
    public string? Description { get; set; }
}
```

#### `DTOs/Responses/CarResponse.cs`

```bash
touch UUcars.API/DTOs/Responses/CarResponse.cs
```

`CarResponse` 是车辆列表和详情接口都会用到的基础响应结构，包含车辆的核心信息：

```csharp
namespace UUcars.API.DTOs.Responses;

public class CarResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```



### 5. 创建 Repository

#### `Repositories/ICarRepository.cs`

```bash
touch UUcars.API/Repositories/ICarRepository.cs
```

这一步只需要新增车辆，但后续步骤（修改、删除、查询）都会用到 Repository，现在把这一步需要的方法定义好，后续步骤再逐步补充：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface ICarRepository
{
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);
}
```

#### `Repositories/EfCarRepository.cs`

```bash
touch UUcars.API/Repositories/EfCarRepository.cs
```

```c#
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfCarRepository : ICarRepository
{
    private readonly AppDbContext _context;

    public EfCarRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default)
    {
        _context.Cars.Add(car);
        await _context.SaveChangesAsync(cancellationToken);
        return car;
    }
}
```



### 6. 创建 CarService

```bash
touch UUcars.API/Services/CarService.cs
```

```c#
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class CarService
{
    private readonly ICarRepository _carRepository;
    private readonly ILogger<CarService> _logger;

    public CarService(ICarRepository carRepository, ILogger<CarService> logger)
    {
        _carRepository = carRepository;
        _logger = logger;
    }

    public async Task<CarResponse> CreateAsync(
        int sellerId,
        CarCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var car = new Car
        {
            Title = request.Title,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            Price = request.Price,
            Mileage = request.Mileage,
            Description = request.Description,
            SellerId = sellerId,            // 从 Token 里取到的当前用户 Id
            Status = CarStatus.Draft,       // 创建时强制为 Draft，客户端无法指定
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _carRepository.AddAsync(car, cancellationToken);

        _logger.LogInformation("Car created: {CarId} by seller {SellerId}", created.Id, sellerId);
        
        // Create 完之后，返回里必须要有 SellerUsername的话，Add 完 → 再查一次
        //因为GetByIdAsync带 Include， 手动加载car.Seller（导航属性），这样car.Seller就不是null了
        var carWithSeller= await _carRepository.GetByIdAsync(created.Id, cancellationToken);
        return MapToResponse(carWithSeller!);

    }

    // 实体 → DTO 的映射方法
    // 注意 SellerUsername 暂时用空字符串——创建时 EF Core 不会自动加载导航属性
    // 后续详情接口会用 Include 加载完整的 Seller 信息
    internal static CarResponse MapToResponse(Car car) => new()
    {
        Id = car.Id,
        Title = car.Title,
        Brand = car.Brand,
        Model = car.Model,
        Year = car.Year,
        Price = car.Price,
        Mileage = car.Mileage,
        Description = car.Description,
        Status = car.Status.ToString(),
        SellerId = car.SellerId,
        SellerUsername = car.Seller?.Username ?? string.Empty,
        CreatedAt = car.CreatedAt,
        UpdatedAt = car.UpdatedAt
    };
}
```

> **为什么 `Status` 在 Service 里强制写成 `CarStatus.Draft`，而不是让客户端传入？** 因为状态流转是核心业务规则，不能由客户端控制。如果客户端能传 `Status`，就可以直接创建一个 `Published` 的车辆绕过审核流程。规则必须在服务端强制执行，`Status = CarStatus.Draft` 硬写在 Service 里，任何人都改不了。

> **`MapToResponse` 为什么用 `internal` 而不是 `private`？** `private` 只有类内部能用。后续步骤里其他方法（比如列表查询）也需要把 `Car` 实体映射成 `CarResponse`，都在 `CarService` 里，用 `internal static` 让同一个类的其他方法都能复用这个映射，又不暴露给外部。



### 7. 创建 CarsController

```bash
touch UUcars.API/Controllers/CarsController.cs
```

```c#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("cars")]
public class CarsController : ControllerBase
{
    private readonly CarService _carService;
    private readonly CurrentUserService _currentUserService;

    public CarsController(CarService carService, CurrentUserService currentUserService)
    {
        _carService = carService;
        _currentUserService = currentUserService;
    }

    // POST /cars
    // 不在 Controller 级别加 [Authorize]，因为后续会有公开接口（车辆列表、搜索、详情）
    // 只在需要登录的 Action 上单独加 [Authorize]
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CarCreateRequest request,
        CancellationToken cancellationToken)
    {
        var sellerId = _currentUserService.GetCurrentUserId();
        if (sellerId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var car = await _carService.CreateAsync(sellerId.Value, request, cancellationToken);

        // 201 Created：创建成功，返回新建的资源
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<CarResponse>.Ok(car, "Car draft created successfully."));
    }
}
```

> **为什么这里不在 Controller 级别加 `[Authorize]`？** `UsersController` 里所有接口都需要登录，所以在 Controller 级别加。但 `CarsController` 里情况不同——车辆列表、搜索、详情这些接口是公开的（不需要登录也能浏览），而发布、修改、删除需要登录。如果在 Controller 级别加 `[Authorize]`，就要在公开接口上逐个加 `[AllowAnonymous]` 来解除限制，反而更麻烦。所以只在需要登录的 Action 上单独加 `[Authorize]`。



### 8. 注册依赖到 Program.cs

```csharp
// 车辆模块
builder.Services.AddScoped<ICarRepository, EfCarRepository>();
builder.Services.AddScoped<CarService>();
```

顶部补上 using：

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

**正常创建车辆草稿（需要登录）：**

先登录拿到 Token，然后：

```json
POST /cars
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......

{
  "title": "2007 BMW 325i – Clean, Reliable & Drives Great !Moving abroad!",
  "brand": "BMW",
  "model": "3 Series",
  "year": 2007,
  "price": 5700,
  "mileage": 122310,
  "description": "Selling my well-maintained 2007 BMW 325i. This car is in great condition for its age and drives smoothly with plenty of power."
}
```

预期（201）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "2007 BMW 325i – Clean, Reliable & Drives Great !Moving abroad!",
    "brand": "BMW",
    "model": "3 Series",
    "year": 2007,
    "price": 5700,
    "mileage": 122310,
    "description": "Selling my well-maintained 2007 BMW 325i. This car is in great condition for its age and drives smoothly with plenty of power.",
    "status": "Draft",
    "sellerId": 1001,
    "sellerUsername": "",
    "createdAt": "2026-04-06T12:34:59.243803Z",
    "updatedAt": "2026-04-06T12:34:59.243803Z"
  },
  "message": "Car draft created successfully.",
  "errors": null
}
```

注意 `status` 字段是 `"Draft"`，不管请求里传了什么，Service 层始终强制为草稿。

**不带 Token 创建（应返回 401）：**

不带 Authorization 头直接请求，预期返回 401。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 11. 此时的目录变化

```
UUcars.API/
├── Controllers/
│   └── CarsController.cs               ← 新增
├── DTOs/
│   ├── Requests/
│   │   └── CarCreateRequest.cs         ← 新增
│   └── Responses/
│       └── CarResponse.cs              ← 新增
├── Exceptions/
│   ├── CarNotFoundException.cs         ← 新增
│   └── ForbiddenException.cs           ← 新增
├── Repositories/
│   ├── ICarRepository.cs               ← 新增
│   └── EfCarRepository.cs              ← 新增
└── Services/
    └── CarService.cs                   ← 新增
```



### 12. Git 提交

```bash
git add .
git commit -m "feat: POST /cars - create car draft"
```



### Step 14 完成状态

```
✅ 切出 feature/cars 分支
✅ 理解为什么新建车辆默认是 Draft 状态
✅ 理解 401 和 403 的区别（CarNotFoundException / ForbiddenException）
✅ CarCreateRequest DTO（含字段验证）
✅ CarResponse DTO
✅ ICarRepository + EfCarRepository
✅ CarService.CreateAsync（Status 强制为 Draft，SellerId 从 Token 取）
✅ CarsController POST /cars（Action 级别 [Authorize]，原因清楚）
✅ 理解 MapToResponse 用 internal 而不是 private 的原因
✅ DI 注册完成
✅ Scalar 测试通过（201 创建成功，401 未登录）
✅ Git commit 完成
```



## Step 15 · 提交审核

### 这一步做什么

Step 14 实现了创建车辆草稿。草稿创建好之后，卖家需要一个操作来把车辆"提交给 Admin 审核"——这就是 `POST /cars/{id}/submit`。

提交后车辆状态从 `Draft` 变成 `PendingReview`，Admin 才能看到并处理它。



### 1. 这个接口的设计思路

`POST /cars/{id}/submit` 是一个**状态变更操作**，不是创建资源也不是修改资源字段，而是触发一个业务动作。

REST 里对这类操作的惯用设计是在资源 URL 后面加一个动词子路径：

```
POST /cars/{id}/submit    ← 提交审核
POST /cars/{id}/cancel    ← 取消（后面订单模块也是这个模式）
POST /orders/{id}/cancel  ← 取消订单
```

用 `POST` 而不是 `PUT` 的原因是：这不是"修改车辆字段"，而是"触发一个不可逆的业务动作"。

`PUT` 通常用于全量更新资源字段，`POST` 更适合表达"执行一个操作"。



### 2. 业务规则

提交审核有两条必须在 Service 层强制执行的规则：

```
1. 只有车主可以提交（别人的车不能提交）
2. 只有 Draft 状态可以提交（PendingReview / Published 的车不能重复提交）
```

为什么要在 Service 层而不是 Controller 层做这些检查？

因为 Controller 只处理 HTTP 层面的事，业务规则应该在 Service 里。这样规则是集中管理的，单元测试也能直接测到。



### 3. 给 ICarRepository 增加新方法

提交审核需要先查出车辆，再更新状态，Repository 需要补上这两个方法：

打开 `UUcars.API/Repositories/ICarRepository.cs`：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface ICarRepository
{
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);

    // 新增
    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfCarRepository.cs`，补充实现：

```csharp
public async Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    return await _context.Cars
        .Include(c => c.Seller)     // 同时加载 Seller 导航属性
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}

public async Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default)
{
    _context.Cars.Update(car);
    await _context.SaveChangesAsync(cancellationToken);
    return car;
}
```

> **`Include(c => c.Seller)` 是什么？** EF Core 默认是**懒加载关闭**的——查出 `Car` 对象时，导航属性 `Seller`（关联的 `User` 对象）默认是 `null`，不会自动去数据库查。`Include` 是**预加载（Eager Loading）**，告诉 EF Core"查 Car 的同时把关联的 Seller 也一起查出来"，生成的 SQL 会是一个 JOIN 语句。
>
> 为什么这里要加 `Include(c => c.Seller)`？因为 `CarResponse` 里有 `SellerUsername` 字段，需要从 `car.Seller.Username` 取值。如果不 `Include`，`car.Seller` 是 `null`，`car.Seller.Username` 会抛 `NullReferenceException`。



### 4. 在 CarService 里添加提交审核的方法

打开 `UUcars.API/Services/CarService.cs`，添加 `SubmitForReviewAsync`：

```csharp
public async Task<CarResponse> SubmitForReviewAsync(
    int carId,
    int currentUserId,
    CancellationToken cancellationToken = default)
{
    var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

    // 1. 车辆不存在
    if (car == null)
        throw new CarNotFoundException(carId);

    // 2. 不是车主
    // 为什么要检查这个？如果不检查，任何登录用户都能提交别人的车去审核，
    // 卖家会莫名其妙发现自己的草稿进入了审核状态
    if (car.SellerId != currentUserId)
        throw new ForbiddenException();

    // 3. 不是 Draft 状态
    // 为什么要检查这个？防止重复提交。已经在 PendingReview 的车再次提交没有意义，
    // Published 的车更不应该被重新提交审核（会破坏状态机）
    if (car.Status != CarStatus.Draft)
        throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

    car.Status = CarStatus.PendingReview;
    car.UpdatedAt = DateTime.UtcNow;

    var updated = await _carRepository.UpdateAsync(car, cancellationToken);

    _logger.LogInformation("Car {CarId} submitted for review by seller {SellerId}",
        car.Id, currentUserId);

    return MapToResponse(updated);
}
```

这里用到了一个新的异常 `CarStatusException`，下面创建它。



### 5. 创建 CarStatusException

状态不合法时，需要一个能清楚表达"当前状态是什么、期望什么状态"的异常，而不是笼统地返回 400：

```bash
touch UUcars.API/Exceptions/CarStatusException.cs
using UUcars.API.Entities.Enums;

namespace UUcars.API.Exceptions;

// 车辆状态不满足操作要求时抛出
// 比如：只有 Draft 状态才能提交审核，当前是 Published 就抛这个异常
public class CarStatusException : AppException
{
    public CarStatusException(int carId, CarStatus currentStatus, CarStatus requiredStatus)
        : base(StatusCodes.Status409Conflict,
            $"Car '{carId}' is in '{currentStatus}' status. Required status: '{requiredStatus}'.")
    {
    }
}
```

> **为什么是 409 Conflict 而不是 400 Bad Request？** 400 表示"请求格式有问题"，比如字段缺失或格式错误。409 表示"请求本身没问题，但当前资源的状态和操作冲突"——车辆已经在审核中，再次提交就是一种状态冲突。409 更准确地表达了这个业务含义。



### 6. 在 CarsController 里添加提交审核端点

打开 `UUcars.API/Controllers/CarsController.cs`，在 `Create` 方法下方添加：

```csharp
// POST /cars/{id}/submit
[HttpPost("{id:int}/submit")]
[Authorize]
public async Task<IActionResult> Submit(int id, CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var car = await _carService.SubmitForReviewAsync(id, currentUserId.Value, cancellationToken);
    return Ok(ApiResponse<CarResponse>.Ok(car, "Car submitted for review successfully."));
}
```

> **路由 `{id:int}` 里的 `:int` 是什么？** 这是路由约束（Route Constraint）。`:int` 表示这个路由参数必须能解析成整数，如果 URL 里传的是非数字（比如 `/cars/abc/submit`），框架直接返回 404，不会进入这个 Action。这是一种轻量级的输入保护，不需要在代码里手动检查。



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



### 8. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常提交审核：**

先登录，再创建一辆车拿到 `id`，然后提交：

```
POST /cars/1/submit
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "status": "PendingReview",
    ...
  },
  "message": "Car submitted for review successfully."
}
```

**重复提交（应返回 409）：**

对同一辆车再次调用 `/cars/1/submit`，预期：

```json
{
  "success": false,
  "message": "Car '1' is in 'PendingReview' status. Required status: 'Draft'."
}
```

**提交别人的车（应返回 403）：**

换一个用户登录，尝试提交不属于自己的车辆，预期返回 403。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 9. 此时的目录变化

```
UUcars.API/
├── Exceptions/
│   └── CarStatusException.cs       ← 新增
└── Repositories/
    ├── ICarRepository.cs           ← 已更新（新增 GetByIdAsync / UpdateAsync）
    └── EfCarRepository.cs          ← 已更新（新增实现，含 Include 说明）
```

`CarService` 和 `CarsController` 已更新（新增方法），不是新文件，不在目录变化里单独列出。



### 10. Git 提交

```bash
git add .
git commit -m "feat: POST /cars/{id}/submit - submit car for review"
git push origin feature/cars
```



### Step 15 完成状态

```
✅ 理解状态变更接口的 REST 设计（POST + 动词子路径）
✅ 理解业务规则为什么在 Service 层而不是 Controller 层
✅ ICarRepository 新增 GetByIdAsync（含 Include 预加载）/ UpdateAsync
✅ EfCarRepository 实现新方法（理解 Include Eager Loading）
✅ CarStatusException（409，清楚表达状态冲突）
✅ 理解 409 vs 400 的语义区别
✅ CarService.SubmitForReviewAsync（车主检查 + 状态检查）
✅ CarsController POST /cars/{id}/submit（路由约束 :int）
✅ Scalar 测试通过（200 成功、409 重复提交、403 非车主）
✅ Git commit + push 完成
```



## Step 16 · 修改车辆

### 这一步做什么

卖家创建草稿之后，可能需要修改车辆信息（比如调整价格、补充描述）。这一步实现 `PUT /cars/{id}`。

但不是什么时候都能修改——已经提交审核或者已上架的车辆不允许修改。原因很直接：如果车辆正在审核中，卖家偷偷修改了信息，Admin 审核的内容就和最终上架的内容不一致；如果车辆已经公开，买家正在查看，卖家随意修改信息会造成信息不一致的问题。所以修改只允许在 `Draft` 状态进行。



### 1. 创建 DTO

#### `DTOs/Requests/CarUpdateRequest.cs`

```bash
touch UUcars.API/DTOs/Requests/CarUpdateRequest.cs
```

`CarUpdateRequest` 和 `CarCreateRequest` 字段基本一致——修改时可以改所有业务字段，但 `Status` 和 `SellerId` 不能改（状态由业务流程控制，卖家是创建时锁定的）：

```csharp
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CarUpdateRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(100, ErrorMessage = "Title must not exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Brand is required.")]
    [MaxLength(50, ErrorMessage = "Brand must not exceed 50 characters.")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [MaxLength(50, ErrorMessage = "Model must not exceed 50 characters.")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year is required.")]
    [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100.")]
    public int Year { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Mileage is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Mileage must be 0 or greater.")]
    public int Mileage { get; set; }

    [MaxLength(2000, ErrorMessage = "Description must not exceed 2000 characters.")]
    public string? Description { get; set; }
}
```

> **`CarUpdateRequest` 和 `CarCreateRequest` 字段几乎一样，为什么不复用同一个类？** 虽然现在字段相同，但两者的语义不同——一个是"创建时传入的数据"，一个是"修改时传入的数据"。将来业务变化时，两者可能会出现差异（比如创建时必填的字段，修改时允许为空）。分开定义是为了保持灵活性，避免将来一个类的改动影响到另一个场景。这是 DTO 设计的常见原则：**按使用场景定义，而不是按字段相似度复用。**



### 2. 在 CarService 里添加修改方法

打开 `UUcars.API/Services/CarService.cs`，在 `SubmitForReviewAsync` 下方添加：

```csharp
public async Task<CarResponse> UpdateAsync(
    int carId,
    int currentUserId,
    CarUpdateRequest request,
    CancellationToken cancellationToken = default)
{
    var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

    if (car == null)
        throw new CarNotFoundException(carId);

    // 只有车主可以修改
    if (car.SellerId != currentUserId)
        throw new ForbiddenException();

    // 只有 Draft 状态可以修改
    // PendingReview：正在审核中，修改会导致审核内容和实际内容不一致
    // Published：已上架，买家正在查看，不允许随意修改
    // Sold / Deleted：已完成或已删除，修改没有意义
    if (car.Status != CarStatus.Draft)
        throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

    car.Title = request.Title;
    car.Brand = request.Brand;
    car.Model = request.Model;
    car.Year = request.Year;
    car.Price = request.Price;
    car.Mileage = request.Mileage;
    car.Description = request.Description;
    car.UpdatedAt = DateTime.UtcNow;

    var updated = await _carRepository.UpdateAsync(car, cancellationToken);

    _logger.LogInformation("Car {CarId} updated by seller {SellerId}", car.Id, currentUserId);

    return MapToResponse(updated);
}
```



### 3. 在 CarsController 里添加 PUT /cars/{id}

打开 `UUcars.API/Controllers/CarsController.cs`，在 `Submit` 方法下方添加：

```csharp
// PUT /cars/{id}
[HttpPut("{id:int}")]
[Authorize]
public async Task<IActionResult> Update(
    int id,
    [FromBody] CarUpdateRequest request,
    CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var car = await _carService.UpdateAsync(id, currentUserId.Value, request, cancellationToken);
    return Ok(ApiResponse<CarResponse>.Ok(car, "Car updated successfully."));
}
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



### 5. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常修改（Draft 状态）：**

先登录，创建一辆车（此时是 Draft），然后修改：

```json
PUT /cars/1
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......

{
    "title": "2009 Nissan Murano", 
    "brand": "Nissan", 
    "model": "Murano", 
    "year": 2009, 
    "price": 3500, 
    "mileage": 240000, 
    "description": "Sunroof, Leather, heated, electric seats. Wof goes out this month has rego. O2 sensor has come up on vehicle scanner, however still runs and drives perfectly, has been my daily vehicle. Please note this is the only reason why the check engine light is on. PM for more details and pictures. Only selling as I’d like to downsize, car has a lot of room and is spacious."
}
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "title": "2020款宝马3系 价格调整",
    "price": 260000,
    "status": "Draft",
    ...
  },
  "message": "Car updated successfully."
}
```

**修改已提交审核的车（应返回 409）：**

先把车辆提交审核（`POST /cars/1/submit`），再尝试修改，预期：

```json
{
  "success": false,
  "message": "Car '1' is in 'PendingReview' status. Required status: 'Draft'."
}
```

**修改别人的车（应返回 403）：**

换一个用户的 Token，尝试修改不属于自己的车，预期返回 403。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 6. 此时的目录变化

```
UUcars.API/
└── DTOs/
    └── Requests/
        └── CarUpdateRequest.cs     ← 新增
```

`CarService` 和 `CarsController` 新增了方法，不是新文件。



### 7. Git 提交

```bash
git add .
git commit -m "feat: PUT /cars/{id} - edit car draft"
git push origin feature/cars
```



### Step 16 完成状态

```
✅ 理解为什么只有 Draft 状态可以修改（审核中和已上架的车不允许随意改）
✅ CarUpdateRequest DTO（理解和 CarCreateRequest 分开定义的原因）
✅ CarService.UpdateAsync（车主检查 + Draft 状态检查）
✅ CarsController PUT /cars/{id}
✅ Scalar 测试通过（200 修改成功、409 非 Draft 状态、403 非车主）
✅ Git commit + push 完成
```



## Step 17 · 删除车辆（逻辑删除）

### 这一步做什么

卖家可以删除自己发布的车辆。但"删除"在这里不是真正把数据从数据库里删掉，而是把 `Status` 改成 `Deleted`——这叫**逻辑删除**。



### 1. 为什么用逻辑删除而不是真正删除

真正删除（物理删除）会把数据库里的行直接删掉，带来几个问题：

**第一，数据无法恢复。** 卖家误操作删了车辆，没有任何补救手段。

**第二，关联数据会出问题。** 如果这辆车有历史订单记录，删掉车辆后订单里的 `CarId` 就成了悬空外键。虽然 Step 06 里配置了 `DeleteBehavior.Restrict` 会阻止这种情况，但这说明物理删除在有关联数据时根本无法执行。

**第三，平台运营需要数据留存。** 即使车辆下架，平台可能仍然需要这条记录做数据分析、纠纷处理等。

逻辑删除把 `Status` 改成 `Deleted`，数据还在，但对外不可见——公开列表过滤掉 `Deleted` 状态的车辆，卖家也看不到它，效果上等同于删除，但数据是安全保留的。



### 2. 哪些状态的车辆可以被删除

并不是所有状态都能删：

```
Draft          → 可以删除（草稿，还没提交审核）
PendingReview  → 不能删除（正在审核中，需要先撤回）
Published      → 不能删除（已上架，可能有买家正在查看）
Sold           → 不能删除（已有成交订单，需要保留记录）
Deleted        → 已经是删除状态，不需要重复操作
```

只有 `Draft` 状态的车辆允许删除。这是一个有意的约束——如果卖家想下架 `Published` 的车辆，后续会有专门的下架流程（目前 V1 不实现，V2 可以扩展）。



### 3. 在 CarService 里添加删除方法

打开 `UUcars.API/Services/CarService.cs`，在 `UpdateAsync` 下方添加：

```csharp
public async Task DeleteAsync(
    int carId,
    int currentUserId,
    CancellationToken cancellationToken = default)
{
    var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

    if (car == null)
        throw new CarNotFoundException(carId);

    // 只有车主可以删除
    if (car.SellerId != currentUserId)
        throw new ForbiddenException();

    // 只有 Draft 状态可以删除
    // 其他状态（PendingReview / Published / Sold）有更严格的业务约束，
    // 不允许直接逻辑删除
    if (car.Status != CarStatus.Draft)
        throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

    // 逻辑删除：只改状态，不删行
    car.Status = CarStatus.Deleted;
    car.UpdatedAt = DateTime.UtcNow;

    await _carRepository.UpdateAsync(car, cancellationToken);

    _logger.LogInformation("Car {CarId} deleted by seller {SellerId}", car.Id, currentUserId);
}
```

> **`DeleteAsync` 为什么不返回 `CarResponse`，而是返回 `void`（`Task`）？** 删除操作成功后，资源已经不存在了，没有有意义的数据需要返回给客户端。Controller 会返回 `204 No Content`，表示"操作成功，没有响应体"。这是 REST 规范对删除操作的标准做法——`200 OK` 带响应体，`204 No Content` 不带响应体。



### 4. 在 CarsController 里添加 DELETE /cars/{id}

打开 `UUcars.API/Controllers/CarsController.cs`，在 `Update` 方法下方添加：

```csharp
// DELETE /cars/{id}
[HttpDelete("{id:int}")]
[Authorize]
public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    await _carService.DeleteAsync(id, currentUserId.Value, cancellationToken);

    // 204 No Content：删除成功，无响应体
    // 不用 ApiResponse 包装，因为没有数据需要返回
    return NoContent();
}
```

> **为什么这里不用 `ApiResponse<T>` 包装？** `ApiResponse<T>` 是为了统一有数据返回的接口格式。`204 No Content` 按 HTTP 规范本来就没有响应体，强行包一层 `ApiResponse` 反而违反了语义。客户端收到 204 就知道操作成功了，不需要解析响应体。



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



### 6. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常删除（Draft 状态）：**

先登录，创建一辆新车（此时是 Draft），然后删除：

```
DELETE /cars/2
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（204）：无响应体，只有状态码。

可以去数据库里查一下这辆车的 `Status` 字段，应该变成了 `Deleted`，行本身还在。

**删除已提交审核的车（应返回 409）：**

先把车辆提交审核（`POST /cars/1/submit`），再尝试删除，预期：

```json
{
  "success": false,
  "message": "Car '1' is in 'PendingReview' status. Required status: 'Draft'."
}
```

**删除别人的车（应返回 403）：**

换一个用户的 Token，尝试删除不属于自己的车，预期返回 403。

**删除不存在的车（应返回 404）：**

```
DELETE /cars/9999
```

预期：

```json
{
  "success": false,
  "message": "Car with id '9999' was not found."
}
```

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 7. 此时的目录变化

这一步没有新增文件，只在已有的 `CarService` 和 `CarsController` 里新增了方法。



### 8. Git 提交

```bash
git add .
git commit -m "feat: DELETE /cars/{id} - soft delete car draft"
git push origin feature/cars
```



### Step 17 完成状态

```
✅ 理解逻辑删除 vs 物理删除（数据保留、关联安全、运营需要）
✅ 理解哪些状态可以删除（只有 Draft）及原因
✅ CarService.DeleteAsync（车主检查 + Draft 状态检查 + 逻辑删除）
✅ 理解 DeleteAsync 返回 Task 而不是 CarResponse 的原因
✅ CarsController DELETE /cars/{id}（返回 204 No Content）
✅ 理解 204 不用 ApiResponse 包装的原因
✅ Scalar 测试通过（204 删除成功、409 非 Draft、403 非车主、404 不存在）
✅ Git commit + push 完成
```



## Step 18 · 车辆图片管理

### 这一步做什么

车辆创建好之后，卖家需要给车辆添加图片。这一步实现两个接口：

```
POST   /cars/{id}/images              ← 给车辆添加一张图片
DELETE /cars/{id}/images/{imageId}    ← 删除车辆的某张图片
```

V1 里图片以 URL 形式提交（卖家填写图片地址），不做真实文件上传。真实文件上传（上传到云存储）是 V2 的内容，但表结构和接口设计现在就按最终形态来做，V2 只需要改 URL 的生成方式，接口本身不需要变。



### 1. 图片操作为什么限制在 Draft 状态

和修改车辆信息的限制一样：

```
Draft         → 可以添加/删除图片（还没提交，随意调整）
PendingReview → 不可以（正在审核，图片不能改）
Published     → 不可以（已上架，买家正在看）
Sold/Deleted  → 不可以（已完成或已删除）
```

图片是车辆信息的一部分，审核时 Admin 看到的图片和最终上架的图片必须一致，所以提交审核之后图片就不能再动了。



### 2. 创建 DTO

#### `DTOs/Requests/CarImageAddRequest.cs`

```bash
touch UUcars.API/DTOs/Requests/CarImageAddRequest.cs
```

```c#
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class CarImageAddRequest
{
    // V1 只支持提交 URL，不做真实文件上传
    // URL 格式验证：[Url] 特性会检查是否是合法的 HTTP/HTTPS 地址
    [Required(ErrorMessage = "ImageUrl is required.")]
    [Url(ErrorMessage = "ImageUrl must be a valid URL.")]
    [MaxLength(500, ErrorMessage = "ImageUrl must not exceed 500 characters.")]
    public string ImageUrl { get; set; } = string.Empty;

    // 显示顺序，默认 0（第一张图片作为封面）
    // 客户端可以传入顺序值控制图片排列
    public int SortOrder { get; set; } = 0;
}
```

#### `DTOs/Responses/CarImageResponse.cs`

```bash
touch UUcars.API/DTOs/Responses/CarImageResponse.cs
```

```c#
namespace UUcars.API.DTOs.Responses;

public class CarImageResponse
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int CarId { get; set; }
}
```



### 3. 创建 ICarImageRepository

图片的数据访问逻辑和车辆本身分开——职责清晰，也方便后续单独扩展（比如 V2 要做图片排序、设置封面等操作）：

```bash
touch UUcars.API/Repositories/ICarImageRepository.cs
touch UUcars.API/Repositories/EfCarImageRepository.cs
```

```c#
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface ICarImageRepository
{
    Task<CarImage> AddAsync(CarImage image, CancellationToken cancellationToken = default);
    Task<CarImage?> GetByIdAsync(int imageId, CancellationToken cancellationToken = default);
    Task DeleteAsync(CarImage image, CancellationToken cancellationToken = default);
}
```

```c#
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfCarImageRepository : ICarImageRepository
{
    private readonly AppDbContext _context;

    public EfCarImageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CarImage> AddAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        _context.CarImages.Add(image);
        await _context.SaveChangesAsync(cancellationToken);
        return image;
    }

    public async Task<CarImage?> GetByIdAsync(int imageId, CancellationToken cancellationToken = default)
    {
        return await _context.CarImages
            .Include(ci => ci.Car)  // 加载关联的 Car，用于后续验证车主和状态
            .FirstOrDefaultAsync(ci => ci.Id == imageId, cancellationToken);
    }

    public async Task DeleteAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        // 图片是真正的物理删除——图片记录本身没有业务含义，删了就是删了
        // 和车辆的逻辑删除不同：车辆有关联订单等历史数据需要保留，
        // 图片只是附属资源，删掉不影响任何业务记录
        _context.CarImages.Remove(image);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

> **这里图片用物理删除，和车辆的逻辑删除形成对比。** 判断依据是：这条数据删了之后，会不会有其他地方找不到它而出问题？车辆删了订单里的 CarId 就成了悬空外键，所以逻辑删除。图片删了没有任何其他表引用它，物理删除完全安全。



### 4. 创建业务异常

```bash
touch UUcars.API/Exceptions/CarImageNotFoundException.cs
namespace UUcars.API.Exceptions;

public class CarImageNotFoundException : AppException
{
    public CarImageNotFoundException(int imageId)
        : base(StatusCodes.Status404NotFound, $"Car image with id '{imageId}' was not found.")
    {
    }
}
```



### 5. 在 CarService 里添加图片管理方法

打开 `UUcars.API/Services/CarService.cs`，构造函数注入 `ICarImageRepository`，并添加两个方法：

先更新构造函数：

```csharp
private readonly ICarRepository _carRepository;
private readonly ICarImageRepository _carImageRepository;  // 新增
private readonly ILogger<CarService> _logger;

public CarService(
    ICarRepository carRepository,
    ICarImageRepository carImageRepository,     // 新增
    ILogger<CarService> logger)
{
    _carRepository = carRepository;
    _carImageRepository = carImageRepository;   // 新增
    _logger = logger;
}
```

再添加两个图片管理方法：

```csharp
public async Task<CarImageResponse> AddImageAsync(
    int carId,
    int currentUserId,
    CarImageAddRequest request,
    CancellationToken cancellationToken = default)
{
    var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

    if (car == null)
        throw new CarNotFoundException(carId);

    if (car.SellerId != currentUserId)
        throw new ForbiddenException();

    // 只有 Draft 状态才能添加图片
    if (car.Status != CarStatus.Draft)
        throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

    var image = new CarImage
    {
        CarId = carId,
        ImageUrl = request.ImageUrl,
        SortOrder = request.SortOrder
    };

    var created = await _carImageRepository.AddAsync(image, cancellationToken);

    _logger.LogInformation("Image added to car {CarId} by seller {SellerId}", carId, currentUserId);

    return MapToImageResponse(created);
}

public async Task DeleteImageAsync(
    int carId,
    int imageId,
    int currentUserId,
    CancellationToken cancellationToken = default)
{
    // 先验证车辆存在（保证 carId 是合法的）
    var car = await _carRepository.GetByIdAsync(carId, cancellationToken);
    if (car == null)
        throw new CarNotFoundException(carId);

    if (car.SellerId != currentUserId)
        throw new ForbiddenException();

    if (car.Status != CarStatus.Draft)
        throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

    // 再验证图片存在，且属于这辆车
    // 为什么要检查 image.CarId == carId？
    // 防止用户构造 /cars/1/images/99 这样的请求来删除属于 car 99 的图片
    // URL 里的 carId 和图片实际的 CarId 必须匹配
    var image = await _carImageRepository.GetByIdAsync(imageId, cancellationToken);
    if (image == null || image.CarId != carId)
        throw new CarImageNotFoundException(imageId);

    await _carImageRepository.DeleteAsync(image, cancellationToken);

    _logger.LogInformation("Image {ImageId} deleted from car {CarId} by seller {SellerId}",
        imageId, carId, currentUserId);
}

// 图片实体 → DTO 映射
private static CarImageResponse MapToImageResponse(CarImage image) => new()
{
    Id = image.Id,
    ImageUrl = image.ImageUrl,
    SortOrder = image.SortOrder,
    CarId = image.CarId
};
```

同时在 `CarService.cs` 顶部补上 using：

```csharp
using UUcars.API.DTOs.Requests;
```



### 6. 在 CarsController 里添加图片管理端点

打开 `UUcars.API/Controllers/CarsController.cs`，在 `Delete` 方法下方添加：

```csharp
// POST /cars/{id}/images
[HttpPost("{id:int}/images")]
[Authorize]
public async Task<IActionResult> AddImage(
    int id,
    [FromBody] CarImageAddRequest request,
    CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var image = await _carService.AddImageAsync(id, currentUserId.Value, request, cancellationToken);

    return StatusCode(StatusCodes.Status201Created,
        ApiResponse<CarImageResponse>.Ok(image, "Image added successfully."));
}

// DELETE /cars/{id}/images/{imageId}
[HttpDelete("{id:int}/images/{imageId:int}")]
[Authorize]
public async Task<IActionResult> DeleteImage(
    int id,
    int imageId,
    CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    await _carService.DeleteImageAsync(id, imageId, currentUserId.Value, cancellationToken);

    return NoContent();
}
```

> **路由 `{id:int}/images/{imageId:int}` 里有两个路由参数，框架如何区分？** ASP.NET Core 路由系统按名字匹配——`{id:int}` 绑定到方法参数 `int id`，`{imageId:int}` 绑定到 `int imageId`，参数名必须和路由模板里的占位符名字一致，大小写不敏感。



### 7. 注册依赖到 Program.cs

```csharp
// 车辆模块
builder.Services.AddScoped<ICarRepository, EfCarRepository>();
builder.Services.AddScoped<ICarImageRepository, EfCarImageRepository>();  // 新增
builder.Services.AddScoped<CarService>();
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



### 9. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常添加图片：**

先登录，创建一辆车（Draft 状态），然后添加图片：

```json
POST /cars/1/images
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......

{
  "imageUrl": "https://example.com/car1-front.jpg",
  "sortOrder": 0
}
```

预期（201）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "imageUrl": "https://example.com/car1-front.jpg",
    "sortOrder": 0,
    "carId": 1
  },
  "message": "Image added successfully."
}
```

**正常删除图片：**

```
DELETE /cars/1/images/1
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（204）：无响应体。

**给已提交审核的车添加图片（应返回 409）：**

提交车辆审核后再尝试添加图片，预期：

```json
{
  "success": false,
  "message": "Car '1' is in 'PendingReview' status. Required status: 'Draft'."
}
```

**删除不属于该车辆的图片（应返回 404）：**

尝试 `DELETE /cars/1/images/99`（图片 99 属于另一辆车），预期返回 404。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 10. 此时的目录变化

```
UUcars.API/
├── DTOs/
│   ├── Requests/
│   │   └── CarImageAddRequest.cs       ← 新增
│   └── Responses/
│       └── CarImageResponse.cs         ← 新增
├── Exceptions/
│   └── CarImageNotFoundException.cs    ← 新增
└── Repositories/
    ├── ICarImageRepository.cs          ← 新增
    └── EfCarImageRepository.cs         ← 新增
```

`CarService` 和 `CarsController` 新增了方法，不是新文件。



### 11. Git 提交

```bash
git add .
git commit -m "feat: car image management (add/delete)"
git push origin feature/cars
```



### Step 18 完成状态

```
✅ 理解图片为什么限制在 Draft 状态操作
✅ 理解图片用物理删除而车辆用逻辑删除的原因
✅ CarImageAddRequest / CarImageResponse DTO
✅ CarImageNotFoundException（404）
✅ ICarImageRepository + EfCarImageRepository
✅ CarService 新增 AddImageAsync / DeleteImageAsync
✅ 理解 DeleteImageAsync 里为什么要验证 image.CarId == carId（防止跨车删图）
✅ CarsController 新增 POST /cars/{id}/images 和 DELETE /cars/{id}/images/{imageId}
✅ 理解双路由参数的绑定规则
✅ DI 注册完成
✅ Scalar 测试通过
✅ Git commit + push 完成
```



## Step 19 · 车辆列表（公开）

### 这一步做什么

前面几步实现了卖家侧的操作（创建、修改、删除、提交审核）。从这一步开始转向买家侧——实现公开的车辆列表接口 `GET /cars`。

这个接口有一个关键过滤规则：**只返回 `Published` 状态的车辆**。`Draft`、`PendingReview`、`Sold`、`Deleted` 的车辆不出现在公开列表里。买家看到的始终是经过审核、可以购买的车辆。



### 1. 为什么要在数据库层过滤，而不是取出来再过滤

有两种实现思路：

**思路一（错误）：取出所有车辆，在内存里过滤**

```csharp
var allCars = await _context.Cars.ToListAsync();
return allCars.Where(c => c.Status == CarStatus.Published).ToList();
```

**思路二（正确）：在数据库层过滤，只取需要的数据**

```csharp
var cars = await _context.Cars
    .Where(c => c.Status == CarStatus.Published)
    .ToListAsync();
```

区别在于：思路一先把所有数据加载进内存，再在内存里筛选，数据量大时会把大量无用数据传输到应用服务器，既浪费网络带宽又占用内存。思路二让数据库执行 `WHERE` 过滤，只返回符合条件的行，这是 EF Core 的核心能力之一——**IQueryable 延迟查询**。

`IQueryable` 和 `IEnumerable` 的关键区别：

```
IEnumerable：数据已经在内存里，操作在内存中执行
IQueryable： 数据还在数据库里，操作被翻译成 SQL 在数据库执行
```

在调用 `ToListAsync()` 之前，`Where`、`OrderBy`、`Skip`、`Take` 等操作都只是在构建 SQL 语句，不会真正查询数据库。只有调用 `ToListAsync()`（或 `FirstOrDefaultAsync` 等终结方法）时，才会把构建好的 SQL 发送到数据库执行。



### 2. 分页的必要性

车辆列表不能一次返回所有数据。如果数据库里有 10000 辆车，一次全部返回会：

- 占用大量服务器内存
- 网络传输慢，客户端等待时间长
- 客户端渲染压力大

分页把数据切成小块，每次只取一页。`Skip` 和 `Take` 是实现分页的两个核心操作：

```
Skip((page - 1) * pageSize)  ← 跳过前面几页的数据
Take(pageSize)               ← 只取这一页的数据
```

比如第 2 页、每页 20 条：`Skip(20).Take(20)`，跳过前 20 条，取第 21-40 条。

返回数据的同时，还要返回 `TotalCount`（总数量），让客户端知道一共有多少条数据，才能计算出总页数、渲染分页控件。



### 3. 创建查询请求 DTO

#### `DTOs/Requests/CarQueryRequest.cs`

```bash
touch UUcars.API/DTOs/Requests/CarQueryRequest.cs
```

```c#
namespace UUcars.API.DTOs.Requests;

public class CarQueryRequest
{
    // 页码，默认第 1 页
    // 为什么用属性而不是方法参数？
    // [FromQuery] 绑定时，框架会把 Query 参数自动映射到这个类的属性上，
    // 比 Action 方法上写一堆参数更清晰
    
    // 页码，默认第 1 页
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;  

    // 每页条数，默认 20 条
    [Range(1, 1000)] // 给个宽一点范围
    public int PageSize { get; set; } = 20; 
}
```



### 4. 给 ICarRepository 增加列表查询方法

打开 `UUcars.API/Repositories/ICarRepository.cs`，新增：

```csharp
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public interface ICarRepository
{
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default);

    // 新增：查询指定状态的车辆列表（支持分页）
    // 返回 (数据列表, 总数量) 的元组
    // 为什么同时返回 totalCount？
    // 分页接口需要告诉客户端一共有多少条数据才能渲染分页控件，
    // 如果分两次查询（一次取数据、一次 COUNT），会有两次数据库往返。
    // 用元组一起返回，在 Repository 里一次性处理两个查询
    Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfCarRepository.cs`，补充实现：

```csharp
public async Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
    CarStatus status,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    // 构建基础查询（此时还没有执行 SQL，只是在构建 IQueryable）
    var query = _context.Cars
        .Include(c => c.Seller)
        .Where(c => c.Status == status);

    // CountAsync：单独执行一次 COUNT(*) 查询，获取符合条件的总数
    // 注意：这里用的是过滤后的 query，不是全表 COUNT
    var totalCount = await query.CountAsync(cancellationToken);

    // 在同一个 query 基础上加分页，执行第二次查询取数据
    // 两次查询共享同一个 WHERE 条件，保证 totalCount 和数据是一致的
    var cars = await query
        .OrderByDescending(c => c.CreatedAt)    // 按创建时间降序（最新发布的在前）
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    // C# 元组语法：(变量名: 值, 变量名: 值)
    return (cars, totalCount);
}
```

> **为什么要两次查询而不是一次？** 分页数据和总数量是两个独立的查询目标。SQL 里无法在一次查询里同时返回"第 2 页的 20 条数据"和"总共有多少条"，必须分两次：一次 `COUNT(*)`，一次 `SELECT ... LIMIT OFFSET`。EF Core 会把这两次查询分别发送到数据库，都在数据库层执行，效率远高于取全部数据再在内存里处理。



### 5. 在 CarService 里添加列表查询方法

打开 `UUcars.API/Services/CarService.cs`，添加：

```csharp
public async Task<PagedResponse<CarResponse>> GetPublishedCarsAsync(
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {

        var page = query.Page;
        // 只做上限保护（业务规则）
        // 单页最多返回 50 条，防止客户端传 pageSize=99999 把服务器打垮
        var pageSize = Math.Min(query.PageSize, 50);

        var (cars, totalCount) = await _carRepository.GetPagedAsync(
            CarStatus.Published,
            page,
            pageSize,
            cancellationToken);

        var items = cars.Select(MapToResponse).ToList();

        // PagedResponse.Create 会自动计算 TotalPages
        return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
    }
```

同时在 `CarService.cs` 顶部补上 using：

```csharp
using UUcars.API.DTOs;
```



### 6. 在 CarsController 里添加列表接口

打开 `UUcars.API/Controllers/CarsController.cs`，在 `Create` 方法**上方**添加（习惯上把公开的读接口放在前面）：

```csharp
// GET /cars?page=1&pageSize=20
// 这个接口不需要 [Authorize]，公开访问
// [FromQuery]：告诉框架从 URL 的 Query 参数里绑定 CarQueryRequest 的属性
// 比如 /cars?page=2&pageSize=10 会自动绑定成 query.Page=2, query.PageSize=10

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] CarQueryRequest request ,CancellationToken cancellationToken)
    {
        var result = await _carService.GetPublishedCarsAsync(request, cancellationToken);
        
        return StatusCode(StatusCodes.Status200OK, ApiResponse<PagedResponse<CarResponse>>.Ok(result, "Cars retrieved successfully."));
    }
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



### 8. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

目前数据库里可能没有 `Published` 状态的车辆（创建的车都是 `Draft`，需要经过 Admin 审核才能变 `Published`）。Admin 审核功能在 Step 29 才实现，现在可以直接在数据库里手动把一辆车的 `Status` 改成 `Published` 来测试：

用数据库客户端执行：

```sql
UPDATE Cars SET Status = 'Published' WHERE Id = 1
```

**不带参数（默认分页）：**

```
GET /cars
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "title": "2020款宝马3系 低里程无事故",
        "status": "Published",
        ...
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

**带分页参数：**

```
GET /cars?page=1&pageSize=5
```

每页 5 条，验证分页参数生效。

**验证过滤：Draft 状态的车不出现在列表里**

数据库里有 `Draft` 状态的车，确认它们不出现在 `GET /cars` 的结果里。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 9. 此时的目录变化

```
UUcars.API/
└── DTOs/
    └── Requests/
        └── CarQueryRequest.cs      ← 新增
```

`ICarRepository`、`EfCarRepository`、`CarService`、`CarsController` 均已更新，不是新文件。



### 10. Git 提交

```bash
git add .
git commit -m "feat: GET /cars - public car listing with pagination"
git push origin feature/cars
```



### Step 19 完成状态

```
✅ 理解 IQueryable 延迟查询 vs IEnumerable 内存查询的区别
✅ 理解分页的必要性（Skip / Take / TotalCount）
✅ 理解两次查询（COUNT + 数据）的原因
✅ CarQueryRequest DTO（Page / PageSize，含默认值）
✅ ICarRepository 新增 GetPagedAsync（返回元组）
✅ EfCarRepository 实现 GetPagedAsync（IQueryable 链式构建，两次查询）
✅ CarService.GetPublishedCarsAsync（分页参数保护 + 只返回 Published）
✅ CarsController GET /cars（[FromQuery] 绑定，无需登录）
✅ Scalar 测试通过（分页返回、Draft 车辆被过滤）
✅ Git commit + push 完成
```



## Step 20 · 我的车辆列表（卖家视角）

### 这一步做什么

Step 19 实现了公开车辆列表，只返回 `Published` 的车辆。但卖家还需要看到自己发布的所有车辆，包括还在 `Draft`、`PendingReview` 状态的——否则卖家创建了草稿之后，完全看不到自己的车在哪里，没办法管理。

这一步实现 `GET /cars/my-listings`，返回当前登录用户作为卖家发布的所有车辆（含所有状态）。



### 1. 为什么需要这个接口

公开列表（`GET /cars`）和卖家列表（`GET /cars/my-listings`）是两个完全不同的视角：

```
GET /cars              ← 买家视角，只看 Published，不需要登录
GET /cars/my-listings  ← 卖家视角，看自己所有状态的车，需要登录
```

如果没有 `GET /cars/my-listings`，卖家创建草稿之后就找不到它了——公开列表不会显示 `Draft` 的车，卖家无法查看、修改、提交审核，整个车辆管理流程就断掉了。



### 2. 路由顺序的问题

这一步有一个值得注意的路由细节。`CarsController` 里已经有：

```
GET /cars          → GetAll()
GET /cars/{id}     → GetById()（Step 22 才实现）
```

现在要加：

```
GET /cars/my-listings → GetMyListings()
```

问题来了：框架在匹配 `GET /cars/my-listings` 时，会不会误以为 `my-listings` 是 `{id}` 的值，把请求路由到 `GetById()` 里？

答案是**不会**，因为 `{id}` 有路由约束 `:int`——`my-listings` 不是整数，所以不会匹配 `GET /cars/{id:int}`。框架会继续往下找，匹配到 `GET /cars/my-listings`。

但如果 `{id}` 没有 `:int` 约束（比如写成 `{id}`），`my-listings` 就会被当成字符串 id 传进去，引发错误。这也是 Step 15 里特意给路由参数加 `:int` 约束的原因之一。



### 3. 给 ICarRepository 增加卖家列表查询方法

打开 `UUcars.API/Repositories/ICarRepository.cs`，新增：

```csharp
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public interface ICarRepository
{
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default);
    Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // 新增：查询某个卖家的所有车辆（不过滤状态，支持分页）
    Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
        int sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfCarRepository.cs`，补充实现：

```csharp
public async Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
    int sellerId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    var query = _context.Cars
        .Include(c => c.Seller)
        .Where(c => c.SellerId == sellerId);
        // 注意：这里没有过滤 Status，返回卖家所有状态的车辆
        // 但排除逻辑删除的车辆——卖家也不需要看到已删除的车
        // 如果将来需要显示已删除的车，可以单独加一个接口

    // 去掉逻辑删除的车（卖家视角也不需要看到已删除的车）
    query = query.Where(c => c.Status != CarStatus.Deleted);

    var totalCount = await query.CountAsync(cancellationToken);

    var cars = await query
        .OrderByDescending(c => c.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return (cars, totalCount);
}
```



### 4. 在 CarService 里添加卖家列表查询方法

打开 `UUcars.API/Services/CarService.cs`，添加：

```csharp
public async Task<PagedResponse<CarResponse>> GetMyListingsAsync(
    int sellerId,
    CarQueryRequest query,
    CancellationToken cancellationToken = default)
{
    var page = query.Page < 1 ? 1 : query.Page;
    var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
    pageSize = Math.Min(pageSize, 50);

    var (cars, totalCount) = await _carRepository.GetBySellerAsync(
        sellerId,
        page,
        pageSize,
        cancellationToken);

    var items = cars.Select(MapToResponse).ToList();

    return PagedResponse<CarResponse>.Create(items, totalCount, page, pageSize);
}
```



### 5. 在 CarsController 里添加 GET /cars/my-listings

打开 `UUcars.API/Controllers/CarsController.cs`，在 `GetAll` 方法下方添加：

```csharp
// GET /cars/my-listings
// 必须放在 GET /cars/{id:int} 之前定义，虽然 :int 约束已经能区分，
// 但显式把固定路径放在参数路由之前是更好的习惯，意图更清晰
[HttpGet("my-listings")]
[Authorize]     // 卖家接口，必须登录
public async Task<IActionResult> GetMyListings(
    [FromQuery] CarQueryRequest query,
    CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var result = await _carService.GetMyListingsAsync(currentUserId.Value, query, cancellationToken);
    return Ok(ApiResponse<PagedResponse<CarResponse>>.Ok(result));
}
```



### 6. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 7. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常获取我的车辆列表：**

登录后访问：

```
GET /cars/my-listings
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "title": "2020款宝马3系 低里程无事故",
        "status": "Published",
        ...
      },
      {
        "id": 2,
        "title": "2019款丰田凯美瑞",
        "status": "Draft",
        ...
      }
    ],
    "totalCount": 2,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

注意结果里同时包含了 `Published` 和 `Draft` 状态的车辆，这是和公开列表的核心区别。

**不带 Token 访问（应返回 401）：**

不带 Authorization 头直接访问，预期返回 401。

**验证只返回自己的车辆：**

换另一个用户的 Token 登录，确认返回的是那个用户发布的车辆，不会看到其他人的。

**验证已删除的车不出现：**

删除一辆车（`DELETE /cars/{id}`），再访问 `GET /cars/my-listings`，确认被删除的车不在列表里。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 8. 此时的目录变化

这一步没有新增文件，只在 `ICarRepository`、`EfCarRepository`、`CarService`、`CarsController` 里新增了方法。



### 9. Git 提交

```bash
git add .
git commit -m "feat: GET /cars/my-listings - seller's own car list"
git push origin feature/cars
```



### Step 20 完成状态

```
✅ 理解卖家列表和公开列表的区别（视角不同、状态过滤不同、权限不同）
✅ 理解路由顺序问题（固定路径 vs 参数路径，:int 约束的保护作用）
✅ ICarRepository 新增 GetBySellerAsync（不过滤状态，排除 Deleted）
✅ EfCarRepository 实现 GetBySellerAsync
✅ CarService.GetMyListingsAsync
✅ CarsController GET /cars/my-listings（需要登录）
✅ Scalar 测试通过（返回所有状态车辆、只返回自己的车、删除的车不出现）
✅ Git commit + push 完成
```



## Step 21 · 车辆搜索

### 这一步做什么

Step 19 实现了公开车辆列表，但只有分页，没有任何过滤条件——买家想找"价格在 20 万以内的宝马"就没办法。这一步在 `GET /cars` 基础上扩展搜索能力，支持按品牌、价格区间、年份区间过滤。



### 1. 搜索参数怎么和分页参数结合

Step 19 已经有了 `CarQueryRequest`（包含 `Page` 和 `PageSize`），现在要往里面加搜索参数。

有两种方式：

**方式一：新建一个 `CarSearchRequest` 继承 `CarQueryRequest`**

```csharp
public class CarSearchRequest : CarQueryRequest
{
    public string? Brand { get; set; }
    // ...
}
```

**方式二：直接在 `CarQueryRequest` 里加搜索字段**

```csharp
public class CarQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Brand { get; set; }
    // ...
}
```

我们用方式二——`GET /cars` 本来就是同一个接口，分页和过滤是同一次请求里的参数，放在同一个 DTO 里更自然。客户端可以同时传 `?brand=BMW&minPrice=10000&page=2`，不需要区分"这是搜索参数"还是"这是分页参数"。



### 2. 更新 CarQueryRequest

打开 `UUcars.API/DTOs/Requests/CarQueryRequest.cs`，添加搜索字段：

```csharp
namespace UUcars.API.DTOs.Requests;

public class CarQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // 以下字段都是可选的（nullable），不传就不过滤
    // 为什么用 string? 而不是 string？
    // 过滤条件是可选的——不传 Brand 表示"不限品牌"，
    // 用 nullable 类型，Service 层可以用 null 判断是否需要应用这个过滤条件

    // 品牌过滤：精确匹配（BMW、Toyota 等）
    public string? Brand { get; set; }

    // 价格区间：minPrice / maxPrice 都是可选的，
    // 可以只传其中一个（比如"最高 20 万"只传 maxPrice）
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    // 年份区间：和价格区间同理
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
}
```



### 3. 更新 ICarRepository 和 EfCarRepository

搜索参数需要传进 Repository 层，在数据库层面做过滤（而不是取出来再在内存里过滤）。

打开 `UUcars.API/Repositories/ICarRepository.cs`，把 `GetPagedAsync` 的签名更新为接收完整的查询请求：

```csharp
using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;

namespace UUcars.API.Repositories;

public interface ICarRepository
{
    Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default);
    Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default);

    // 新增：查询指定状态的车辆列表（支持分页 + 过滤）
    // 更新：原来只接收 status / page / pageSize，现在接收完整的 query 对象
    Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        CarQueryRequest query,
        CancellationToken cancellationToken = default);


    //卖家列表页更新
    // 更新：原来只接收 status / page / pageSize，现在接收完整的 query 对象
    Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
        int sellerId,
        CarQueryRequest query,
        CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfCarRepository.cs`，更新 `GetPagedAsync` 的实现：

```csharp
 public async Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        // 构建基础查询（此时还没有执行 SQL，只是在构建 IQueryable）
        // 从 status 过滤开始构建 IQueryable
        // 后续每一个 Where 都是在已有的查询基础上追加条件，
        // 最终 EF Core 会把所有条件合并成一条 SQL 语句
        var query = _context.Cars
            .Include(c => c.Seller)
            .Where(c => c.Status == status);

        // 品牌过滤：只有传了 Brand 才追加这个条件
        // string.IsNullOrWhiteSpace：同时处理 null、空字符串、纯空格的情况
        if (!string.IsNullOrWhiteSpace(request.Brand))
        {
            // Contains + ToLower：大小写不敏感的模糊匹配
            // 比如搜 "bmw" 能匹配 "BMW"、"Bmw"
            // EF Core 会把这个翻译成 SQL 的 LIKE '%bmw%'（在 SQL Server 里不区分大小写）
            query = query.Where(c => c.Brand.Contains(request.Brand.ToLower()));
        }

        // 价格区间：MinPrice 和 MaxPrice 各自独立，可以只传其中一个
        if (request.MinPrice.HasValue)
        {
            query = query.Where(c => c.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(c => c.Price <= request.MaxPrice.Value);
        }

        if (request.MinYear.HasValue)
        {
            query = query.Where(c => c.Year >= request.MinYear.Value);
        }

        if (request.MaxYear.HasValue)
        {
            query = query.Where(c => c.Year <= request.MaxYear.Value);
        }
        
        // 到这里 query 还是 IQueryable，所有 Where 条件都还没有执行 SQL
        // CountAsync 触发第一次数据库查询：SELECT COUNT(*) WHERE ...（含所有过滤条件）
        var totalCount = await query.CountAsync(cancellationToken);

        // 在同一个 query 基础上加分页，执行第二次查询取数据
        // 两次查询共享同一个 WHERE 条件，保证 totalCount 和数据是一致的
        
        var page = request.Page;
        var pageSize = Math.Min(50, request.PageSize);
        
        var cars = await query
            .OrderByDescending(c => c.CreatedAt)    // 按创建时间降序（最新发布的在前）
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // C# 元组语法：(变量名: 值, 变量名: 值)
        return (cars, totalCount);
    }
```

> **为什么把分页参数的保护（`page < 1`、`Math.Min`）移到 Repository 里？** 原来在 Service 里处理，但搜索参数也在 `CarQueryRequest` 里，整个 query 对象现在直接传进 Repository，分页计算放在 Repository 里更自然——数据分页是数据访问层的职责。Service 层只负责组装查询条件，不再重复处理分页边界。



### 4. 更新 CarService

打开 `UUcars.API/Services/CarService.cs`，更新 `GetPublishedCarsAsync`，去掉重复的分页边界处理（已移入 Repository）：

```csharp
public async Task<PagedResponse<CarResponse>> GetPublishedCarsAsync(
        CarQueryRequest request,
        CancellationToken cancellationToken = default)
    {


        var (cars, totalCount) = await _carRepository.GetPagedAsync(
            CarStatus.Published,
            request,
            cancellationToken);

        var items = cars.Select(MapToResponse).ToList();

        // PagedResponse.Create 会自动计算 TotalPages
        return PagedResponse<CarResponse>.Create(items, totalCount, request.Page, request.PageSize);
    }
```



### 5. CarsController 不需要改动

`CarsController` 里的 `GetAll` 方法已经把整个 `CarQueryRequest` 对象传给了 Service，搜索参数自动包含在里面，Controller 层不需要任何改动。

这正是把请求参数封装成 DTO 的好处——新增字段时，Controller 层完全不受影响，只有 DTO、Service、Repository 需要更新。



### 6. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 7. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

先确保数据库里有几辆 `Published` 状态、不同品牌/价格/年份的车辆（可以直接在数据库里改 Status，或者用 Admin 账号手动改）。

**按品牌过滤：**

```
GET /cars?brand=BMW
```

预期：只返回品牌包含 "BMW" 的车辆。

**按价格区间过滤：**

```
GET /cars?minPrice=100000&maxPrice=300000
```

预期：只返回价格在 10 万到 30 万之间的车辆。

**按年份过滤：**

```
GET /cars?minYear=2019&maxYear=2022
```

预期：只返回 2019 年到 2022 年之间的车辆。

**组合查询（最接近真实用法）：**

```
GET /cars?brand=BMW&maxPrice=300000&minYear=2020&page=1&pageSize=10
```

预期：过滤、分页同时生效，返回满足所有条件的车辆。

**不带任何过滤参数（验证不影响原有行为）：**

```
GET /cars
```

预期：和 Step 19 一样，返回所有 `Published` 车辆，分页正常工作。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 8. 此时的目录变化

这一步没有新增文件，只更新了已有的文件：

```
UUcars.API/
├── DTOs/Requests/CarQueryRequest.cs    ← 已更新（新增搜索字段）
├── Repositories/
│   ├── ICarRepository.cs              ← 已更新（GetPagedAsync 签名变更）
│   └── EfCarRepository.cs             ← 已更新（GetPagedAsync 实现搜索逻辑）
└── Services/CarService.cs             ← 已更新（GetPublishedCarsAsync 简化）
```



### 9. Git 提交

```bash
git add .
git commit -m "feat: car search with query filters (brand/price/year)"
git push origin feature/cars
```



### Step 21 完成状态

```
✅ 理解搜索参数和分页参数放在同一个 DTO 里的原因
✅ 理解 nullable 类型在可选过滤条件中的作用
✅ CarQueryRequest 新增搜索字段（Brand / MinPrice / MaxPrice / MinYear / MaxYear）
✅ 理解 IQueryable 链式追加 Where 条件的方式（所有条件合并成一条 SQL）
✅ EfCarRepository.GetPagedAsync 实现搜索过滤（品牌模糊匹配 + 价格/年份区间）
✅ 理解为什么在数据库层过滤而不是内存过滤
✅ Controller 层无需改动（DTO 封装的好处）
✅ Scalar 测试通过（单条件过滤、组合过滤、不带参数保持原有行为）
✅ Git commit + push 完成
```



## Step 22 · 车辆详情

### 这一步做什么

实现 `GET /cars/{id}`，返回单辆车的完整信息，包括车辆图片列表和卖家基本信息。

这个接口比列表接口复杂的地方在于**权限规则**：不同身份的用户能看到不同状态的车辆——

```
未登录用户      只能查看 Published 状态的车辆
已登录的车主    还能看到自己的 Draft / PendingReview 状态车辆
Admin          可以查看所有状态的车辆
```

这个设计符合真实平台的需求：买家只能看已上架的车，卖家需要能预览自己还未上架的草稿，Admin 需要能看到待审核的车辆。

完成这个接口之后，还要补充 `CarService` 的单元测试——这是大纲里这一步的要求，也是前面讨论过的：车辆状态流转和权限检查有明确的业务规则，值得用测试保护起来。



### 1. 详情接口为什么需要返回图片列表和卖家信息

列表接口（`GET /cars`）返回的是每辆车的摘要信息，用于展示搜索结果卡片。详情接口（`GET /cars/{id}`）返回的是完整信息，用于展示车辆详情页，买家在这里决定是否下单。

详情页需要的信息比列表多：

```
列表页：标题、品牌、价格、里程、封面图（一张）
详情页：所有信息 + 全部图片 + 卖家联系信息
```

所以需要一个专门的 `CarDetailResponse`，而不是复用 `CarResponse`。



### 2. 创建 CarDetailResponse

```bash
touch UUcars.API/DTOs/Responses/CarDetailResponse.cs
```

```c#
namespace UUcars.API.DTOs.Responses;

// 车辆详情响应，比 CarResponse 多了图片列表
// 用独立的类而不是在 CarResponse 里加字段，
// 原因：列表接口不需要图片数据，如果都用 CarDetailResponse，
// 列表查询就要 Include Images，每辆车都加载图片列表，
// 数据量大时会带来严重的性能问题
public class CarDetailResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;
    public List<CarImageResponse> Images { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```



### 3. 更新 ICarRepository 和 EfCarRepository

详情接口需要同时加载图片列表，`GetByIdAsync` 目前只 `Include` 了 `Seller`，没有 `Include` 图片。

打开 `UUcars.API/Repositories/ICarRepository.cs`，新增一个专门用于详情查询的方法：

```csharp
// 新增：查询车辆详情（同时加载 Seller 和 Images）
// 为什么不直接修改 GetByIdAsync？
// GetByIdAsync 目前被状态变更操作（提交审核、修改、删除）使用，
// 这些操作不需要加载图片列表，加了反而是多余的查询。
// 用途不同的查询用不同的方法，各自只加载需要的数据
Task<Car?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default);
```

打开 `UUcars.API/Repositories/EfCarRepository.cs`，补充实现：

```csharp
public async Task<Car?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
{
    return await _context.Cars
        .Include(c => c.Seller)
        .Include(c => c.Images)     // 同时加载图片列表
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
```



### 4. 在 CarService 里实现权限判断逻辑

权限判断的核心思路：先查出车辆，再根据当前用户身份决定能不能看。

打开 `UUcars.API/Services/CarService.cs`，添加 `GetDetailAsync`：

```csharp
public async Task<CarDetailResponse> GetDetailAsync(
    int carId,
    int? currentUserId, // nullable：未登录时为 null
    bool isAdmin, // 是否是 Admin 角色
    CancellationToken cancellationToken = default)
{
    var car = await _carRepository.GetDetailByIdAsync(carId, cancellationToken);

    if (car == null)
        throw new CarNotFoundException(carId);

    // Published 的车是公开资源，任何人都可以直接查看，不需要权限判断
    if (car.Status == CarStatus.Published)
        return MapToDetailResponse(car);

    // 非 Published 的车是受保护资源，需要验证身份
    // Admin 可以查看任何状态的车辆
    if (isAdmin)
        return MapToDetailResponse(car);

    // 车主可以查看自己发布的车辆（Draft / PendingReview 等）
    var isOwner = currentUserId.HasValue && car.SellerId == currentUserId.Value;
    if (isOwner)
        return MapToDetailResponse(car);

    // 既不是 Published，也没有对应权限，对外表现为"不存在"
    throw new CarNotFoundException(carId);
}

// 详情实体 → DTO 的映射（包含图片列表）
private static CarDetailResponse MapToDetailResponse(Car car) => new()
{
    Id = car.Id,
    Title = car.Title,
    Brand = car.Brand,
    Model = car.Model,
    Year = car.Year,
    Price = car.Price,
    Mileage = car.Mileage,
    Description = car.Description,
    Status = car.Status.ToString(),
    SellerId = car.SellerId,
    SellerUsername = car.Seller?.Username ?? string.Empty,
    Images = car.Images
        .OrderBy(i => i.SortOrder)  // 按 SortOrder 排序，确保图片顺序正确
        .Select(i => new CarImageResponse
        {
            Id = i.Id,
            ImageUrl = i.ImageUrl,
            SortOrder = i.SortOrder,
            CarId = i.CarId
        })
        .ToList(),
    CreatedAt = car.CreatedAt,
    UpdatedAt = car.UpdatedAt
};
```

> **判断逻辑**
>
> Published 的车是公开资源，任何人都能看，不需要权限判断。 非 Published 的车是受保护资源，需要验证身份
>
> **为什么没有权限时返回 404 而不是 403？** 403 的语义是"我知道这个资源存在，但你没权限访问"，这会向外部透露"这辆车存在于系统里"。对于 `Draft` 状态的车来说，它的存在本身就不应该被其他人知道。返回 404，对没有权限的用户来说，这辆车"根本不存在"，更安全。



### 5. 在 CarsController 里添加详情接口

打开 `UUcars.API/Controllers/CarsController.cs`，在 `GetMyListings` 下方添加：

```csharp
// GET /cars/{id}
// 不需要 [Authorize]：公开接口，但权限判断在 Service 层处理
[HttpGet("{id:int}")]
public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
{
    // 从 Token 里获取当前用户信息（未登录时为 null）
    var currentUserId = _currentUserService.GetCurrentUserId();

    // 判断当前用户是否是 Admin
    // User.IsInRole：ASP.NET Core 提供的方法，读取 ClaimsPrincipal 里的 Role Claim
    // 未登录时 User.IsInRole 返回 false，不会抛异常
    var isAdmin = User.IsInRole("Admin");

    var car = await _carService.GetDetailAsync(id, currentUserId, isAdmin, cancellationToken);

    return StatusCode(StatusCodes.Status200OK,
        ApiResponse<CarDetailResponse>.Ok(car, "Car retrieved successfully."));
}
```



### 6. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 7. 单元测试

这一步要进行 `CarService` 的单元测试，重点覆盖状态流转和权限检查这两块业务规则。

#### 7.1 创建 FakeCarRepository

```bash
touch UUcars.Tests/Fakes/FakeCarRepository.cs
```

```c#
using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeCarRepository : ICarRepository
{
    private readonly Dictionary<int, Car> _store = new();
    private int _nextId = 1;

    public void Seed(Car car)
    {
        if (car.Id == 0) car.Id = _nextId++;
        _store[car.Id] = car;
    }

    public Task<Car> AddAsync(Car car, CancellationToken cancellationToken = default)
    {
        car.Id = _nextId++;
        _store[car.Id] = car;
        return Task.FromResult(car);
    }

    public Task<Car?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var car);
        return Task.FromResult(car);
    }

    public Task<Car?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Fake 里的 GetDetail 和 GetById 行为一致，
        // 测试里不需要真实的 Include，导航属性直接在 Seed 时设置好
        _store.TryGetValue(id, out var car);
        return Task.FromResult(car);
    }

    public Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken = default)
    {
        _store[car.Id] = car;
        return Task.FromResult(car);
    }

    public Task<(List<Car> Cars, int TotalCount)> GetPagedAsync(
        CarStatus status,
        CarQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(c => c.Status == status)
            .ToList();
        return Task.FromResult((result, result.Count));
    }

    public Task<(List<Car> Cars, int TotalCount)> GetBySellerAsync(
        int sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(c => c.SellerId == sellerId && c.Status != CarStatus.Deleted)
            .ToList();
        return Task.FromResult((result, result.Count));
    }
}
```

#### 7.2 创建 FakeCarImageRepository

```bash
touch UUcars.Tests/Fakes/FakeCarImageRepository.cs
```

```c#
using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeCarImageRepository : ICarImageRepository
{
    private readonly Dictionary<int, CarImage> _store = new();
    private int _nextId = 1;

    public Task<CarImage> AddAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        image.Id = _nextId++;
        _store[image.Id] = image;
        return Task.FromResult(image);
    }

    public Task<CarImage?> GetByIdAsync(int imageId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(imageId, out var image);
        return Task.FromResult(image);
    }

    public Task DeleteAsync(CarImage image, CancellationToken cancellationToken = default)
    {
        _store.Remove(image.Id);
        return Task.CompletedTask;
    }
}
```

#### 7.3 编写 CarService 单元测试

```bash
mkdir -p UUcars.Tests/Services
touch UUcars.Tests/Services/CarServiceTests.cs
```

```c#
using Microsoft.Extensions.Logging.Abstractions;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class CarServiceTests
{
    private static CarService CreateService(
        FakeCarRepository carRepo,
        FakeCarImageRepository? imageRepo = null)
    {
        return new CarService(
            carRepo,
            imageRepo ?? new FakeCarImageRepository(),
            NullLogger<CarService>.Instance
        );
    }

    // ===== 提交审核的测试 =====

    [Fact]
    public async Task SubmitForReviewAsync_WhenDraftAndOwner_ShouldChangeToPendingReview()
    {
        // Arrange
        var repo = new FakeCarRepository();
        repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.Draft });
        var service = CreateService(repo);

        // Act
        var result = await service.SubmitForReviewAsync(1, currentUserId: 10);

        // Assert
        Assert.Equal("PendingReview", result.Status);
    }

    [Fact]
    public async Task SubmitForReviewAsync_WhenNotOwner_ShouldThrowForbiddenException()
    {
        // Arrange
        var repo = new FakeCarRepository();
        repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.Draft });
        var service = CreateService(repo);

        // Act + Assert：用不同的 userId（99）操作
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.SubmitForReviewAsync(1, currentUserId: 99));
    }

    [Fact]
    public async Task SubmitForReviewAsync_WhenAlreadyPendingReview_ShouldThrowCarStatusException()
    {
        // Arrange：车辆已经是 PendingReview 状态
        var repo = new FakeCarRepository();
        repo.Seed(new Car { Id = 1, SellerId = 10, Status = CarStatus.PendingReview });
        var service = CreateService(repo);

        // Act + Assert：不能重复提交
        await Assert.ThrowsAsync<CarStatusException>(
            () => service.SubmitForReviewAsync(1, currentUserId: 10));
    }

    // ===== 车辆详情权限的测试 =====

    [Fact]
    public async Task GetDetailAsync_WhenPublished_ShouldBeVisibleToAnyone()
    {
        // Arrange：Published 状态，未登录用户（currentUserId = null）
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Published,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act：未登录（null），不是 Admin（false）
        var result = await service.GetDetailAsync(1, currentUserId: null, isAdmin: false);

        // Assert：Published 的车任何人都能看到
        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
    }

    [Fact]
    public async Task GetDetailAsync_WhenDraftAndOwner_ShouldBeVisible()
    {
        // Arrange：Draft 状态，车主查看
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Draft,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act：车主（currentUserId = 10）查看自己的草稿
        var result = await service.GetDetailAsync(1, currentUserId: 10, isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal("Draft", result.Status);
    }

    [Fact]
    public async Task GetDetailAsync_WhenDraftAndNotOwner_ShouldThrowCarNotFoundException()
    {
        // Arrange：Draft 状态，非车主查看
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Draft,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act + Assert：非车主看不到 Draft 车辆，返回 404（而不是 403）
        await Assert.ThrowsAsync<CarNotFoundException>(
            () => service.GetDetailAsync(1, currentUserId: 99, isAdmin: false));
    }

    [Fact]
    public async Task GetDetailAsync_WhenDraftAndAdmin_ShouldBeVisible()
    {
        // Arrange：Draft 状态，Admin 查看
        var repo = new FakeCarRepository();
        repo.Seed(new Car
        {
            Id = 1,
            SellerId = 10,
            Status = CarStatus.Draft,
            Seller = new User { Id = 10, Username = "seller" },
            Images = []
        });
        var service = CreateService(repo);

        // Act：Admin（isAdmin = true）可以看任何状态的车辆
        var result = await service.GetDetailAsync(1, currentUserId: 99, isAdmin: true);

        Assert.NotNull(result);
    }
}
```

#### 7.4 运行测试

```bash
dotnet test
```

预期：

```
Test summary: total: 11, failed: 0, succeeded: 11, skipped: 0
```

原来的 6 个用户测试 + 新增的 5 个车辆测试，全部通过。



### 8. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**未登录查看 Published 车辆（应返回 200）：**

```
GET /cars/1
```

不带 Token，预期正常返回车辆详情，包含图片列表和卖家用户名。

**未登录查看 Draft 车辆（应返回 404）：**

找一辆 `Draft` 状态的车，不带 Token 访问，预期返回 404——对未登录用户来说这辆车"不存在"。

**车主查看自己的 Draft 车辆（应返回 200）：**

用车主账号登录，带 Token 访问自己的 Draft 车辆，预期正常返回。

**Admin 查看任意状态车辆（应返回 200）：**

用 Admin 账号（`admin@uucars.com`）登录，访问 `Draft` 状态的车辆，预期正常返回。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 9. 此时的目录变化

```
UUcars.API/
└── DTOs/
    └── Responses/
        └── CarDetailResponse.cs            ← 新增

UUcars.Tests/
├── Fakes/
│   ├── FakeCarRepository.cs               ← 新增
│   └── FakeCarImageRepository.cs          ← 新增
└── Services/
    └── CarServiceTests.cs                 ← 新增
```

`ICarRepository`、`EfCarRepository`、`CarService`、`CarsController` 均已更新，不是新文件。



### 10. Git 提交 + 合并分支

车辆模块（Step 14 到 Step 22）全部完成：

```bash
git add .
git commit -m "feat: GET /cars/{id} - car detail with permission rules + test: CarService unit tests (status transitions, permissions)"

git push origin feature/cars
```

合并回 `develop`：

```bash
git checkout develop
git merge --no-ff feature/cars -m "merge: feature/cars into develop"
git push origin develop
git branch -D feature/cars
git push origin --delete feature/cars
```



### Step 22 完成状态

```
✅ 理解详情接口为什么需要独立的 CarDetailResponse（性能考虑）
✅ 理解三种身份的权限规则（未登录 / 车主 / Admin）
✅ 理解无权限时返回 404 而不是 403 的安全设计原因
✅ ICarRepository 新增 GetDetailByIdAsync（Include Images）
✅ CarService.GetDetailAsync（权限判断逻辑）
✅ CarsController GET /cars/{id}（User.IsInRole 判断 Admin 角色）
✅ FakeCarRepository / FakeCarImageRepository 建立
✅ 5 个 CarService 单元测试（状态流转 + 权限检查）
✅ 全部 11 个测试通过
✅ Scalar 测试通过
✅ feature/cars 合并回 develop
```



## Step 23 · 收藏车辆

### 这一步做什么

进入第六阶段——收藏系统。买家浏览车辆时，可以把感兴趣的车收藏起来，方便后续查看。这一步实现 `POST /favorites/{carId}`。

收藏系统乍看简单，但有几个细节值得思考：只能收藏 `Published` 的车、同一用户不能重复收藏同一辆车、卖家不能收藏自己的车（虽然大纲里没有这条限制，但真实平台通常会有）。这一步按大纲来，重点处理前两条。



### 1. 切出 feature/favorites 分支

```bash
git checkout develop
git checkout -b feature/favorites
git push -u origin feature/favorites
```



### 2. 为什么只能收藏 Published 的车

`Draft` 和 `PendingReview` 状态的车对买家是不可见的，买家根本不应该知道这些车的存在。如果允许收藏任意状态的车，就等于变相暴露了系统里未上架的车辆 Id。

`Sold` 和 `Deleted` 状态的车虽然买家可能知道它们存在，但收藏已售出或已删除的车没有实际意义，也不允许。



### 3. 联合主键在收藏表里的作用

Step 04 和 Step 06 里设计 `Favorites` 表时，用 `(UserId, CarId)` 作为联合主键。这带来一个直接好处：数据库层面天然防止重复收藏——同一个用户对同一辆车的收藏记录只能有一行，如果尝试插入重复记录，数据库会直接报主键冲突错误。

但我们不能让数据库错误直接传给客户端，需要在 Service 层提前检查，遇到重复收藏时抛出一个有意义的业务异常（409）。



### 4. 创建业务异常

```bush
touch UUcars.API/Exceptions/AlreadyFavoritedException.cs
```

```c#
namespace UUcars.API.Exceptions;

// 用户尝试重复收藏同一辆车时抛出
public class AlreadyFavoritedException : AppException
{
    public AlreadyFavoritedException(int carId)
        : base(StatusCodes.Status409Conflict,
            $"Car '{carId}' is already in your favorites.")
    {
    }
}
```



### 5. 创建 DTO

收藏操作的请求不需要 Request DTO——`carId` 已经在路由里了，没有额外的请求体。只需要一个响应 DTO：

```bash
touch UUcars.API/DTOs/Responses/FavoriteResponse.cs
```

```c#
namespace UUcars.API.DTOs.Responses;

public class FavoriteResponse
{
    public int CarId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // 返回被收藏的车辆摘要信息，方便客户端直接展示
    // 而不需要再发一次 GET /cars/{id} 请求
    public CarResponse? Car { get; set; }
}
```



### 6. 创建 IFavoriteRepository 和 EfFavoriteRepository

```bash
touch UUcars.API/Repositories/IFavoriteRepository.cs
touch UUcars.API/Repositories/EfFavoriteRepository.cs
```

```c#
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IFavoriteRepository
{
    // 查询某用户是否已收藏某辆车
    Task<Favorite?> GetAsync(int userId, int carId, CancellationToken cancellationToken = default);

    // 添加收藏
    Task<Favorite> AddAsync(Favorite favorite, CancellationToken cancellationToken = default);

    // 删除收藏（Step 24 使用）
    Task DeleteAsync(Favorite favorite, CancellationToken cancellationToken = default);

    // 获取某用户的所有收藏（Step 25 使用）
    Task<(List<Favorite> Favorites, int TotalCount)> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
```

> **为什么把 Step 24 和 Step 25 需要的方法也一起定义了？** 收藏系统的三个操作（添加、删除、列表）是一个完整功能块，Repository 接口把它们一起定义清楚，后续步骤只需要补充实现，不需要再改接口。接口的变动成本比实现高——改接口意味着所有实现（包括测试里的 Fake）都要跟着改。

```csharp
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfFavoriteRepository : IFavoriteRepository
{
    private readonly AppDbContext _context;

    public EfFavoriteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Favorite?> GetAsync(int userId, int carId,
        CancellationToken cancellationToken = default)
    {
        // Favorites 表用的是联合主键（UserId, CarId）
        // FindAsync 支持联合主键，传入多个值按主键定义的顺序匹配
        return await _context.Favorites
            .FindAsync([userId, carId], cancellationToken);
    }

    public async Task<Favorite> AddAsync(Favorite favorite,
        CancellationToken cancellationToken = default)
    {
        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task DeleteAsync(Favorite favorite,
        CancellationToken cancellationToken = default)
    {
        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<Favorite> Favorites, int TotalCount)> GetByUserAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Favorites
            .Include(f => f.Car)
                .ThenInclude(c => c.Seller)  // 加载车辆的同时也加载车辆的卖家
            .Where(f => f.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var favorites = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (favorites, totalCount);
    }
}
```

> **`ThenInclude` 是什么？** `Include` 是加载直接导航属性（`Favorite → Car`），`ThenInclude` 是在 `Include` 的基础上继续加载更深层的导航属性（`Car → Seller`）。这里的查询链是：`Favorite → Car → Seller`，需要用 `Include` 再接 `ThenInclude` 来表达这个两层加载关系。



### 7. 创建 FavoriteService

```bash
touch UUcars.API/Services/FavoriteService.cs
```

```c#
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class FavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly ICarRepository _carRepository;
    private readonly ILogger<FavoriteService> _logger;

    public FavoriteService(
        IFavoriteRepository favoriteRepository,
        ICarRepository carRepository,
        ILogger<FavoriteService> logger)
    {
        _favoriteRepository = favoriteRepository;
        _carRepository = carRepository;
        _logger = logger;
    }

    public async Task<FavoriteResponse> AddFavoriteAsync(
        int userId,
        int carId,
        CancellationToken cancellationToken = default)
    {
        // 1. 验证车辆存在且是 Published 状态
        // 这里用 GetByIdAsync（不用 GetDetailByIdAsync），
        // 因为不需要加载图片列表，只需要验证车辆状态
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 只能收藏 Published 状态的车辆
        if (car.Status != CarStatus.Published)
            throw new CarNotFoundException(carId);  // 对用户表现为"不存在"，原因同详情接口

        // 2. 检查是否已收藏
        var existing = await _favoriteRepository.GetAsync(userId, carId, cancellationToken);
        if (existing != null)
            throw new AlreadyFavoritedException(carId);

        // 3. 创建收藏记录
        var favorite = new Favorite
        {
            UserId = userId,
            CarId = carId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _favoriteRepository.AddAsync(favorite, cancellationToken);

        _logger.LogInformation("User {UserId} favorited car {CarId}", userId, carId);

        return new FavoriteResponse
        {
            CarId = created.CarId,
            UserId = created.UserId,
            CreatedAt = created.CreatedAt,
            Car = CarService.MapToResponse(car)
        };
    }
}
```

> **为什么收藏非 Published 的车也返回 404 而不是 400？** 同样的安全考虑——买家不应该知道系统里存在一辆 `Draft` 状态的车。对买家来说，`Draft` 的车"不存在"，尝试收藏它也应该返回 404。



### 8. 创建 FavoritesController

```bash
touch UUcars.API/Controllers/FavoritesController.cs
```

```c#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("favorites")]
[Authorize]     // 收藏系统所有接口都需要登录
public class FavoritesController : ControllerBase
{
    private readonly FavoriteService _favoriteService;
    private readonly CurrentUserService _currentUserService;

    public FavoritesController(
        FavoriteService favoriteService,
        CurrentUserService currentUserService)
    {
        _favoriteService = favoriteService;
        _currentUserService = currentUserService;
    }

    // POST /favorites/{carId}
    [HttpPost("{carId:int}")]
    public async Task<IActionResult> AddFavorite(int carId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var result = await _favoriteService.AddFavoriteAsync(userId.Value, carId, cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<FavoriteResponse>.Ok(result, "Car added to favorites."));
    }
}
```



### 9. 注册依赖到 Program.cs

```csharp
// 收藏模块
builder.Services.AddScoped<IFavoriteRepository, EfFavoriteRepository>();
builder.Services.AddScoped<FavoriteService>();
```

顶部补上 using：

```csharp
using UUcars.API.Services;
```



### 10. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 11. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常收藏车辆：**

登录后收藏一辆 `Published` 状态的车：

```
POST /favorites/1
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（201）：

```json
{
  "success": true,
  "data": {
    "carId": 1,
    "userId": 2,
    "createdAt": "...",
    "car": {
      "id": 1,
      "title": "2020款宝马3系",
      "status": "Published",
      ...
    }
  },
  "message": "Car added to favorites."
}
```

**重复收藏同一辆车（应返回 409）：**

再次发送同样的请求，预期：

```json
{
  "success": false,
  "message": "Car '1' is already in your favorites."
}
```

**收藏 Draft 状态的车（应返回 404）：**

尝试收藏一辆 `Draft` 状态的车，预期返回 404——对买家来说这辆车"不存在"。

**不带 Token 访问（应返回 401）：**

不带 Authorization 头，预期返回 401。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 12. 此时的目录变化

```
UUcars.API/
├── Controllers/
│   └── FavoritesController.cs          ← 新增
├── DTOs/
│   └── Responses/
│       └── FavoriteResponse.cs         ← 新增
├── Exceptions/
│   └── AlreadyFavoritedException.cs    ← 新增
├── Repositories/
│   ├── IFavoriteRepository.cs          ← 新增
│   └── EfFavoriteRepository.cs         ← 新增
└── Services/
    └── FavoriteService.cs              ← 新增
```



### 13. Git 提交

```bash
git add .
git commit -m "feat: POST /favorites/{carId} - add car to favorites"
git push origin feature/favorites
```



### Step 23 完成状态

```
✅ 切出 feature/favorites 分支
✅ 理解只能收藏 Published 车辆的原因（安全 + 业务意义）
✅ 理解联合主键在防重复收藏中的作用
✅ AlreadyFavoritedException（409）
✅ FavoriteResponse DTO（含车辆摘要）
✅ IFavoriteRepository（一次定义完整接口，减少后续改动）
✅ EfFavoriteRepository（FindAsync 联合主键查询，ThenInclude 两层加载）
✅ 理解 ThenInclude 的作用（多层导航属性加载）
✅ FavoriteService.AddFavoriteAsync（状态验证 + 重复检查）
✅ FavoritesController POST /favorites/{carId}（Controller 级别 [Authorize]）
✅ DI 注册完成
✅ Scalar 测试通过（201 收藏成功、409 重复收藏、404 非 Published、401 未登录）
✅ Git commit + push 完成
```



## Step 24 · 取消收藏

### 这一步做什么

实现 `DELETE /favorites/{carId}`，让买家能从收藏列表里移除一辆车。



### 1. 取消收藏的业务规则

取消收藏比添加收藏简单——只需要验证两件事：

```
1. 当前用户已经收藏了这辆车（没收藏过就无法取消）
2. 操作的是自己的收藏（不能取消别人的收藏）
```

第二条不需要单独检查——`GetAsync(userId, carId)` 按 `(UserId, CarId)` 联合主键查询，本身就限定了是"当前用户对这辆车的收藏"，查不到就是没有，不存在查到别人收藏的情况。



### 2. 创建业务异常

```bash
touch UUcars.API/Exceptions/FavoriteNotFoundException.cs
```

```c#
namespace UUcars.API.Exceptions;

// 用户尝试取消一个不存在的收藏时抛出
public class FavoriteNotFoundException : AppException
{
    public FavoriteNotFoundException(int carId)
        : base(StatusCodes.Status404NotFound,
            $"Car '{carId}' is not in your favorites.")
    {
    }
}
```



### 3. 在 FavoriteService 里添加取消收藏方法

打开 `UUcars.API/Services/FavoriteService.cs`，添加 `RemoveFavoriteAsync`：

```csharp
public async Task RemoveFavoriteAsync(
    int userId,
    int carId,
    CancellationToken cancellationToken = default)
{
    // 查询当前用户对这辆车的收藏记录
    // GetAsync 按 (UserId, CarId) 联合主键查询，天然限定了是当前用户自己的收藏
    var favorite = await _favoriteRepository.GetAsync(userId, carId, cancellationToken);

    if (favorite == null)
        throw new FavoriteNotFoundException(carId);

    await _favoriteRepository.DeleteAsync(favorite, cancellationToken);

    _logger.LogInformation("User {UserId} removed car {CarId} from favorites", userId, carId);
}
```



### 4. 在 FavoritesController 里添加 DELETE /favorites/{carId}

打开 `UUcars.API/Controllers/FavoritesController.cs`，在 `AddFavorite` 方法下方添加：

```csharp
// DELETE /favorites/{carId}
[HttpDelete("{carId:int}")]
public async Task<IActionResult> RemoveFavorite(int carId, CancellationToken cancellationToken)
{
    var userId = _currentUserService.GetCurrentUserId();
    if (userId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    await _favoriteService.RemoveFavoriteAsync(userId.Value, carId, cancellationToken);

    // 删除成功返回 204 No Content，和 DELETE /cars/{id} 保持一致
    return NoContent();
}
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



### 6. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常取消收藏：**

先收藏一辆车（`POST /favorites/1`），然后取消：

```
DELETE /favorites/1
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（204）：无响应体。

**取消一个没有收藏过的车（应返回 404）：**

直接取消一辆从未收藏过的车：

```json
{
  "success": false,
  "message": "Car '2' is not in your favorites."
}
```

**取消后重新收藏（应返回 201）：**

取消收藏后再次 `POST /favorites/1`，预期能重新收藏成功，说明删除操作真正清除了记录，不是逻辑删除。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 7. 此时的目录变化

```
UUcars.API/
└── Exceptions/
    └── FavoriteNotFoundException.cs    ← 新增
```

`FavoriteService` 和 `FavoritesController` 新增了方法，不是新文件。



### 8. Git 提交

```bash
git add .
git commit -m "feat: DELETE /favorites/{carId} - remove car from favorites"
git push origin feature/favorites
```



### Step 24 完成状态

```
✅ 理解取消收藏的业务规则（联合主键天然限定了操作范围）
✅ FavoriteNotFoundException（404）
✅ FavoriteService.RemoveFavoriteAsync
✅ FavoritesController DELETE /favorites/{carId}（返回 204）
✅ Scalar 测试通过（204 取消成功、404 未收藏过、取消后可重新收藏）
✅ Git commit + push 完成
```



## Step 25 · 我的收藏列表

### 这一步做什么

实现 `GET /favorites`，返回当前登录用户的收藏车辆列表，支持分页。完成后收藏系统三个接口全部就绪，合并分支。



### 1. 在 FavoriteService 里添加列表查询方法

`IFavoriteRepository` 在 Step 23 里已经定义了 `GetByUserAsync`，`EfFavoriteRepository` 也已经实现了，这里直接在 Service 层调用：

打开 `UUcars.API/Services/FavoriteService.cs`，在顶部补上 using：

```csharp
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
```

然后添加 `GetMyFavoritesAsync`：

```csharp
public async Task<PagedResponse<FavoriteResponse>> GetMyFavoritesAsync(
    int userId,
    CarQueryRequest query,
    CancellationToken cancellationToken = default)
{
    var page = query.Page < 1 ? 1 : query.Page;
    var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
    pageSize = Math.Min(pageSize, 50);

    var (favorites, totalCount) = await _favoriteRepository.GetByUserAsync(
        userId,
        page,
        pageSize,
        cancellationToken);

    var items = favorites.Select(f => new FavoriteResponse
    {
        CarId = f.CarId,
        UserId = f.UserId,
        CreatedAt = f.CreatedAt,
        // f.Car 在 EfFavoriteRepository 里已经通过 Include + ThenInclude 加载好了
        Car = f.Car != null ? CarService.MapToResponse(f.Car) : null
    }).ToList();

    return PagedResponse<FavoriteResponse>.Create(items, totalCount, page, pageSize);
}
```



### 2. 在 FavoritesController 里添加 GET /favorites

打开 `UUcars.API/Controllers/FavoritesController.cs`，在顶部补上 using：

```csharp
using UUcars.API.DTOs.Requests;
```

在 `RemoveFavorite` 方法下方添加：

```csharp
// GET /favorites
[HttpGet]
public async Task<IActionResult> GetMyFavorites(
    [FromQuery] CarQueryRequest query,
    CancellationToken cancellationToken)
{
    var userId = _currentUserService.GetCurrentUserId();
    if (userId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var result = await _favoriteService.GetMyFavoritesAsync(userId.Value, query, cancellationToken);
    return Ok(ApiResponse<PagedResponse<FavoriteResponse>>.Ok(result, "Favorites retrieved successfully."));
}
```



### 3. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 4. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**获取我的收藏列表：**

先收藏几辆车，然后：

```
GET /favorites
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "carId": 1,
        "userId": 2,
        "createdAt": "...",
        "car": {
          "id": 1,
          "title": "2020款宝马3系",
          "status": "Published",
          ...
        }
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

**分页测试：**

```
GET /favorites?page=1&pageSize=2
```

收藏多辆车后，验证分页参数生效。

**空收藏列表：**

换一个没有收藏过任何车辆的账号登录，访问 `GET /favorites`，预期返回空列表而不是报错：

```json
{
  "success": true,
  "data": {
    "items": [],
    "totalCount": 0,
    "page": 1,
    "pageSize": 20,
    "totalPages": 0
  }
}
```

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 5. 此时的目录变化

这一步没有新增文件，只在 `FavoriteService` 和 `FavoritesController` 里新增了方法。



### 6. Git 提交 + 合并分支

收藏系统三个接口全部完成：

```bash
git add .
git commit -m "feat: GET /favorites - my favorites list with pagination"
git push origin feature/favorites
```

合并回 `develop`：

```bash
git checkout develop
git merge --no-ff feature/favorites -m "merge: feature/favorites into develop"
git push origin develop
git branch -D feature/favorites
git push origin --delete feature/favorites
```



### Step 25 完成状态

```
✅ FavoriteService.GetMyFavoritesAsync（分页 + 映射包含车辆摘要）
✅ FavoritesController GET /favorites（[FromQuery] 分页参数）
✅ Scalar 测试通过（带车辆信息的收藏列表、分页、空列表）
✅ feature/favorites 合并回 develop
```



## Step 26 · 创建订单

### 这一步做什么

进入第七阶段——订单系统。

买家看中一辆车之后，通过下单来发起购买。这一步实现 `POST /orders`。

订单系统是整个平台业务闭环的关键一环，涉及的业务规则比前面几个模块都多，而且有一个重要的联动操作：创建订单的同时，车辆状态从 `Published` 变成 `Sold`。



### 1. 切出 feature/orders 分支

```bash
git checkout develop
git checkout -b feature/orders
git push -u origin feature/orders
```



### 2. 创建订单的业务规则

```
1. 车辆必须存在且是 Published 状态（不能对未上架的车下单）
2. 买家不能是卖家本人（不能自己买自己的车）
3. 创建订单时锁定当前车辆价格（防止卖家事后改价）
4. 创建订单时记录 SellerId（方便后续查询"我卖出的订单"）
5. 创建订单后车辆状态变为 Sold（一辆车只能卖给一个买家）
```

规则 3 和 4 在 Step 04 设计数据库时已经解释过，这里是真正落地的地方。

规则 5 是一个**跨实体的联动操作**——创建订单和更新车辆状态必须同时成功或同时失败，这就需要用到**数据库事务**。



### 3. 什么是数据库事务

事务（Transaction）是数据库的一个核心概念，保证一组操作**要么全部成功，要么全部回滚**，不会出现"订单创建成功但车辆状态没更新"的中间状态。

```
没有事务的风险：
  第一步：创建订单（成功）
  第二步：更新车辆状态为 Sold（失败，比如网络抖动）
  结果：订单存在，但车辆还是 Published 状态
       → 另一个买家可能同时也在下单，数据不一致

有事务的保证：
  BEGIN TRANSACTION
    第一步：创建订单
    第二步：更新车辆状态为 Sold
  COMMIT（两步都成功才提交）
  或 ROLLBACK（任何一步失败则全部回滚）
```

在 EF Core 里，同一个 `DbContext` 实例的 `SaveChangesAsync()` 默认就是事务性的——一次 `SaveChangesAsync` 里的所有变更，要么全部写入，要么全部回滚。所以我们只需要把"创建订单"和"更新车辆状态"放在同一次 `SaveChangesAsync` 里，事务就自动生效了。



### 4. 创建业务异常

```bash
touch UUcars.API/Exceptions/OrderNotFoundException.cs
touch UUcars.API/Exceptions/CarNotAvailableException.cs
touch UUcars.API/Exceptions/CannotOrderOwnCarException.cs
```

```c#

namespace UUcars.API.Exceptions;

public class OrderNotFoundException : AppException
{
    public OrderNotFoundException(int id)
        : base(StatusCodes.Status404NotFound, $"Order with id '{id}' was not found.")
    {
    }
}
namespace UUcars.API.Exceptions;

// 车辆不是 Published 状态，无法下单
// 用专门的异常而不是复用 CarNotFoundException，
// 原因：这辆车是存在的，只是状态不对，语义上不是"找不到"
public class CarNotAvailableException : AppException
{
    public CarNotAvailableException(int carId)
        : base(StatusCodes.Status409Conflict,
            $"Car '{carId}' is not available for purchase.")
    {
    }
}
namespace UUcars.API.Exceptions;

// 买家尝试购买自己的车
public class CannotOrderOwnCarException : AppException
{
    public CannotOrderOwnCarException()
        : base(StatusCodes.Status400BadRequest,
            "You cannot purchase your own car.")
    {
    }
}
```



### 5. 创建 DTO

#### `DTOs/Requests/OrderCreateRequest.cs`

```bash
touch UUcars.API/DTOs/Requests/OrderCreateRequest.cs
```

```c#
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class OrderCreateRequest
{
    // 买家只需要传 CarId，其他信息（Price、SellerId）由服务端从车辆信息里取
    // 不让客户端传 Price，防止客户端篡改价格
    [Required(ErrorMessage = "CarId is required.")]
    public int CarId { get; set; }
}
```

#### `DTOs/Responses/OrderResponse.cs`

```bash
touch UUcars.API/DTOs/Responses/OrderResponse.cs
```

```c#
namespace UUcars.API.DTOs.Responses;

public class OrderResponse
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public string CarTitle { get; set; } = string.Empty;
    public int BuyerId { get; set; }
    public string BuyerUsername { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;

    // 锁定的成交价格（创建订单时从车辆复制过来）
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```



### 6. 创建 IOrderRepository 和 EfOrderRepository

```bash
touch UUcars.API/Repositories/IOrderRepository.cs
touch UUcars.API/Repositories/EfOrderRepository.cs
```

和 `IFavoriteRepository` 一样，把这个阶段所有方法一次性定义好，减少后续改接口的成本：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);

    // 买家视角：查询某用户作为买家的订单列表
    Task<(List<Order> Orders, int TotalCount)> GetByBuyerAsync(
        int buyerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // 卖家视角：查询某用户作为卖家的订单列表
    Task<(List<Order> Orders, int TotalCount)> GetBySellerAsync(
        int sellerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
using Microsoft.EntityFrameworkCore;
using UUcars.API.Data;
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public class EfOrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public EfOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Car)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<(List<Order> Orders, int TotalCount)> GetByBuyerAsync(
        int buyerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.Car)
            .Include(o => o.Seller)
            .Where(o => o.BuyerId == buyerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
            .Include(o => o.Car)
            .Include(o => o.Buyer)
            .Where(o => o.SellerId == sellerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }
}
```

> **买家视角和卖家视角的查询为什么 Include 的字段不一样？** 买家查"我买的订单"，关心的是"我买的是什么车、卖家是谁"，所以 Include `Car` 和 `Seller`，不需要 `Buyer`（买家就是自己）。卖家查"我卖的订单"，关心的是"谁买了我的哪辆车"，所以 Include `Car` 和 `Buyer`，不需要 `Seller`（卖家就是自己）。只加载需要的数据，避免多余的 JOIN。



### 7. 创建 OrderService

这里有一个关键点：创建订单和更新车辆状态需要在同一个事务里完成。前面说过 EF Core 同一个 `DbContext` 的 `SaveChangesAsync` 是事务性的，所以 `OrderService` 需要直接访问 `AppDbContext`，在一次 `SaveChangesAsync` 里同时保存订单和车辆变更。

```bash
touch UUcars.API/Services/OrderService.cs
```

```c#
using UUcars.API.Data;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICarRepository _carRepository;
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        ICarRepository carRepository,
        AppDbContext context,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _carRepository = carRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateAsync(
        int buyerId,
        OrderCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. 验证车辆存在
        var car = await _carRepository.GetByIdAsync(request.CarId, cancellationToken);
        if (car == null)
            throw new CarNotFoundException(request.CarId);

        // 2. 验证车辆是 Published 状态（只有上架的车才能下单）
        if (car.Status != CarStatus.Published)
            throw new CarNotAvailableException(request.CarId);

        // 3. 验证买家不是卖家本人
        if (car.SellerId == buyerId)
            throw new CannotOrderOwnCarException();

        // 4. 创建订单实体
        // Price 从车辆当前价格复制过来并锁定，后续卖家改价不影响这个订单
        // SellerId 从车辆冗余存储，方便后续"我卖出的订单"查询
        var order = new Order
        {
            CarId = car.Id,
            BuyerId = buyerId,
            SellerId = car.SellerId,    // 从车辆冗余存储
            Price = car.Price,          // 锁定当前价格
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 5. 更新车辆状态为 Sold
        car.Status = CarStatus.Sold;
        car.UpdatedAt = DateTime.UtcNow;

        // 6. 在同一个事务里保存订单和车辆状态变更
        // 为什么能保证事务性？
        // _context 是同一个 DbContext 实例，Add 和 Update 都只是把变更记录到追踪器里，
        // 只有调用 SaveChangesAsync 时才真正写数据库。
        // EF Core 会把所有待写入的变更包在一个数据库事务里一起提交，
        // 任何一步失败都会全部回滚。
        _context.Orders.Add(order);
        _context.Cars.Update(car);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} created: buyer {BuyerId} purchased car {CarId} from seller {SellerId}",
            order.Id, buyerId, car.Id, car.SellerId);

        // 7. 查出完整订单（含关联的 Car / Buyer / Seller 信息）用于响应
        // 为什么要重新查一次？
        // 刚创建的 order 对象里，导航属性（Car、Buyer、Seller）是 null，
        // 因为 EF Core 在 Add 时不会自动加载关联对象。
        // 重新查一次，通过 Include 把所有关联数据加载进来，
        // 才能正确映射出 CarTitle、BuyerUsername、SellerUsername 等字段
        var created = await _orderRepository.GetByIdAsync(order.Id, cancellationToken);
        return MapToResponse(created!);
    }

    internal static OrderResponse MapToResponse(Order order) => new()
    {
        Id = order.Id,
        CarId = order.CarId,
        CarTitle = order.Car?.Title ?? string.Empty,
        BuyerId = order.BuyerId,
        BuyerUsername = order.Buyer?.Username ?? string.Empty,
        SellerId = order.SellerId,
        SellerUsername = order.Seller?.Username ?? string.Empty,
        Price = order.Price,
        Status = order.Status.ToString(),
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt
    };
}
```



### 8. 创建 OrdersController

```bash
touch UUcars.API/Controllers/OrdersController.cs
```

```c#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("orders")]
[Authorize]     // 订单系统所有接口都需要登录
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly CurrentUserService _currentUserService;

    public OrdersController(OrderService orderService, CurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    // POST /orders
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] OrderCreateRequest request,
        CancellationToken cancellationToken)
    {
        var buyerId = _currentUserService.GetCurrentUserId();
        if (buyerId == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var order = await _orderService.CreateAsync(buyerId.Value, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<OrderResponse>.Ok(order, "Order created successfully."));
    }
}
```



### 9. 注册依赖到 Program.cs

```csharp
// 订单模块
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<OrderService>();
```



### 10. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 11. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常下单：**

用买家账号登录，对一辆 `Published` 的车下单：

```json
POST /orders
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......

{
  "carId": 1
}
```

预期（201）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "carId": 1,
    "carTitle": "2020款宝马3系",
    "buyerId": 2,
    "buyerUsername": "testuser",
    "sellerId": 3,
    "sellerUsername": "seller",
    "price": 260000,
    "status": "Pending",
    "createdAt": "..."
  },
  "message": "Order created successfully."
}
```

去数据库里确认：`Cars` 表里这辆车的 `Status` 应该已经变成了 `Sold`。

**再次对同一辆车下单（应返回 409）：**

车辆已经变成 `Sold` 状态，再次下单预期：

```json
{
  "success": false,
  "message": "Car '1' is not available for purchase."
}
```

**买家尝试购买自己的车（应返回 400）：**

用卖家账号登录，对自己发布的车下单，预期：

```json
{
  "success": false,
  "message": "You cannot purchase your own car."
}
```

**对不存在的车下单（应返回 404）：**

```json
{
  "carId": 9999
}
```

预期返回 404。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 12. 此时的目录变化

```
UUcars.API/
├── Controllers/
│   └── OrdersController.cs             ← 新增
├── DTOs/
│   ├── Requests/
│   │   └── OrderCreateRequest.cs       ← 新增
│   └── Responses/
│       └── OrderResponse.cs            ← 新增
├── Exceptions/
│   ├── OrderNotFoundException.cs       ← 新增
│   ├── CarNotAvailableException.cs     ← 新增
│   └── CannotOrderOwnCarException.cs   ← 新增
├── Repositories/
│   ├── IOrderRepository.cs             ← 新增
│   └── EfOrderRepository.cs            ← 新增
└── Services/
    └── OrderService.cs                 ← 新增
```



### 13. Git 提交

```bash
git add .
git commit -m "feat: POST /orders - create order with car status update"
git push origin feature/orders
```



### Step 26 完成状态

```
✅ 切出 feature/orders 分支
✅ 理解创建订单的五条业务规则
✅ 理解数据库事务的必要性（订单创建 + 车辆状态更新必须原子性）
✅ 理解 EF Core 同一 DbContext 的 SaveChangesAsync 自动事务
✅ OrderNotFoundException / CarNotAvailableException / CannotOrderOwnCarException
✅ OrderCreateRequest（只传 CarId，价格由服务端锁定）/ OrderResponse
✅ IOrderRepository（一次定义完整接口）+ EfOrderRepository
✅ 理解买家/卖家查询为什么 Include 不同字段
✅ OrderService.CreateAsync（完整业务规则 + 事务性保存 + 重新查询加载关联数据）
✅ 理解创建后为什么要重新查一次订单
✅ OrdersController POST /orders（Controller 级别 [Authorize]）
✅ Scalar 测试通过（201 下单成功、409 车辆不可用、400 自己买自己、404 车辆不存在）
✅ Git commit + push 完成
```



## Step 27 · 取消订单

### 这一步做什么

实现 `POST /orders/{id}/cancel`，让买家可以取消一个待处理的订单。

取消订单不是简单地改一个状态字段——它涉及一个重要的**状态回滚**：取消订单后，车辆状态必须从 `Sold` 恢复到 `Published`，否则这辆车就永远卖不出去了。和 Step 26 一样，这个联动操作需要在同一个事务里完成。



### 1. 取消订单的业务规则

```
1. 订单必须存在
2. 只有买家可以取消（卖家无权取消买家的订单）
3. 只有 Pending 状态的订单可以取消
   （Completed 的订单已经完成交易，不能反悔）
4. 取消后订单状态变为 Cancelled
5. 取消后车辆状态从 Sold 恢复到 Published（车辆重新上架）
```

规则 5 是这里最关键的业务逻辑。如果只改订单状态而不恢复车辆状态，车辆会永远停留在 `Sold`，其他买家看不到这辆车，卖家也无法重新出售。



### 2. 取消操作为什么用 POST 而不是 DELETE 或 PATCH

这个问题在 Step 15 提交审核时讨论过，这里再强调一次：

```
DELETE /orders/{id}          → 删除订单记录（物理删除）
PATCH  /orders/{id}          → 部分更新订单字段
POST   /orders/{id}/cancel   → 触发"取消"这个业务动作
```

取消订单不是删除它，订单记录需要保留（用于历史查询、纠纷处理）。也不是简单地改一个字段，而是触发了一系列联动操作（改订单状态 + 恢复车辆状态）。`POST + 动词子路径` 是表达"执行一个业务动作"的标准 REST 设计。



### 3. 创建业务异常

取消订单时，订单状态不是 `Pending` 的情况需要一个专门的异常，复用 `CarStatusException` 的设计思路：

```bash
touch UUcars.API/Exceptions/OrderStatusException.cs
```

```c#
using UUcars.API.Entities.Enums;

namespace UUcars.API.Exceptions;

// 订单状态不满足操作要求时抛出
// 比如：只有 Pending 状态才能取消，Completed 的订单不能取消
public class OrderStatusException : AppException
{
    public OrderStatusException(int orderId, OrderStatus currentStatus, OrderStatus requiredStatus)
        : base(StatusCodes.Status409Conflict,
            $"Order '{orderId}' is in '{currentStatus}' status. Required status: '{requiredStatus}'.")
    {
    }
}
```



### 4. 在 OrderService 里添加取消订单方法

打开 `UUcars.API/Services/OrderService.cs`，添加 `CancelAsync`：

```csharp
public async Task<OrderResponse> CancelAsync(
    int orderId,
    int currentUserId,
    CancellationToken cancellationToken = default)
{
    // 1. 查询订单（含关联的 Car / Buyer / Seller）
    var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

    if (order == null)
        throw new OrderNotFoundException(orderId);

    // 2. 只有买家可以取消订单
    // 为什么卖家不能取消？
    // 买家已经下单，代表买家有购买意愿。如果卖家可以随意取消，
    // 会严重损害买家体验，也违背了平台交易的基本规则。
    // 取消权在买家，卖家如果不想卖，应该走其他流程（平台客服介入等）
    if (order.BuyerId != currentUserId)
        throw new ForbiddenException();

    // 3. 只有 Pending 状态可以取消
    if (order.Status != OrderStatus.Pending)
        throw new OrderStatusException(orderId, order.Status, OrderStatus.Pending);

    // 4. 更新订单状态
    order.Status = OrderStatus.Cancelled;
    order.UpdatedAt = DateTime.UtcNow;

    // 5. 恢复车辆状态为 Published
    // 为什么要恢复？
    // Step 26 里创建订单时把车辆状态改成了 Sold，
    // 取消订单等于这笔交易没有成立，车辆应该重新上架供其他买家购买
    var car = await _carRepository.GetByIdAsync(order.CarId, cancellationToken);
    if (car != null)
    {
        car.Status = CarStatus.Published;
        car.UpdatedAt = DateTime.UtcNow;
    }

    // 6. 在同一个事务里保存订单和车辆状态变更
    // 原理同 Step 26：同一个 DbContext 的 SaveChangesAsync 是原子性的
    // 订单取消和车辆恢复要么同时成功，要么同时回滚
    _context.Orders.Update(order);
    if (car != null)
        _context.Cars.Update(car);
    await _context.SaveChangesAsync(cancellationToken);

    _logger.LogInformation(
        "Order {OrderId} cancelled by buyer {BuyerId}, car {CarId} restored to Published",
        orderId, currentUserId, order.CarId);

    return MapToResponse(order);
}
```

> **为什么查车辆时用 `if (car != null)` 而不是直接抛异常？** 正常情况下，订单里的 `CarId` 一定能查到对应的车辆——创建订单时已经验证过车辆存在了。这里加 `null` 检查是**防御性编程**：即使极端情况下车辆数据出现了问题，订单仍然能被取消，不会因为车辆查不到而导致取消操作完全失败。日志里会记录异常情况，方便后续排查。



### 5. 在 OrdersController 里添加取消订单端点

打开 `UUcars.API/Controllers/OrdersController.cs`，在 `Create` 方法下方添加：

```csharp
// POST /orders/{id}/cancel
[HttpPost("{id:int}/cancel")]
public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var order = await _orderService.CancelAsync(id, currentUserId.Value, cancellationToken);
    return Ok(ApiResponse<OrderResponse>.Ok(order, "Order cancelled successfully."));
}
```



### 6. 验证编译

```bash
dotnet build
```

预期：

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```



### 7. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常取消订单：**

先创建一个订单，然后用买家账号取消：

```
POST /orders/1/cancel
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "status": "Cancelled",
    ...
  },
  "message": "Order cancelled successfully."
}
```

去数据库确认：`Cars` 表里对应车辆的 `Status` 应该已经从 `Sold` 恢复成 `Published`。

**重新对恢复的车辆下单（应返回 201）：**

车辆恢复 `Published` 后，重新下单应该成功，验证整个流程的闭环。

**重复取消同一个订单（应返回 409）：**

已经 `Cancelled` 的订单再次取消，预期：

```json
{
  "success": false,
  "message": "Order '1' is in 'Cancelled' status. Required status: 'Pending'."
}
```

**用卖家账号取消订单（应返回 403）：**

换卖家账号的 Token，尝试取消买家的订单，预期返回 403。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 8. 此时的目录变化

```
UUcars.API/
└── Exceptions/
    └── OrderStatusException.cs     ← 新增
```

`OrderService` 和 `OrdersController` 新增了方法，不是新文件。



### 9. Git 提交

```bash
git add .
git commit -m "feat: POST /orders/{id}/cancel - cancel order"
git push origin feature/orders
```



### Step 27 完成状态

```
✅ 理解取消订单为什么用 POST 而不是 DELETE 或 PATCH
✅ 理解取消订单后为什么要恢复车辆状态（交易闭环）
✅ 理解为什么只有买家可以取消（卖家不能随意取消买家订单）
✅ OrderStatusException（409，清楚表达状态冲突）
✅ OrderService.CancelAsync（买家验证 + 状态检查 + 事务性保存两个实体）
✅ 理解防御性编程在 car != null 检查上的应用
✅ OrdersController POST /orders/{id}/cancel
✅ Scalar 测试通过（200 取消成功、409 重复取消、403 卖家无权、车辆状态恢复验证）
✅ Git commit + push 完成
```

## Step 28 · 我的订单

### 这一步做什么

实现两个订单查询接口：

```
GET /orders/my-purchases  ← 买家视角：我买的订单
GET /orders/my-sales      ← 卖家视角：我卖出的订单
```

这两个接口是 Step 26 里在 `Orders` 表冗余存储 `SellerId` 的直接体现——有了 `SellerId`，卖家查自己的订单只需要按 `SellerId` 过滤，不需要 JOIN `Cars` 表。

完成后补充 `OrderService` 的单元测试，订单系统全部功能就完成了。



### 1. 为什么要分两个接口而不是一个

一个直觉上的问题：能不能用一个 `GET /orders` 返回"和我相关的所有订单"？

可以，但有问题：同一个用户既可以是买家也可以是卖家，"和我相关的订单"混在一起会让客户端很难区分——这个订单我是买家还是卖家？客户端需要额外判断逻辑。

分成两个接口，职责清晰：

```
GET /orders/my-purchases  → 我作为买家的订单，关心的是"我买了什么车、付了多少钱"
GET /orders/my-sales      → 我作为卖家的订单，关心的是"谁买了我的车、收了多少钱"
```

两个接口虽然查的是同一张表，但视角不同，关心的信息侧重点也不同。



### 2. 在 OrderService 里添加查询方法

`IOrderRepository` 在 Step 26 里已经定义了 `GetByBuyerAsync` 和 `GetBySellerAsync`，`EfOrderRepository` 也已经实现了，这里直接在 Service 层调用：

打开 `UUcars.API/Services/OrderService.cs`，在顶部补上 using：

```csharp
using UUcars.API.DTOs;
using UUcars.API.DTOs.Requests;
```

然后添加两个查询方法：

```csharp
public async Task<PagedResponse<OrderResponse>> GetMyPurchasesAsync(
    int buyerId,
    CarQueryRequest query,
    CancellationToken cancellationToken = default)
{
    var page = query.Page < 1 ? 1 : query.Page;
    var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
    pageSize = Math.Min(pageSize, 50);

    var (orders, totalCount) = await _orderRepository.GetByBuyerAsync(
        buyerId,
        page,
        pageSize,
        cancellationToken);

    var items = orders.Select(MapToResponse).ToList();

    return PagedResponse<OrderResponse>.Create(items, totalCount, page, pageSize);
}

public async Task<PagedResponse<OrderResponse>> GetMySalesAsync(
    int sellerId,
    CarQueryRequest query,
    CancellationToken cancellationToken = default)
{
    var page = query.Page < 1 ? 1 : query.Page;
    var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
    pageSize = Math.Min(pageSize, 50);

    var (orders, totalCount) = await _orderRepository.GetBySellerAsync(
        sellerId,
        page,
        pageSize,
        cancellationToken);

    var items = orders.Select(MapToResponse).ToList();

    return PagedResponse<OrderResponse>.Create(items, totalCount, page, pageSize);
}
```



### 3. 在 OrdersController 里添加查询端点

打开 `UUcars.API/Controllers/OrdersController.cs`，在顶部补上 using：

```csharp
using UUcars.API.DTOs.Requests;
```

在 `Cancel` 方法下方添加：

```csharp
// GET /orders/my-purchases
// 注意：必须放在 GET /orders/{id:int} 之前定义（如果以后有这个接口）
// 原因和 Step 20 里 /cars/my-listings 的路由顺序问题一样：
// 固定路径要优先于参数路径，:int 约束虽然能保护，但显式顺序更清晰
[HttpGet("my-purchases")]
public async Task<IActionResult> GetMyPurchases(
    [FromQuery] CarQueryRequest query,
    CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var result = await _orderService.GetMyPurchasesAsync(
        currentUserId.Value, query, cancellationToken);

    return Ok(ApiResponse<PagedResponse<OrderResponse>>.Ok(result));
}

// GET /orders/my-sales
[HttpGet("my-sales")]
public async Task<IActionResult> GetMySales(
    [FromQuery] CarQueryRequest query,
    CancellationToken cancellationToken)
{
    var currentUserId = _currentUserService.GetCurrentUserId();
    if (currentUserId == null)
        return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

    var result = await _orderService.GetMySalesAsync(
        currentUserId.Value, query, cancellationToken);

    return Ok(ApiResponse<PagedResponse<OrderResponse>>.Ok(result));
}
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



### 5. 补充单元测试

这一步要补充 `OrderService` 的单元测试，重点测试创建订单的各种边界情况。

#### 5.1 创建 FakeOrderRepository

```bash
touch UUcars.Tests/Fakes/FakeOrderRepository.cs
```

```c#
using UUcars.API.Entities;
using UUcars.API.Repositories;

namespace UUcars.Tests.Fakes;

public class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<int, Order> _store = new();
    private int _nextId = 1;

    public void Seed(Order order)
    {
        if (order.Id == 0) order.Id = _nextId++;
        _store[order.Id] = order;
    }

    public Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        order.Id = _nextId++;
        _store[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var order);
        return Task.FromResult(order);
    }

    public Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _store[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<(List<Order> Orders, int TotalCount)> GetByBuyerAsync(
        int buyerId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(o => o.BuyerId == buyerId)
            .ToList();
        return Task.FromResult((result, result.Count));
    }

    public Task<(List<Order> Orders, int TotalCount)> GetBySellerAsync(
        int sellerId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var result = _store.Values
            .Where(o => o.SellerId == sellerId)
            .ToList();
        return Task.FromResult((result, result.Count));
    }
}
```

#### 5.2 OrderService 的单元测试需要 AppDbContext

`OrderService` 直接依赖 `AppDbContext`（用于事务性保存），单元测试里不能用真实数据库。

EF Core 提供了一个**内存数据库（InMemory）**，专门用于测试——它的行为和真实数据库一样（支持 Add、Update、SaveChanges），但数据存在内存里，测试结束就消失，不需要真实的 SQL Server。

先安装 InMemory 包：

```bash
dotnet add UUcars.Tests/UUcars.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory
```

#### 5.3 编写 OrderService 单元测试

```bash
touch UUcars.Tests/Services/OrderServiceTests.cs
```

```c#
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using UUcars.API.Data;
using UUcars.API.DTOs.Requests;
using UUcars.API.Entities;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;
using UUcars.API.Services;
using UUcars.Tests.Fakes;

namespace UUcars.Tests.Services;

public class OrderServiceTests
{
    // 每个测试用一个独立的 InMemory 数据库（通过唯一的数据库名区分）
    // 为什么要独立？InMemory 数据库是共享的，如果多个测试用同一个，
    // 一个测试插入的数据会影响另一个测试的结果
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static OrderService CreateService(
        AppDbContext context,
        FakeCarRepository? carRepo = null)
    {
        // 这个测试需要验证事务性保存，用真实的 EfOrderRepository + InMemory 数据库
        // 而不是 FakeOrderRepository——因为 OrderService 直接操作 _context 保存，
        // FakeOrderRepository._store 感知不到这个操作，GetByIdAsync 会返回 null
        var orderRepo = new EfOrderRepository(context);
        return new OrderService(
            orderRepo,
            carRepo ?? new FakeCarRepository(),
            context,
            NullLogger<OrderService>.Instance
        );
    }

    // ===== 创建订单的测试 =====

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateOrderAndMarkCarAsSold()
    {
        // Arrange
        var context = CreateDbContext();
        var carRepo = new FakeCarRepository();

        // 把 Buyer、Seller、Car 都加进 InMemory 数据库
        // 原因：EfOrderRepository.GetByIdAsync 用了 Include(o => o.Buyer) 和 Include(o => o.Seller)
        // InMemory 数据库里必须有这些关联数据，Include 才能正确加载
        var buyer = new User
        {
            Id = 2,
            Username = "buyer",
            Email = "buyer@test.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var seller = new User
        {
            Id = 10,
            Username = "seller",
            Email = "seller@test.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var car = new Car
        {
            Id = 1,
            SellerId = 10,
            Price = 260000,
            Status = CarStatus.Published,
            Title = "宝马3系",
            Brand = "BMW",
            Model = "3 Series",
            Year = 2020,
            Mileage = 15000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(buyer, seller);
        context.Cars.Add(car);
        await context.SaveChangesAsync();
        carRepo.Seed(car);

        var service = CreateService(context, carRepo);

        // Act
        var result = await service.CreateAsync(
            2,
            new OrderCreateRequest { CarId = 1 });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OrderStatus.Pending.ToString(), result.Status);
        Assert.Equal(260000, result.Price);
        Assert.Equal(10, result.SellerId);
        Assert.Equal("buyer", result.BuyerUsername); // 现在能正确加载了
        Assert.Equal("seller", result.SellerUsername); // 现在能正确加载了

        var updatedCar = await context.Cars.FindAsync(1);
        Assert.Equal(CarStatus.Sold, updatedCar!.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenCarNotPublished_ShouldThrowCarNotAvailableException()
    {
        // Arrange
        var context = CreateDbContext();
        var carRepo = new FakeCarRepository();

        // 不需要User数据
        // 在业务规则判断阶段就抛了异常，不涉及 Include 查询

        // Arrange：车辆是 Draft 状态，不可下单
        var car = new Car
        {
            Id = 1,
            SellerId = 10,
            Price = 260000,
            Status = CarStatus.Draft,
            Title = "宝马3系",
            Brand = "BMW",
            Model = "3 Series",
            Year = 2020,
            Mileage = 15000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        carRepo.Seed(car);
        var service = CreateService(context, carRepo);

        // Act + Assert

        // Act + Assert
        await Assert.ThrowsAsync<CarNotAvailableException>(() => service.CreateAsync(
            2,
            new OrderCreateRequest { CarId = 1 }));
    }

    [Fact]
    public async Task CreateAsync_WhenBuyerIsSeller_ShouldThrowCannotOrderOwnCarException()
    {
        // Arrange
        var context = CreateDbContext();
        var carRepo = new FakeCarRepository();

        // 不需要准备user的数据
        // 在业务规则判断阶段就抛了异常，不涉及 Include 查询
        var car = new Car
        {
            Id = 1,
            SellerId = 10,
            Price = 260000,
            Status = CarStatus.Published, // ← 改成 Published，让代码能走到买家/卖家检查
            Title = "宝马3系",
            Brand = "BMW",
            Model = "3 Series",
            Year = 2020,
            Mileage = 15000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        carRepo.Seed(car);

        var service = CreateService(context, carRepo);

        // Act + Assert：buyerId = 10，和 SellerId = 10 相同
        await Assert.ThrowsAsync<CannotOrderOwnCarException>(() =>
            service.CreateAsync(10, new OrderCreateRequest { CarId = 1 }));
    }

    [Fact]
    public async Task CancelAsync_WhenPendingAndBuyer_ShouldCancelOrderAndRestoreCar()
    {
        // Arrange
        var context = CreateDbContext();
        var carRepo = new FakeCarRepository();
        
        // 和创建订单的测试一样，CancelAsync 也调用了 _orderRepository.GetByIdAsync（含 Include），
        // 所以订单、车辆、用户都需要放进 InMemory 数据库。

        var buyer = new User
        {
            Id = 2, Username = "buyer", Email = "buyer@test.com",
            PasswordHash = "hash", Role = UserRole.User,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var seller = new User
        {
            Id = 10, Username = "seller", Email = "seller@test.com",
            PasswordHash = "hash", Role = UserRole.User,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var car = new Car
        {
            Id = 1, SellerId = 10, Status = CarStatus.Sold,
            Title = "宝马", Brand = "BMW", Model = "3系", Year = 2020,
            Mileage = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var order = new Order
        {
            Id = 1, CarId = 1, BuyerId = 2, SellerId = 10,
            Price = 260000, Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(buyer, seller);
        context.Cars.Add(car);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        carRepo.Seed(car);

        var service = CreateService(context, carRepo);

        // Act
        var result = await service.CancelAsync(1, 2);

        // Assert：订单已取消
        Assert.Equal(OrderStatus.Cancelled.ToString(), result.Status);

        // 验证车辆状态已恢复为 Published
        var updatedCar = await context.Cars.FindAsync(1);
        Assert.Equal(CarStatus.Published, updatedCar!.Status);
    }

    [Fact]
    public async Task CancelAsync_WhenNotBuyer_ShouldThrowForbiddenException()
    {
        // Arrange
        var context = CreateDbContext();
        var carRepo = new FakeCarRepository();

        var buyer = new User
        {
            Id = 2, Username = "buyer", Email = "buyer@test.com",
            PasswordHash = "hash", Role = UserRole.User,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var seller = new User
        {
            Id = 10, Username = "seller", Email = "seller@test.com",
            PasswordHash = "hash", Role = UserRole.User,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var car = new Car
        {
            Id = 1, SellerId = 10, Status = CarStatus.Sold,
            Title = "宝马", Brand = "BMW", Model = "3系", Year = 2020,
            Mileage = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var order = new Order
        {
            Id = 1, CarId = 1, BuyerId = 2, SellerId = 10,
            Price = 260000, Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(buyer, seller);
        context.Cars.Add(car);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        carRepo.Seed(car);
        var service = CreateService(context, carRepo);

        // Act + Assert：currentUserId = 10（卖家），不是买家，不能取消
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CancelAsync(1, 10));
    }

    [Fact]
    public async Task CancelAsync_WhenAlreadyCancelled_ShouldThrowOrderStatusException()
    {
        // Arrange
        var context = CreateDbContext();
        var carRepo = new FakeCarRepository();

        var buyer = new User
        {
            Id = 2, Username = "buyer", Email = "buyer@test.com",
            PasswordHash = "hash", Role = UserRole.User,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var seller = new User
        {
            Id = 10, Username = "seller", Email = "seller@test.com",
            PasswordHash = "hash", Role = UserRole.User,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var car = new Car
        {
            Id = 1, SellerId = 10, Status = CarStatus.Sold,
            Title = "宝马", Brand = "BMW", Model = "3系", Year = 2020,
            Mileage = 0, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        var order = new Order
        {
            Id = 1, CarId = 1, BuyerId = 2, SellerId = 10,
            Price = 260000, Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(buyer, seller);
        context.Cars.Add(car);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        carRepo.Seed(car);

        var service = CreateService(context, carRepo);

        // Act + Assert：不能重复取消
        await Assert.ThrowsAsync<OrderStatusException>(() => service.CancelAsync(1, 2));
    }
}
```

#### 5.4 运行测试

```bash
dotnet test
```

预期：

```
Test summary: total: 17, failed: 0, succeeded: 17, skipped: 0
```

原来的 11 个测试 + 新增的 6 个订单测试，全部通过。



### 6. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**GET /orders/my-purchases（买家视角）：**

用买家账号登录，创建几个订单后：

```
GET /orders/my-purchases
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "carTitle": "2020款宝马3系",
        "sellerUsername": "seller",
        "price": 260000,
        "status": "Pending",
        ...
      }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
    "totalPages": 1
  }
}
```

**GET /orders/my-sales（卖家视角）：**

换卖家账号的 Token：

```
GET /orders/my-sales
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......
```

预期：返回该卖家车辆被购买的订单，包含买家用户名。

**验证两个接口的数据隔离：**

买家调用 `GET /orders/my-sales`，因为买家没有卖出过任何车，应该返回空列表——而不是看到别人的订单。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 7. 此时的目录变化

```
UUcars.Tests/
├── Fakes/
│   └── FakeOrderRepository.cs      ← 新增
└── Services/
    └── OrderServiceTests.cs        ← 新增
```

`OrderService` 和 `OrdersController` 新增了方法，不是新文件。



### 8. Git 提交 + 合并分支

```bash
git add .
git commit -m "feat: GET /orders/my-purchases + GET /orders/my-sales + test: OrderService unit tests (create/cancel/edge cases)"
git push origin feature/orders
```

合并回 `develop`，同时这是 v0.5 里程碑节点：

```bash
git checkout develop
git merge --no-ff feature/orders -m "merge: feature/orders into develop"
git push origin develop
git branch -D feature/orders
git push origin --delete feature/orders
```

用户 + 车辆 + 收藏 + 订单核心业务全部完成，合并到 `main` 打 v0.5 tag：

```bash
git checkout main
git merge --no-ff develop -m "release: v0.5 - core business modules complete"
git tag -a v0.5 -m "Auth + Cars + Favorites + Orders all working"
git push origin main
git push origin v0.5
git checkout develop
```



### Step 28 完成状态

```
✅ 理解为什么要分两个接口（买家/卖家视角不同，关心的信息侧重点不同）
✅ OrderService 新增 GetMyPurchasesAsync / GetMySalesAsync
✅ OrdersController 新增 GET /orders/my-purchases + GET /orders/my-sales
✅ FakeOrderRepository 建立
✅ 理解 InMemory 数据库的用途（测试时替代真实数据库）
✅ 理解每个测试用独立 InMemory 数据库的原因（测试隔离）
✅ 6 个 OrderService 单元测试（创建订单边界 + 取消订单边界）
✅ 全部 17 个测试通过
✅ Scalar 测试通过（买家/卖家视角、数据隔离验证）
✅ feature/orders 合并回 develop
✅ v0.5 里程碑 tag 打完
```



## Step 29 · 审核车辆（通过）

### 这一步做什么

进入第八阶段——管理员系统。Admin 的核心职责是审核卖家提交的车辆，决定是否允许上架。这一步实现审核通过：`POST /admin/cars/{id}/approve`。

------

### 1. 切出 feature/admin 分支

```bash
git checkout develop
git checkout -b feature/admin
git push -u origin feature/admin
```

------

### 2. Admin 接口的权限控制

前面所有接口的权限控制都是"登录 vs 未登录"。Admin 接口多了一层——不只是要登录，还要是 `Admin` 角色。

ASP.NET Core 提供了一个简洁的写法：

```csharp
[Authorize(Roles = "Admin")]
```

这一行特性做了两件事：

- 确保用户已登录（Token 有效）
- 确保用户的 Role Claim 包含 `"Admin"`

如果未登录，返回 401。如果已登录但不是 Admin，返回 403。这两种情况框架会自动处理，不需要在 Controller 里手动判断。

Step 10 里生成 Token 时用的是 `ClaimTypes.Role` 存角色值，`[Authorize(Roles = "Admin")]` 就是读取这个 Claim 来判断的——这就是当时为什么必须用 `ClaimTypes.Role` 而不是随便写个 `"role"` Key 的原因。

------

### 3. Admin 接口为什么用 `/admin/` 前缀

```
POST /admin/cars/{id}/approve
POST /admin/cars/{id}/reject
GET  /admin/cars/pending
DELETE /admin/cars/{id}
```

用 `/admin/` 前缀而不是直接复用 `/cars/` 有两个好处：

**第一，职责清晰。** `/cars/` 是用户侧的接口，`/admin/cars/` 是管理员侧的接口，虽然操作的是同一张表，但操作语义完全不同——用户的 `DELETE /cars/{id}` 是卖家删除自己的草稿，管理员的 `DELETE /admin/cars/{id}` 是强制下架违规车辆。分开前缀，一眼就知道这是管理员操作。

**第二，权限配置方便。** 可以在 Controller 级别用 `[Authorize(Roles = "Admin")]` 覆盖整个 `/admin/` 下的所有接口，不需要每个 Action 单独配置。

------

### 4. 创建 AdminCarService

Admin 的车辆操作和卖家的车辆操作虽然都涉及 `Car` 实体，但业务逻辑完全不同，单独建一个 `AdminCarService`，不混入 `CarService`：

```bash
touch UUcars.API/Services/AdminCarService.cs
```

```c#
using UUcars.API.DTOs.Responses;
using UUcars.API.Entities.Enums;
using UUcars.API.Exceptions;
using UUcars.API.Repositories;

namespace UUcars.API.Services;

public class AdminCarService
{
    private readonly ICarRepository _carRepository;
    private readonly ILogger<AdminCarService> _logger;

    public AdminCarService(ICarRepository carRepository, ILogger<AdminCarService> logger)
    {
        _carRepository = carRepository;
        _logger = logger;
    }

    public async Task<CarResponse> ApproveAsync(
        int carId,
        CancellationToken cancellationToken = default)
    {
        var car = await _carRepository.GetByIdAsync(carId, cancellationToken);

        if (car == null)
            throw new CarNotFoundException(carId);

        // 只有 PendingReview 状态才能审核通过
        // 已经 Published 的车不需要重复审核
        // Draft 的车还没提交，不应该直接被 Admin 通过
        if (car.Status != CarStatus.PendingReview)
            throw new CarStatusException(car.Id, car.Status, CarStatus.PendingReview);

        car.Status = CarStatus.Published;
        car.UpdatedAt = DateTime.UtcNow;

        var updated = await _carRepository.UpdateAsync(car, cancellationToken);

        _logger.LogInformation("Car {CarId} approved by admin, now Published", carId);

        return CarService.MapToResponse(updated);
    }
}
```

> **为什么复用 `CarService.MapToResponse` 而不是自己写一个？** `MapToResponse` 在 Step 14 里定义为 `internal static`，同一个程序集（Assembly）内都可以访问。`CarResponse` 的映射逻辑只有一份，不需要在 `AdminCarService` 里重复写一遍——重复代码意味着将来改 `CarResponse` 时要改两个地方，容易遗漏。

------

### 5. 创建 AdminController

```bash
touch UUcars.API/Controllers/AdminController.cs
```

```c#
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UUcars.API.DTOs;
using UUcars.API.DTOs.Responses;
using UUcars.API.Services;

namespace UUcars.API.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]    // Controller 级别：所有 Admin 接口都需要 Admin 角色
public class AdminController : ControllerBase
{
    private readonly AdminCarService _adminCarService;

    public AdminController(AdminCarService adminCarService)
    {
        _adminCarService = adminCarService;
    }

    // POST /admin/cars/{id}/approve
    [HttpPost("cars/{id:int}/approve")]
    public async Task<IActionResult> ApproveCar(int id, CancellationToken cancellationToken)
    {
        var car = await _adminCarService.ApproveAsync(id, cancellationToken);
        return Ok(ApiResponse<CarResponse>.Ok(car, "Car approved and published successfully."));
    }
}
```

------

### 6. 注册依赖到 Program.cs

```csharp
// Admin 模块
builder.Services.AddScoped<AdminCarService>();
```

------

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

------

### 8. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常审核通过：**

用 Admin 账号（`admin@uucars.com` / `Admin@123456`）登录，找一辆 `PendingReview` 状态的车（先用卖家账号创建一辆车并提交审核）：

```
POST /admin/cars/1/approve
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......（Admin Token）
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "status": "Published",
    ...
  },
  "message": "Car approved and published successfully."
}
```

审核通过后，这辆车应该出现在 `GET /cars` 的公开列表里。

**审核 Draft 状态的车（应返回 409）：**

尝试审核一辆还在 `Draft` 状态的车（还没提交），预期：

```json
{
  "success": false,
  "message": "Car '1' is in 'Draft' status. Required status: 'PendingReview'."
}
```

**用普通用户 Token 调用（应返回 403）：**

换普通用户的 Token，访问 `POST /admin/cars/1/approve`，预期返回 403。

**不带 Token 访问（应返回 401）：**

不带 Authorization 头，预期返回 401。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```

------

### 9. 此时的目录变化

```
UUcars.API/
├── Controllers/
│   └── AdminController.cs      ← 新增
└── Services/
    └── AdminCarService.cs      ← 新增
```

------

### 10. Git 提交

```bash
git add .
git commit -m "feat: POST /admin/cars/{id}/approve - approve car for publishing"
git push origin feature/admin
```

------

### Step 29 完成状态

```
✅ 切出 feature/admin 分支
✅ 理解 [Authorize(Roles = "Admin")] 的工作原理（依赖 ClaimTypes.Role）
✅ 理解 /admin/ 前缀的作用（职责清晰 + 权限配置方便）
✅ 理解为什么单独建 AdminCarService 而不是混入 CarService
✅ AdminCarService.ApproveAsync（PendingReview → Published）
✅ 复用 CarService.MapToResponse（internal static，不重复映射逻辑）
✅ AdminController（Controller 级别 [Authorize(Roles = "Admin")]）
✅ DI 注册完成
✅ Scalar 测试通过（200 审核成功、409 状态不对、403 非 Admin、401 未登录）
✅ Git commit + push 完成
```

## Step 30 · 审核车辆（拒绝）

### 这一步做什么

实现 `POST /admin/cars/{id}/reject`——Admin 审核不通过，把车辆退回给卖家修改。

------

### 1. 拒绝审核的业务逻辑

拒绝审核和通过审核的流程基本对称，但方向相反：

```
通过：PendingReview → Published（上架）
拒绝：PendingReview → Draft   （退回卖家修改）
```

退回 `Draft` 而不是 `Deleted` 的原因：拒绝不是永久封禁，只是"这次提交有问题，请修改后重新提交"。卖家收到退回后，可以修改车辆信息再次提交审核，走完整的流程。

------

### 2. 在 AdminCarService 里添加拒绝方法

打开 `UUcars.API/Services/AdminCarService.cs`，在 `ApproveAsync` 下方添加：

```csharp
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

    // 拒绝后退回 Draft，让卖家修改后重新提交
    car.Status = CarStatus.Draft;
    car.UpdatedAt = DateTime.UtcNow;

    var updated = await _carRepository.UpdateAsync(car, cancellationToken);

    _logger.LogInformation("Car {CarId} rejected by admin, returned to Draft", carId);

    return CarService.MapToResponse(updated);
}
```

------

### 3. 在 AdminController 里添加拒绝端点

打开 `UUcars.API/Controllers/AdminController.cs`，在 `ApproveCar` 方法下方添加：

```csharp
// POST /admin/cars/{id}/reject
[HttpPost("cars/{id:int}/reject")]
public async Task<IActionResult> RejectCar(int id, CancellationToken cancellationToken)
{
    var car = await _adminCarService.RejectAsync(id, cancellationToken);
    return Ok(ApiResponse<CarResponse>.Ok(car, "Car rejected and returned to seller for revision."));
}
```

------

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

------

### 5. 用 Scalar 测试

启动项目：

```bash
cd UUcars.API && dotnet run
```

**正常拒绝审核：**

用 Admin Token，找一辆 `PendingReview` 状态的车：

```
POST /admin/cars/1/reject
Authorization: Bearer eyJhbGciOiJIUzI1NiJ9......（Admin Token）
```

预期（200）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "status": "Draft",
    ...
  },
  "message": "Car rejected and returned to seller for revision."
}
```

**验证卖家可以重新提交：**

拒绝后车辆回到 `Draft`，卖家应该能修改后再次 `POST /cars/1/submit`，验证整个流程闭环。

**拒绝已经 Published 的车（应返回 409）：**

预期：

```json
{
  "success": false,
  "message": "Car '1' is in 'Published' status. Required status: 'PendingReview'."
}
```

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```

------

### 6. Git 提交

```bash
git add .
git commit -m "feat: POST /admin/cars/{id}/reject - reject car back to draft"
git push origin feature/admin
```

------

### Step 30 完成状态

```
✅ 理解拒绝审核退回 Draft 而不是 Deleted 的原因（允许修改重新提交）
✅ AdminCarService.RejectAsync（PendingReview → Draft）
✅ AdminController POST /admin/cars/{id}/reject
✅ Scalar 测试通过（200 拒绝成功、409 状态不对、卖家可重新提交验证）
✅ Git commit + push 完成
```

