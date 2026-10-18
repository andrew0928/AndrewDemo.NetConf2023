using AndrewDemo.NetConf2023.Core.Products;
using Microsoft.Extensions.Logging;
using AndrewDemo.NetConf2023.Core.Effects;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Core.Checkouts;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewDemo.NetConf2023.Core.Contexts;

public static class ExecutionServices
{
    public static IServiceCollection AddShopExecutionContexts(this IServiceCollection services)
    {
        services.AddScoped<IdentityContext>();
        services.AddScoped<IIdentityContext>(sp => sp.GetRequiredService<IdentityContext>());
        services.AddScoped<TransactionContext>();
        services.AddScoped<ITransactionContext>(sp => sp.GetRequiredService<TransactionContext>());
        services.AddScoped<CheckoutContextLoader>();
        services.AddScoped<OperationEffects>();
        services.AddScoped(sp => new ProtectedWriteExecutor(sp.GetRequiredService<IIdentityContext>(),
            sp.GetRequiredService<ITransactionContext>(), sp.GetRequiredService<OperationEffects>(),
            sp.GetService<ILogger<ProtectedWriteExecutor>>()));
        services.AddScoped<ProductMaintenanceService>();
        services.AddScoped<CheckoutService>();
        return services;
    }
}

public enum CheckoutContextStatus { Bound, NotFound, Forbidden }

/// <summary>只讀取身分、ID 與保存用途，不讓舊 PaymentId 格式阻擋既定的 409 分支。</summary>
public sealed class CheckoutContextLoader(IShopDatabaseContext database, IIdentityContext identity, TransactionContext transaction)
{
    public CheckoutContextStatus Bind(int id)
    {
        transaction.Clear();
        if (identity.Current.Kind != IdentityKind.Member || identity.Current.MemberId == null)
            throw new ExecutionContextInvalidException("載入 Checkout 時缺少會員身分。");
        var stored = database.Database.GetCollection("checkout_transactions").FindById(id);
        if (stored == null) return CheckoutContextStatus.NotFound;
        if (!stored[nameof(CheckoutTransactionRecord.MemberId)].IsInt32)
            throw new ExecutionContextInvalidException("Checkout 買方資料無效。");
        if (stored[nameof(CheckoutTransactionRecord.MemberId)].AsInt32 != identity.Current.MemberId)
            return CheckoutContextStatus.Forbidden;
        var isTest = stored[nameof(CheckoutTransactionRecord.IsTest)];
        if (stored.ContainsKey(nameof(CheckoutTransactionRecord.IsTest)) && !isTest.IsBoolean)
            throw new ExecutionContextInvalidException("Checkout 用途資料無效。");
        transaction.Bind(new TransactionSnapshot(TransactionKind.Checkout, id, !isTest.IsNull && isTest.AsBoolean));
        return CheckoutContextStatus.Bound;
    }
}

/// <summary>CLI 等非 HTTP 操作的獨立 scope；每次從 Token 恢復身分，不沿用外層 context。</summary>
public sealed class ShopOperationScope : IDisposable
{
    private readonly IServiceScope _scope;
    public IServiceProvider Services => _scope.ServiceProvider;
    private ShopOperationScope(IServiceScope scope) => _scope = scope;

    public static ShopOperationScope Open(IServiceScopeFactory factory, string? token)
    {
        var scope = factory.CreateScope();
        try
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityContext>();
            if (token != null)
            {
                var member = scope.ServiceProvider.GetRequiredService<MemberService>().ValidateToken(token)
                    ?? throw new UnauthorizedAccessException("Token 無效或已過期。");
                identity.Initialize(IdentitySnapshot.ForMember(member.Id, member.IsTest));
            }
            return new ShopOperationScope(scope);
        }
        catch { scope.Dispose(); throw; }
    }

    public void Dispose() => _scope.Dispose();
}
