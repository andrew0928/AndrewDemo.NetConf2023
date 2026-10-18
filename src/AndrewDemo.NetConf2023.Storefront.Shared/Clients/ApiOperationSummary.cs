using System.Globalization;
using System.Net.Http.Headers;

namespace AndrewDemo.NetConf2023.Storefront.Shared.Clients;

/// <summary>HTTP 出口摘要；header 沒有逐筆目標，不假造完整 SkippedEffect。</summary>
public sealed record ApiOperationSummary(bool? IsTestBehavior, string? Outcome,
    string[] SkippedEffectCodes, ApiTransactionSummary? Transaction)
{
    public static ApiOperationSummary FromHeaders(HttpResponseHeaders headers)
    {
        string? Single(string name) => headers.TryGetValues(name, out var values) && values.Count() == 1 ? values.Single() : null;
        bool? behavior = Single("X-Shop-Behavior") switch { "test" => true, "normal" => false, _ => null };
        var outcome = Single("X-Shop-Outcome");
        if (outcome is not ("succeeded" or "rejected" or "failed")) outcome = null;
        var kind = Single("X-Shop-Transaction-Type");
        ApiTransactionSummary? transaction = null;
        if (kind is "checkout" or "order" &&
            int.TryParse(Single("X-Shop-Transaction-Id"), NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 &&
            Single("X-Shop-Transaction-Is-Test") is "true" or "false")
            transaction = new(kind, id, Single("X-Shop-Transaction-Is-Test") == "true");
        var codes = (Single("X-Shop-Skipped-Effects") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return new(behavior, outcome, codes, transaction);
    }

    public string BehaviorText => IsTestBehavior switch { true => "測試操作（test）", false => "一般操作（normal）", _ => "用途未確認" };
    public string OutcomeText => Outcome switch { "succeeded" => "成功（succeeded）", "rejected" => "拒絕（rejected）", "failed" => "失敗（failed）", _ => "結果未提供" };
    public static string EffectText(string code) => code switch
    {
        "inventory.deduct" => "庫存扣減", "product.update" => "商品更新", "sku.update" => "SKU 更新",
        "payment.charge" => "付款扣款", "shipment.create" => "訂單出貨", "statistics.update" => "商業統計更新", _ => code
    };
}

public sealed record ApiTransactionSummary(string Kind, int Id, bool IsTest);
