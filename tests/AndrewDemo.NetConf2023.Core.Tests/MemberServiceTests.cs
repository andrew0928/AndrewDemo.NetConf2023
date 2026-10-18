using AndrewDemo.NetConf2023.Core.Testing;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class MemberServiceTests : ShopDatabaseTestBase
{
    private MemberService Service(params string[] names) => new(Context, new TestAllowlist(names, []), FixedTimeProvider);

    [Fact]
    public void Registration_ClassifiesNameAndTokenInheritsPersistedPurpose()
    {
        var service = Service("test");
        var normal = service.GetOrCreate("normal");
        var test = service.GetOrCreate("test");
        Assert.Equal(1, normal.Id);
        Assert.Equal(2, test.Id);
        Assert.False(Context.Members.FindById(1).IsTest);
        Assert.True(Context.Members.FindById(2).IsTest);
        foreach (var member in new[] { normal, test })
        {
            var token = Service().IssueToken(member.Id, FixedUtcNow.AddHours(1));
            Assert.Equal(member.IsTest, Context.MemberTokens.FindById(token.Token).IsTest);
            Assert.Equal(member.IsTest, Service().ValidateToken(token.Token)!.IsTest);
        }
    }

    [Fact]
    public void ControlledInitialization_UsesDefaultAllowlistAndNeverReclassifiesExistingMember()
    {
        var service = new MemberService(Context, new TestAllowlist(), FixedTimeProvider);
        var member = service.InitializeMember(900001, "test");
        Assert.True(member.IsTest);
        Assert.True(Service().InitializeMember(900001, "test").IsTest);
        Assert.True(Service().GetOrCreate("test").IsTest);
        Assert.Throws<InvalidOperationException>(() => service.InitializeMember(42, "test"));
        Assert.Single(Context.Members.FindAll());
    }

    [Fact]
    public void LegacyBson_RemainsNormalAfterAllowlistChangesAndLogin()
    {
        Context.Database.GetCollection("members").Insert(new BsonDocument { ["_id"] = 900001, ["Name"] = "legacy" });
        Context.Database.GetCollection("member_tokens").Insert(new BsonDocument
        {
            ["_id"] = "legacy-token", ["MemberId"] = 900001, ["Expire"] = FixedUtcNow.AddHours(1)
        });
        var service = Service("legacy");
        Assert.False(service.GetOrCreate("legacy").IsTest);
        Assert.False(service.ValidateToken("legacy-token")!.IsTest);
        Assert.False(service.IssueToken(900001, FixedUtcNow.AddHours(1)).IsTest);
        Assert.False(Context.Database.GetCollection("members").FindById(900001).ContainsKey("IsTest"));
        Assert.False(Context.Database.GetCollection("member_tokens").FindById("legacy-token").ContainsKey("IsTest"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TokenPurposeContradiction_StopsWithErrorAndDoesNotRepair(bool memberIsTest)
    {
        var service = Service(memberIsTest ? "member" : "other");
        var member = service.GetOrCreate("member");
        Context.MemberTokens.Insert(new MemberAccessTokenRecord
        {
            Token = "secret", MemberId = member.Id, IsTest = !member.IsTest, Expire = FixedUtcNow.AddHours(1)
        });
        var logger = new RecordingLogger();
        service = new MemberService(Context, new TestAllowlist(), FixedTimeProvider, logger);
        Assert.Throws<ExecutionContextInvalidException>(() => service.ValidateToken("secret"));
        Assert.Equal(!member.IsTest, Context.MemberTokens.FindById("secret").IsTest);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("execution_context_invalid"));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("secret"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Updates_RejectPurposeChangesAndPreservePurposeOnLegalUpdates(bool isTest)
    {
        var logger = new RecordingLogger();
        var service = new MemberService(Context, new TestAllowlist(isTest ? ["member"] : [], []), FixedTimeProvider, logger);
        var member = service.GetOrCreate("member");
        var token = service.IssueToken(member.Id, FixedUtcNow.AddHours(1));
        Assert.Throws<InvalidOperationException>(() => service.UpdateMember(new Member
        {
            Id = member.Id, Name = member.Name, IsTest = !isTest, ShopNotes = "rejected"
        }));
        Assert.Throws<InvalidOperationException>(() => service.UpdateToken(new MemberAccessTokenRecord
        {
            Token = token.Token, MemberId = member.Id, IsTest = !isTest, Expire = FixedUtcNow.AddHours(2)
        }));
        Assert.Null(Context.Members.FindById(member.Id).ShopNotes);
        Assert.Equal(token.Expire, Context.MemberTokens.FindById(token.Token).Expire.ToUniversalTime());
        member.ShopNotes = "saved";
        service.UpdateMember(member);
        token.Expire = FixedUtcNow.AddHours(2);
        service.UpdateToken(token);
        Assert.Equal("saved", Context.Members.FindById(member.Id).ShopNotes);
        Assert.Equal(isTest, Context.Members.FindById(member.Id).IsTest);
        Assert.Equal(isTest, Context.MemberTokens.FindById(token.Token).IsTest);
        Assert.Equal(token.Expire, Context.MemberTokens.FindById(token.Token).Expire.ToUniversalTime());
        Assert.Equal(2, logger.Entries.Count(e => e.Level == LogLevel.Warning));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains(token.Token));
    }

    [Fact]
    public void Validation_RejectsMissingExpiredAndOrphanTokens()
    {
        var service = Service();
        Assert.Null(service.ValidateToken(null));
        Assert.Null(service.ValidateToken("missing"));
        var member = service.GetOrCreate("member");
        var expired = service.IssueToken(member.Id, FixedUtcNow);
        Assert.Null(service.ValidateToken(expired.Token));
        var valid = service.IssueToken(member.Id, FixedUtcNow.AddHours(1));
        Context.Members.Delete(member.Id);
        Assert.Null(service.ValidateToken(valid.Token));
        Assert.Throws<InvalidOperationException>(() => service.IssueToken(member.Id, FixedUtcNow.AddHours(1)));
        Assert.Equal(2, Context.MemberTokens.Count());
    }

    [Fact]
    public void FailedRegistration_RollsBackAndRetryUsesFinalIdWithoutChangingExistingPurpose()
    {
        var service = Service("original");
        var original = service.InitializeMember(1, "original");
        Assert.Throws<LiteException>(() => service.InitializeMember(1, "retry"));
        Assert.Null(Context.Members.FindOne(m => m.Name == "retry"));
        var retried = service.GetOrCreate("retry");
        Assert.Equal(2, retried.Id);
        Assert.False(retried.IsTest);
        Assert.True(Context.Members.FindById(original.Id).IsTest);
        Assert.Equal(retried.Id, Service("retry").GetOrCreate("retry").Id);
        Assert.False(Service("retry").GetOrCreate("retry").IsTest);
    }

    [Fact]
    public async Task ConcurrentRegistration_UsesUniqueIdsAcrossServiceInstancesAndDeduplicatesName()
    {
        var members = await Task.WhenAll(Enumerable.Range(0, 32).Select(i => Task.Run(() => Service("member-2", "member-4", "member-6").GetOrCreate($"member-{i}"))));
        Assert.Equal(32, members.Select(m => m.Id).Distinct().Count());
        Assert.All(members, m => Assert.Equal(new[] { "member-2", "member-4", "member-6" }.Contains(m.Name), Context.Members.FindById(m.Id).IsTest));
        var same = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => Task.Run(() => Service().GetOrCreate("same"))));
        Assert.Single(same.Select(m => m.Id).Distinct());
        Assert.Equal(33, Context.Members.Count());
    }

    [Fact]
    public async Task ConcurrentSharedConnections_SerializeIdAllocation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tip-shared-{Guid.NewGuid():N}.db");
        try
        {
            using var first = new ShopDatabaseContext(new ShopDatabaseOptions { ConnectionString = $"Filename={path};Connection=Shared" });
            using var second = new ShopDatabaseContext(new ShopDatabaseOptions { ConnectionString = $"Filename={path};Connection=Shared" });
            var members = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => Task.Run(() =>
                new MemberService(i % 2 == 0 ? first : second, new TestAllowlist(["shared-2", "shared-4"], []), FixedTimeProvider).GetOrCreate($"shared-{i}"))));
            Assert.Equal(20, members.Select(m => m.Id).Distinct().Count());
            Assert.Equal(20, first.Members.Count());
            Assert.All(members, m => Assert.Equal(m.Name is "shared-2" or "shared-4", first.Members.FindById(m.Id).IsTest));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FailureAfterInsert_RollsBackAndRetryClassifiesBeforeCommit()
    {
        var failCommit = true;
        var interceptedDatabase = Intercept<ILiteDatabase>(Context.Database, (method, args) =>
        {
            if (method.Name != nameof(ILiteDatabase.Commit)) return (false, null);
            // INSERT 已執行但尚未 COMMIT；第一筆寫入就必須帶有最終用途。
            Assert.True(Context.Members.FindOne(m => m.Name == "atomic").IsTest);
            if (failCommit) throw new IOException("測試注入：提交前故障");
            return (false, null);
        });
        var interceptedContext = Intercept<IShopDatabaseContext>(Context, (method, args) =>
            method.Name == "get_Database" ? (true, interceptedDatabase) : (false, null));
        var service = new MemberService(interceptedContext, new TestAllowlist(["atomic"], []), FixedTimeProvider);
        Assert.Throws<IOException>(() => service.GetOrCreate("atomic"));
        Assert.Empty(Context.Members.FindAll());
        Assert.Empty(Context.MemberTokens.FindAll());
        failCommit = false;
        var member = service.GetOrCreate("atomic");
        Assert.Equal(1, member.Id);
        Assert.True(Context.Members.FindById(1).IsTest);
    }

    [Fact]
    public void DefaultNames_DoNotInterpretNumericNameOrIdAsTest()
    {
        var service = new MemberService(Context, new TestAllowlist(), FixedTimeProvider);
        Assert.False(service.InitializeMember(900001, "900001").IsTest);
        Assert.True(service.GetOrCreate("test").IsTest);
    }

    [Fact]
    public void ExistingNormalTestName_RemainsNormal()
    {
        var original = Service().GetOrCreate("test");
        var service = new MemberService(Context, new TestAllowlist(), FixedTimeProvider);
        Assert.False(service.GetOrCreate("test").IsTest);
        Assert.False(service.IssueToken(original.Id, FixedUtcNow.AddHours(1)).IsTest);
    }

    [Theory]
    [InlineData("en-US/IgnoreCase", "TEST", true)]
    [InlineData("en-US/None", "TEST", false)]
    [InlineData("tr-TR/IgnoreCase", "İ", true)]
    public void Registration_UsesDatabaseNameCollation(string collation, string name, bool expected)
    {
        using var db = new ShopDatabaseContext(new ShopDatabaseOptions
        {
            ConnectionString = $"Filename=:memory:;Collation={collation}"
        });
        var listedName = name == "İ" ? "i" : "test";
        var service = new MemberService(db, new TestAllowlist([listedName], []), FixedTimeProvider);
        var created = service.GetOrCreate(name);
        Assert.Equal(expected, created.IsTest);
        var found = db.Members.FindOne(m => m.Name == listedName);
        Assert.Equal(expected, found != null);
        Assert.Equal(expected, service.GetOrCreate(listedName).Id == created.Id);
    }

    [Fact]
    public void TokenUpdate_CannotReassignMember()
    {
        var service = Service();
        var first = service.GetOrCreate("first");
        var second = service.GetOrCreate("second");
        var token = service.IssueToken(first.Id, FixedUtcNow.AddHours(1));
        token.MemberId = second.Id;
        Assert.Throws<InvalidOperationException>(() => service.UpdateToken(token));
        Assert.Equal(first.Id, Context.MemberTokens.FindById(token.Token).MemberId);
    }

    private static T Intercept<T>(T target, Func<System.Reflection.MethodInfo, object?[]?, (bool Handled, object? Result)> before) where T : class
    {
        var proxy = System.Reflection.DispatchProxy.Create<T, CallInterceptor>();
        var interceptor = (CallInterceptor)(object)proxy;
        interceptor.Target = target;
        interceptor.Before = before;
        return proxy;
    }

    public class CallInterceptor : System.Reflection.DispatchProxy
    {
        public object Target { get; set; } = null!;
        public Func<System.Reflection.MethodInfo, object?[]?, (bool Handled, object? Result)> Before { get; set; } = null!;
        protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args)
        {
            var intercepted = Before(targetMethod!, args);
            if (intercepted.Handled) return intercepted.Result;
            try { return targetMethod!.Invoke(Target, args); }
            catch (System.Reflection.TargetInvocationException error) when (error.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }

    private sealed class RecordingLogger : ILogger<MemberService>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
