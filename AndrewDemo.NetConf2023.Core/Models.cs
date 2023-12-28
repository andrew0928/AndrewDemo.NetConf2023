using System.Text.Json.Serialization;

namespace AndrewDemo.NetConf2023.Core
{
    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public static Member Login(string name, string password)
        {
            // ignore password
            var m = _database.Where(x => x.Value.Name == name).Select(x => x.Value).FirstOrDefault();

            if (m != null)
            {
                MemberLoggedIn?.Invoke(m, new EventArgs() { });
                return m;
            }

            return null;
        }

        public static (bool result, int registeredMemberId) Register(string name)
        {
            if ((from x in _database where x.Value.Name == name select x.Value).Any())
            {
                return (false, 0);
            }

            var m = new Member()
            {
                Id = _current_number++,
                Name = name
            };

            MemberRegistered?.Invoke(m, new EventArgs() { });

            _database.Add(m.Id, m);
            return (true, m.Id);
        }

        public static event EventHandler<EventArgs> MemberRegistered;
        public static event EventHandler<EventArgs> MemberLoggedIn;

        private static int _current_number = 3;
        private static Dictionary<int, Member> _database = new Dictionary<int, Member>()
        {
            { 1, new Member() { Id = 1, Name = "andrew" } },
            { 2, new Member() { Id = 2, Name = "poy"} }
        };

        [Obsolete("member: cross model data access!")]
        public static Dictionary<int, Member> Database { get { return _database; } }
    }


    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }





        private static Dictionary<int, Product> _database = new Dictionary<int, Product>()
        {
            { 1, new Product() { Id = 1, Name = "18天", Price = 65.00m } },
            { 2, new Product() { Id = 2, Name = "可樂", Price = 18.00m} }
        };

        [Obsolete("product: cross model data access!")]
        public static Dictionary<int, Product> Database { get { return _database; } }
    }


    //public class SKU
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //}


    internal class DiscountEngine
    {
        public static IEnumerable<DiscountRecord> Calculate(Cart cart, Member consumer)
        {
            // 18天 第二罐六折
            var p = Product.Database.Where(p => p.Value.Name.Equals("18天")).FirstOrDefault().Value;
            var pid = p.Id;

            if (cart.ProdQtyMap.ContainsKey(pid) && cart.ProdQtyMap[pid] > 2)
            {
                for (int index = 1; index <= cart.ProdQtyMap[pid]; index++)
                {
                    if (index % 2 == 0) yield return new DiscountRecord()
                    {
                        Name = "18天 第二件六折優惠",
                        DiscountAmount = p.Price * -0.4m,
                    };
                }
            }
        }

        public class DiscountRecord
        {
            public string Name { get; set; }
            public decimal DiscountAmount { get; set; }
        }
    }


    public class Cart
    {
        internal Dictionary<int, int> ProdQtyMap = new Dictionary<int, int>();

        public int Id { get; private set; }

        private static int _sn = 1;
        internal static Dictionary<int, Cart> _database = new Dictionary<int, Cart>();

        public static Cart Create()
        {
            var c = new Cart()
            {
                Id = _sn++
            };

            _database.Add(c.Id, c);

            return c;
        }

        private Cart()
        {
        }

        public static Cart Get(int id)
        {
            if (!_database.ContainsKey(id)) return null;
            return _database[id];
        }


        // sku CRUD
        public bool AddProducts(int productId, int qty = 1)
        {
            if (this.ProdQtyMap.ContainsKey(productId))
            {
                this.ProdQtyMap[productId] += qty;
            }
            else
            {
                this.ProdQtyMap[productId] = qty;
            }
            return true;
        }

        public decimal EstimatePrice()
        {
            decimal total = 0m;
            foreach (var ci in this.ProdQtyMap)
            {
                Product p = Product.Database[ci.Key];
                int pid = p.Id;
                int qty = ci.Value;
                Console.WriteLine($"- [{pid}] {p.Name}(單價: ${p.Price}) x {qty},     ${p.Price * qty}");
                total += p.Price * qty;
            }
            foreach (var dr in DiscountEngine.Calculate(this, null))
            {
                Console.WriteLine($"- [優惠] {dr.Name},   ${dr.DiscountAmount}");
                total += dr.DiscountAmount;
            }

            return total;
        }

        public IEnumerable<CartLineItem> LineItems
        {
            get 
            {
                foreach (var ci in this.ProdQtyMap)
                {
                    yield return new CartLineItem()
                    {
                        ProductId = ci.Key,
                        Qty = ci.Value
                    };
                }
            }
        }

        public class CartLineItem
        {
            public int ProductId { get; set; }
            public int Qty { get; set; }
        }
    }


    public class Checkout
    {
        private static int _serial_number = 1;
        private static Dictionary<int, (int tid, Cart cart, Member consumer)> _database = new Dictionary<int, (int tid, Cart cart, Member consumer)>();

        public static int Create(int cartId, Member consumer)
        {
            var cart = Cart.Get(cartId);
            if (cart == null) throw new ArgumentOutOfRangeException("cartId");

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
            Console.WriteLine($"[checkout] check system status, please wait ...");
            await ticket.WaitUntilCanRunAsync();
            Console.WriteLine($"[checkout] checkout process start...");


            var transaction = _database[transactionId];
            _database.Remove(transactionId);
            var order = new Order(transactionId);

            order.buyer = transaction.consumer;

            decimal total = 0m;

            foreach (var p in transaction.cart.ProdQtyMap)
            {
                Product product = Product.Database[p.Key];
                int qty = p.Value;
                total += product.Price * qty;

                order.LineItems.Add(new Order.OrderLineItem()
                {
                    Title = $"商品: {product.Name}, 單價: {product.Price} x {qty} 件 = {product.Price * qty:C}",
                    Price = product.Price * qty
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

            


            Console.WriteLine($"[checkout] checkout process complete... order created({order.Id})");
            Console.WriteLine();

            CheckoutCompleted?.Invoke(order, new EventArgs());

            return order;
        }
    }




    // ref: https://queue-it.com/how-does-queue-it-work/
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

            Console.WriteLine($"[waiting-room] issue ticket: {this.Id} @ {this._created} (estimate: {this._released})");
        }

        public async Task WaitUntilCanRunAsync()
        {
            if (DateTime.Now > this._released) return;
            await Task.Delay(this._released - DateTime.Now);
        }
    }




    public class Order
    {
        public static Dictionary<int, Order> _database = new Dictionary<int, Order>();

        public Order(int transactionId)
        {
            this.Id = transactionId;
            _database.Add(transactionId, this);
        }



        public int Id { get; private set; }

        public Member buyer { get; set; }

        public List<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();

        public decimal TotalPrice { get; set; }


        public static IEnumerable<Order> GetOrders(int memberId)
        {
            return _database.Values.Where(x => x.buyer.Id == memberId);
        }


        public class OrderLineItem
        {
            public string Title { get; set; }
            public decimal Price { get; set; }
        }
    }
}