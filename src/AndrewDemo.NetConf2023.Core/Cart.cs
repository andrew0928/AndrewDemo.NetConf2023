using System;
using System.Collections.Generic;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Cart
    {
        public Cart()
        {
            ProdQtyMap ??= new Dictionary<int, int>();
        }

        [BsonId(true)]
        public int Id { get; set; }

        public Dictionary<int, int> ProdQtyMap { get; set; } = new Dictionary<int, int>();

        public static Cart Create()
        {
            var cart = new Cart();
            LiteDbContext.Carts.Insert(cart);
            return cart;
        }

        public static Cart? Get(int id)
        {
            var cart = LiteDbContext.Carts.FindById(id);
            if (cart != null && cart.ProdQtyMap == null)
            {
                cart.ProdQtyMap = new Dictionary<int, int>();
            }

            return cart;
        }


        // sku CRUD
        public bool AddProducts(int productId, int qty = 1)
        {
            ProdQtyMap ??= new Dictionary<int, int>();
            if (this.ProdQtyMap.ContainsKey(productId))
            {
                this.ProdQtyMap[productId] += qty;
            }
            else
            {
                this.ProdQtyMap[productId] = qty;
            }
            LiteDbContext.Carts.Upsert(this);
            return true;
        }

        public decimal EstimatePrice()
        {
            decimal total = 0m;
            foreach (var lineitem in this.LineItems)
            {
                var product = Product.GetById(lineitem.ProductId) ?? throw new InvalidOperationException($"product {lineitem.ProductId} not found");
                //Console.WriteLine($"- [{product.Id}] {product.Name}(單價: ${product.Price}) x {lineitem.Qty},     ${product.Price * lineitem.Qty}");
                total += product.Price * lineitem.Qty;
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
                foreach (var d in DiscountEngine.Calculate(this, consumer: null!))
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