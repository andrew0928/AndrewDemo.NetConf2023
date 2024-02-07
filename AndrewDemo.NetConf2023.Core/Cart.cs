namespace AndrewDemo.NetConf2023.Core
{
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
                //Console.WriteLine($"- [{p.Id}] {p.Name}(單價: ${p.Price}) x {lineitem.Qty},     ${p.Price * lineitem.Qty}");
                total += p.Price * lineitem.Qty;
            }
            foreach (var discount in this.EstimateDiscounts())
            {
                //Console.WriteLine($"- [優惠] {discount.Name},   ${discount.DiscountAmount}");
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
                        Description = d.Description, //$"[{d.Name}]: ${d.DiscountAmount}",
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
}