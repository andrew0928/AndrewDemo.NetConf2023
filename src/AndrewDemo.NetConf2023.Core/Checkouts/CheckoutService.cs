using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Products;

namespace AndrewDemo.NetConf2023.Core.Checkouts
{
    public sealed class CheckoutService
    {
        private readonly IShopDatabaseContext _database;
        private readonly DiscountEngine _discountEngine;
        private readonly IProductService _productService;
        private readonly ShopManifest _shopManifest;

        public CheckoutService(IShopDatabaseContext database, DiscountEngine discountEngine, IProductService productService, ShopManifest shopManifest)
        {
            _database = database;
            _discountEngine = discountEngine;
            _productService = productService;
            _shopManifest = shopManifest;
        }

        public CheckoutCreateResult Create(CheckoutCreateCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.RequestMember);

            var cart = _database.Carts.FindById(command.CartId);
            if (cart == null)
            {
                return CheckoutCreateResult.CreateCartNotFound("Cart not found");
            }

            var transactionStartAt = DateTime.UtcNow;
            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = command.RequestMember.Id,
                CreatedAt = transactionStartAt
            };

            _database.CheckoutTransactions.Insert(transaction);

            return CheckoutCreateResult.CreateSucceeded(
                transaction.TransactionId,
                transactionStartAt,
                command.RequestMember.Id,
                command.RequestMember.Name);
        }

        public async Task<CheckoutCompleteResult> CompleteAsync(CheckoutCompleteCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.RequestMember);

            var ticket = new WaitingRoomTicket();
            await ticket.WaitUntilCanRunAsync();

            var transaction = _database.CheckoutTransactions.FindById(command.TransactionId);
            if (transaction == null)
            {
                return CheckoutCompleteResult.CreateTransactionNotFound("Transaction not found");
            }

            if (transaction.MemberId != command.RequestMember.Id)
            {
                return CheckoutCompleteResult.CreateBuyerMismatch("Transaction buyer mismatch");
            }

            var cart = _database.Carts.FindById(transaction.CartId);
            if (cart == null)
            {
                return CheckoutCompleteResult.CreateCartNotFound("Cart not found");
            }

            var consumer = _database.Members.FindById(transaction.MemberId);
            if (consumer == null)
            {
                return CheckoutCompleteResult.CreateConsumerNotFound("Consumer not found");
            }

            var order = new Order(command.TransactionId)
            {
                Buyer = consumer,
                FulfillmentStatus = OrderFulfillmentStatus.Pending
            };

            decimal total = 0m;
            var completedAt = DateTime.UtcNow;
            var inventoryRequirements = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var lineitem in cart.LineItems)
            {
                var product = _productService.GetProductById(lineitem.ProductId);
                if (product == null)
                {
                    return CheckoutCompleteResult.CreateProductNotFound($"Product {lineitem.ProductId} not found");
                }

                var skuId = NormalizeSkuId(product.SkuId);

                total += product.Price * lineitem.Quantity;

                order.ProductLines.Add(new Order.OrderProductLine
                {
                    ProductId = product.Id,
                    SkuId = skuId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = lineitem.Quantity,
                    LineAmount = product.Price * lineitem.Quantity
                });

                if (skuId == null)
                {
                    continue;
                }

                inventoryRequirements.TryGetValue(skuId, out var currentQuantity);
                inventoryRequirements[skuId] = currentQuantity + lineitem.Quantity;
            }

            var discountContext = CartContextFactory.Create(_shopManifest, cart, consumer, _productService);
            foreach (var discount in _discountEngine.Evaluate(discountContext))
            {
                if (discount.Kind != Abstract.Discounts.DiscountRecordKind.Discount)
                {
                    continue;
                }

                order.DiscountLines.Add(new Order.OrderDiscountLine
                {
                    RuleId = discount.RuleId,
                    Name = discount.Name,
                    Description = discount.Description,
                    Amount = discount.Amount
                });

                total += discount.Amount;
            }

            order.TotalPrice = total;
            order.ShopNotes = new Order.OrderShopNotes
            {
                BuyerSatisfaction = command.Satisfaction,
                Comments = command.ShopComments
            };

            var ownsTransaction = _database.Database.BeginTrans();
            try
            {
                var inventoryResult = ValidateAndDeductInventory(inventoryRequirements, completedAt);
                if (inventoryResult != null)
                {
                    if (ownsTransaction)
                    {
                        _database.Database.Rollback();
                    }

                    return inventoryResult;
                }

                _database.Orders.Upsert(order);
                _database.CheckoutTransactions.Delete(command.TransactionId);

                if (ownsTransaction)
                {
                    _database.Database.Commit();
                }
            }
            catch
            {
                if (ownsTransaction)
                {
                    _database.Database.Rollback();
                }

                throw;
            }

            try
            {
                var productEvent = ProductOrderEventFactory.CreateCompletedEvent(_shopManifest, order, completedAt);
                _productService.HandleOrderCompleted(productEvent);
                order.FulfillmentStatus = OrderFulfillmentStatus.Succeeded;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[checkout] product fulfillment failed for order {order.Id}: {ex}");
                order.FulfillmentStatus = OrderFulfillmentStatus.Failed;
            }

            _database.Orders.Upsert(order);

            return CheckoutCompleteResult.CreateSucceeded(
                command.TransactionId,
                command.PaymentId,
                completedAt,
                command.RequestMember.Id,
                command.RequestMember.Name,
                order);
        }

        private CheckoutCompleteResult? ValidateAndDeductInventory(
            IReadOnlyDictionary<string, int> inventoryRequirements,
            DateTime updatedAt)
        {
            if (inventoryRequirements.Count == 0)
            {
                return null;
            }

            var inventoryRecords = new Dictionary<string, InventoryRecord>(StringComparer.OrdinalIgnoreCase);

            foreach (var requirement in inventoryRequirements)
            {
                var inventoryRecord = _database.InventoryRecords
                    .Query()
                    .Where(x => x.SkuId == requirement.Key)
                    .ForUpdate()
                    .FirstOrDefault();

                if (inventoryRecord == null)
                {
                    return CheckoutCompleteResult.CreateInventoryInsufficient($"Inventory record not found for sku {requirement.Key}");
                }

                if (inventoryRecord.AvailableQuantity < requirement.Value)
                {
                    return CheckoutCompleteResult.CreateInventoryInsufficient(
                        $"Inventory is insufficient for sku {requirement.Key}. required={requirement.Value}, available={inventoryRecord.AvailableQuantity}");
                }

                inventoryRecords[requirement.Key] = inventoryRecord;
            }

            foreach (var requirement in inventoryRequirements)
            {
                var inventoryRecord = inventoryRecords[requirement.Key];
                inventoryRecord.AvailableQuantity -= requirement.Value;
                inventoryRecord.UpdatedAt = updatedAt;
                _database.InventoryRecords.Update(inventoryRecord);
            }

            return null;
        }

        private static string? NormalizeSkuId(string? skuId)
        {
            return string.IsNullOrWhiteSpace(skuId) ? null : skuId;
        }
    }
}
