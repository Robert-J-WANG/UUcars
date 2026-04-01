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