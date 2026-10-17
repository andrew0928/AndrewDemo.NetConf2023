using AndrewDemo.NetConf2023.Core;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class TipObjectPersistenceTests : ShopDatabaseTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PurposeAndPaymentFields_RoundTripThroughLiteDb(bool isTest)
    {
        var member = new Member { Name = "tip-member", IsTest = isTest };
        Context.Members.Insert(member);
        var token = new MemberAccessTokenRecord
        {
            Token = "tip-token",
            MemberId = member.Id,
            IsTest = member.IsTest,
            Expire = FixedUtcNow.AddHours(1)
        };
        Context.MemberTokens.Insert(token);
        var checkout = new CheckoutTransactionRecord
        {
            CartId = 11,
            MemberId = member.Id,
            CreatedAt = FixedUtcNow,
            IsTest = isTest,
            PaymentId = 9527
        };
        Context.CheckoutTransactions.Insert(checkout);
        var order = new Order(101)
        {
            Buyer = member,
            IsTest = checkout.IsTest,
            CheckoutTransactionId = checkout.TransactionId
        };
        Context.Orders.Insert(order);

        Assert.Equal(isTest, Context.Members.FindById(member.Id).IsTest);
        Assert.Equal(isTest, Context.MemberTokens.FindById(token.Token).IsTest);
        var restoredCheckout = Context.CheckoutTransactions.FindById(checkout.TransactionId);
        Assert.Equal(isTest, restoredCheckout.IsTest);
        Assert.Equal(9527, restoredCheckout.PaymentId);
        var restoredOrder = Context.Orders.FindById(order.Id);
        Assert.Equal(isTest, restoredOrder.IsTest);
        Assert.Equal(isTest, restoredOrder.Buyer.IsTest);
        Assert.Equal(checkout.TransactionId, restoredOrder.CheckoutTransactionId);

        // 確認用途確實寫入文件，不是只在記憶體保留。
        Assert.Equal(isTest, Context.Database.GetCollection("members").FindById(member.Id)["IsTest"].AsBoolean);
        Assert.Equal(isTest, Context.Database.GetCollection("member_tokens").FindById(token.Token)["IsTest"].AsBoolean);
        Assert.Equal(isTest, Context.Database.GetCollection("checkout_transactions").FindById(checkout.TransactionId)["IsTest"].AsBoolean);
        Assert.Equal(isTest, Context.Database.GetCollection("orders").FindById(order.Id)["IsTest"].AsBoolean);
    }

    [Fact]
    public void ArchivedCheckout_IsStoredSeparatelyAndReferencedByOrder()
    {
        var checkout = new CheckoutTransactionRecord
        {
            TransactionId = 17,
            CartId = 23,
            MemberId = 31,
            CreatedAt = FixedUtcNow,
            IsTest = true,
            PaymentId = 9527
        };
        Context.CheckoutTransactions.Insert(checkout);
        Context.ArchivedCheckoutTransactions.Insert(checkout);
        Context.CheckoutTransactions.Delete(checkout.TransactionId);

        // 正式會員可持有測試交易；Order ID 與 Checkout ID 可不同。
        var order = new Order(42)
        {
            Buyer = new Member { Id = checkout.MemberId, Name = "regular-member" },
            IsTest = checkout.IsTest,
            CheckoutTransactionId = checkout.TransactionId
        };
        Context.Orders.Insert(order);

        Assert.Null(Context.CheckoutTransactions.FindById(checkout.TransactionId));
        var restoredOrder = Context.Orders.FindById(order.Id);
        Assert.True(restoredOrder.IsTest);
        Assert.False(restoredOrder.Buyer.IsTest);
        var archived = Context.ArchivedCheckoutTransactions.FindById(restoredOrder.CheckoutTransactionId!.Value);
        Assert.Equal(checkout.TransactionId, archived.TransactionId);
        Assert.Equal(checkout.CartId, archived.CartId);
        Assert.Equal(checkout.MemberId, archived.MemberId);
        Assert.Equal(checkout.CreatedAt, archived.CreatedAt.ToUniversalTime());
        Assert.Equal(checkout.IsTest, archived.IsTest);
        Assert.Equal(checkout.PaymentId, archived.PaymentId);
        Assert.NotNull(Context.Database.GetCollection("archived_checkout_transactions").FindById(checkout.TransactionId));
    }

    [Fact]
    public void AccountingTimestamps_RoundTripWithoutDependingOnCurrentCart()
    {
        var closedAt = FixedUtcNow.AddMinutes(5);
        var checkout = new CheckoutTransactionRecord
        {
            TransactionId = 17,
            CartId = 23,
            MemberId = 31,
            CreatedAt = FixedUtcNow,
            ClosedAt = closedAt,
            PaymentId = 9527
        };
        Context.ArchivedCheckoutTransactions.Insert(checkout);
        Context.Orders.Insert(new Order(42)
        {
            Buyer = new Member { Id = 31, Name = "buyer" },
            CheckoutTransactionId = checkout.TransactionId,
            CreatedAt = closedAt,
            TotalPrice = 180m,
            ProductLines = [new Order.OrderProductLine
            {
                ProductId = "product-1", ProductName = "商品快照",
                UnitPrice = 100m, Quantity = 2, LineAmount = 200m
            }],
            DiscountLines = [new Order.OrderDiscountLine
            {
                RuleId = "discount-1", Name = "折扣快照", Amount = -20m
            }]
        });

        var order = Context.Orders.FindById(42);
        var archived = Context.ArchivedCheckoutTransactions.FindById(order.CheckoutTransactionId!.Value);
        Assert.Equal(closedAt, order.CreatedAt!.Value.ToUniversalTime());
        Assert.Equal(closedAt, archived.ClosedAt!.Value.ToUniversalTime());
        Assert.Equal(FixedUtcNow, archived.CreatedAt.ToUniversalTime());
        Assert.Null(Context.Carts.FindById(checkout.CartId));
        Assert.Equal(180m, order.TotalPrice);
        Assert.Equal(200m, Assert.Single(order.ProductLines).LineAmount);
        Assert.Equal(-20m, Assert.Single(order.DiscountLines).Amount);
    }

    [Fact]
    public void ActiveCheckout_HasNoClosingTimestamp()
    {
        var checkout = new CheckoutTransactionRecord
        {
            CartId = 23, MemberId = 31, CreatedAt = FixedUtcNow, PaymentId = 9527
        };
        Context.CheckoutTransactions.Insert(checkout);

        Assert.Null(Context.CheckoutTransactions.FindById(checkout.TransactionId).ClosedAt);
    }

    [Fact]
    public void StoredPurpose_CanExcludeTestOrdersForTheSameRegularBuyer()
    {
        var buyer = new Member { Id = 31, Name = "regular-buyer", IsTest = false };
        Context.Orders.Insert(new Order(41) { Buyer = buyer, CreatedAt = FixedUtcNow, TotalPrice = 100m });
        Context.Orders.Insert(new Order(42) { Buyer = buyer, CreatedAt = FixedUtcNow, TotalPrice = 900m, IsTest = true });

        // 驗證儲存欄位可作為過濾條件；查詢服務與預設政策留在 Phase 2。
        var regularOrders = Context.Orders.Find(order => order.IsTest == false).ToList();
        Assert.Equal(41, Assert.Single(regularOrders).Id);
        Assert.Equal(100m, regularOrders.Sum(order => order.TotalPrice));
        Assert.Equal(2, Context.Orders.FindAll().Count());
    }

    [Fact]
    public void OrderWithoutCheckout_PreservesNullRelationship()
    {
        var order = new Order(42) { Buyer = new Member { Name = "external-buyer" } };
        Context.Orders.Insert(order);

        var restored = Context.Orders.FindById(order.Id);
        Assert.Null(restored.CheckoutTransactionId);
        Assert.False(restored.IsTest);
    }

    [Fact]
    public void LegacyDocuments_DefaultToRegularPurposeWithoutInventingOrderRelationship()
    {
        // 用原始 BSON 模擬改版前沒有新增欄位的文件。
        Context.Database.GetCollection("members").Insert(new BsonDocument
        {
            ["_id"] = 7, ["Name"] = "legacy-member"
        });
        Context.Database.GetCollection("member_tokens").Insert(new BsonDocument
        {
            ["_id"] = "legacy-token", ["MemberId"] = 7, ["Expire"] = FixedUtcNow
        });
        Context.Database.GetCollection("checkout_transactions").Insert(new BsonDocument
        {
            ["_id"] = 42, ["MemberId"] = 7, ["CartId"] = 11, ["CreatedAt"] = FixedUtcNow
        });
        Context.Database.GetCollection("orders").Insert(new BsonDocument
        {
            ["_id"] = 42,
            ["Buyer"] = new BsonDocument { ["_id"] = 7, ["Name"] = "legacy-member" }
        });

        Assert.False(Context.Members.FindById(7).IsTest);
        Assert.False(Context.MemberTokens.FindById("legacy-token").IsTest);
        var checkout = Context.CheckoutTransactions.FindById(42);
        Assert.False(checkout.IsTest);
        Assert.Null(checkout.ClosedAt);
        var order = Context.Orders.FindById(42);
        Assert.False(order.IsTest);
        Assert.False(order.Buyer.IsTest);
        Assert.Null(order.CheckoutTransactionId);
        Assert.Null(order.CreatedAt);
        Assert.Empty(Context.ArchivedCheckoutTransactions.FindAll());
    }
}
