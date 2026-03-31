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

