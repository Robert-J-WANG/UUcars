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
