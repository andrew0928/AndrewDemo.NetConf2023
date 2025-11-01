using System.Collections.Generic;
using System.Linq;

namespace AndrewDemo.NetConf2023.Core
{
    public static class DiscountEngine
    {
        public static IEnumerable<DiscountRecord> Calculate(Cart cart, Member? consumer, IShopDatabaseContext context)
        {
            // 18天(ID: 1) 第二罐六折
            //var p = Product.Database[1];//.Where(p => p.Value.Id.Equals(1)).FirstOrDefault().Value;
            var pid = 1;

            //if (cart.ProdQtyMap.ContainsKey(pid) && cart.ProdQtyMap[pid] > 2)
            var lineitem = cart.LineItems.Where(lt => (lt.ProductId == pid && lt.Qty >= 2)).FirstOrDefault();

            if (lineitem != null)
            {
                var product = context.Products.FindById(lineitem.ProductId);
                if (product == null)
                {
                    yield break;
                }
                for (int index = 1; index <= lineitem.Qty; index++)
                {
                    if (index % 2 == 0) yield return new DiscountRecord()
                    {
                        Name = $"第二件六折",
                        Description = $"符合商品: {product.Name} x 2",
                        DiscountAmount = product.Price * -0.4m,
                    };
                }
            }
        }

        public class DiscountRecord
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal DiscountAmount { get; set; }
        }
    }
}