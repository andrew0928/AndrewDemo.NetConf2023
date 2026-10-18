using AndrewDemo.NetConf2023.Core.Testing;
using AndrewDemo.NetConf2023.Core.Time;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndrewDemo.NetConf2023.Core;

/// <summary>會員與 Token 的應用寫入入口；用途只在首次保存時決定。</summary>
public sealed class MemberService(
    IShopDatabaseContext database,
    TestAllowlist allowlist,
    TimeProvider timeProvider,
    ILogger<MemberService>? logger = null)
{
    private readonly ILogger<MemberService> _logger = logger ?? NullLogger<MemberService>.Instance;

    public Member GetOrCreate(string name) => Create(name, null);

    /// <summary>僅供受控資料初始化使用，HTTP request 不暴露指定 ID 的能力。</summary>
    public Member InitializeMember(int memberId, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(memberId);
        return Create(name, memberId);
    }

    private Member Create(string name, int? explicitId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Write(() =>
        {
            var existing = database.Members.FindOne(m => m.Name == name);
            if (existing != null)
            {
                if (explicitId.HasValue && existing.Id != explicitId.Value)
                    throw new InvalidOperationException("會員名稱已對應其他 ID。");
                return existing;
            }

            // 取得資料庫寫入鎖後才讀取最大值，並在同一交易完成首次保存。
            var max = database.Members.Query().OrderByDescending(m => m.Id).FirstOrDefault()?.Id ?? 0;
            var id = explicitId ?? checked(max + 1);
            var member = new Member { Id = id, Name = name, IsTest = allowlist.IsTestMember(name, database.Database.Collation) };
            database.Members.Insert(member);
            return member;
        });
    }

    public MemberAccessTokenRecord IssueToken(int memberId, DateTime expiresAt) => Write(() =>
    {
        var member = database.Members.FindById(memberId)
            ?? throw new InvalidOperationException("無法為不存在的會員簽發 Token。");
        var token = new MemberAccessTokenRecord
        {
            Token = Guid.NewGuid().ToString("N"), MemberId = member.Id,
            IsTest = member.IsTest, Expire = expiresAt
        };
        database.MemberTokens.Insert(token);
        return token;
    });

    public Member? ValidateToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return null;
        var token = database.MemberTokens.FindById(accessToken);
        if (token == null || token.Expire.ToUniversalTime() <= timeProvider.GetUtcDateTime()) return null;
        var member = database.Members.FindById(token.MemberId);
        if (member == null) return null;
        if (token.IsTest != member.IsTest)
        {
            _logger.LogError("execution_context_invalid: Token 與 Member 用途矛盾，MemberId={MemberId}", member.Id);
            throw new ExecutionContextInvalidException("Token 與 Member 保存的用途不一致。");
        }
        return member;
    }

    public void UpdateMember(Member member) => Write(() =>
    {
        var saved = database.Members.FindById(member.Id)
            ?? throw new InvalidOperationException("會員不存在。");
        EnsurePurpose(saved.IsTest, member.IsTest, "Member", member.Id);
        database.Members.Update(member);
        return true;
    });

    public void UpdateToken(MemberAccessTokenRecord token) => Write(() =>
    {
        var saved = database.MemberTokens.FindById(token.Token)
            ?? throw new InvalidOperationException("Token 不存在。");
        EnsurePurpose(saved.IsTest, token.IsTest, "Token", saved.MemberId);
        if (saved.MemberId != token.MemberId)
        {
            _logger.LogWarning("拒絕改寫 Token 所屬會員，MemberId={MemberId}", saved.MemberId);
            throw new InvalidOperationException("Token 所屬會員不可變更。");
        }
        var member = database.Members.FindById(saved.MemberId);
        if (member == null || member.IsTest != saved.IsTest)
        {
            _logger.LogError("execution_context_invalid: Token 的會員不存在或用途矛盾，MemberId={MemberId}", saved.MemberId);
            throw new ExecutionContextInvalidException("Token 的會員不存在或用途矛盾。");
        }
        database.MemberTokens.Update(token);
        return true;
    });

    private void EnsurePurpose(bool saved, bool requested, string type, int memberId)
    {
        if (saved == requested) return;
        _logger.LogWarning("拒絕改寫用途，TargetType={TargetType}, MemberId={MemberId}", type, memberId);
        throw new InvalidOperationException("已保存物件的用途不可變更。");
    }

    private T Write<T>(Func<T> action)
    {
        if (!database.Database.BeginTrans())
            throw new InvalidOperationException("會員服務必須擁有獨立的資料庫交易邊界。");
        try
        {
            // 固定文件的寫入鎖涵蓋不同服務實例與 Shared 連線，不只依賴 process lock。
            database.Database.GetCollection("member_write_lock").Upsert(new BsonDocument { ["_id"] = 1 });
            var result = action();
            database.Database.Commit();
            return result;
        }
        catch
        {
            database.Database.Rollback();
            throw;
        }
    }
}

/// <summary>用途資料矛盾；host 應映射為 execution_context_invalid，不能降級為匿名。</summary>
public sealed class ExecutionContextInvalidException(string message) : InvalidOperationException(message);
