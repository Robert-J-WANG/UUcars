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



### 6. 实现 ResendEmailService

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
        // Uri.EscapeDataString：Token 是 Base64 编码，可能含有 +、/、= 等
        // 在 URL 里有特殊含义的字符，必须转义，否则前端收到的 token 会损坏
        var verificationLink =
            $"{_emailSettings.BaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";

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
            $"{_emailSettings.BaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";

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



### 7. 注册到 DI 容器

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



### 9. 手动验证邮件能发出去

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



### 10. 此时的目录变化

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



### 11. Git 提交

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
✅ 理解为什么需要接口（可测试、可替换）
✅ 理解 Resend SDK 官方注册方式（四行，来自官方文档）
✅ ResendEmailService 实现（含 HTML 邮件模板、Token URL 转义）
✅ EmailExtensions 扩展方法（和 AuthExtensions/CorsExtensions 风格一致）
✅ 注册进 Program.cs
✅ 临时测试端点验证邮件发送成功后删除
✅ dotnet build 通过
✅ Git commit 完成
```
