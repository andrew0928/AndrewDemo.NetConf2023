using AndrewDemo.NetConf2023.Abstract.Contexts;

namespace AndrewDemo.NetConf2023.Core.Contexts;

/// <summary>
/// 由執行邊界初始化的身分 context；預設為匿名，供 scoped injection 使用。
/// </summary>
public sealed class IdentityContext : IIdentityContext
{
    public IdentitySnapshot Current { get; private set; } = IdentitySnapshot.Anonymous;

    public void Initialize(IdentitySnapshot identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Current = identity;
    }
}
