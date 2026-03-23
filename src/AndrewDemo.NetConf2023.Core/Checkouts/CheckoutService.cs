using System;
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

            foreach (var lineitem in cart.LineItems)
            {
                var product = _productService.GetProductById(lineitem.ProductId);
                if (product == null)
                {
                    return CheckoutCompleteResult.CreateProductNotFound($"Product {lineitem.ProductId} not found");
                }

                total += product.Price * lineitem.Quantity;

                order.ProductLines.Add(new Order.OrderProductLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = lineitem.Quantity,
                    LineAmount = product.Price * lineitem.Quantity
                });
            }

            var discountContext = CartContextFactory.Create(_shopManifest, cart, consumer, _productService);
            foreach (var discount in _discountEngine.Evaluate(discountContext))
            {
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

            _database.Orders.Upsert(order);
            _database.CheckoutTransactions.Delete(command.TransactionId);

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
    }
}
