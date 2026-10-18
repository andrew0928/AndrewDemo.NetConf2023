using AndrewDemo.NetConf2023.Core.Checkouts;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.AdminQueries;

/// <summary>管理查核讀取；由同份完整結果產生統計與分頁，不修改業務資料。</summary>
public sealed class AdminQueryService(IShopDatabaseContext database, TimeProvider timeProvider)
{
    public AdminOrderResult GetOrders(AdminQueryOptions options) => Read(() =>
    {
        var queriedAt = timeProvider.GetUtcNow().UtcDateTime;
        var candidates = database.Database.GetCollection("orders").FindAll().Select(OrderRow)
            .Where(x => MatchesPurpose(x.IsTest, options) && MatchesMember(x.MemberId, options)).ToArray();
        var excluded = candidates.Count(x => HasTimeFilter(options) && x.CreatedAt == null);
        var rows = candidates.Where(x => MatchesTime(x.CreatedAt, options)).OrderByDescending(x => x.Id).ToArray();
        var unknown = rows.Count(x => x.TotalPrice == null);
        decimal known;
        try { known = rows.Sum(x => x.TotalPrice ?? 0m); }
        catch (OverflowException) { throw new AdminDataInvalidException("訂單金額超過可表示範圍。"); }
        return new AdminOrderResult(Page(rows, options, queriedAt, excluded), unknown == 0 ? known : null, known, unknown);
    });

    public AdminPage<AdminCheckoutRow> GetCheckouts(AdminQueryOptions options) => Read(() =>
    {
        // LiteDB 5.0.17 每個 collection 分別取得 read version；僅 BeginTrans 不足以協調封存搬移。
        // ForUpdate 在未建立 collection 時仍取得名稱鎖，且 addIfNotExists=false；不 Upsert 或建立文件。
        // 與 CheckoutService.BeginWrite 使用相同鎖，直到本次讀取結束才釋放。
        database.Database.GetCollection("checkout_write_lock").Query().ForUpdate().ToList();
        var queriedAt = timeProvider.GetUtcNow().UtcDateTime;
        var active = database.Database.GetCollection("checkout_transactions").FindAll()
            .Select(x => CheckoutRow(x, "active")).ToArray();
        var archived = database.Database.GetCollection("archived_checkout_transactions").FindAll()
            .Select(x => CheckoutRow(x, "archived")).ToArray();
        var all = active.Concat(archived).ToArray();
        if (all.Select(x => x.TransactionId).Distinct().Count() != all.Length)
            throw new AdminDataInvalidException("Checkout 分區存在重複識別碼。");
        var candidates = all.Where(x => MatchesPurpose(x.IsTest, options) && MatchesMember(x.MemberId, options)
            && (options.State == "all" || x.State == options.State)).ToArray();
        DateTime? Time(AdminCheckoutRow row) => options.TimeField == "closedAt" ? row.ClosedAt : row.CreatedAt;
        var excluded = candidates.Count(x => HasTimeFilter(options) && Time(x) == null);
        var rows = candidates.Where(x => MatchesTime(Time(x), options)).OrderByDescending(x => x.TransactionId).ToArray();
        return Page(rows, options, queriedAt, excluded);
    });

    public AdminPage<AdminMemberRow> GetMembers(AdminQueryOptions options) => Read(() =>
    {
        var queriedAt = timeProvider.GetUtcNow().UtcDateTime;
        var collation = database.Database.Collation;
        var rows = database.Database.GetCollection("members").FindAll()
            .Select(x => new AdminMemberRow(Id(x), Text(x, "Name"), Purpose(x)))
            .Where(x => MatchesPurpose(x.IsTest, options) && MatchesMember(x.Id, options)
                && (options.Name == null || collation.Compare(x.Name, options.Name) == 0))
            .OrderByDescending(x => x.Id).ToArray();
        return Page(rows, options, queriedAt, 0);
    });

    private T Read<T>(Func<T> read)
    {
        var db = database.Database;
        if (!db.BeginTrans()) throw new InvalidOperationException("管理查詢不可參與外層交易。");
        try { return read(); }
        finally { db.Rollback(); } // 釋放快照與協調鎖；查詢沒有寫入可提交。
    }

    private static AdminPage<T> Page<T>(T[] rows, AdminQueryOptions options, DateTime time, int excluded)
    {
        var skip = ((long)options.Page - 1) * options.PageSize;
        var items = skip >= rows.Length ? [] : rows.Skip((int)skip).Take(options.PageSize).ToArray();
        return new(time, rows.Length, excluded, items);
    }
    private static bool MatchesPurpose(bool isTest, AdminQueryOptions options) =>
        options.Purpose == "all" || isTest == (options.Purpose == "test");
    private static bool MatchesMember(int? id, AdminQueryOptions options) => options.MemberId == null || id == options.MemberId;
    private static bool HasTimeFilter(AdminQueryOptions options) => options.From != null || options.To != null;
    private static bool MatchesTime(DateTime? time, AdminQueryOptions options) => !HasTimeFilter(options)
        || (time != null && (options.From == null || time >= options.From) && (options.To == null || time < options.To));

    private static AdminOrderRow OrderRow(BsonDocument doc)
    {
        var buyer = doc["Buyer"];
        if (!buyer.IsNull && !buyer.IsDocument) throw Invalid("Buyer");
        return new(Id(doc), buyer.IsNull ? null : Integer(buyer.AsDocument, "_id"),
            buyer.IsNull ? null : Text(buyer.AsDocument, "Name"), Purpose(doc), Date(doc, "CreatedAt"),
            Money(doc, "TotalPrice"), EnumName<OrderFulfillmentStatus>(doc, "FulfillmentStatus", OrderFulfillmentStatus.Pending),
            Integer(doc, "CheckoutTransactionId"));
    }
    private static AdminCheckoutRow CheckoutRow(BsonDocument doc, string storage) => new(
        Id(doc), Integer(doc, "MemberId"), Integer(doc, "CartId"), Integer(doc, "PaymentId"), Purpose(doc),
        EnumName<CheckoutState>(doc, "State", CheckoutState.Unknown), Date(doc, "CreatedAt"), Date(doc, "ClosedAt"),
        Money(doc, "TotalPrice"), storage);

    private static int Id(BsonDocument doc) => Integer(doc, "_id") ?? throw Invalid("_id");
    private static bool Purpose(BsonDocument doc)
    {
        if (!doc.ContainsKey("IsTest")) return false;
        return doc["IsTest"].IsBoolean ? doc["IsTest"].AsBoolean : throw Invalid("IsTest");
    }
    private static int? Integer(BsonDocument doc, string field)
    {
        var value = doc[field];
        if (value.IsNull) return null;
        if (value.IsInt32) return value.AsInt32;
        if (value.IsInt64 && value.AsInt64 >= int.MinValue && value.AsInt64 <= int.MaxValue) return (int)value.AsInt64;
        throw Invalid(field);
    }
    private static string? Text(BsonDocument doc, string field) => doc[field].IsNull ? null
        : doc[field].IsString ? doc[field].AsString : throw Invalid(field);
    private static DateTime? Date(BsonDocument doc, string field) => doc[field].IsNull ? null
        : doc[field].IsDateTime ? doc[field].AsDateTime.ToUniversalTime() : throw Invalid(field);
    private static decimal? Money(BsonDocument doc, string field)
    {
        if (doc[field].IsNull) return null;
        if (!doc[field].IsNumber) throw Invalid(field);
        try { return doc[field].AsDecimal; }
        catch (OverflowException) { throw Invalid(field); }
    }
    private static string EnumName<T>(BsonDocument doc, string field, T missing) where T : struct, Enum
    {
        var value = doc[field];
        if (value.IsNull) return missing.ToString().ToLowerInvariant();
        var text = value.IsString ? value.AsString : value.IsInt32 ? value.AsInt32.ToString() : "";
        if (Enum.TryParse<T>(text, true, out var parsed) && Enum.IsDefined(parsed)) return parsed.ToString().ToLowerInvariant();
        throw Invalid(field);
    }
    private static AdminDataInvalidException Invalid(string field) => new($"保存欄位 {field} 無法判讀。");
}
