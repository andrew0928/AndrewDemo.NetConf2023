using System;
using System.Linq;
using System.Threading.Tasks;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class CheckoutPersistenceTests
    {
        [Fact]
        public async Task CompleteAsync_CreatesOrderAndClearsTransaction()
        {
            decimal price = 75m;
            int productId = TestDataFactory.CreateProduct(price);
            var (member, token) = TestDataFactory.RegisterMember();

            var cart = Cart.Create();
            cart.AddProducts(productId, 2);

            int transactionId = Checkout.Create(cart.Id, token);

            var order = await Checkout.CompleteAsync(transactionId, paymentId: 123, satisfaction: 8, comments: "ok");
            Assert.NotNull(order);
            Assert.Equal(member.Id, order.Buyer.Id);
            Assert.Equal(price * 2, order.TotalPrice);

            var orders = Order.GetOrders(member.Id).ToList();
            Assert.Contains(orders, o => o.Id == order.Id);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Checkout.CompleteAsync(transactionId, paymentId: 123, satisfaction: 0, comments: null));
        }
    }
}
