using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Checkouts;

internal static class CheckoutStoredContent
{
    internal static void ValidatePurpose(BsonDocument stored)
    {
        if (stored.ContainsKey("IsTest") && !stored["IsTest"].IsBoolean)
            throw new ExecutionContextInvalidException("Checkout 用途資料無效。");
    }

    internal static string? MissingContent(BsonDocument stored)
    {
        foreach (var field in new[] { "PaymentId", "State", "TotalPrice", "ProductLines", "DiscountLines" })
            if (!stored.ContainsKey(field) || stored[field].IsNull)
                return $"Checkout 未保存 {field}，請重新建立 Checkout。";
        if ((stored["State"].IsString && stored["State"].AsString == nameof(CheckoutState.Unknown)) || stored["State"] == new BsonValue(0))
            return "Checkout 未保存有效 State，請重新建立 Checkout。";
        foreach (var entry in stored["ProductLines"].AsArray)
            foreach (var field in new[] { "LineId", "ProductId", "UnitPrice", "Quantity", "LineAmount" })
                if (!entry.AsDocument.ContainsKey(field) || entry[field].IsNull)
                    return $"Checkout 商品明細未保存 {field}，請重新建立 Checkout。";
        foreach (var entry in stored["DiscountLines"].AsArray)
            foreach (var field in new[] { "RuleId", "Amount", "RelatedLineIds" })
                if (!entry.AsDocument.ContainsKey(field) || entry[field].IsNull)
                    return $"Checkout 折扣明細未保存 {field}，請重新建立 Checkout。";
        return null;
    }

    internal static void Validate(CheckoutTransactionRecord transaction)
    {
        var ids = transaction.ProductLines.Select(x => x.LineId).ToHashSet(StringComparer.Ordinal);
        if (transaction.State != CheckoutState.Created || transaction.ClosedAt != null
            || ids.Count != transaction.ProductLines.Count
            || transaction.ProductLines.Any(x => string.IsNullOrWhiteSpace(x.LineId) || string.IsNullOrWhiteSpace(x.ProductId)
                || x.Quantity <= 0 || x.LineAmount != x.UnitPrice * x.Quantity
                || (x.ParentLineId != null && (!ids.Contains(x.ParentLineId) || x.ParentLineId == x.LineId)))
            || transaction.DiscountLines.Any(x => x.RelatedLineIds.Any(id => !ids.Contains(id)))
            || transaction.TotalPrice != transaction.ProductLines.Sum(x => x.LineAmount) + transaction.DiscountLines.Sum(x => x.Amount))
            throw new InvalidOperationException("Checkout 保存內容矛盾；停止完成，不能重新計價或修補歷史資料。");
    }
}
