using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Core.Products;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class CheckoutServiceTests : ShopDatabaseTestBase
    {
        [Fact]
        public void Create_WithExistingCart_CreatesCheckoutTransaction()
        {
            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts("1", 1, FixedUtcNow);
            Context.Carts.Insert(cart);

            var service = CreateCheckoutService();

            var result = service.Create(new CheckoutCreateCommand
            {
                CartId = cart.Id,
                PaymentId = 9527,
                RequestMember = member
            });

            Assert.Equal(CheckoutCreateStatus.Succeeded, result.Status);
            Assert.NotEqual(0, result.TransactionId);
            Assert.Equal(member.Id, result.ConsumerId);

            var transaction = Context.CheckoutTransactions.FindById(result.TransactionId);
            Assert.NotNull(transaction);
            Assert.Equal(cart.Id, transaction!.CartId);
            Assert.Equal(member.Id, transaction.MemberId);
            Assert.Equal(9527, transaction.PaymentId);
        }

        [Fact]
        public async Task CompleteAsync_WithExistingTransaction_CreatesOrderAndDeletesTransaction()
        {
            Context.Products.Upsert(new Product
            {
                Id = "1",
                Name = "Test Beer",
                Price = 50m,
                IsPublished = true
            });

            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts("1", 2, FixedUtcNow);
            Context.Carts.Insert(cart);

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow,
                PaymentId = 9527
            };
            Context.CheckoutTransactions.Insert(transaction);

            var service = CreateCheckoutService();

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = transaction.TransactionId,
                Satisfaction = 9,
                ShopComments = "checkout ok",
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
            Assert.Equal(transaction.TransactionId, result.TransactionId);
            Assert.Equal(9527, result.PaymentId);
            Assert.NotNull(result.OrderDetail);
            Assert.Equal(OrderFulfillmentStatus.Succeeded, result.OrderDetail!.FulfillmentStatus);
            Assert.Equal(80m, result.OrderDetail.TotalPrice);
            Assert.Single(result.OrderDetail.ProductLines);
            Assert.Single(result.OrderDetail.DiscountLines);
            Assert.Null(Context.CheckoutTransactions.FindById(transaction.TransactionId));

            var storedOrder = Context.Orders.FindById(transaction.TransactionId);
            Assert.NotNull(storedOrder);
            Assert.Equal(OrderFulfillmentStatus.Succeeded, storedOrder!.FulfillmentStatus);
        }

        [Fact]
        public async Task CompleteAsync_WhenSatisfactionIsNotProvided_PersistsNullShopSatisfaction()
        {
            Context.Products.Upsert(new Product
            {
                Id = "1",
                Name = "Test Beer",
                Price = 50m,
                IsPublished = true
            });

            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts("1", 1, FixedUtcNow);
            Context.Carts.Insert(cart);

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow,
                PaymentId = 9527
            };
            Context.CheckoutTransactions.Insert(transaction);

            var service = CreateCheckoutService();

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = transaction.TransactionId,
                Satisfaction = null,
                ShopComments = null,
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
            Assert.NotNull(result.OrderDetail);
            Assert.NotNull(result.OrderDetail!.ShopNotes);
            Assert.Null(result.OrderDetail.ShopNotes!.BuyerSatisfaction);
            Assert.Null(result.OrderDetail.ShopNotes.Comments);

            var storedOrder = Context.Orders.FindById(transaction.TransactionId);
            Assert.NotNull(storedOrder);
            Assert.NotNull(storedOrder!.ShopNotes);
            Assert.Null(storedOrder.ShopNotes!.BuyerSatisfaction);
            Assert.Null(storedOrder.ShopNotes.Comments);
        }

        [Fact]
        public async Task CompleteAsync_WhenProductMissing_KeepsTransactionForRetry()
        {
            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts("missing-product", 1, FixedUtcNow);
            Context.Carts.Insert(cart);

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow,
                PaymentId = 9527
            };
            Context.CheckoutTransactions.Insert(transaction);

            var service = CreateCheckoutService();

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = transaction.TransactionId,
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.ProductNotFound, result.Status);
            Assert.NotNull(Context.CheckoutTransactions.FindById(transaction.TransactionId));
            Assert.Null(Context.Orders.FindById(transaction.TransactionId));
        }

        [Fact]
        public async Task CompleteAsync_WhenBuyerDoesNotMatch_ReturnsBuyerMismatchAndKeepsTransaction()
        {
            Context.Products.Upsert(new Product
            {
                Id = "1",
                Name = "Test Beer",
                Price = 50m,
                IsPublished = true
            });

            var (buyer, _) = TestDataFactory.RegisterMember(Context);
            var (otherMember, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts("1", 1, FixedUtcNow);
            Context.Carts.Insert(cart);

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = buyer.Id,
                CreatedAt = DateTime.UtcNow,
                PaymentId = 9527
            };
            Context.CheckoutTransactions.Insert(transaction);

            var service = CreateCheckoutService();

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = transaction.TransactionId,
                RequestMember = otherMember
            });

            Assert.Equal(CheckoutCompleteStatus.BuyerMismatch, result.Status);
            Assert.NotNull(Context.CheckoutTransactions.FindById(transaction.TransactionId));
            Assert.Null(Context.Orders.FindById(transaction.TransactionId));
        }

        [Fact]
        public async Task CompleteAsync_WithStockTrackedProduct_DeductsInventoryWithinCheckout()
        {
            var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 120m, availableQuantity: 5);
            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts(productId, 2, FixedUtcNow);
            Context.Carts.Insert(cart);

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow,
                PaymentId = 9527
            };
            Context.CheckoutTransactions.Insert(transaction);

            var service = CreateCheckoutService();

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = transaction.TransactionId,
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
            Assert.NotNull(result.OrderDetail);
            Assert.Equal(skuId, result.OrderDetail!.ProductLines[0].SkuId);
            Assert.Equal(3, Context.InventoryRecords.FindById(skuId)!.AvailableQuantity);
            Assert.Null(Context.CheckoutTransactions.FindById(transaction.TransactionId));
        }

        [Fact]
        public async Task CompleteAsync_WhenStockIsInsufficient_KeepsTransactionAndDoesNotCreateOrder()
        {
            var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 120m, availableQuantity: 1);
            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts(productId, 2, FixedUtcNow);
            Context.Carts.Insert(cart);

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow,
                PaymentId = 9527
            };
            Context.CheckoutTransactions.Insert(transaction);

            var service = CreateCheckoutService();

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = transaction.TransactionId,
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.InventoryInsufficient, result.Status);
            Assert.NotNull(Context.CheckoutTransactions.FindById(transaction.TransactionId));
            Assert.Null(Context.Orders.FindById(transaction.TransactionId));
            Assert.Equal(1, Context.InventoryRecords.FindById(skuId)!.AvailableQuantity);
        }

        [Fact]
        public async Task CompleteAsync_WhenRuleReturnsHint_DoesNotAffectTotalOrPersistDiscountLines()
        {
            Context.Products.Upsert(new Product
            {
                Id = "1",
                Name = "Test Beer",
                Price = 50m,
                IsPublished = true
            });

            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts("1", 1, FixedUtcNow);
            Context.Carts.Insert(cart);

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow,
                PaymentId = 9527
            };
            Context.CheckoutTransactions.Insert(transaction);

            var service = CreateCheckoutService(new HintOnlyDiscountRule());

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = transaction.TransactionId,
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
            Assert.NotNull(result.OrderDetail);
            Assert.Equal(50m, result.OrderDetail!.TotalPrice);
            Assert.Empty(result.OrderDetail.DiscountLines);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CompleteAsync_WhenStoredPaymentIsMissing_RequiresRecreationWithoutEffects(bool hasNullPayment)
        {
            var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 100m, 10);
            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts(productId, 2, FixedUtcNow);
            Context.Carts.Insert(cart);
            var document = new LiteDB.BsonDocument
            {
                ["_id"] = 42,
                ["CartId"] = cart.Id,
                ["MemberId"] = member.Id,
                ["CreatedAt"] = FixedUtcNow
            };
            if (hasNullPayment) document["PaymentId"] = LiteDB.BsonValue.Null;
            Context.Database.GetCollection("checkout_transactions").Insert(document);

            var result = await CreateCheckoutService().CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = 42,
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.RecreateRequired, result.Status);
            Assert.Null(Context.ArchivedCheckoutTransactions.FindById(42));
            Assert.Empty(Context.Orders.FindAll());
            Assert.Equal(10, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
            Assert.Single(Context.Carts.FindById(cart.Id).LineItems);
            var stored = Context.Database.GetCollection("checkout_transactions").FindById(42);
            Assert.NotNull(stored);
            Assert.Equal(hasNullPayment, stored.ContainsKey("PaymentId"));
            if (hasNullPayment) Assert.True(stored["PaymentId"].IsNull);
        }

        [Fact]
        public async Task CreateAndComplete_WithExplicitZeroPayment_PreservesItAsProvidedValue()
        {
            var productId = TestDataFactory.CreateProduct(Context, 100m);
            var (member, _) = TestDataFactory.RegisterMember(Context);
            var cart = new Cart();
            cart.AddProducts(productId, 1, FixedUtcNow);
            Context.Carts.Insert(cart);
            var service = CreateCheckoutService();
            var created = service.Create(new CheckoutCreateCommand
            {
                CartId = cart.Id,
                PaymentId = 0,
                RequestMember = member
            });

            var result = await service.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = created.TransactionId,
                RequestMember = member
            });

            Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
            Assert.Equal(0, result.PaymentId);
        }

        private CheckoutService CreateCheckoutService(params IDiscountRule[] rules)
        {
            var enabledRules = rules.Length == 0
                ? new IDiscountRule[] { new Product1SecondItemDiscountRule() }
                : rules;

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                ProductServiceId = DefaultProductService.ServiceId,
                OrderEventDispatcherId = DefaultOrderEventDispatcher.DispatcherId,
                EnabledDiscountRuleIds = enabledRules.Select(x => x.RuleId).ToList()
            };

            return new CheckoutService(
                Context,
                new DiscountEngine(enabledRules),
                new DefaultOrderEventDispatcher(),
                new DefaultProductService(Context),
                manifest,
                FixedTimeProvider);
        }

        private sealed class HintOnlyDiscountRule : IDiscountRule
        {
            public string RuleId => "test-hint-rule";

            public int Priority => 10;

            public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
            {
                return new List<DiscountRecord>
                {
                    new DiscountRecord
                    {
                        RuleId = RuleId,
                        Kind = DiscountRecordKind.Hint,
                        Name = "BTS 提示",
                        Description = "目前不符合折扣條件",
                        Amount = 0m,
                        RelatedLineIds = context.LineItems.Select(x => x.LineId).ToList()
                    }
                };
            }
        }
    }
}
