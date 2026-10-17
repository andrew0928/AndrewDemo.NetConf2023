namespace AndrewDemo.NetConf2023.Abstract.Contexts
{
    public enum IdentityKind
    {
        Anonymous = 0,
        Member = 1,
        System = 2
    }

    /// <summary>
    /// 執行身分的不可變快照；只有會員身分具有 MemberId。
    /// </summary>
    public sealed class IdentitySnapshot
    {
        public IdentityKind Kind { get; }
        public int? MemberId { get; }
        public bool IsTest { get; }

        private IdentitySnapshot(IdentityKind kind, int? memberId, bool isTest)
        {
            Kind = kind;
            MemberId = memberId;
            IsTest = isTest;
        }

        public static IdentitySnapshot Anonymous { get; } = new(IdentityKind.Anonymous, null, false);
        public static IdentitySnapshot System { get; } = new(IdentityKind.System, null, false);

        public static IdentitySnapshot ForMember(int memberId, bool isTest) => new(IdentityKind.Member, memberId, isTest);
    }

    /// <summary>
    /// 目前執行身分；生命週期與交易 context 獨立。
    /// </summary>
    public interface IIdentityContext
    {
        IdentitySnapshot Current { get; }
    }
}
