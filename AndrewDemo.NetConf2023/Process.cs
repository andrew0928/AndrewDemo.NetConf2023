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
            // init
            Checkout.CheckoutCompleted += (sender, args) =>
            {
                Console.WriteLine($"[system] checkout-completed event, order({((Order)sender).Id}) created.");
            };
            Member.MemberLoggedIn += (sender, args) =>
            {
                Console.WriteLine($"[system] member-loggined event, member({((Member)sender).Name}) loggined.");
            };

            // login
            Console.WriteLine("步驟 1, 登入");

            var member = //(from m in Member.Database where m.Value.Name == "andrew" select m.Value).FirstOrDefault();
                Member.Login("andrew", "123456");
            //Console.WriteLine($"user {member.Name}(id: {member.Id}) logged in.");

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

            var cart = Cart.Create();

            //decimal total = 0m;
            cart.AddProducts(1, 6);
            cart.AddProducts(2, 1);

            var _estimate_price = cart.EstimatePrice();
            Console.WriteLine($"預估結帳金額: ${_estimate_price}");


            // checkout
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("步驟 4, 結帳");

            int tid = Checkout.Create(cart.Id, member);
            // 這中間還要做:
            // 1. 確認訂單內容
            // 2. 運送地址
            // 3. 選擇支付方式
            // 4. 付款
            var checkout_result = Checkout.CompleteAsync(tid, 0);

            // 若結帳需要時間, 等待期間 UI 可以做些別的提示，或是引導 user 稍後再回來
            while (checkout_result.Wait(1000) == false)
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
                Console.WriteLine($"- [{lineitem.Title}]    {lineitem.Price}");
            }
            Console.WriteLine($"結帳金額: {order.TotalPrice}");



            return;
        }

    }
}
