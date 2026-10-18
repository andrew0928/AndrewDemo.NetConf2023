using AndrewDemo.NetConf2023.Core.Effects;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Core.Testing;
using AndrewDemo.NetConf2023.Core.Time;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndrewDemo.NetConf2023.Core.Checkouts;

/// <summary>已定案的業務拒絕，由 host 映射為 400；不改變 command／response 合約。</summary>
public sealed class CheckoutRejectedException(string message) : InvalidOperationException(message);

public sealed class CheckoutService
{
    private readonly IShopDatabaseContext _database;
    private readonly DiscountEngine _discountEngine;
    private readonly IOrderEventDispatcher _orderEventDispatcher;
    private readonly IProductService _productService;
    private readonly ShopManifest _shopManifest;
    private readonly TimeProvider _timeProvider;
    private readonly TestAllowlist _allowlist;
    private readonly TransactionContext _transactionContext;
    private readonly OperationEffects _effects;
    private readonly ProtectedWriteExecutor _writes;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(IShopDatabaseContext database, DiscountEngine discountEngine,
        IOrderEventDispatcher orderEventDispatcher, IProductService productService,
        ShopManifest shopManifest, TimeProvider timeProvider, TestAllowlist? allowlist = null,
        TransactionContext? transactionContext = null, OperationEffects? effects = null,
        ILogger<CheckoutService>? logger = null, ProtectedWriteExecutor? writes = null)
    {
        _database = database;
        _discountEngine = discountEngine;
        _orderEventDispatcher = orderEventDispatcher;
        _productService = productService;
        _shopManifest = shopManifest;
        _timeProvider = timeProvider;
        _allowlist = allowlist ?? new TestAllowlist();
        _transactionContext = transactionContext ?? new TransactionContext();
        _effects = effects ?? new OperationEffects();
        _logger = logger ?? NullLogger<CheckoutService>.Instance;
        _writes = writes ?? new ProtectedWriteExecutor(new IdentityContext(), _transactionContext, _effects, _logger);
    }

    public CheckoutCreateResult Create(CheckoutCreateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.RequestMember);
        _transactionContext.Clear();
        _effects.Clear();
        var cart = _database.Carts.FindById(command.CartId);
        if (cart == null) return CheckoutCreateResult.CreateCartNotFound("Cart not found");
        var member = _database.Members.FindById(command.RequestMember.Id)
            ?? throw new CheckoutRejectedException("會員不存在。");
        ValidateMember(command.RequestMember, member);
        var createdAt = BusinessTime();
        var lines = new List<CheckoutProductLine>();
        var pricingLines = new List<LineItem>();
        foreach (var line in cart.LineItems)
        {
            var product = _productService.GetProductById(line.ProductId)
                ?? throw new CheckoutRejectedException($"商品 {line.ProductId} 不存在或不可購買。");
            if (line.Quantity <= 0) throw new CheckoutRejectedException("商品數量必須大於零。");
            var skuId = NormalizeSkuId(product.SkuId);
            if (skuId != null && _database.Skus.FindById(skuId) == null)
                throw new CheckoutRejectedException($"SKU {skuId} 不存在。");
            lines.Add(new CheckoutProductLine
            {
                LineId = line.LineId, ParentLineId = line.ParentLineId, ProductId = line.ProductId,
                SkuId = skuId, UnitPrice = product.Price, Quantity = line.Quantity,
                LineAmount = product.Price * line.Quantity
            });
            pricingLines.Add(new LineItem
            {
                LineId = line.LineId, ParentLineId = line.ParentLineId, AddedAt = line.AddedAt,
                ProductId = line.ProductId, ProductName = product.Name,
                UnitPrice = product.Price, Quantity = line.Quantity
            });
        }
        var discounts = _discountEngine.Evaluate(new CartContext
        {
            ShopId = _shopManifest.ShopId, ConsumerId = member.Id, ConsumerName = member.Name,
            EvaluatedAt = createdAt, LineItems = pricingLines
        }).Where(x => x.Kind == DiscountRecordKind.Discount).Select(x => new CheckoutDiscountLine
        {
            RuleId = x.RuleId, Name = x.Name, Description = x.Description, Amount = x.Amount,
            RelatedLineIds = x.RelatedLineIds.ToList()
        }).ToList();
        var transaction = new CheckoutTransactionRecord
        {
            CartId = cart.Id, MemberId = member.Id, CreatedAt = createdAt,
            PaymentId = command.PaymentId, IsTest = member.IsTest || _allowlist.IsTestPayment(command.PaymentId),
            State = CheckoutState.Created, ProductLines = lines, DiscountLines = discounts,
            TotalPrice = lines.Sum(x => x.LineAmount) + discounts.Sum(x => x.Amount)
        };
        CheckoutStoredContent.Validate(transaction);
        BeginWrite();
        try
        {
            transaction.TransactionId = checked(Math.Max(MaxId("checkout_transactions"), MaxId("archived_checkout_transactions")) + 1);
            _database.CheckoutTransactions.Insert(transaction);
            _database.Database.Commit();
        }
        catch { _database.Database.Rollback(); throw; }
        Bind(transaction);
        return CheckoutCreateResult.CreateSucceeded(transaction.TransactionId, createdAt, member.Id, member.Name);
    }

    public async Task<CheckoutCompleteResult> CompleteAsync(CheckoutCompleteCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.RequestMember);
        _effects.Clear();
        _transactionContext.Clear();
        var ticket = new WaitingRoomTicket(_timeProvider);
        await ticket.WaitUntilCanRunAsync();
        // LiteDB transaction 為 thread-bound；此同步區段不可 await。
        var result = CompleteTransaction(command);
        if (result.Status != CheckoutCompleteStatus.Succeeded) return result;
        var order = result.OrderDetail!;
        try
        {
            _orderEventDispatcher.Dispatch(OrderEventFactory.CreateCompletedEvent(_shopManifest, order, result.TransactionCompleteAt));
            order.FulfillmentStatus = OrderFulfillmentStatus.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "訂單 {OrderId} 履約失敗；保留已提交的交易與封存。", order.Id);
            order.FulfillmentStatus = OrderFulfillmentStatus.Failed;
        }
        // 只改履約欄位，不以事件取得的可變 Order 覆寫用途或交易內容。
        _database.Database.GetCollection("orders").UpdateMany(
            BsonExpression.Create("{ FulfillmentStatus: @0 }", new BsonValue(order.FulfillmentStatus.ToString())),
            BsonExpression.Create("_id = @0", new BsonValue(order.Id)));
        return result;
    }

    private CheckoutCompleteResult CompleteTransaction(CheckoutCompleteCommand command)
    {
        BeginWrite();
        try
        {
            var stored = _database.Database.GetCollection("checkout_transactions").FindById(command.TransactionId);
            if (stored == null) return Reject(CheckoutCompleteResult.CreateTransactionNotFound("Transaction not found"));
            if (stored["MemberId"].AsInt32 != command.RequestMember.Id)
                return Reject(CheckoutCompleteResult.CreateBuyerMismatch("Transaction buyer mismatch"));
            CheckoutStoredContent.ValidatePurpose(stored);
            _transactionContext.Bind(new TransactionSnapshot(TransactionKind.Checkout, command.TransactionId,
                stored.ContainsKey("IsTest") && stored["IsTest"].AsBoolean));
            var legacyReason = CheckoutStoredContent.MissingContent(stored);
            if (legacyReason != null) return Reject(CheckoutCompleteResult.CreateRecreateRequired(legacyReason));
            var transaction = _database.Database.Mapper.ToObject<CheckoutTransactionRecord>(stored);
            CheckoutStoredContent.Validate(transaction);
            var consumer = _database.Members.FindById(transaction.MemberId);
            if (consumer == null) return Reject(CheckoutCompleteResult.CreateConsumerNotFound("Consumer not found"));
            ValidateMember(command.RequestMember, consumer);
            var completedAt = BusinessTime();
            var order = new Order(checked(MaxId("orders") + 1))
            {
                Buyer = consumer, IsTest = transaction.IsTest, CheckoutTransactionId = transaction.TransactionId,
                CreatedAt = completedAt, TotalPrice = transaction.TotalPrice!.Value,
                ShopNotes = new Order.OrderShopNotes { BuyerSatisfaction = command.Satisfaction, Comments = command.ShopComments }
            };
            var requirements = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in transaction.ProductLines)
            {
                var product = _productService.GetProductById(line.ProductId);
                if (product == null || !string.Equals(NormalizeSkuId(product.SkuId), line.SkuId, StringComparison.OrdinalIgnoreCase)
                    || (line.SkuId != null && _database.Skus.FindById(line.SkuId) == null))
                    return Reject(CheckoutCompleteResult.CreateProductNotFound($"商品 {line.ProductId} 不可購買或 SKU 關聯已變更。"));
                order.ProductLines.Add(new Order.OrderProductLine
                {
                    ProductId = line.ProductId, SkuId = line.SkuId, ProductName = product.Name,
                    UnitPrice = line.UnitPrice, Quantity = line.Quantity, LineAmount = line.LineAmount
                });
                if (line.SkuId != null)
                {
                    requirements.TryGetValue(line.SkuId, out var quantity);
                    requirements[line.SkuId] = checked(quantity + line.Quantity);
                }
            }
            order.DiscountLines = transaction.DiscountLines.Select(x => new Order.OrderDiscountLine
            { RuleId = x.RuleId, Name = x.Name, Description = x.Description, Amount = x.Amount }).ToList();
            var inventory = new Dictionary<string, Products.InventoryRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var requirement in requirements)
            {
                var record = _database.InventoryRecords.Query().Where(x => x.SkuId == requirement.Key).ForUpdate().FirstOrDefault();
                if (record == null || record.AvailableQuantity < requirement.Value)
                    return Reject(CheckoutCompleteResult.CreateInventoryInsufficient($"SKU {requirement.Key} 庫存不足。"));
                inventory.Add(requirement.Key, record);
            }
            foreach (var requirement in requirements)
            {
                _writes.ForCheckout(transaction.TransactionId, "inventory.deduct", "inventory", requirement.Key,
                    requirement.Value, () =>
                    {
                        var record = inventory[requirement.Key];
                        record.AvailableQuantity -= requirement.Value;
                        record.UpdatedAt = completedAt;
                        if (!_database.InventoryRecords.Update(record)) throw new InvalidOperationException("庫存更新失敗。");
                    });
            }
            _database.Orders.Insert(order);
            // 使用原 BSON，保留所有原欄位（包括未知的擴充欄位），只設定終態與時間。
            stored["State"] = CheckoutState.Completed.ToString();
            stored["ClosedAt"] = completedAt;
            _database.Database.GetCollection("archived_checkout_transactions").Insert(stored);
            if (!_database.CheckoutTransactions.Delete(command.TransactionId)) throw new InvalidOperationException("作用中交易移除失敗。");
            _database.Database.Commit();
            return CheckoutCompleteResult.CreateSucceeded(command.TransactionId, transaction.PaymentId, completedAt,
                consumer.Id, consumer.Name, order);
        }
        catch (Exception ex)
        {
            _database.Database.Rollback();
            _logger.LogError(ex, "Checkout {TransactionId} 完成失敗，未提交交易。", command.TransactionId);
            throw;
        }
    }

    private CheckoutCompleteResult Reject(CheckoutCompleteResult result)
    {
        _database.Database.Rollback();
        _logger.LogInformation("Checkout 拒絕：{Status}，{Reason}", result.Status, result.ErrorMessage);
        return result;
    }

    private void BeginWrite()
    {
        if (!_database.Database.BeginTrans()) throw new InvalidOperationException("Checkout 不接受外層資料庫交易。");
        try
        {
            // 先取得跨 instance／Shared connection 的寫入鎖，再讀取 ID 與交易狀態。
            _database.Database.GetCollection("checkout_write_lock").Upsert(new BsonDocument { ["_id"] = 1 });
        }
        catch { _database.Database.Rollback(); throw; }
    }

    private int MaxId(string collection) => _database.Database.GetCollection(collection)
        .Query().OrderByDescending("_id").FirstOrDefault()?["_id"].AsInt32 ?? 0;
    private DateTime BusinessTime()
    {
        var utc = _timeProvider.GetUtcDateTime();
        // LiteDB 時間精度為毫秒；回應與兩筆持久化時間採完全相同的 UTC 值。
        return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    }
    private void Bind(CheckoutTransactionRecord transaction) => _transactionContext.Bind(
        new TransactionSnapshot(TransactionKind.Checkout, transaction.TransactionId, transaction.IsTest));
    private static void ValidateMember(Member supplied, Member stored)
    {
        if (supplied.IsTest != stored.IsTest) throw new ExecutionContextInvalidException("RequestMember 與保存的會員用途矛盾。");
    }
    private static string? NormalizeSkuId(string? skuId) => string.IsNullOrWhiteSpace(skuId) ? null : skuId;
}
