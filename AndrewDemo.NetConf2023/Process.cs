using AndrewDemo.NetConf2023.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewDemo.NetConf2023
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // login
            Console.WriteLine("步驟 1, 登入");

            var member = (from m in Member.Database where m.Value.Name == "andrew" select m.Value).FirstOrDefault();
            Console.WriteLine($"user {member.Name}(id: {member.Id}) logged in.");

            // browse product catalog
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("步驟 2, 瀏覽商品");
            foreach (var p in Product.Database.Values)
            {
                Console.WriteLine($"- [{p.Id}] {p.Name}, ${p.Price}");
            }

            // add to shopping cart
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("步驟 3, 加入購物車, 帳款試算");

            var cart = new Cart();
            //decimal total = 0m;
            cart.AddProducts(1, 6);
            cart.AddProducts(2, 1);
            //foreach(var ci in cart.ProdQtyMap)
            //{
            //    Product p = Product.Database[ci.Key];
            //    int pid = p.Id;
            //    int qty = ci.Value;
            //    Console.WriteLine($"- [{pid}] {p.Name}(單價: ${p.Price}) x {qty},     ${p.Price * qty}");
            //    total += p.Price * qty;
            //}
            //foreach(var dr in DiscountEngine.Calculate(cart, member))
            //{
            //    Console.WriteLine($"- [優惠] {dr.Name},   ${dr.DiscountAmount}");
            //    total += dr.DiscountAmount;
            //}
            Console.WriteLine($"預估結帳金額: ${cart.EstimatePrice()}");


            // checkout
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("步驟 4, 結帳");

            int tid = Checkout.Create(cart, member);
            var checkout_result = Checkout.CompleteAsync(tid, 0);

            while(checkout_result.Wait(1000) == false)
            {
                Console.WriteLine($"[checkout] waiting process queue...");
            }

            // order created
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("步驟 5, 訂單成立");



            var order = await checkout_result;
            Console.WriteLine($"oders info:");
            Console.WriteLine($"訂單編號: {order.Id}");
            Console.WriteLine($"買家: {order.buyer.Name} ({order.buyer.Id})");
            Console.WriteLine($"明細:");
            foreach(var lineitem in order.LineItems)
            {
                Console.WriteLine($"- [{lineitem.title}]    {lineitem.price}");
            }
            Console.WriteLine($"結帳金額: {order.TotalPrice}");



            return;
        }
    }
}
