## Step 36 · CORS 配置

### 这一步做什么

V1 的 API 跑在 `localhost:5065`，前端将跑在 `localhost:5173`（Vite 默认端口）。两个不同的端口，在浏览器看来是**不同的源（Origin）**，浏览器会拦截跨源请求——这就是 CORS 问题。

不解决 CORS，前端发出的任何请求都会被浏览器直接拦截，联调完全无法进行。所以 CORS 是 V2 第一步必须解决的事。



### 1. 什么是跨域（CORS）

浏览器有一个安全策略叫**同源策略（Same-Origin Policy）**：默认情况下，网页只能向和自己**同源**的服务器发请求。

**同源**的定义：协议 + 域名 + 端口三者完全一致。

```
前端地址：http://localhost:5173
后端地址：http://localhost:8080

协议相同（http）✅
域名相同（localhost）✅
端口不同（5173 vs 8080）❌

→ 跨域，浏览器拦截
```

注意，**CORS 是浏览器的行为**，不是服务器的行为。用 Scalar 或 curl 直接请求 API 不会有问题，因为它们不是浏览器，没有同源策略。只有浏览器里的 JavaScript 发出的请求才受同源策略限制。



### 2. 什么是 Preflight 请求

浏览器处理跨域请求时，对于非简单请求（比如带 `Authorization` 头、或者 `Content-Type: application/json` 的请求），会**先发一个 OPTIONS 请求**询问服务器"我可以跨域访问吗"，这个请求叫 **Preflight（预检请求）**。

```
前端发 POST /auth/login（带 Content-Type: application/json）
         ↓
浏览器先自动发：
  OPTIONS /auth/login
  Origin: http://localhost:5173
  Access-Control-Request-Method: POST
  Access-Control-Request-Headers: Content-Type, Authorization
         ↓
服务器响应（如果允许跨域）：
  Access-Control-Allow-Origin: http://localhost:5173
  Access-Control-Allow-Methods: GET, POST, PUT, DELETE
  Access-Control-Allow-Headers: Content-Type, Authorization
         ↓
浏览器确认可以跨域 → 发出真正的 POST /auth/login
```

如果服务器不响应 OPTIONS 请求或者不返回正确的 CORS 头，浏览器就会直接报错，真正的请求根本不会发出去。

所以配置 CORS 就是告诉服务器：**收到 OPTIONS 请求时，返回正确的响应头，让浏览器知道允许哪些跨域访问**。



### 3. 切出 feature/v2-backend 分支

V2 后端升级（Step 36-42）作为一个完整的功能块，切一个分支来做：

```bash
git checkout develop
git checkout -b feature/v2-backend
git push -u origin feature/v2-backend
```



### 4. 配置 CORS

#### 4.1 更新 appsettings.json

先在配置文件里定义允许的前端地址，不要硬编码在代码里：

打开 `UUcars.API/appsettings.json`，添加 `Cors` 配置节：

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
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
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

> **为什么同时允许 5173 和 3000？** Vite 默认端口是 5173，但某些情况下（比如已有服务占用）会变成其他端口。3000 是 Create React App 的默认端口。列出常用开发端口，省得联调时因为端口变了又回来改配置。生产环境会单独配置真实域名，不走这里。

#### 4.2 创建 CorsExtensions.cs

把 CORS 配置同样抽成扩展方法，延续 V1 里 `AuthExtensions` 的风格，放进 `Extensions/` 目录：

```bash
touch UUcars.API/Extensions/CorsExtensions.cs
```

```c#
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
                    // V2 的 Refresh Token 存在 HttpOnly Cookie 里，需要这个
                    .AllowCredentials();
            });
        });

        return services;
    }
}
```

> **`AllowAnyOrigin` 为什么不能用？** 很多教程里图省事直接写 `AllowAnyOrigin()`，意味着任何网站都可以调用你的 API。一旦 API 需要携带 Cookie（比如 Refresh Token 用 HttpOnly Cookie），浏览器会直接报错——规范要求携带凭证时不能用通配符 Origin。而且从安全角度，允许任意来源意味着任何网站都可以代表用户的浏览器发请求，有 CSRF 风险。始终应该明确指定允许的域名。

#### 4.3 更新 Program.cs

打开 `UUcars.API/Program.cs`，在服务注册区域添加：

```csharp
using UUcars.API.Extensions;
```

在 `AddJwtAuthentication` 下方添加：

```csharp
// CORS 配置
builder.Services.AddUUcarsCors(builder.Configuration);
```

在中间件管道里，`UseAuthentication()` **之前**添加 `UseCors()`：

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

// CORS 必须在 Authentication 和 Authorization 之前
// 原因：Preflight 请求（OPTIONS）不携带 Token，
// 如果 CORS 在 Auth 之后，Preflight 会因为没有 Token 被拦截，
// 导致浏览器认为服务器不支持跨域
app.UseCors(CorsExtensions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

> **CORS 中间件的顺序为什么很重要？** Preflight 请求（OPTIONS）浏览器不会携带 Authorization Token——因为它只是在询问"我能不能访问"，还没有真正发请求。如果 CORS 中间件放在 Authentication 之后，OPTIONS 请求会因为没有 Token 被 `[Authorize]` 拦截，返回 401，浏览器就认为服务器不允许跨域访问，后续请求全部失败。CORS 放在最前面，OPTIONS 直接被处理返回允许，不走认证流程。



### 5. 处理生产环境的 CORS

生产环境不能用 `localhost`，需要配置真实域名。在 `appsettings.json` 里的 AllowedOrigins 只是默认配置，生产环境通过环境变量覆盖（和 V1 的连接字符串一样的机制）：

```bash
# docker-compose.yml 或服务器环境变量里配置
Cors__AllowedOrigins__0=https://uucars.yourdomain.com
Cors__AllowedOrigins__1=https://www.uucars.yourdomain.com
```

这样不需要改代码，只需要改环境变量就能适应不同环境。



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



### 7. 验证 CORS 是否生效

启动 API：

```bash
cd UUcars.API && dotnet run
```

用 curl 模拟浏览器的 Preflight 请求：

```bash
curl -v -X OPTIONS http://localhost:5065/auth/login \
  -H "Origin: http://localhost:5173" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type, Authorization"
```

在响应头里应该能看到：

```
Access-Control-Allow-Origin: http://localhost:5173
Access-Control-Allow-Methods: POST
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Allow-Credentials: true
```

看到这些响应头，说明 CORS 配置正确。

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 8. 此时的目录变化

```
UUcars.API/
├── Extensions/
│   ├── AuthExtensions.cs   （已有）
│   └── CorsExtensions.cs   ← 新增
└── appsettings.json        ← 已更新（新增 Cors 配置节）
```



### 9. Git 提交

```bash
git add .
git commit -m "feat: CORS configuration for frontend development"
git push origin feature/v2-backend
```



### Step 36 完成状态

```
✅ 理解跨域（同源策略）和 CORS 的本质
✅ 理解 Preflight 请求的作用和处理方式
✅ 理解为什么不能用 AllowAnyOrigin（安全 + Cookie 限制）
✅ CorsExtensions.cs（从配置读取允许的 Origin 列表）
✅ appsettings.json 新增 Cors 配置节（开发环境允许 localhost:5173）
✅ Program.cs 注册 CORS，中间件顺序正确（在 Auth 之前）
✅ 理解 CORS 中间件为什么要放在认证之前
✅ 生产环境通过环境变量覆盖域名（不改代码）
✅ curl 验证 Preflight 请求返回正确响应头
✅ Git commit + push 完成
```



好，重新生成之前先把所有涉及第三方库的用法查证一遍。官方文档确认完毕。现在生成完整的 Step 37。



## Step 37 · 邮件服务基础设施

### 这一步做什么

V1 的注册接口创建用户时，`EmailConfirmed` 直接写入 `false`，邮箱验证功能是空壳。

要让它真正工作：

- 注册时发验证邮件
- 忘记密码时发重置邮件

在写这两个功能之前，先把发邮件的基础设施搭好。这一步的产出是一个可以在整个项目里复用的邮件服务，后续所有需要发邮件的地方直接注入使用。



### 1. 邮件发送的工作原理

**邮件发送涉及两个角色：**

```
应用程序（发件方）→ 邮件服务商（中转）→ 用户邮箱（收件方）
```

应用程序不能直接把邮件"推"到用户的 Gmail 或 Outlook 收件箱里。互联网邮件系统有严格的信任机制——如果任何人都能随意发送邮件并声称自己是 `noreply@uucars.com`，垃圾邮件就会泛滥。所以必须通过一个**有发信资质的第三方邮件服务商**中转，由它来代发送并保证送达率。

**常见选择对比：**

```
SendGrid    老牌服务，功能全，但免费额度近年大幅缩减，注册流程繁琐
Mailgun     开发者友好，但免费版只能发给验证过的收件人，调试不方便
Resend      2023年出现的新服务，API设计简洁，有官方.NET SDK，免费额度够用（每天100封）
            
```

**开发环境怎么收测试邮件？**

Resend 免费账号提供一个内置的测试发件地址 `onboarding@resend.dev`，无需验证域名就可以直接发送。收件人可以是自己的邮箱。这意味着开发时可以用真实邮箱收测试邮件，验证整个流程，不需要任何额外工具。

生产上线时再把发件地址换成自己的域名邮箱（在 Resend 控制台验证域名即可），代码不需要任何改动。



### 2. 安装 Resend SDK

```bash
dotnet add UUcars.API/UUcars.API.csproj package Resend
```

Resend SDK 的使用分两部分：

**第一部分：在 DI 容器里注册**（在 `Program.cs` 或扩展方法里）：

```csharp
// 官方文档的标准注册方式，四行缺一不可
builder.Services.AddOptions();                  // 启用 Options 模式
builder.Services.AddHttpClient<ResendClient>(); // 注册托管的 HttpClient
builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = "你的ApiKey";  // 告诉 SDK 用哪个 Key
});
builder.Services.AddTransient<IResend, ResendClient>(); // 注册 IResend 实现
```

> **为什么需要这四行？**
>
> `AddOptions()` 启用 ASP.NET Core 的 Options 模式，`ResendClientOptions` 需要它才能工作。
>
> `AddHttpClient<ResendClient>()` 为 `ResendClient` 注册一个托管的 `HttpClient`。`ResendClient` 内部通过 HTTP 调用 Resend 的 API，必须有 `HttpClient`。用 `AddHttpClient` 而不是手动 `new HttpClient()` 是 .NET 的标准做法，框架负责管理连接池，避免资源泄露。
>
> `Configure<ResendClientOptions>` 配置的是 **SDK 自己的选项类** `ResendClientOptions`，SDK 从这里读取 API Key。
>
> `AddTransient<IResend, ResendClient>()` 把 `ResendClient` 注册为 `IResend` 的实现，之后就可以在构造函数里注入 `IResend` 使用了。

**第二部分：注入后使用**：

```csharp
// 注入 IResend，构建 EmailMessage，调用 EmailSendAsync
var message = new EmailMessage();
message.From = "UUcars <onboarding@resend.dev>";
message.To.Add("user@example.com");
message.Subject = "验证你的邮箱";
message.HtmlBody = "<p>邮件正文</p>";

await _resend.EmailSendAsync(message);
```

`EmailMessage` 的关键字段：

```
From      发件人，格式是 "显示名称 <邮箱地址>"
To        收件人集合，用 .Add() 添加，支持多个收件人
Subject   邮件主题
HtmlBody  HTML 格式正文
```



### 3. 注册 Resend 账号并获取 API Key

配置SDK的选项类`ResendClientOptions`时，需要读取API Key， 因此先需要一个API Key。

API Key 是 Resend 用来识别"是谁在发邮件"的凭证。每次调用 Resend 发邮件时，请求里都会带上这个 Key，Resend 验证通过后才会真正发出邮件。

前往 [resend.com](https://resend.com/) 注册免费账号。

登录后进入 **API Keys** 页面，点击 **Create API Key**，权限选择 **Sending access**，创建后**立即复制**这个 Key（页面关闭后不再显示）。

> API Key 必须保密——拿到它就能以你的名义发邮件，还会消耗你的免费额度。



### 4. 配置管理

拿到API Key之后， 应该放在哪里？

另外，发邮件需要用到其他几个配置项：API Key、发件人地址、发件人名称，以及邮件里链接要跳转到的前端地址。

这些配置和 V1 里的 `JwtSettings`、数据库连接字符串一样，都属于"外部配置"，应该从配置文件读取，不能硬编码在代码里。

因此，遵循 V1 建立的规范，建一个对应的配置绑定类。

#### 4.1 创建 EmailSettings 配置绑定类

```bash
touch UUcars.API/Configurations/EmailSettings.cs
```

```c#
namespace UUcars.API.Configurations;

public class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    // 前端地址，用于拼接邮件里的跳转链接
    // 邮件里的验证链接需要指向前端页面，不是后端 API
    // 比如：http://localhost:5173/verify-email?token=xxx
    // 开发环境：http://localhost:5173（Vite 默认端口）
    // 生产环境：https://yourdomain.com
    public string BaseUrl { get; set; } = string.Empty;
}
```

#### 4.2 更新 appsettings.json

打开 `UUcars.API/appsettings.json`，添加 `EmailSettings` 配置节：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "JwtSettings": {
    "Secret": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS",
    "ExpiresInMinutes": 60,
    "Issuer": "UUcars",
    "Audience": "UUcarsUsers"
  },
  "EmailSettings": {
    "ApiKey": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS",
    "FromEmail": "onboarding@resend.dev",
    "FromName": "UUcars",
    "BaseUrl": "http://localhost:5173"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
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

#### 4.3 把 API Key 存入 User Secrets

API Key 是敏感信息，和 V1 的 JWT Secret、数据库密码一样，绝不能进 Git：

```bash
cd UUcars.API
dotnet user-secrets set "EmailSettings:ApiKey" "re_XRDQUDbv_F4aCECAvS8DNQyyuAKWVEUQ9"
cd ..
```

验证设置成功：

```bash
cd UUcars.API && dotnet user-secrets list && cd ..
```

应该能看到 `EmailSettings:ApiKey` 已经设置。



### 5. 定义 IEmailService 接口

现在配置好了，发邮件的代码怎么写? 

最直接的想法是：在 `UserService` 里直接使用 Resend，调用它的发邮件方法。

**如果把 Resend 的调用直接写死在 `UserService` 里，会有什么问题？**

```csharp
// 只是思考，不要真的这样写
public class UserService
{
    public async Task RegisterAsync(RegisterRequest request)
    {
        // ... 创建用户 ...

        // 直接依赖 Resend 的具体类
        // 问题1：单元测试时会真的发邮件，慢且消耗免费额度
        // 问题2：将来换邮件服务商，要改 UserService 内部代码
    }
}
```

因此，和 V1 里处理数据库访问的方式完全一样：**定义一个接口，业务代码依赖接口，具体实现通过 DI 注入。** 而测试时用 Fake 实现替换，不发真实邮件。

先定义IEmailService 接口， 要实现2个功能：

- 发送邮箱验证邮件
- 发送密码重置邮件

```bash
mkdir -p UUcars.API/Services/Email
touch UUcars.API/Services/Email/IEmailService.cs
```

```c#
namespace UUcars.API.Services.Email;

public interface IEmailService
{
    // 发送邮箱验证邮件
    // email：收件人地址
    // token：验证Token，前端用它调用验证接口
    Task SendEmailVerificationAsync(string email, string token,
        CancellationToken cancellationToken = default);

    // 发送密码重置邮件
    Task SendPasswordResetAsync(string email, string token,
        CancellationToken cancellationToken = default);
}
```



### 6. 验证邮件里的 Token 是什么

引入一个新概念：**一次性随机验证 Token**。

目前我们涉及到的三种不同的 Token ，虽然它们名字相似但用途完全不同：

```jade
邮箱验证 Token（这一步新增）

  是什么：一个随机字符串，比如 aB3kR9mXpQ2...
  存在哪：数据库 Users 表的 EmailConfirmationToken 字段（临时）
  用途：  证明"点击链接的人，确实能收到这个邮箱的邮件"
  生命周期：用完立刻清除，只能用一次，24小时过期

JWT Token（V1 已有）

  是什么：登录成功后颁发的身份凭证，格式是三段 Base64， 用 . 连接
  存在哪：不存数据库，保存在客户端（浏览器）
  用途：  证明"发这个请求的人是已登录的某某用户"
  生命周期：60分钟，过期自动失效

Resend API Key（Step 37 配置的）

  是什么：Resend 平台分配给你账号的凭证，比如 re_abc123...
  存在哪：User Secrets / 环境变量
  用途：  你的应用调用 Resend 发邮件时证明身份用
  生命周期：长期有效，在 Resend 控制台手动撤销
```

三者没有任何关联，只是都叫"Token"或"Key"。

**为什么验证 Token 必须是随机的？**

这个 Token 会被放进邮件链接里对外暴露：

```
http://localhost:5173/verify-email?token=aB3kR9mXpQ2...
```

如果 Token 是可预测的（比如用用户 ID、注册时间拼接），攻击者可以猜出其他人的验证链接，替别人完成验证，绕过邮箱所有权的确认。

常见的做法是: 用 `RandomNumberGenerator.GetBytes(32)` 生成 32 字节（256位）的随机数，转成 Base64url字符串。

发送邮箱验证邮件和发送密码重置邮件时都需要验证Token，封装通用的token生成器。

定义一个工具类，用于生成加密安全的随机 Token：

```bash
touch UUcars.API/Auth/TokenGenerator.cs
```

```c#
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace UUcars.API.Auth;

public class TokenGenerator
{
    // 生成加密安全的随机 Token
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return WebEncoders.Base64UrlEncode(bytes);
    }
}
```

>说明：
>
>- RandomNumberGenerator 是 .NET 内置的加密安全随机数生成器
>- 32 字节 = 256 位随机性，碰撞概率可以忽略不计
>- 使用 WebEncoders，它将字节直接转为 URL 安全的字符串（Base64url标准）：自动把 + 换成 -，把 / 换成 _，并去掉末尾的 =
>- 放进 URL 时不需要 格外的转义

**注意：如果是Base64 可能含有 +、/、= 字符，放进 URL 时需要 Uri.EscapeDataString 转义**



### 7. 实现 ResendEmailService

实现IEmailService功能

```bash
touch UUcars.API/Services/Email/ResendEmailService.cs
```

```c#
using Microsoft.Extensions.Options;
using Resend;
using UUcars.API.Configurations;

namespace UUcars.API.Services.Email;

public class ResendEmailService : IEmailService
{
    // resend的核心对象
    private readonly IResend _resend;
    // 获取email参数的配置
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        IResend resend,
        IOptions<EmailSettings> emailSettings,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailVerificationAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        // 拼接前端验证页面的完整链接
        var verificationLink =
            $"{_emailSettings.BaseUrl}/verify-email?token={token}";

        var message = new EmailMessage();
        message.From = $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>";
        message.To.Add(email);
        message.Subject = "验证你的 UUcars 邮箱";
        message.HtmlBody = BuildVerificationEmailHtml(verificationLink);

        await _resend.EmailSendAsync(message, cancellationToken);

        _logger.LogInformation("Verification email sent to {Email}", email);
    }

    public async Task SendPasswordResetAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        var resetLink =
            $"{_emailSettings.BaseUrl}/reset-password?token={token}";

        var message = new EmailMessage();
        message.From = $"{_emailSettings.FromName} <{_emailSettings.FromEmail}>";
        message.To.Add(email);
        message.Subject = "重置你的 UUcars 密码";
        message.HtmlBody = BuildPasswordResetEmailHtml(resetLink);

        await _resend.EmailSendAsync(message, cancellationToken);

        _logger.LogInformation("Password reset email sent to {Email}", email);
    }

    // =============================================
    // 邮件 HTML 模板
    // =============================================

    private static string BuildVerificationEmailHtml(string verificationLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px;
                     margin: 0 auto; padding: 20px;">
            <h2 style="color: #333;">验证你的邮箱</h2>
            <p>感谢注册 UUcars！请点击下方按钮完成邮箱验证：</p>
            <a href="{verificationLink}"
               style="display: inline-block; padding: 12px 24px;
                      background-color: #2563eb; color: white;
                      text-decoration: none; border-radius: 6px; margin: 16px 0;">
                验证邮箱
            </a>
            <p style="color: #666; font-size: 14px;">
                链接有效期为 24 小时。如果你没有注册 UUcars，请忽略此邮件。
            </p>
            <p style="color: #999; font-size: 12px;">
                如果按钮无法点击，请复制以下链接到浏览器：<br/>
                <a href="{verificationLink}">{verificationLink}</a>
            </p>
        </body>
        </html>
        """;

    private static string BuildPasswordResetEmailHtml(string resetLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: Arial, sans-serif; max-width: 600px;
                     margin: 0 auto; padding: 20px;">
            <h2 style="color: #333;">重置你的密码</h2>
            <p>我们收到了你的密码重置请求，请点击下方按钮设置新密码：</p>
            <a href="{resetLink}"
               style="display: inline-block; padding: 12px 24px;
                      background-color: #2563eb; color: white;
                      text-decoration: none; border-radius: 6px; margin: 16px 0;">
                重置密码
            </a>
            <p style="color: #666; font-size: 14px;">
                链接有效期为 1 小时。如果你没有发起此请求，
                请忽略此邮件，你的密码不会被修改。
            </p>
            <p style="color: #999; font-size: 12px;">
                如果按钮无法点击，请复制以下链接到浏览器：<br/>
                <a href="{resetLink}">{resetLink}</a>
            </p>
        </body>
        </html>
        """;
}
```



### 8. 注册到 DI 容器

实现类写好了，接下来把所有相关服务注册进 DI 容器。和 V1 的 `AuthExtensions`、`CorsExtensions` 保持一致的风格，抽成扩展方法：

```bash
touch UUcars.API/Extensions/EmailExtensions.cs
```

```c#
using Resend;
using UUcars.API.Configurations;
using UUcars.API.Services.Email;

namespace UUcars.API.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 绑定我们自己的 EmailSettings（FromEmail、FromName、BaseUrl 等）
        // 和 JwtSettings 的绑定方式完全一样
        services.Configure<EmailSettings>(
            configuration.GetSection("EmailSettings"));

        // 读取 API Key
        // 启动时就验证配置是否存在，而不是等到第一次发邮件才报错
        var apiKey = configuration["EmailSettings:ApiKey"]
                     ?? throw new InvalidOperationException(
                         "EmailSettings:ApiKey is not configured. " +
                         "Run: dotnet user-secrets set \"EmailSettings:ApiKey\" \"re_xxx\"");

        // 以下是 Resend 官方文档的标准注册方式
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = apiKey;
        });
        services.AddTransient<IResend, ResendClient>();

        return services;
    }
}
```

打开 `Program.cs`，在 `AddUUcarsCors` 下方添加：

```csharp
// 配置邮件服务
builder.Services.AddEmailService(builder.Configuration);
```

注册邮件，用户模块后面添加

```c#
// 邮件服务
builder.Services.AddScoped<IEmailService, ResendEmailService>();
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



### 10. 手动验证邮件能发出去

进入 下一步 之前，先验证邮件服务本身是不是通的。

打开 `UUcars.API/Controllers/AuthController.cs`，临时添加一个测试端点：

```csharp
// 临时测试接口，验证邮件服务配置是否正常
// 测试完成后立刻删除
[HttpPost("test-email")]
public async Task<IActionResult> TestEmail(
    [FromQuery] string to,
    [FromServices] IEmailService emailService,
    CancellationToken cancellationToken)
{
    await emailService.SendEmailVerificationAsync(
        to, "test-token-12345", cancellationToken);
    return Ok(ApiResponse<object>.Ok(null, $"Test email sent to {to}"));
}
```

启动项目，用 Scalar 调用：

```
POST /auth/test-email?to=你的邮箱地址
```

检查邮箱，收到验证邮件，说明配置正确。

**立刻删除这个临时端点**，回到 `AuthController.cs` 把它去掉。



### 11. 此时的目录变化

```
UUcars.API/
├── Configurations/
│   └── EmailSettings.cs            ← 新增
├── Extensions/
│   └── EmailExtensions.cs          ← 新增
└── Services/
    └── Email/                      ← 新增目录
        ├── IEmailService.cs        ← 新增
        └── ResendEmailService.cs   ← 新增
```



### 12. Git 提交

```bash
git add .
git commit -m "feat: email service infrastructure with Resend"
git push origin feature/v2-backend
```



### Step 37 完成状态

```
✅ 理解邮件发送原理（应用→服务商→收件人）
✅ 注册 Resend 账号，API Key 存入 User Secrets
✅ EmailSettings 配置绑定类（含 BaseUrl）
✅ appsettings.json 新增 EmailSettings 配置节
✅ IEmailService 接口（两个发邮件方法）
✅ TokenGenerator 工具类（加密安全随机 Token）
✅ 理解为什么需要接口（可测试、可替换）
✅ 理解 Resend SDK 官方注册方式（四行，来自官方文档）
✅ ResendEmailService 实现（含 HTML 邮件模板、Token URL 转义）
✅ EmailExtensions 扩展方法（和 AuthExtensions/CorsExtensions 风格一致）
✅ 注册进 Program.cs
✅ 临时测试端点验证邮件发送成功后删除
✅ dotnet build 通过
✅ Git commit 完成
```



## Step 38 · 邮箱验证注册

### 这一步做什么

V1 的注册接口创建用户时 `EmailConfirmed = false`，但从来不检查这个字段——未验证的用户照样能登录、发车、下单，`EmailConfirmed` 只是一个摆设。

这一步让它真正工作。



### 1. 完整的注册流程

完整的注册流程分两个阶段：

- 创建账号
- 激活账号（邮箱验证）

```
阶段一：创建账号（立刻完成）

  用户提交邮箱+密码
        ↓
  服务端创建账号，EmailConfirmed = false
        ↓
  服务端发一封验证邮件到用户邮箱
        ↓
  注册接口返回成功

阶段二：验证邮箱（用户主动完成激活）

  用户去邮箱点击验证链接
        ↓
  EmailConfirmed = true
        ↓
  账号激活，可以登录
```

v1版的注册接口只做了阶段一——创建账号、发邮件。

**它不知道这个邮箱是否真实存在**，也不知道用户有没有收到邮件。任何人都可以用 `abc@假域名.com` 这种不存在的邮箱注册，账号创建会成功，但邮件永远送不出去。

所以**登录时必须检查 `EmailConfirmed`**，这一步是在问：

```
这个用户注册之后，有没有真正点击过验证链接，去激活账号？
证明他确实拥有这个邮箱？
```

没有验证过的账号，邮箱可能是假的或者是别人的，不应该允许登录。

> **登录时没有任何邮件发送，也没有链接跳转。** 登录流程只是多了一个检查：密码正确之后，再看 `EmailConfirmed` 是否为 `true`。是则颁发 JWT Token，否则返回 403 提示"请先验证邮箱"。



### 3. 数据库 Migration

我们新增一些字段，用了保存邮件验证的操作：

- EmailConfirmationToken
- EmailConfirmationTokenExpiry

因此，要执行数据库的迁移操作，来更新数据结构。

#### 切分支确认

```bash
git checkout feature/v2-backend
```

#### 更新 User 实体

打开 `UUcars.API/Entities/User.cs`，添加两个字段：

```csharp
using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool EmailConfirmed { get; set; } = false;

    // V2 新增：邮箱验证
    // 用户未发起验证时这两个字段为 NULL
    // 发送验证邮件时写入，验证成功后立刻清除
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    // 导航属性
    public ICollection<Car> Cars { get; set; } = [];
    public ICollection<Order> BuyerOrders { get; set; } = [];
    public ICollection<Order> SellerOrders { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
```

#### 更新 UserConfiguration

打开 `UUcars.API/Configurations/UserConfiguration.cs`，在 `EmailConfirmed` 配置下方补上新字段：

```csharp
builder.Property(u => u.EmailConfirmed)
    .IsRequired()
    .HasDefaultValue(false);

// V2 新增：允许为 NULL（用户未发起验证时没有 Token）
builder.Property(u => u.EmailConfirmationToken)
    .HasMaxLength(200);

builder.Property(u => u.EmailConfirmationTokenExpiry);
```

#### 生成并执行 Migration

```bash
dotnet ef migrations add AddEmailVerificationFields \
    --project UUcars.API/UUcars.API.csproj

dotnet ef database update \
    --project UUcars.API/UUcars.API.csproj
```

> **增量 Migration 和初始 Migration 的区别：** V1 的 `InitialCreate` 是从零建所有表。这次的 `AddEmailVerificationFields` 只生成针对变化的 SQL——`ALTER TABLE Users ADD EmailConfirmationToken nvarchar(200) NULL` 这样的增量语句。EF Core 通过对比当前实体定义和 `AppDbContextModelSnapshot.cs` 里记录的上一次状态，自动计算出差异。已有数据不受影响。



### 4. 扩展 IUserRepository

验证邮箱需要按 Token 查找用户，Repository 需要新增这个方法。

打开 `UUcars.API/Repositories/IUserRepository.cs`：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email,
        CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id,
        CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user,
        CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user,
        CancellationToken cancellationToken = default);

    // V2 新增：按邮箱验证 Token 查找用户
    // 用户点击验证链接时，前端把 Token 传回来，服务端用它找到对应的用户
    Task<User?> GetByEmailConfirmationTokenAsync(string token,
        CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfUserRepository.cs`，补充实现：

```csharp
public async Task<User?> GetByEmailConfirmationTokenAsync(string token,
    CancellationToken cancellationToken = default)
{
    return await _context.Users
        .FirstOrDefaultAsync(
            u => u.EmailConfirmationToken == token, cancellationToken);
}
```



### 5. 创建业务异常

- 用户点了两次验证链接
- Token 无效或已过期
- 邮箱未验证时尝试登录

```bash
touch UUcars.API/Exceptions/EmailAlreadyConfirmedException.cs
touch UUcars.API/Exceptions/InvalidTokenException.cs
touch UUcars.API/Exceptions/EmailNotConfirmedException.cs
```

```c#
namespace UUcars.API.Exceptions;

// 多次点击验证链接
public class EmailAlreadyConfirmedException : AppException
{
    public EmailAlreadyConfirmedException()
        : base(StatusCodes.Status409Conflict,
            "Email is already confirmed.")
    {
    }
}
namespace UUcars.API.Exceptions;

// Token 无效或已过期（邮箱验证和密码重置共用）
// 不区分"Token不存在"和"Token过期"两种情况
// 原因：区分会让攻击者知道Token是否存在，带来信息泄露风险
public class InvalidTokenException : AppException
{
    public InvalidTokenException()
        : base(StatusCodes.Status400BadRequest,
            "The token is invalid or has expired.")
    {
    }
}
namespace UUcars.API.Exceptions;

// 邮箱未验证时尝试登录
// 为什么用 403 而不是 401？
// 401（Unauthorized）：没有提供任何身份凭证，服务器不知道你是谁
// 403（Forbidden）：  身份已确认（密码正确），但这个账号当前不允许访问
// 未验证邮箱属于后者：密码是对的，只是账号还没完成激活
// 前端收到 403 就知道要显示"请验证邮箱"的提示，而不是"密码错误"
public class EmailNotConfirmedException : AppException
{
    public EmailNotConfirmedException()
        : base(StatusCodes.Status403Forbidden,
            "Please verify your email address before logging in.")
    {
    }
}
```



### 6. 更新 UserService

邮件验证业务思路：

- 注册：用户点击注册（调用RegisterAsync方法），服务端先保存用户，再发验证邮件（IEmailService里的SendEmailVerificationAsync方法）
- 激活：用户点击验证邮件激活（需要调用一个新的方法：VerifyEmailAsync），服务端匹配token，并更新EmailConfirmed字段为true，激活成功，清空token
- 重发激活邮件： 如果某些原因，没有收到验证邮件，用户点击重新发送验证邮件（需要调用一个新的方法：ResendVerificationAsync），服务端生成新的token，再发验证邮件（IEmailService里的SendEmailVerificationAsync方法）
- 登录：用户点击登录（调用LoginAsync方法），服务端需要格外检查邮件是否已经激活（EmailConfirmed=true），否则返回403，前端收到 403 后可以显示"请先验证邮箱，或重新发送验证邮件"的提示

因此，对 UserService的更新包含：

- 更新注册逻辑（RegisterAsync）：注册后发验证邮件
- 更新登录逻辑（LoginAsync）：外检查邮件是否已经激活
- 新增激活邮件的逻辑（VerifyEmailAsync）
- 新增重发激活邮件的逻辑（ResendVerificationAsync）

打开 `UUcars.API/Services/UserService.cs`，构造函数新增 `IEmailService` 依赖，更新四个方法：

```csharp
...

namespace UUcars.API.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        JwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _logger = logger;
    }

	// =============================================
    // 更新注册（注册后发验证邮件）
    // =============================================
    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(
            request.Email, cancellationToken);
        if (existing != null)
            throw new UserAlreadyExistsException(request.Email);

        var confirmationToken = TokenGenerator.Generate();

        var user = new User
        {
            Username = request.Username,
            Email = request.Email.ToLower(),
            Role = UserRole.User,
            EmailConfirmed = false,
            
            // 验证邮件的token
            EmailConfirmationToken = confirmationToken,
            EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24),
            
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var created = await _userRepository.AddAsync(user, cancellationToken);

        // 先保存用户，再发邮件
        // 顺序很重要：确保用户已写入数据库，邮件里的验证链接才有意义
        // 如果反过来——邮件发出去了但数据库写入失败，
        // 用户点链接时服务端找不到这个 Token，验证永远失败
        // 如果邮件发送失败，用户可以通过"重新发送"功能补救
        await _emailService.SendEmailVerificationAsync(
            created.Email, confirmationToken, cancellationToken);

        _logger.LogInformation(
            "New user registered: {Email}, verification email sent", created.Email);

        return MapToResponse(created);
    }

    // =============================================
    // 更新登录（检查邮箱是否已验证）
    // =============================================
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(
            request.Email, cancellationToken);

        if (user == null)
            throw new InvalidCredentialsException();

        var result = _passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new InvalidCredentialsException();

        // 邮箱未验证不允许登录
        // 密码正确但邮箱未验证 → 403，提示用户去验证邮箱
        // 前端收到 403 后可以显示"请先验证邮箱，或重新发送验证邮件"的提示
        if (!user.EmailConfirmed)
            throw new EmailNotConfirmedException();

        var token = _jwtTokenGenerator.GenerateToken(user);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToResponse(user)
        };
    }

    // =============================================
    // 新增：验证邮箱
    // =============================================
    public async Task VerifyEmailAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // 用 Token 找到对应的用户
        // 找不到说明 Token 不存在（从未发过或已被清除）
        var user = await _userRepository
            .GetByEmailConfirmationTokenAsync(token, cancellationToken);

        if (user == null)
            throw new InvalidTokenException();

        if (user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
            throw new InvalidTokenException();

        // 已经验证过（用户点了两次验证链接）
        if (user.EmailConfirmed)
            throw new EmailAlreadyConfirmedException();

        // 验证通过：标记已验证，清除 Token（一次性使用，用完即删）
        user.EmailConfirmed = true;
        user.EmailConfirmationToken = null;
        user.EmailConfirmationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Email verified for user: {Email}", user.Email);
    }

    // =============================================
    // 新增：重新发送验证邮件
    // =============================================
    public async Task ResendVerificationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(
            email, cancellationToken);

        // 安全设计：用户不存在时静默返回，不透露邮箱是否注册
        // 和 Step 39 忘记密码接口保持一致的安全原则：
        // 不能让攻击者通过这个接口枚举系统里注册过哪些邮箱
        if (user == null)
        {
            _logger.LogWarning(
                "Resend verification requested for non-existent email: {Email}",
                email);
            return;
        }

        if (user.EmailConfirmed)
            throw new EmailAlreadyConfirmedException();

        // 生成新 Token，旧 Token 同时作废
        // 每次重发都刷新 Token 和有效期，避免旧链接仍然有效
        var newToken = TokenGenerator.Generate();
        user.EmailConfirmationToken = newToken;
        user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _emailService.SendEmailVerificationAsync(
            user.Email, newToken, cancellationToken);

        _logger.LogInformation("Verification email resent to: {Email}", email);
    }

    // =============================================
    // 其他方法不变，保留 V1 实现
    // =============================================
   
    ...

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



### 7. 创建请求 DTO

```bash
touch UUcars.API/DTOs/Requests/VerifyEmailRequest.cs
touch UUcars.API/DTOs/Requests/ResendVerificationRequest.cs
```

```c#
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class ResendVerificationRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
}
```



### 8. 更新 AuthController

controller的更新包含：

- 注册接口（Register）：更新注册提示文字
- 登录接口 （Login）：不用改变
- 新增：激活验证邮件接口（ VerifyEmail ）
- 新增：充分激活验证邮件接口 （ResendVerification）

打开 `UUcars.API/Controllers/AuthController.cs`，更新注册提示文字，添加两个新端点：

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register(
    [FromBody] RegisterRequest request,
    CancellationToken cancellationToken)
{
    var user = await _userService.RegisterAsync(request, cancellationToken);
    return StatusCode(StatusCodes.Status201Created,
        ApiResponse<UserResponse>.Ok(user,
            "Registration successful. Please check your email to verify your account."));
}

[HttpPost("login")]
public async Task<IActionResult> Login(
    [FromBody] LoginRequest request,
    CancellationToken cancellationToken)
{
    var result = await _userService.LoginAsync(request, cancellationToken);
    return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
}

// GET /auth/verify-email?token=xxx
// 为什么用 GET 而不是 POST？
// 验证链接直接放在邮件里，用户点击就是一个 GET 请求
// 浏览器打开链接 → 前端页面从 URL 读取 token → 调用这个接口
// 如果用 POST，用户点邮件链接后需要前端页面额外触发一次 POST 请求，
// 但 GET 更简单：前端页面加载时直接把 URL 里的 token 传给这个接口即可
[HttpGet("verify-email")]
public async Task<IActionResult> VerifyEmail(
    [FromQuery] VerifyEmailRequest request,
    CancellationToken cancellationToken)
{
    await _userService.VerifyEmailAsync(request.Token, cancellationToken);
    return Ok(ApiResponse<object>.Ok(null,
        "Email verified successfully. You can now log in."));
}

[HttpPost("resend-verification")]
public async Task<IActionResult> ResendVerification(
    [FromBody] ResendVerificationRequest request,
    CancellationToken cancellationToken)
{
    await _userService.ResendVerificationAsync(request.Email, cancellationToken);

    // 无论用户是否存在，始终返回相同的成功响应
    // 不让外部通过响应差异判断邮箱是否注册
    return Ok(ApiResponse<object>.Ok(null,
        "If this email is registered and unverified, " +
        "a new verification email has been sent."));
}
```



### 9. 更新单元测试

`UserService` 构造函数新增了 `IEmailService` 依赖，现有测试的 `CreateService` 方法需要更新，否则编译报错。同时补充新业务逻辑的测试。

#### 创建 FakeEmailService

```
touch UUcars.Tests/Fakes/FakeEmailService.cs
```

```c#
using UUcars.API.Services.Email;

namespace UUcars.Tests.Fakes;

// 测试用的假邮件服务，不发真实邮件
// 记录发送记录，供测试里断言"邮件是否发送、发给了谁、Token是什么"
public class FakeEmailService : IEmailService
{
    public List<(string Email, string Token)> SentVerificationEmails { get; } = [];
    public List<(string Email, string Token)> SentPasswordResetEmails { get; } = [];

    public Task SendEmailVerificationAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        SentVerificationEmails.Add((email, token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string token,
        CancellationToken cancellationToken = default)
    {
        SentPasswordResetEmails.Add((email, token));
        return Task.CompletedTask;
    }
}
```

#### 更新 FakeUserRepository

补上 `GetByEmailConfirmationTokenAsync` 方法：

```csharp
public Task<User?> GetByEmailConfirmationTokenAsync(string token,
    CancellationToken cancellationToken = default)
{
    var user = _store.Values.FirstOrDefault(
        u => u.EmailConfirmationToken == token);
    return Task.FromResult(user);
}
```

#### 更新 UserServiceTests

打开 `UUcars.Tests/Services/UserServiceTests.cs`，完整更新如下：

```csharp
...

namespace UUcars.Tests.Services;

public class UserServiceTests
{
    // 返回 (service, fakeEmailService) 元组
    // fakeEmailService 供测试里断言邮件是否发送、发给谁、Token 是什么
    private static (UserService Service, FakeEmailService EmailService)
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

        var service = new UserService(
            repo,
            new PasswordHasher<User>(),
            new JwtTokenGenerator(jwtSettings),
            fakeEmailService,
            NullLogger<UserService>.Instance
        );

        return (service, fakeEmailService);
    }

    // ===== 注册测试（更新）=====

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnUserResponseAndSendEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("User", result.Role);

        // V2 新增：验证邮件已发送
        Assert.Single(emailService.SentVerificationEmails);
        Assert.Equal("test@example.com",
            emailService.SentVerificationEmails[0].Email);
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

        await Assert.ThrowsAsync<UserAlreadyExistsException>(
            () => service.RegisterAsync(new RegisterRequest
            {
                Username = "newuser",
                Email = "existing@example.com",
                Password = "Test@123456"
            }));
    }

    [Fact]
    public async Task RegisterAsync_ShouldNotStorePasswordAsPlainText()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        var stored = await repo.GetByEmailAsync("test@example.com");
        Assert.NotNull(stored);
        Assert.NotEqual("Test@123456", stored.PasswordHash);
        Assert.True(stored.PasswordHash.Length > 20);
    }

    // ===== 登录测试（更新）=====

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ShouldThrowEmailNotConfirmedException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        // 注册但不验证邮箱
        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // 直接尝试登录，应该被拒绝（密码正确但邮箱未验证）
        await Assert.ThrowsAsync<EmailNotConfirmedException>(
            () => service.LoginAsync(new LoginRequest
            {
                Email = "test@example.com",
                Password = "Test@123456"
            }));
    }

    [Fact]
    public async Task LoginAsync_WithVerifiedEmail_ShouldReturnLoginResponse()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // 用注册时发出的 Token 完成邮箱验证
        var token = emailService.SentVerificationEmails[0].Token;
        await service.VerifyEmailAsync(token);

        // 验证后登录应该成功
        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "Test@123456"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

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
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest
            {
                Email = "notexist@example.com",
                Password = "Test@123456"
            }));
    }

    // ===== 邮箱验证测试（V2 新增）=====

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldConfirmEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        // 从 FakeEmailService 里取出注册时发出的 Token
        var token = emailService.SentVerificationEmails[0].Token;

        await service.VerifyEmailAsync(token);

        var user = await repo.GetByEmailAsync("test@example.com");
        Assert.True(user!.EmailConfirmed);
        Assert.Null(user.EmailConfirmationToken);       // Token 已清除
        Assert.Null(user.EmailConfirmationTokenExpiry);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldThrowInvalidTokenException()
    {
        var repo = new FakeUserRepository();
        var (service, _) = CreateService(repo);

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => service.VerifyEmailAsync("completely-wrong-token"));
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

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => service.VerifyEmailAsync("expired-token"));
    }

    [Fact]
    public async Task ResendVerificationAsync_WithUnverifiedEmail_ShouldSendNewEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        await service.RegisterAsync(new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123456"
        });

        Assert.Single(emailService.SentVerificationEmails);

        await service.ResendVerificationAsync("test@example.com");

        // 重发后共有两封验证邮件
        Assert.Equal(2, emailService.SentVerificationEmails.Count);
    }

    [Fact]
    public async Task ResendVerificationAsync_WithNonExistentEmail_ShouldNotThrowOrSendEmail()
    {
        var repo = new FakeUserRepository();
        var (service, emailService) = CreateService(repo);

        // 不存在的邮箱：静默返回，不发邮件，不抛异常
        await service.ResendVerificationAsync("nobody@example.com");

        Assert.Empty(emailService.SentVerificationEmails);
    }
}
```

#### 运行测试

```bash
dotnet test
```

预期：

```
Test summary: total: 27, failed: 0, succeeded: 27, skipped: 0
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

**完整注册验证流程：**

第一步，注册：

```json
POST /auth/register
{
  "username": "testuser",
  "email": "你的真实邮箱",
  "password": "Test@123456"
}
```

预期（201）：收到成功响应，同时邮箱里收到验证邮件。

第二步，未验证前尝试登录（应返回 403）：

```json
POST /auth/login
{
  "email": "你的真实邮箱",
  "password": "Test@123456"
}
```

预期（403）：

```json
{
  "success": false,
  "message": "Please verify your email address before logging in."
}
```

第三步，点击邮件里的验证链接，或手动调用验证接口：

```
GET /auth/verify-email?token=邮件链接里的token参数
```

预期（200）：验证成功。

第四步，验证后再次登录（应返回 200）：登录成功，拿到 JWT Token。

**重发验证邮件（验证安全设计）：**

用不存在的邮箱请求重发，应返回 200 且不发邮件（不透露邮箱是否注册）：

```json
POST /auth/resend-verification
{
  "email": "nobody@example.com"
}
```

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 12. 此时的目录变化

```
UUcars.API/
├── Auth/
│   └── TokenGenerator.cs                       ← 新增
├── DTOs/Requests/
│   ├── ResendVerificationRequest.cs            ← 新增
│   └── VerifyEmailRequest.cs                   ← 新增
├── Entities/
│   └── User.cs                                 ← 已更新（新增两个字段）
├── Exceptions/
│   ├── EmailAlreadyConfirmedException.cs       ← 新增
│   ├── EmailNotConfirmedException.cs           ← 新增
│   └── InvalidTokenException.cs               ← 新增
├── Migrations/
│   └── [timestamp]_AddEmailVerificationFields  ← 新增
├── Repositories/
│   ├── IUserRepository.cs                      ← 已更新
│   └── EfUserRepository.cs                     ← 已更新
└── Services/
    └── UserService.cs                          ← 已更新

UUcars.Tests/
├── Fakes/
│   ├── FakeEmailService.cs                     ← 新增
│   └── FakeUserRepository.cs                   ← 已更新
└── Services/
    └── UserServiceTests.cs                     ← 已更新
```



### 13. Git 提交

```bash
git add .
git commit -m "feat: email verification flow (register + verify + resend)"
git push origin feature/v2-backend
```



### Step 38 完成状态

```
✅ 理解注册成功 ≠ 邮箱验证成功（两个独立阶段）
✅ 理解三种 Token 的区别（验证Token / JWT Token / API Key）
✅ 理解为什么验证 Token 必须随机（防止猜测攻击）
✅ 理解登录时检查 EmailConfirmed 的原因（确认邮箱真实存在）
✅ 理解 403 vs 401 的语义区别
✅ 数据库 Migration（AddEmailVerificationFields）
✅ 三个业务异常（含注释说明设计原因）
✅ IUserRepository 新增 GetByEmailConfirmationTokenAsync
✅ UserService 更新：注册发邮件、登录检查验证状态
✅ UserService 新增：VerifyEmailAsync / ResendVerificationAsync
✅ AuthController 新增两个端点（含 GET vs POST 的解释）
✅ FakeEmailService（记录发送记录供断言）
✅ FakeUserRepository 补充新方法
✅ 27 个单元测试全部通过
✅ Scalar 端到端流程验证通过
✅ Git commit 完成
```



## Step 39 · 密码重置

### 这一步做什么

用户忘记密码时，需要一个安全的方式重新设置密码。

最直观的想法是"直接把新密码发到用户邮箱"。

但这样做有两个问题：第一，邮件是明文传输的，新密码可能被截获；第二，任何知道用户邮箱的人都可以触发这个操作，随意修改别人的密码。

标准的安全做法是**一次性 Token 机制**：

```
用户提交邮箱
        ↓
服务端生成短期有效的 Token，存入数据库，发到用户邮箱
        ↓
用户点击邮件链接 → 前端读取 Token → 让用户输入新密码
        ↓
服务端验证 Token 有效 → 用新密码覆盖旧密码 → Token 立即销毁
```

Token 只能使用一次，有效期只有 1 小时，即使邮件被截获，攻击者的操作窗口也极短。



### 1. 这一步和 Step 38 的关系

密码重置和邮箱验证用的是完全相同的安全模式，只有三点不同：

```
                    邮箱验证（Step 38）      密码重置（Step 39）
────────────────────────────────────────────────────────────
Token 有效期        24 小时                  1 小时（更敏感）
验证通过后做什么    EmailConfirmed = true    更新 PasswordHash
Token 存储字段      EmailConfirmationToken   ResetPasswordToken
```

密码重置的 Token 有效期更短，因为密码是比邮箱验证更敏感的操作——有效窗口越短，被截获后的风险越低。



### 2. 数据库 Migration

我们新增一些字段，用了保存密码重置的操作：

- ResetPasswordToken
- ResetPasswordTokenExpiry

执行数据库的迁移操作，来更新数据结构。

#### 2.1 更新 User 实体

打开 `UUcars.API/Entities/User.cs`，添加密码重置字段：

```csharp
using UUcars.API.Entities.Enums;

namespace UUcars.API.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool EmailConfirmed { get; set; } = false;

    // Step 38：邮箱验证
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpiry { get; set; }

    // Step 39：密码重置
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordTokenExpiry { get; set; }

    // 导航属性
    public ICollection<Car> Cars { get; set; } = [];
    public ICollection<Order> BuyerOrders { get; set; } = [];
    public ICollection<Order> SellerOrders { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
```

#### 2.2 更新 UserConfiguration

打开 `UUcars.API/Configurations/UserConfiguration.cs`，在 Step 38 的字段配置下方补上：

```csharp
// Step 38 已有
builder.Property(u => u.EmailConfirmationToken)
    .HasMaxLength(200);
builder.Property(u => u.EmailConfirmationTokenExpiry);

// Step 39 新增
builder.Property(u => u.ResetPasswordToken)
    .HasMaxLength(200);
builder.Property(u => u.ResetPasswordTokenExpiry);
```

#### 2.3 生成并执行 Migration

```bash
dotnet ef migrations add AddPasswordResetFields \
    --project UUcars.API/UUcars.API.csproj

dotnet ef database update \
    --project UUcars.API/UUcars.API.csproj
```



### 3. 扩展 IUserRepository

密码重置需要按 ResetPasswordToken 查找用户，补上这个方法。

打开 `UUcars.API/Repositories/IUserRepository.cs`：

```csharp
using UUcars.API.Entities;

namespace UUcars.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email,
        CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id,
        CancellationToken cancellationToken = default);
    Task<User> AddAsync(User user,
        CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user,
        CancellationToken cancellationToken = default);
    Task<User?> GetByEmailConfirmationTokenAsync(string token,
        CancellationToken cancellationToken = default);

    // Step 39 新增
    Task<User?> GetByResetPasswordTokenAsync(string token,
        CancellationToken cancellationToken = default);
}
```

打开 `UUcars.API/Repositories/EfUserRepository.cs`，补充实现：

```csharp
public async Task<User?> GetByResetPasswordTokenAsync(string token,
    CancellationToken cancellationToken = default)
{
    return await _context.Users
        .FirstOrDefaultAsync(
            u => u.ResetPasswordToken == token, cancellationToken);
}
```



### 4. 创建请求 DTO

```bash
touch UUcars.API/DTOs/Requests/ForgotPasswordRequest.cs
touch UUcars.API/DTOs/Requests/ResetPasswordRequest.cs
```

```c#
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace UUcars.API.DTOs.Requests;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain " +
                       "uppercase, lowercase, and a number.")]
    public string NewPassword { get; set; } = string.Empty;
}
```

> **为什么 `ResetPasswordRequest` 同时包含 Token 和新密码？** 密码重置分两步：用户点击邮件链接到达前端页面（URL 里有 token），然后输入新密码提交。提交时前端把 token 和 newPassword 一起放在请求体里，服务端一次调用同时完成 Token 验证和密码更新，不需要拆成两个接口。



### 5. 更新UserService 

密码重置的逻辑，不会涉及到注册和登录。

前端有单独的点击链接：

- 忘记密码：用户点击忘记密码链接（调用新的方法ForgotPasswordAsync），服务端生成token，发送重置邮件（调用 IEmailService里的SendPasswordResetAsync）
- 重置密码：用户点击重置密码邮箱链接，填写新的密码，点击重置（调用新的方法ResetPasswordAsync），服务端验证token，保存新的密码，清空token

因此UserService的更新包括：

- 新增发送重置邮件（ForgotPasswordAsync）
- 新增重置密码（ResetPasswordAsync）

打开 `UUcars.API/Services/UserService.cs`，在 `ResendVerificationAsync` 下方添加两个方法：

```csharp
// =============================================
// 忘记密码：发送重置邮件（Step 39 新增）
// =============================================
public async Task ForgotPasswordAsync(
    ForgotPasswordRequest request,
    CancellationToken cancellationToken = default)
{
    var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

    // 安全设计：用户不存在时静默返回，不透露邮箱是否注册
    // 和 ResendVerificationAsync 保持完全一致的安全原则：
    // 攻击者无法通过这个接口的响应差异，枚举系统里注册过哪些邮箱
    if (user == null)
    {
        _logger.LogWarning(
            "Password reset requested for non-existent email: {Email}", request.Email);
        return;
    }

    // 未验证邮箱的账号不允许重置密码
    // 原因：如果允许未验证账号重置密码，攻击者可以用别人的邮箱注册，
    // 然后通过密码重置拿到 Token，等于变相控制了这个邮箱对应的账号
    if (!user.EmailConfirmed)
    {
        _logger.LogWarning(
            "Password reset requested for unverified email: {Email}", request.Email);
        return;
    }

    var resetToken = TokenGenerator.Generate();
    user.ResetPasswordToken = resetToken;
    // 有效期只有 1 小时，比邮箱验证（24小时）更短
    // 密码是更敏感的操作，万一邮件被截获，留给攻击者的时间窗口应该尽可能短
    user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);
    user.UpdatedAt = DateTime.UtcNow;

    await _userRepository.UpdateAsync(user, cancellationToken);

    await _emailService.SendPasswordResetAsync(
        user.Email, resetToken, cancellationToken);

    _logger.LogInformation("Password reset email sent to: {Email}", request.Email);
}

// =============================================
// 重置密码：验证 Token + 更新密码（Step 39 新增）
// =============================================
public async Task ResetPasswordAsync(
    ResetPasswordRequest request,
    CancellationToken cancellationToken = default)
{
    var user = await _userRepository.GetByResetPasswordTokenAsync(
        request.Token, cancellationToken);

    if (user == null)
        throw new InvalidTokenException();

    if (user.ResetPasswordTokenExpiry < DateTime.UtcNow)
        throw new InvalidTokenException();

    // 用新密码覆盖旧密码
    user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);

    // Token 立即销毁，防止同一个 Token 被重复使用
    // 即使邮件被截获，Token 用完一次就彻底失效
    user.ResetPasswordToken = null;
    user.ResetPasswordTokenExpiry = null;
    user.UpdatedAt = DateTime.UtcNow;

    await _userRepository.UpdateAsync(user, cancellationToken);

    _logger.LogInformation(
        "Password reset successfully for: {Email}", user.Email);
}
```



### 6. 更新 AuthController

打开 `UUcars.API/Controllers/AuthController.cs`，添加两个新端点：

```csharp
// POST /auth/forgot-password
[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordRequest request,
    CancellationToken cancellationToken)
{
    await _userService.ForgotPasswordAsync(request, cancellationToken);

    // 无论用户是否存在、邮箱是否已验证，始终返回相同的成功响应
    // 保持和 ResendVerification 一致的安全设计
    return Ok(ApiResponse<object>.Ok(null,
        "If this email is registered, a password reset link has been sent."));
}

// POST /auth/reset-password
[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword(
    [FromBody] ResetPasswordRequest request,
    CancellationToken cancellationToken)
{
    await _userService.ResetPasswordAsync(request, cancellationToken);
    return Ok(ApiResponse<object>.Ok(null,
        "Password has been reset successfully. You can now log in with your new password."));
}
```



### 7. 更新单元测试

#### 7.1 更新 FakeUserRepository

补上 `GetByResetPasswordTokenAsync`：

打开 `UUcars.Tests/Fakes/FakeUserRepository.cs`，添加：

```csharp
public Task<User?> GetByResetPasswordTokenAsync(string token,
    CancellationToken cancellationToken = default)
{
    var user = _store.Values.FirstOrDefault(
        u => u.ResetPasswordToken == token);
    return Task.FromResult(user);
}
```

#### 7.2 在 UserServiceTests 里添加密码重置测试

打开 `UUcars.Tests/Services/UserServiceTests.cs`，在已有测试下方添加：

```csharp
// ===== 密码重置测试（Step 39 新增）=====

[Fact]
public async Task ForgotPasswordAsync_WithValidVerifiedEmail_ShouldSendResetEmail()
{
    var repo = new FakeUserRepository();
    var (service, emailService) = CreateService(repo);

    // 注册 + 完成邮箱验证
    await service.RegisterAsync(new RegisterRequest
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "Test@123456"
    });
    var verifyToken = emailService.SentVerificationEmails[0].Token;
    await service.VerifyEmailAsync(new VerifyEmailRequest
    {
        Token = verifyToken
    });

    await service.ForgotPasswordAsync(new ForgotPasswordRequest
    {
        Email = "test@example.com"
    });

    Assert.Single(emailService.SentPasswordResetEmails);
    Assert.Equal("test@example.com",
        emailService.SentPasswordResetEmails[0].Email);
}

[Fact]
public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldNotSendEmail()
{
    var repo = new FakeUserRepository();
    var (service, emailService) = CreateService(repo);

    // 不存在的邮箱：静默返回，不发邮件
    await service.ForgotPasswordAsync(new ForgotPasswordRequest
    {
        Email = "nobody@example.com"
    });

    Assert.Empty(emailService.SentPasswordResetEmails);
}

[Fact]
public async Task ForgotPasswordAsync_WithUnverifiedEmail_ShouldNotSendEmail()
{
    var repo = new FakeUserRepository();
    var (service, emailService) = CreateService(repo);

    // 注册但不验证邮箱
    await service.RegisterAsync(new RegisterRequest
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "Test@123456"
    });

    // 未验证的账号不能重置密码
    await service.ForgotPasswordAsync(new ForgotPasswordRequest
    {
        Email = "test@example.com"
    });

    Assert.Empty(emailService.SentPasswordResetEmails);
}

[Fact]
public async Task ResetPasswordAsync_WithValidToken_ShouldUpdatePasswordAndClearToken()
{
    var repo = new FakeUserRepository();
    var (service, emailService) = CreateService(repo);

    // 注册 + 验证邮箱 + 发起重置
    await service.RegisterAsync(new RegisterRequest
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "Test@123456"
    });
    var verifyToken = emailService.SentVerificationEmails[0].Token;
    await service.VerifyEmailAsync(new VerifyEmailRequest
    {
        Token = verifyToken
    });

    await service.ForgotPasswordAsync(new ForgotPasswordRequest
    {
        Email = "test@example.com"
    });
    var resetToken = emailService.SentPasswordResetEmails[0].Token;

    // 用新密码重置
    await service.ResetPasswordAsync(new ResetPasswordRequest
    {
        Token = resetToken,
        NewPassword = "NewPassword@123"
    });

    // 验证：用新密码能登录
    var loginResult = await service.LoginAsync(new LoginRequest
    {
        Email = "test@example.com",
        Password = "NewPassword@123"
    });
    Assert.NotEmpty(loginResult.Token);

    // 验证：旧密码不能再登录
    await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(new LoginRequest
    {
        Email = "test@example.com",
        Password = "Test@123456"
    }));

    // 验证：Token 已清除，不能重复使用
    var updatedUser = await repo.GetByEmailAsync("test@example.com");
    Assert.Null(updatedUser!.ResetPasswordToken);
    Assert.Null(updatedUser.ResetPasswordTokenExpiry);
}

[Fact]
public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowInvalidTokenException()
{
    var repo = new FakeUserRepository();
    var (service, _) = CreateService(repo);

    await Assert.ThrowsAsync<InvalidTokenException>(() => service.ResetPasswordAsync(new ResetPasswordRequest
    {
        Token = "invalid-token",
        NewPassword = "NewPassword@123"
    }));
}

[Fact]
public async Task ResetPasswordAsync_WithExpiredToken_ShouldThrowInvalidTokenException()
{
    var repo = new FakeUserRepository();

    // 直接注入一个已过期 Token 的用户
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

    await Assert.ThrowsAsync<InvalidTokenException>(() => service.ResetPasswordAsync(new ResetPasswordRequest
    {
        Token = "expired-reset-token",
        NewPassword = "NewPassword@123"
    }));
}
```

#### 7.3 运行测试

```bash
dotnet test
```

预期：

```
Test summary: total: 33, failed: 0, succeeded: 33, skipped: 0
```

原来的 27 个 + 新增的 6 个密码重置测试，全部通过。



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

**完整密码重置流程：**

第一步，用已验证的账号发起重置：

```json
POST /auth/forgot-password
{
  "email": "你的真实邮箱"
}
```

预期（200）：收到邮件。

第二步，用新密码重置（Token 从邮件链接里取）：

```json
POST /auth/reset-password
{
  "token": "邮件链接里的token参数",
  "newPassword": "NewPassword@123"
}
```

预期（200）：重置成功。

第三步，用新密码登录：

```json
POST /auth/login
{
  "email": "你的真实邮箱",
  "password": "NewPassword@123"
}
```

预期（200）：登录成功。

**安全边界测试：**

不存在的邮箱发起重置（应返回 200，不透露信息）：

```json
POST /auth/forgot-password
{
  "email": "nobody@example.com"
}
```

预期（200）：返回和正常流程完全相同的响应。

用同一个 Token 重置两次（应返回 400）：

第一次重置成功后再次提交同一个 Token，预期：

```json
{
  "success": false,
  "message": "The token is invalid or has expired."
}
```

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 10. 此时的目录变化

```
UUcars.API/
├── DTOs/Requests/
│   ├── ForgotPasswordRequest.cs        ← 新增
│   └── ResetPasswordRequest.cs         ← 新增
├── Entities/
│   └── User.cs                         ← 已更新（新增两个字段）
├── Migrations/
│   └── [timestamp]_AddPasswordReset    ← 新增
├── Repositories/
│   ├── IUserRepository.cs              ← 已更新
│   └── EfUserRepository.cs             ← 已更新
└── Services/
    └── UserService.cs                  ← 已更新

UUcars.Tests/
├── Fakes/
│   └── FakeUserRepository.cs           ← 已更新
└── Services/
    └── UserServiceTests.cs             ← 已更新（新增6个测试）
```



### 11. Git 提交

```bash
git add .
git commit -m "feat: password reset flow (forgot-password + reset-password)"
git push origin feature/v2-backend
```



### Step 39 完成状态

```
✅ 理解一次性 Token 机制的安全设计（为什么不直接发新密码）
✅ 理解密码重置和邮箱验证的异同（同一套模式，有效期更短）
✅ 理解未验证账号不能重置密码的原因（防止攻击者用别人邮箱注册）
✅ 数据库 Migration（AddPasswordResetFields）
✅ IUserRepository 新增 GetByResetPasswordTokenAsync
✅ ForgotPasswordRequest / ResetPasswordRequest DTO
✅ UserService 新增：ForgotPasswordAsync / ResetPasswordAsync
✅ 安全设计：不存在/未验证邮箱静默返回，Token 一次性销毁
✅ AuthController 新增两个端点
✅ FakeUserRepository 补充新方法
✅ 6 个新单元测试全部通过（共 33 个）
✅ Scalar 端到端流程验证通过（含安全边界测试）
✅ Git commit 完成
```



## Step 40 · 真实图片上传

### 这一步做什么

V1 的图片接口接收的是 URL 字符串——卖家手动填写一个图片地址。这显然不是真实产品的做法，用户不可能自己找图片托管服务再复制链接。

这一步把接口改成接收真实的图片文件，上传后存到 Cloudflare R2，返回一个可公开访问的图片 URL。



### 1. 文件上传的工作原理

普通的 JSON 请求体只能传文本数据。图片是二进制文件，不能直接放进 JSON 里。传文件用的是另一种请求格式：**multipart/form-data**。

```
普通 JSON 请求：
  Content-Type: application/json
  Body: {"title": "宝马3系", "price": 260000}

文件上传请求：
  Content-Type: multipart/form-data; boundary=----FormBoundary
  Body: 多个"part"拼在一起，每个part可以是文本字段或文件
        ----FormBoundary
        Content-Disposition: form-data; name="file"; filename="car.jpg"
        Content-Type: image/jpeg
        [图片的二进制数据]
        ----FormBoundary--
```

ASP.NET Core 用 `IFormFile` 接收上传的文件。`IFormFile` 是框架提供的接口，封装了文件名、文件类型、文件大小、文件内容流等信息，不需要手动解析 multipart 格式。



### 2. 为什么用 Cloudflare R2

图片上传后需要一个**可公开访问的 URL**，让前端能直接加载图片。本地文件系统做不到这一点——文件只在你的服务器上，换个环境就找不到了。

R2 是云对象存储，文件上传后有一个固定的公开 URL，任何人任何地方都能访问。

**R2 的免费额度：**

```
存储：每月 10GB 免费
读取操作：每月 100 万次免费
写入操作：每月 100 万次免费
出口带宽：完全免费（这是 R2 相比 AWS S3 最大的优势）
```

开发和生产都用同一个 R2 bucket，不需要切换环境，行为完全一致。

**R2 使用 S3 兼容 API：**

R2 不需要专属 SDK，直接用 AWS 的 `AWSSDK.S3` 包操作，只需要把 endpoint 指向 R2 的地址。



### 3. 安装 AWS SDK

R2 使用 S3 兼容 API，直接用 AWS 的 S3 SDK：

```bash
dotnet add UUcars.API/UUcars.API.csproj package AWSSDK.S3
```



### 4. R2 SDK 的使用方式

根据[官方文档](https://developers.cloudflare.com/r2/examples/aws/aws-sdk-net/)，R2 使用标准的 `AWSSDK.S3` 包，初始化时把 endpoint 指向 R2 的地址：

```csharp
var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
var s3Client = new AmazonS3Client(credentials, new AmazonS3Config
{
    ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com"
});
```

上传文件：

```csharp
var request = new PutObjectRequest
{
    BucketName = bucketName,
    Key = fileName,           // 文件在 bucket 里的路径/名称
    InputStream = fileStream,
    ContentType = contentType,
    // 这两个标志必须设置，R2 不支持 AWS 默认的 Streaming SigV4
    DisablePayloadSigning = true,
    DisableDefaultChecksumValidation = true
};
await s3Client.PutObjectAsync(request);
```

删除文件：

```csharp
await s3Client.DeleteObjectAsync(bucketName, fileName);
```

上传成功后，图片的公开 URL 是：

```
{PublicUrl}/{fileName}
// 比如：https://pub-xxxxxxxx.r2.dev/cars/abc123.jpg
```

由此可见，需要如下几个参数：

- accessKeyId
- secretAccessKey
- accountId - 用于拼接ServiceURL地址
- PublicUrl - 用于拼接图片的公开URL地址

这些参数需要注册Cloudflare 账号来获得。



### 5. 注册 Cloudflare 账号并创建 R2 Bucket

**第一步：注册账号**

前往 [cloudflare.com](https://cloudflare.com/) 注册免费账号。

**第二步：创建 Bucket**

登录后进入 **R2 Object Storage**，点击 **Create bucket**，填写 bucket 名称（比如 `uucars-images`），选择离你最近的地区，点击创建。

**第三步：开启公开访问**

Bucket 创建后默认是私有的，需要开启公开读取：进入 bucket 设置，找到 **Public access**，点击 **Allow Access**。开启后会得到一个公开域名，格式是：

```
https://pub-dfa53d7a7aba4cfbaba02e7282e5e007.r2.dev
```

这就是图片 URL 的前缀，上传一张图片后访问地址就是 `https://pub-xxxxxxxx.r2.dev/图片文件名`。

**第四步：生成 API Token**

在 R2 控制台找到 **Manage R2 API Tokens**，点击 **Create API Token**，权限选择 **Object Read & Write**，选择刚才创建的 bucket，创建后记录下：

```
Access Key ID
Secret Access Key
账号 ID（Account ID，在 R2 首页右侧可以看到）
```



### 6. 配置管理

#### 6.1 创建 StorageSettings 配置绑定类

对所需的参数定义配置类

```bash
touch UUcars.API/Configurations/StorageSettings.cs
```

```c#
namespace UUcars.API.Configurations;

public class StorageSettings
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;

    // R2 的 S3 兼容 endpoint，格式固定
    // 这个值由 AccountId 决定，在 StorageExtensions 里动态拼接
    // 不需要手动填写
    public string PublicUrl { get; set; } = string.Empty;
}
```

#### 6.2 更新 appsettings.json

```json
"StorageSettings": {
  "AccessKeyId": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS",
  "SecretAccessKey": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS",
  "AccountId": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS",
  "BucketName": "uucars-images",
  "PublicUrl": "PLACEHOLDER_OVERRIDE_WITH_USER_SECRETS"
}
```

#### 6.3 把敏感配置存入 User Secrets

```bash
cd UUcars.API

dotnet user-secrets set "StorageSettings:AccessKeyId" "17c962041449314ac345133944a6c849"
dotnet user-secrets set "StorageSettings:SecretAccessKey" "ba0c9bad900c7aed96e29be873ceeb2c122fe3553cf9e7768d12d43b629e1446"
dotnet user-secrets set "StorageSettings:AccountId" "a3dfee9bb1c425fda7ec5face2911597"
dotnet user-secrets set "StorageSettings:PublicUrl" "https://pub-dfa53d7a7aba4cfbaba02e7282e5e007.r2.dev"

cd ..
```

验证设置成功：

```bash
cd UUcars.API && dotnet user-secrets list && cd ..
```

应该能看到 `EmailSettings:ApiKey` 已经设置。



### 7. 定义 IStorageService 接口

和 `IEmailService` 一样，存储服务也抽成接口，业务代码不直接依赖具体的云服务商 SDK。业务功能包含：

- 上传图片： 返回图片的URL
- 删除图片

```bash
mkdir -p UUcars.API/Services/Storage
touch UUcars.API/Services/Storage/IStorageService.cs
```

```c#
namespace UUcars.API.Services.Storage;

public interface IStorageService
{
    // 上传文件，返回可公开访问的 URL
    // fileName：存储时使用的文件名（调用方负责生成唯一文件名）
    // contentType：文件 MIME 类型，比如 image/jpeg
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default);

    // 删除文件
    // fileName：存储时使用的文件名
    Task DeleteAsync(string fileName,
        CancellationToken cancellationToken = default);
}
```



### 8. 实现 R2StorageService

通过Amazon.S3 sdk提供的接口IAmazonS3：

-  封装了方法PutObjectAsync实现上传文件
- 封装了DeleteObjectAsync方法实现删除文件

```bash
touch UUcars.API/Services/Storage/R2StorageService.cs
```

```c#
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Options;
using UUcars.API.Configurations;

namespace UUcars.API.Services.Storage;

public class R2StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageSettings _storageSettings;
    private readonly ILogger<R2StorageService> _logger;

    public R2StorageService(
        IAmazonS3 s3Client,
        IOptions<StorageSettings> storageSettings,
        ILogger<R2StorageService> logger)
    {
        _s3Client = s3Client;
        _storageSettings = storageSettings.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _storageSettings.BucketName,
            Key = fileName,
            InputStream = fileStream,
            ContentType = contentType,
            // R2 必须设置这两个标志
            // 原因：R2 不支持 AWS SDK 默认的 Streaming SigV4 签名方式
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        // 拼接公开访问 URL
        var publicUrl = $"{_storageSettings.PublicUrl.TrimEnd('/')}/{fileName}";

        _logger.LogInformation("File uploaded to R2: {Url}", publicUrl);

        return publicUrl;
    }

    public async Task DeleteAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await _s3Client.DeleteObjectAsync(
            _storageSettings.BucketName, fileName, cancellationToken);

        _logger.LogInformation("File deleted from R2: {FileName}", fileName);
    }
}
```



### 9. 创建 StorageExtensions 扩展方法

对s3Client对象进行配置

```bash
touch UUcars.API/Extensions/StorageExtensions.cs
```

```c#
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
using UUcars.API.Configurations;
using UUcars.API.Services.Storage;

namespace UUcars.API.Extensions;

public static class StorageExtensions
{
    public static IServiceCollection AddStorageService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StorageSettings>(
            configuration.GetSection("StorageSettings"));

        // 读取配置，启动时就验证是否存在
        var settings = configuration.GetSection("StorageSettings").Get<StorageSettings>()
                       ?? throw new InvalidOperationException(
                           "StorageSettings is not configured.");

        // 初始化 S3 客户端，指向 R2 的 endpoint
        var credentials = new BasicAWSCredentials(
            settings.AccessKeyId,
            settings.SecretAccessKey);

        var s3Client = new AmazonS3Client(credentials, new AmazonS3Config
        {
            // R2 的 S3 兼容 endpoint 格式
            ServiceURL = $"https://{settings.AccountId}.r2.cloudflarestorage.com"
        });

        // 注册 IAmazonS3（Singleton：S3 客户端是线程安全的，可以全局共享）
        services.AddSingleton<IAmazonS3>(s3Client);

        return services;
    }
}
```

打开 `Program.cs`，在 `AddEmailService` 下方添加：

```csharp
// 存储服务
builder.Services.AddStorageService(builder.Configuration);

// 注册我们自己的 IStorageService 实现
builder.Services.AddScoped<IStorageService, R2StorageService>();
```



### 10. 更新图片上传接口

V1 的 `POST /cars/{id}/images` 接收的是 URL 字符串，现在改成接收文件。

#### 10.1 更新请求 DTO

打开 `UUcars.API/DTOs/Requests/CarImageAddRequest.cs`，替换原来的内容：

```csharp
namespace UUcars.API.DTOs.Requests;

// V2 更新：从接收 URL 改为接收文件
// 不用 [FromBody]，文件上传用 [FromForm]
// IFormFile 是 ASP.NET Core 提供的接口，封装了上传文件的所有信息
public class CarImageAddRequest
{
    public IFormFile File { get; set; } = null!;
    public int SortOrder { get; set; } = 0;
}
```

#### 10.2 创建文件验证工具类

文件类型验证和大小限制是独立的逻辑，抽成工具类：

```baah
touch UUcars.API/Services/Storage/FileValidator.cs
```



```c#
namespace UUcars.API.Services.Storage;

public static class FileValidator
{
    // 允许的图片类型
    private static readonly HashSet<string> AllowedContentTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    };

    // 最大文件大小：5MB
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static (bool IsValid, string? Error) Validate(IFormFile file)
    {
        if (file.Length == 0)
            return (false, "File is empty.");

        if (file.Length > MaxFileSizeBytes)
            return (false, "File size must not exceed 5MB.");

        if (!AllowedContentTypes.Contains(file.ContentType))
            return (false, "Only JPEG, PNG, and WebP images are allowed.");

        return (true, null);
    }

    // 根据原始文件名生成唯一的存储文件名
    // 格式：cars/{guid}.{扩展名}
    // 用 GUID 避免文件名冲突，用 cars/ 前缀在 bucket 里组织文件
    public static string GenerateFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        return $"cars/{Guid.NewGuid()}{extension}";
    }
}
```

> **为什么用 GUID 做文件名而不是保留原始文件名？** 用户上传的原始文件名可能重复（两个用户都上传了 `car.jpg`），也可能包含特殊字符导致 URL 出问题。GUID 保证每个文件名全局唯一，`cars/` 前缀让所有车辆图片在 bucket 里有统一的组织结构。

#### 10.3 更新 CarService

打开 `UUcars.API/Services/CarService.cs`，构造函数新增 `IStorageService` 依赖，更新 `AddImageAsync` 方法：

```csharp
// 构造函数新增 IStorageService
private readonly IStorageService _storageService;

public CarService(
    ICarRepository carRepository,
    ICarImageRepository carImageRepository,
    IStorageService storageService,
    ILogger<CarService> logger)
{
    _carRepository = carRepository;
    _carImageRepository = carImageRepository;
    _storageService = storageService;
    _logger = logger;
}
```

更新 `AddImageAsync` 方法：

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

    if (car.Status != CarStatus.Draft)
        throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

    // 验证文件
    var (isValid, error) = FileValidator.Validate(request.File);
    if (!isValid)
        throw new AppException(StatusCodes.Status400BadRequest, error!);

    // 生成唯一文件名，上传到 R2
    var fileName = FileValidator.GenerateFileName(request.File.FileName);

    string imageUrl;
    await using (var stream = request.File.OpenReadStream())
    {
        imageUrl = await _storageService.UploadAsync(
            stream, fileName, request.File.ContentType, cancellationToken);
    }

    var image = new CarImage
    {
        CarId = carId,
        ImageUrl = imageUrl,
        SortOrder = request.SortOrder
    };

    var created = await _carImageRepository.AddAsync(image, cancellationToken);

    _logger.LogInformation(
        "Image uploaded for car {CarId}: {Url}", carId, imageUrl);

    return MapToImageResponse(created);
}
```

同时更新 `DeleteImageAsync`，删除图片时同步删除 R2 里的文件：

```csharp
public async Task DeleteImageAsync(
    int carId,
    int imageId,
    int currentUserId,
    CancellationToken cancellationToken = default)
{
    var car = await _carRepository.GetByIdAsync(carId, cancellationToken);
    if (car == null)
        throw new CarNotFoundException(carId);

    if (car.SellerId != currentUserId)
        throw new ForbiddenException();

    if (car.Status != CarStatus.Draft)
        throw new CarStatusException(car.Id, car.Status, CarStatus.Draft);

    var image = await _carImageRepository.GetByIdAsync(imageId, cancellationToken);
    if (image == null || image.CarId != carId)
        throw new CarImageNotFoundException(imageId);

    // 从 URL 里提取文件名（格式：{PublicUrl}/cars/{guid}.jpg）
    // 只需要 "cars/{guid}.jpg" 这部分来删除 R2 里的文件
    var uri = new Uri(image.ImageUrl);
    var fileName = uri.AbsolutePath.TrimStart('/');

    await _storageService.DeleteAsync(fileName, cancellationToken);

    await _carImageRepository.DeleteAsync(image, cancellationToken);

    _logger.LogInformation(
        "Image {ImageId} deleted from car {CarId}", imageId, carId);
}
```

#### 10.4 更新 CarsController

打开 `UUcars.API/Controllers/CarsController.cs`，更新 `AddImage` Action：

```csharp
// POST /cars/{id}/images
// [FromForm] 而不是 [FromBody]：文件上传必须用 multipart/form-data 格式
// [FromBody] 只能接收 JSON，无法接收文件
[HttpPost("{id:int}/images")]
[Authorize]
public async Task<IActionResult> AddImage(
    int id,
    [FromForm] CarImageAddRequest request,
    CancellationToken cancellationToken)
{
 	...
}
```



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

**上传图片测试：**

在 Scalar 里，`POST /cars/{id}/images` 接口现在接收 `multipart/form-data` 格式，需要在请求里选择文件而不是填写 JSON。

先登录拿到 Token，创建一辆草稿车辆，然后上传图片：

```
POST /cars/1/images
Authorization: Bearer ...
Content-Type: multipart/form-data

file: [选择一张本地图片]
sortOrder: 0
```

预期（201）：

```json
{
  "success": true,
  "data": {
    "id": 1,
    "imageUrl": "https://pub-xxxxxxxx.r2.dev/cars/abc123-def456.jpg",
    "sortOrder": 0,
    "carId": 1
  },
  "message": "Image uploaded successfully."
}
```

复制返回的 `imageUrl`，在浏览器里直接访问，应该能看到上传的图片。

**文件验证测试：**

上传一个超过 5MB 的文件，应返回 400：

```json
{
  "success": false,
  "message": "File size must not exceed 5MB."
}
```

上传一个非图片文件（比如 `.txt`），应返回 400：

```json
{
  "success": false,
  "message": "Only JPEG, PNG, and WebP images are allowed."
}
```

`Ctrl+C` 停止，回到根目录：

```bash
cd ..
```



### 13. 此时的目录变化

```
UUcars.API/
├── Configurations/
│   └── StorageSettings.cs              ← 新增
├── DTOs/Requests/
│   └── CarImageAddRequest.cs           ← 已更新（IFormFile替换URL）
├── Extensions/
│   └── StorageExtensions.cs            ← 新增
└── Services/
    ├── CarService.cs                   ← 已更新（上传/删除文件）
    └── Storage/                        ← 新增目录
        ├── IStorageService.cs          ← 新增
        ├── R2StorageService.cs         ← 新增
        └── FileValidator.cs            ← 新增
```



### 14. Git 提交

```bash
git add .
git commit -m "feat: real image upload with Cloudflare R2"
git push origin feature/v2-backend
```



### Step 40 完成状态

```
✅ 理解 multipart/form-data 和 IFormFile 的工作原理
✅ 理解为什么直接用 R2（开发生产一套，无需切换）
✅ 注册 Cloudflare 账号，创建 R2 Bucket，开启公开访问
✅ 安装 AWSSDK.S3，敏感配置存入 User Secrets
✅ StorageSettings 配置绑定类
✅ IStorageService 接口（Upload / Delete）
✅ R2StorageService 实现（含必须的 DisablePayloadSigning 标志）
✅ FileValidator 工具类（类型验证、大小限制、唯一文件名生成）
✅ StorageExtensions 扩展方法
✅ CarService 更新：上传文件到 R2，删除时同步删除 R2 文件
✅ CarsController 更新：[FromForm] 替换 [FromBody]
✅ dotnet build 通过
✅ Scalar 测试通过（上传成功、URL可访问、验证错误返回400）
✅ Git commit 完成
```
