using System.Text.Json.Serialization;

namespace AndrewDemo.NetConf2023.Core
{
    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // not implement password in this demo, just for demo
        // any non-empty string is valid
        public static string Login(string name, string password)
        {
            var m = _database.Where(x => x.Value.Name == name).Select(x => x.Value).FirstOrDefault();
            if (m == null) return null;

            // ignore password
            //if (string.IsNullOrEmpty(password)) return null;

            return CreateAccessToken(m);
        }

        /// <summary>
        /// 註冊成功會傳回 access token
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string Register(string name)
        {
            if ((from x in _database where x.Value.Name == name select x.Value).Any())
            {
                return null;
            }

            var m = new Member()
            {
                Id = _current_number++,
                Name = name
            };

            MemberRegistered?.Invoke(m, new EventArgs() { });

            _database.Add(m.Id, m);
            return CreateAccessToken(m);
        }

        public static Member GetCurrentMember(string accessToken)
        {
            // access token validation
            if (AccessTokens.ContainsKey(accessToken))
            {
                var (expire, consumer) = AccessTokens[accessToken];
                if (expire > DateTime.Now)
                {
                    return consumer;
                }
            }

            return null;
        }

        public static event EventHandler<EventArgs> MemberRegistered;
        public static event EventHandler<EventArgs> MemberLoggedIn;

        private static int _current_number = 1;
        private static Dictionary<int, Member> _database = new Dictionary<int, Member>()
        {
            //{ 1, new Member() { Id = 1, Name = "andrew" } },
            //{ 2, new Member() { Id = 2, Name = "poy"} }
        };

        //[Obsolete("member: cross model data access!")]
        private static Dictionary<int, Member> Database { get { return _database; } }

        //
        private static Dictionary<string, (DateTime expire, Member consumer)> AccessTokens = new Dictionary<string, (DateTime expire, Member consumer)>();

        private static string CreateAccessToken(Member consumer)
        {
            string token = Guid.NewGuid().ToString("N");
            AccessTokens.Add(token, (DateTime.MaxValue, consumer));

            return token;
        }
    }


    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description {  get; set; }
        public decimal Price { get; set; }





        private static Dictionary<int, Product> _database = new Dictionary<int, Product>()
        {
            //{ 1, new Product() { Id = 1, Name = "18天", Price = 65.00m } },
            //{ 2, new Product() { Id = 2, Name = "可樂", Price = 18.00m} }
        };

        [Obsolete("product: cross model data access!")]
        public static Dictionary<int, Product> Database { get { return _database; } }
    }


    internal class DiscountEngine
    {
        public static IEnumerable<DiscountRecord> Calculate(Cart cart, Member consumer)
        {
            // 18天(ID: 1) 第二罐六折
            //var p = Product.Database[1];//.Where(p => p.Value.Id.Equals(1)).FirstOrDefault().Value;
            var pid = 1;

            //if (cart.ProdQtyMap.ContainsKey(pid) && cart.ProdQtyMap[pid] > 2)
            var lineitem = cart.LineItems.Where(lt => (lt.ProductId == pid && lt.Qty > 2)).FirstOrDefault();

            if (lineitem != null)
            {
                var p = Product.Database[lineitem.ProductId];
                for (int index = 1; index <= lineitem.Qty; index++)
                {
                    if (index % 2 == 0) yield return new DiscountRecord()
                    {
                        Name = $"{p.Name} 第二件六折",
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
        private Dictionary<int, int> ProdQtyMap = new Dictionary<int, int>();

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
            foreach (var lineitem in this.LineItems)
            {
                Product p = Product.Database[lineitem.ProductId];
                Console.WriteLine($"- [{p.Id}] {p.Name}(單價: ${p.Price}) x {lineitem.Qty},     ${p.Price * lineitem.Qty}");
                total += p.Price * lineitem.Qty;
            }
            foreach (var discount in this.EstimateDiscounts())
            {
                Console.WriteLine($"- [優惠] {discount.Name},   ${discount.DiscountAmount}");
                total += discount.DiscountAmount;
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

        public IEnumerable<CartDiscountHint> EstimateDiscounts()
        {
            {
                foreach (var d in DiscountEngine.Calculate(this, null))
                {
                    yield return new CartDiscountHint()
                    {
                        Name = d.Name,
                        Description = $"[{d.Name}]: ${d.DiscountAmount}",
                        DiscountAmount = d.DiscountAmount
                    };
                }
            }
        }

        public class CartLineItem
        {
            public int ProductId { get; set; }
            public int Qty { get; set; }
        }

        public class CartDiscountHint
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal DiscountAmount { get; set; }
        }
    }


    public class Checkout
    {
        private static int _serial_number = 1;
        private static Dictionary<int, (int tid, Cart cart, Member consumer)> _database = new Dictionary<int, (int tid, Cart cart, Member consumer)>();

        public static int Create(int cartId, string token)
        {
            var cart = Cart.Get(cartId);
            if (cart == null) throw new ArgumentOutOfRangeException("cartId");

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
            Console.WriteLine($"[checkout] check system status, please wait ...");
            await ticket.WaitUntilCanRunAsync();
            Console.WriteLine($"[checkout] checkout process start...");


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