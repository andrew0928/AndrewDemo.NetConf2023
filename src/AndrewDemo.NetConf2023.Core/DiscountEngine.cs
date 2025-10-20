namespace AndrewDemo.NetConf2023.Core
{
    internal class DiscountEngine
    {
        public static IEnumerable<DiscountRecord> Calculate(Cart cart, Member consumer)
        {
            // 18天(ID: 1) 第二罐六折
            //var p = Product.Database[1];//.Where(p => p.Value.Id.Equals(1)).FirstOrDefault().Value;
            var pid = 1;

            //if (cart.ProdQtyMap.ContainsKey(pid) && cart.ProdQtyMap[pid] > 2)
            var lineitem = cart.LineItems.Where(lt => (lt.ProductId == pid && lt.Qty >= 2)).FirstOrDefault();

            if (lineitem != null)
            {
                var p = Product.Database[lineitem.ProductId];
                for (int index = 1; index <= lineitem.Qty; index++)
                {
                    if (index % 2 == 0) yield return new DiscountRecord()
                    {
                        Name = $"第二件六折",
                        Description = $"符合商品: {p.Name} x 2",
                        DiscountAmount = p.Price * -0.4m,
                    };
                }
            }
        }

        public class DiscountRecord
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal DiscountAmount { get; set; }
        }
    }
}