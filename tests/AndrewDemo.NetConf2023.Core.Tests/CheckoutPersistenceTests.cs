using System;
using System.Linq;
using System.Threading.Tasks;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class CheckoutPersistenceTests : ShopDatabaseTestBase
    {
        [Fact]
        public async Task CompleteAsync_CreatesOrderAndClearsTransaction()
        {
            decimal price = 75m;
            int productId = TestDataFactory.CreateProduct(price);
            var (member, token) = TestDataFactory.RegisterMember();

            var cart = ShopDatabase.Create(new Cart());
            cart.AddProducts(productId, 2);

            var tokenRecord = ShopDatabase.Current.MemberTokens.FindById(token) ?? throw new InvalidOperationException("token missing");

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = tokenRecord.MemberId,
                CreatedAt = DateTime.UtcNow
            };

            ShopDatabase.Create(transaction);
            int transactionId = transaction.TransactionId;

#pragma warning disable 612, 618
            var order = await Checkout.CompleteAsync(transactionId, paymentId: 123, satisfaction: 8, comments: "ok");
#pragma warning restore 612, 618
            Assert.NotNull(order);
            Assert.Equal(member.Id, order.Buyer.Id);
            Assert.Equal(price * 2, order.TotalPrice);

            var orders = ShopDatabase.Current.Orders.Find(o => o.Buyer.Id == member.Id).ToList();
            Assert.Contains(orders, o => o.Id == order.Id);

#pragma warning disable 612, 618
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Checkout.CompleteAsync(transactionId, paymentId: 123, satisfaction: 0, comments: null));
#pragma warning restore 612, 618
        }
    }
}
