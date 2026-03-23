using System;
using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Core.Discounts;
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
            // 移除自動 Upsert，改由呼叫端明確處理持久化
            return true;
        }

        public decimal EstimatePrice(IShopDatabaseContext context, IDiscountEngine discountEngine, string shopId, Member? consumer = null)
        {
            decimal total = 0m;
            foreach (var lineitem in this.LineItems)
            {
                var product = context.Products.FindById(lineitem.ProductId) ?? throw new InvalidOperationException($"product {lineitem.ProductId} not found");
                //Console.WriteLine($"- [{product.Id}] {product.Name}(單價: ${product.Price}) x {lineitem.Qty},     ${product.Price * lineitem.Qty}");
                total += product.Price * lineitem.Qty;
            }
            foreach (var discount in this.EstimateDiscounts(context, discountEngine, shopId, consumer))
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

        public IEnumerable<CartDiscountHint> EstimateDiscounts(IShopDatabaseContext context, IDiscountEngine discountEngine, string shopId, Member? consumer = null)
        {
            var discountContext = DiscountEvaluationContextFactory.Create(shopId, this, consumer, context);
            {
                foreach (var d in discountEngine.Evaluate(discountContext))
                {
                    yield return new CartDiscountHint()
                    {
                        Name = d.Name,
                        Description = d.Description,
                        DiscountAmount = d.Amount
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
