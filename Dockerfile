# =============================================
# 第一阶段：构建
# 使用包含 .NET SDK 的镜像来编译项目
# AS build：给这个阶段命名为 "build"，后面可以引用
# =============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 先只复制 .csproj 文件，利用 Docker 的层缓存机制
# 原理：如果 .csproj 没变，这一层会从缓存读取，不重新执行 restore
# 好处：依赖没变时，构建速度快很多
COPY UUcars.API/UUcars.API.csproj UUcars.API/

# 恢复 NuGet 依赖
# restore 只针对 API 项目
RUN dotnet restore UUcars.API/UUcars.API.csproj

# 复制所有源代码（在 restore 之后，避免源码变化使 restore 缓存失效）
COPY . .

# 发布 API 项目
# -c Release：使用 Release 配置（优化过的生产版本）
# -o /app/publish：发布产物输出到 /app/publish 目录
# --no-restore：跳过重复的 restore（前面已经做过了）
RUN dotnet publish UUcars.API/UUcars.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# =============================================
# 第二阶段：运行
# 使用更小的 runtime 镜像，只包含运行所需的东西
# =============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# 创建日志目录（Serilog 写文件时需要）
RUN mkdir -p /app/logs

# 只从第一阶段复制发布产物，不包含源代码和 SDK
COPY --from=build /app/publish .

# 声明应用监听 8080 端口（容器内部端口）
EXPOSE 8080

# 设置环境变量，告诉 ASP.NET Core 监听 8080 而不是默认的 5000/5001
ENV ASPNETCORE_URLS=http://+:8080

# 容器启动时执行的命令
ENTRYPOINT ["dotnet", "UUcars.API.dll"]