using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class CheckoutRecordPersistenceTests : ShopDatabaseTestBase
{
    [Theory]
    [InlineData(CheckoutState.Completed, false)]
    [InlineData(CheckoutState.Completed, true)]
    [InlineData(CheckoutState.Cancelled, false)]
    [InlineData(CheckoutState.Cancelled, true)]
    public void TerminalRecord_PreservesAllFieldsWhenStoredInArchive(CheckoutState state, bool isTest)
    {
        var record = new CheckoutTransactionRecord
        {
            TransactionId = 17, CartId = 23, MemberId = 31,
            CreatedAt = FixedUtcNow, State = CheckoutState.Created,
            IsTest = isTest, PaymentId = 9527, TotalPrice = 180m,
            ProductLines = [new CheckoutProductLine
            {
                LineId = "line-1", ProductId = "product-1", SkuId = "sku-1",
                UnitPrice = 100m, Quantity = 2, LineAmount = 200m
            }],
            DiscountLines = [new CheckoutDiscountLine
            {
                RuleId = "rule-1", Name = "已套用折扣", Description = "折抵 20 元",
                Amount = -20m, RelatedLineIds = ["line-1"]
            }]
        };
        Context.CheckoutTransactions.Insert(record);
        var restored = Context.CheckoutTransactions.FindById(17);
        Assert.Equal(CheckoutState.Created, restored.State);
        Assert.Null(restored.ClosedAt);

        // 此處驗證兩個 collection 的資料結構，不宣稱服務已實作封存或凍結保護。
        restored.State = state;
        restored.ClosedAt = FixedUtcNow.AddMinutes(5);
        Context.CheckoutTransactions.Update(restored);
        var before = Context.Database.GetCollection("checkout_transactions").FindById(17).ToString();
        Context.ArchivedCheckoutTransactions.Insert(restored);
        Context.CheckoutTransactions.Delete(17);

        var archived = Context.ArchivedCheckoutTransactions.FindById(17);
        Assert.Equal(state, archived.State);
        Assert.Equal(isTest, archived.IsTest);
        Assert.Equal(9527, archived.PaymentId);
        Assert.Equal(23, archived.CartId);
        Assert.Equal(31, archived.MemberId);
        Assert.Equal(FixedUtcNow, archived.CreatedAt.ToUniversalTime());
        Assert.Equal(FixedUtcNow.AddMinutes(5), archived.ClosedAt!.Value.ToUniversalTime());
        Assert.Equal(180m, archived.TotalPrice);
        var product = Assert.Single(archived.ProductLines);
        Assert.Equal("line-1", product.LineId);
        Assert.Equal("product-1", product.ProductId);
        Assert.Equal("sku-1", product.SkuId);
        Assert.Equal(100m, product.UnitPrice);
        Assert.Equal(2, product.Quantity);
        Assert.Equal(200m, product.LineAmount);
        var discount = Assert.Single(archived.DiscountLines);
        Assert.Equal("rule-1", discount.RuleId);
        Assert.Equal(-20m, discount.Amount);
        Assert.Equal("line-1", Assert.Single(discount.RelatedLineIds));
        var document = Context.Database.GetCollection("archived_checkout_transactions").FindById(17);
        Assert.Equal(before, document.ToString());
        Assert.Null(Context.CheckoutTransactions.FindById(17));
        Assert.Null(Context.Carts.FindById(23));

        // 持久化明細只有交易數值與識別，沒有商品名稱、描述、規格或整份 Product。
        var keys = document["ProductLines"].AsArray[0].AsDocument.Keys.OrderBy(key => key).ToArray();
        // LiteDB 預設省略 null 欄位；有值的 ParentLineId 另由下方案例驗證。
        Assert.Equal(new[] { "LineAmount", "LineId", "ProductId", "Quantity", "SkuId", "UnitPrice" }, keys);
    }

    [Fact]
    public void ExplicitZeroAmount_IsDifferentFromLegacyMissingAmount()
    {
        Context.CheckoutTransactions.Insert(new CheckoutTransactionRecord
        {
            TransactionId = 17, State = CheckoutState.Created, TotalPrice = 0m
        });
        Context.Database.GetCollection("checkout_transactions").Insert(new LiteDB.BsonDocument { ["_id"] = 18 });

        var current = Context.CheckoutTransactions.FindById(17);
        var legacy = Context.CheckoutTransactions.FindById(18);
        Assert.Equal(0m, current.TotalPrice);
        Assert.Equal(CheckoutState.Created, current.State);
        Assert.Null(legacy.TotalPrice);
        Assert.Equal(CheckoutState.Unknown, legacy.State);
        Assert.Null(legacy.ClosedAt);
        Assert.Empty(legacy.ProductLines);
        Assert.Empty(legacy.DiscountLines);
    }

    [Fact]
    public void StoredTransactionDetails_AreIndependentOfCallerAndOrderCollections()
    {
        var products = new List<CheckoutProductLine>
        {
            new() { LineId = "parent", ProductId = "p1", UnitPrice = 100m, Quantity = 1, LineAmount = 100m },
            new() { LineId = "child", ParentLineId = "parent", ProductId = "p2", UnitPrice = 0m, Quantity = 1 }
        };
        Context.CheckoutTransactions.Insert(new CheckoutTransactionRecord
        {
            TransactionId = 17, State = CheckoutState.Created, TotalPrice = 100m, ProductLines = products
        });
        products.Clear();
        Context.Orders.Insert(new Order(42)
        {
            Buyer = new Member { Name = "buyer" }, CheckoutTransactionId = 17, TotalPrice = 100m,
            ProductLines = [new Order.OrderProductLine { ProductId = "p1", ProductName = "訂單顯示名稱", Quantity = 1, UnitPrice = 100m, LineAmount = 100m }]
        });

        var record = Context.CheckoutTransactions.FindById(17);
        Assert.Equal(100m, record.TotalPrice);
        Assert.Equal(2, record.ProductLines.Count);
        Assert.Equal("parent", record.ProductLines[1].ParentLineId);
        Assert.Empty(record.DiscountLines);
        Assert.Single(Context.Orders.FindById(42).ProductLines);
    }
}
