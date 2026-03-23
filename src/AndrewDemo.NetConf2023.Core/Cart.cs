using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Carts;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Cart
    {
        public Cart()
        {
            ProdQtyMap ??= new Dictionary<string, int>();
        }

        [BsonId(true)]
        public int Id { get; set; }

        public Dictionary<string, int> ProdQtyMap { get; set; } = new Dictionary<string, int>();


        // sku CRUD
        public bool AddProducts(string productId, int qty = 1)
        {
            ProdQtyMap ??= new Dictionary<string, int>();
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

        public IEnumerable<LineItem> LineItems
        {
            get 
            {
                foreach (var ci in this.ProdQtyMap)
                {
                    yield return new LineItem()
                    {
                        ProductId = ci.Key,
                        Quantity = ci.Value
                    };
                }
            }
        }
    }
}
