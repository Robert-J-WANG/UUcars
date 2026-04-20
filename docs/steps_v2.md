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
