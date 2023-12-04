﻿namespace AndrewDemo.NetConf2023.Core
{
    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public static Member Login(string name, string password)
        {
            // ignore password
            return _database.Where(x => x.Value.Name == name).Select(x => x.Value).FirstOrDefault();
        }

        public static Member Register(string name)
        {
            throw new NotImplementedException();
        }

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
        public Dictionary<int, int> ProdQtyMap = new Dictionary<int, int>();

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
                //Console.WriteLine($"- [{pid}] {p.Name}(單價: ${p.Price}) x {qty},     ${p.Price * qty}");
                total += p.Price * qty;
            }
            foreach (var dr in DiscountEngine.Calculate(this, null))
            {
                Console.WriteLine($"- [優惠] {dr.Name},   ${dr.DiscountAmount}");
                total += dr.DiscountAmount;
            }

            return total;
        }
    }


    public class Checkout
    {
        private static int _serial_number = 1;
        private static Dictionary<int, (int tid, Cart cart, Member consumer)> _temp = new Dictionary<int, (int tid, Cart cart, Member consumer)>();

        public static int Create(Cart cart, Member consumer)
        {
            int tid = (_serial_number++);
            _temp.Add(tid, (tid, cart, consumer));
            return tid;
        }

        // confirm info / shipping / payment

        public static async Task<Order> CompleteAsync(int transactionId, int paymentId)
        {
            // 這邊要處理:
            // 1. 分散式交易
            // 2. 排隊機制
            // 3. 服務水準偵測、預警、監控
            // 4. 整體負載控制
            // 因此改用 async 來模擬，呼叫端必須有 async 的接收能力，即使透過 API (ex: 用 webhook / notification 也要能配合)

            var order = new Order();
            var result = Task.Delay(1);

            var transaction = _temp[transactionId];
            order.buyer = transaction.consumer;

            decimal total = 0m;


            foreach (var p in transaction.cart.ProdQtyMap)
            {
                Product product = Product.Database[p.Key];
                int qty = p.Value;
                total += product.Price * qty;

                order.LineItems.Add((
                    $"商品: {product.Name} (單價: {product.Price} x {qty} = {product.Price * qty}",
                    product.Price * qty));
            }

            foreach (var dr in DiscountEngine.Calculate(transaction.cart, transaction.consumer))
            {
                order.LineItems.Add((
                    $"優惠: {dr.Name} {dr.DiscountAmount}",
                    dr.DiscountAmount));
                total += dr.DiscountAmount;
            }

            order.TotalPrice = total;
            order.Id = transactionId;

            await result;
            return order;
        }
    }


    public class Order
    {
        public int Id { get; set; }

        public Member buyer;

        public List<(string title, decimal price)> LineItems = new List<(string title, decimal price)>();

        public decimal TotalPrice { get; set; }
    }
}