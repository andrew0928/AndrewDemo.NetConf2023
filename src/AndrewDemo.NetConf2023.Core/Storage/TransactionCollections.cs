using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Storage;

/// <summary>交易用途與生命週期的唯一 collection 對照；不依賴執行身分。</summary>
public static class TransactionCollections
{
    internal const string Lock = "checkout_write_lock";
    internal const string Metadata = "transaction_storage";
    internal const int Version = 1;
    public static string Orders(bool isTest) => isTest ? "test_orders" : "orders";
    public static string Checkouts(bool isTest, bool archived = false) => (isTest ? "test_" : "")
        + (archived ? "archived_checkout_transactions" : "checkout_transactions");

    internal static IEnumerable<(string Name, bool IsTest, bool? Archived)> All()
    {
        foreach (var isTest in new[] { false, true })
        {
            yield return (Orders(isTest), isTest, null);
            yield return (Checkouts(isTest), isTest, false);
            yield return (Checkouts(isTest, true), isTest, true);
        }
    }

    internal static bool Purpose(BsonDocument doc)
    {
        if (!doc.ContainsKey("IsTest")) return false;
        if (!doc["IsTest"].IsBoolean) throw Invalid("交易用途無法判讀。");
        return doc["IsTest"].AsBoolean;
    }

    internal static int Id(BsonDocument doc)
    {
        var id = doc["_id"];
        if (!id.IsInt32 || id.AsInt32 <= 0) throw Invalid("交易識別碼無效。");
        return id.AsInt32;
    }

    internal static void Validate(BsonDocument doc, bool isTest)
    {
        Id(doc);
        if (Purpose(doc) != isTest) throw Invalid("交易用途與所在 collection 矛盾。");
    }

    internal static IEnumerable<bool> Purposes(string purpose) => purpose switch
    {
        "normal" => [false], "test" => [true], "all" => [false, true],
        _ => throw new ArgumentException("交易查詢用途必須為 normal、test 或 all。", nameof(purpose))
    };

    internal static ExecutionContextInvalidException Invalid(string reason) => new(reason);
}
