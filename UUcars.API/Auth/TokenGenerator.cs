using System.Security.Cryptography;

namespace UUcars.API.Auth;

public class TokenGenerator
{
    // 生成加密安全的随机 Token
    // RandomNumberGenerator 是 .NET 内置的加密安全随机数生成器
    // 32 字节 = 256 位随机性，碰撞概率可以忽略不计
    // ToBase64String 把字节数组转成可打印字符串，适合放进 URL
    // 注意：Base64 可能含有 +、/、= 字符，放进 URL 时需要 Uri.EscapeDataString 转义
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}