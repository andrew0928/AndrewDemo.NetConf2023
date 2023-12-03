namespace AndrewDemo.NetConf2023
{

    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }


        public static Dictionary<int, Member> Database = new Dictionary<int, Member>()
        {
            { 1, new Member() { Id = 1, Name = "andrew" } },
            { 2, new Member() { Id = 2, Name = "poy"} }
        };
    }


    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public static Dictionary<int, Product> Database = new Dictionary<int, Product>()
        {
            { 1, new Product() { Id = 1, Name = "18天", Price = 65.00m } },
            { 2, new Product() { Id = 2, Name = "可樂", Price = 18.00m} }
        };
    }


    //public class SKU
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //}


    public class DiscountEngine
    {
        public static IEnumerable<DiscountRecord> Calculate(Cart cart, Member consumer)
        {
            // 18天 第二罐六折
            var p = Product.Database.Where(p => p.Value.Name.Equals("18天")).FirstOrDefault().Value;
            var pid = p.Id;

            if (cart._ProdQtyMap.ContainsKey(pid) && cart._ProdQtyMap[pid] > 2)
            {
                for(int index = 1; index <= cart._ProdQtyMap[pid]; index++)
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
        public Dictionary<int, int> _ProdQtyMap = new Dictionary<int, int>();

        // sku CRUD
        public bool AddProducts(int productId, int qty = 1)
        {
            if (this._ProdQtyMap.ContainsKey(productId))
            {
                this._ProdQtyMap[productId] += qty;
            }
            else
            {
                this._ProdQtyMap[productId] = qty;
            }
            return true;
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

        public static Order CompleteWithPayment(int transactionId, int paymentId)
        { 
            var order = new Order();

            var transaction = _temp[transactionId];
            order.buyer = transaction.consumer;

            decimal total = 0m;
            foreach(var p in transaction.cart._ProdQtyMap)
            {
                Product product = Product.Database[p.Key];
                int qty = p.Value;
                total += product.Price * qty;

                order.LineItems.Add((
                    $"商品: {product.Name} (單價: {product.Price} x {qty} = {product.Price * qty}",
                    product.Price * qty));
            }

            foreach(var dr in DiscountEngine.Calculate(transaction.cart, transaction.consumer))
            {
                order.LineItems.Add((
                    $"優惠: {dr.Name} {dr.DiscountAmount}",
                    dr.DiscountAmount));
                total += dr.DiscountAmount;
            }

            order.TotalPrice = total;
            order.Id = transactionId;

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