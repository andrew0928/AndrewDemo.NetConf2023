namespace AndrewDemo.NetConf2023.Core
{
    public class Checkout
    {
        private static int _serial_number = 1;
        private static Dictionary<int, (int tid, Cart cart, Member consumer)> _database = new Dictionary<int, (int tid, Cart cart, Member consumer)>();

        public static int Create(int cartId, string token)
        {
            var cart = Cart.Get(cartId);
            if (cart == null) throw new ArgumentOutOfRangeException("cartId");

            if (cart.LineItems?.Count() == 0) throw new InvalidOperationException("cart is empty.");

            var consumer = Member.GetCurrentMember(token);
            if (consumer == null) throw new ArgumentOutOfRangeException("token");

            int tid = (_serial_number++);
            _database.Add(tid, (tid, cart, consumer));
            return tid;
        }

        public static event EventHandler CheckoutCompleted;

        public static async Task<Order> CompleteAsync(int transactionId, int paymentId)
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


            var transaction = _database[transactionId];
            _database.Remove(transactionId);
            var order = new Order(transactionId);

            order.buyer = transaction.consumer;

            decimal total = 0m;

            //foreach (var p in transaction.cart.ProdQtyMap)
            foreach (var lineitem in transaction.cart.LineItems)
            {
                Product product = Product.Database[lineitem.ProductId];
                
                total += product.Price * lineitem.Qty;

                order.LineItems.Add(new Order.OrderLineItem()
                {
                    Title = $"商品: {product.Name}, 單價: {product.Price} x {lineitem.Qty} 件 = {product.Price * lineitem.Qty:C}",
                    Price = product.Price * lineitem.Qty
                });
            }

            foreach (var dr in DiscountEngine.Calculate(transaction.cart, transaction.consumer))
            {
                order.LineItems.Add(new Order.OrderLineItem()
                {
                    Title = $"優惠: {dr.Name}, 折扣 {-1 * dr.DiscountAmount:C}",
                    Price = dr.DiscountAmount
                });

                total += dr.DiscountAmount;
            }

            order.TotalPrice = total;

            


            //Console.WriteLine($"[checkout] checkout process complete... order created({order.Id})");
            //Console.WriteLine();

            CheckoutCompleted?.Invoke(order, new EventArgs());

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