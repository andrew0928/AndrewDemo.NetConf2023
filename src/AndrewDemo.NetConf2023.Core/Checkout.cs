using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AndrewDemo.NetConf2023.Core
{
    public class Checkout
    {
        [Obsolete("請改用 IShopDatabaseContext.CheckoutTransactions 直接管理交易紀錄。")]
        public static int Create(int cartId, string token)
        {
            var cart = Cart.Get(cartId);
            if (cart == null) throw new ArgumentOutOfRangeException("cartId");

            if (cart.LineItems?.Count() == 0) throw new InvalidOperationException("cart is empty.");

            var consumer = Member.GetCurrentMember(token);
            if (consumer == null) throw new ArgumentOutOfRangeException("token");

            var transaction = new CheckoutTransactionRecord()
            {
                CartId = cart.Id,
                MemberId = consumer.Id,
                CreatedAt = DateTime.UtcNow
            };

            ShopDatabase.Current.CheckoutTransactions.Insert(transaction);
            return transaction.TransactionId;
        }

        public static event EventHandler? CheckoutCompleted;

        [Obsolete("請改用自訂服務搭配 IShopDatabaseContext 完成結帳流程。")]
        public static async Task<Order> CompleteAsync(int transactionId, int paymentId, int satisfaction = 0, string? comments = null)
        {
            // 這邊要處理:
            // 1. 分散式交易
            // 2. 排隊機制
            // 3. 服務水準偵測、預警、監控
            // 4. 整體負載控制
            // 因此改用 async 來模擬，呼叫端必須有 async 的接收能力，即使透過 API (ex: 用 webhook / notification 也要能配合)

            // 模擬排隊機制, 避免瞬間結帳人數過多衝垮後端系統
            var ticket = new WaitingRoomTicket();
            //Console.WriteLine($"[checkout] check system status, please wait ...");
            await ticket.WaitUntilCanRunAsync();
            //Console.WriteLine($"[checkout] checkout process start...");


            var transaction = ShopDatabase.Current.CheckoutTransactions.FindById(transactionId);
            if (transaction == null)
            {
                throw new ArgumentOutOfRangeException(nameof(transactionId));
            }

            ShopDatabase.Current.CheckoutTransactions.Delete(transactionId);

            var cart = Cart.Get(transaction.CartId) ?? throw new InvalidOperationException($"cart {transaction.CartId} not found");
            var consumer = ShopDatabase.Current.Members.FindById(transaction.MemberId) ?? throw new InvalidOperationException($"member {transaction.MemberId} not found");

            var order = new Order(transactionId)
            {
                Buyer = consumer
            };

            decimal total = 0m;

            //foreach (var p in transaction.cart.ProdQtyMap)
            foreach (var lineitem in cart.LineItems)
            {
                var product = Product.GetById(lineitem.ProductId);
                if (product == null)
                {
                    throw new InvalidOperationException($"product {lineitem.ProductId} not found");
                }
                
                total += product.Price * lineitem.Qty;

                order.LineItems.Add(new Order.OrderLineItem()
                {
                    Title = $"商品: {product.Name}, 單價: {product.Price} x {lineitem.Qty} 件 = {product.Price * lineitem.Qty:C}",
                    Price = product.Price * lineitem.Qty
                });
            }

            foreach (var dr in DiscountEngine.Calculate(cart, consumer))
            {
                order.LineItems.Add(new Order.OrderLineItem()
                {
                    Title = $"優惠: {dr.Name}, 折扣 {-1 * dr.DiscountAmount:C}",
                    Price = dr.DiscountAmount
                });

                total += dr.DiscountAmount;
            }

            order.TotalPrice = total;

            order.ShopNotes = new Order.OrderShopNotes()
            {
                BuyerSatisfaction = satisfaction,
                Comments = comments
            };

            Order.Upsert(order);


            //Console.WriteLine($"[checkout] checkout process complete... order created({order.Id})");
            //Console.WriteLine();

            CheckoutCompleted?.Invoke(order, EventArgs.Empty);

            return order;
        }
    }


    public class WaitingRoomTicket
    {
        private static int _sn = 0;

        public int Id { get; private set; }
        private DateTime _created = DateTime.MinValue;
        private DateTime _released = DateTime.MinValue;

        public WaitingRoomTicket()
        {
            this.Id = Interlocked.Increment(ref _sn);
            this._created = DateTime.Now;

            Random random = new Random();
            this._released = this._created + TimeSpan.FromSeconds(random.Next(1, 3));

            //Console.WriteLine($"[waiting-room] issue ticket: {this.Id} @ {this._created} (estimate: {this._released})");
        }

        public async Task WaitUntilCanRunAsync()
        {
            if (DateTime.Now > this._released) return;
            await Task.Delay(this._released - DateTime.Now);
        }
    }

}