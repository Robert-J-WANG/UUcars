using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace UUcars.API.Auth;

public class TokenGenerator
{
    // 生成加密安全的随机 Token
    // RandomNumberGenerator 是 .NET 内置的加密安全随机数生成器
    // 32 字节 = 256 位随机性，碰撞概率可以忽略不计

    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        // ToBase64String 把字节数组转成可打印字符串，适合放进 URL
        // 注意：Base64 可能含有 +、/、= 字符，放进 URL 时需要 Uri.EscapeDataString 转义
        // return Convert.ToBase64String(bytes);

        // 因此我们使用 WebEncoders
        // 它将字节直接转为 URL 安全的字符串
        // 它会自动把 + 换成 -，把 / 换成 _，并去掉末尾的 =
        return WebEncoders.Base64UrlEncode(bytes);
    }
}