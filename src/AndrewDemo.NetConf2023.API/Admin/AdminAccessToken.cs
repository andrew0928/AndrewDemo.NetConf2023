using System.Security.Cryptography;
using System.Text;

namespace AndrewDemo.NetConf2023.API.Admin;

/// <summary>啟動時固定的獨立管理憑證；不保存會員 token，也不公開秘密。</summary>
public sealed class AdminAccessToken
{
    private readonly byte[]? _digest;

    /// <summary>空設定停用；非法設定立即拒絕啟動且不回顯設定值。</summary>
    public AdminAccessToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return;
        if (!ValidFormat(token)) throw new InvalidOperationException("SHOP_ADMIN_ACCESS_TOKEN 格式不合法。");
        _digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    /// <summary>是否啟用管理查核入口。</summary>
    public bool Enabled => _digest != null;

    /// <summary>以固定長度摘要進行固定時間比對。</summary>
    public bool Matches(string token) => _digest != null && ValidFormat(token)
        && CryptographicOperations.FixedTimeEquals(_digest, SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool ValidFormat(string value) => value.StartsWith("admin_", StringComparison.Ordinal)
        && value.Length > 6
        && value[6..].All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9');
}
