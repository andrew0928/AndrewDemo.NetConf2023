using AndrewDemo.NetConf2023.Core;
using System.Data;
using System.Runtime.CompilerServices;

namespace AndrewDemo.NetConf2023.ConsoleUI
{
    internal partial class Program
    {
        #region command processors
        private static void ShowMyInfoCommandProcessor(string[] args)
        {
            var member = Member.GetCurrentMember(_access_token);

            int count = 0;
            decimal amount = 0;
            var orders = Order.GetOrders(member.Id).ToList();

            foreach (var order in orders)
            {
                count++;
                amount += order.TotalPrice;
            }

            AssistantOutput($"您好, 以下是您的帳號資訊與購買紀錄. 您購買過 {count} 次，總消費金額為 {amount:C} 元。");

            Console.WriteLine($"Id:   {member.Id}");
            Console.WriteLine($"Name: {member.Name}");
            Console.WriteLine($"Orders:");
            foreach (var order in orders)
            {
                Console.WriteLine($"- {order.Id}\t{order.TotalPrice}");
                foreach (var item in order.LineItems)
                {
                    Console.WriteLine($"  - {item.Title}\t{item.Price:C}");
                }
                Console.WriteLine();
            }

            CopilotNotify($"我在查詢我的帳號資訊。查詢結果顯示，我在這裡購買過 {count} 次，總共花了 {amount:C} 元。");
        }




        private static void EmptyMyCartCommandProcessor(string[] args)
        {
            ShopFunction_EmptyCart();
            AssistantOutput("Your cart is empty now.");
            CopilotNotify($"我的購物車目前有 {Cart.Get(_cartId).LineItems.Count()} 件商品在裡面，我清空了我的購物車");
        }

        private static void CheckoutCommandProcessor(string[] args)
        {
            int payment_id = int.Parse(args[0]);

            AssistantOutput($"結帳前有任何要求可以跟我說，若無我將替您結帳 (直接輸入或是 ENTER 跳過)");
            var rules = Console.ReadLine();


            var result = CopilotCheckoutConfirm(rules);

            if (!result.confirm)
            {
                Console.WriteLine($"助理店長提醒:\n{result.message}");
                AssistantOutput($"您可以決定是否修正購買內容喔，是否要直接結帳 ( Y / N )?");
                if (Console.ReadLine().ToLower() != "y")
                {
                    AssistantOutput($"結帳取消");
                    CopilotNotify($"我取消了結帳操作");
                    return;
                }
            }


            var order = ShopFunction_Checkout(payment_id);

            if (order != null)
            {
                AssistantOutput($"Checkout completed successfully. your order number is: [{order.Id}]");
                CopilotNotify($"我已經結帳完成，成立的訂單編號是: {order.Id}");
            }
            else
            {
                CopilotNotify($"我結帳操作失敗，沒有成立訂單");
            }
        }

        private static void RemoveItemsCommandProcessor(string[] args)
        {
            int pid = int.Parse(args[0]);
            var product = Product.Database[pid];
            if (args.Length < 2 || !int.TryParse(args[1], out int qty)) qty = 1;

            if (ShopFunction_AddItemToCart(pid, -qty))
            {
                AssistantOutput($"商品 [{pid}] 已經從您的購物車中移除了 {qty} 件.");
                CopilotNotify($"我在購物車內移除了 {qty} 件商品 (商品 ID: {pid}, {product.Name})");
            }
            else
            {
                CopilotNotify($"我嘗試在購物車內移除 {qty} 件商品 (商品 ID: {pid}, {product.Name}) 但是失敗了");
            }
        }

        private static void AddItemsCommandProcessor(string[] args)
        {
            int pid = int.Parse(args[0]);
            var product = Product.Database[pid];
            if (args.Length < 2 || !int.TryParse(args[1], out int qty)) qty = 1;

            if (ShopFunction_AddItemToCart(pid, qty))
            {
                AssistantOutput($"商品 [{pid}] x {qty} 件, 已經加入您的購物車了。");
                CopilotNotify($"我在購物車內加入了 {qty} 件商品 (商品 ID: {pid}, {product.Name})");
            }
            else
            {
                CopilotNotify($"我嘗試在購物車內加入 {qty} 件商品 (商品 ID: {pid}, {product.Name}) 但是失敗了");
            }
        }

        private static void AddItemsWithBudgetCommandProcessor(string[] args)
        {
            int pid = int.Parse(args[0]);
            decimal budget = decimal.Parse(args[1]);

            var cart = Cart.Get(_cartId);
            var product = Product.Database[pid];


            if (cart.EstimatePrice() > budget)
            {
                AssistantOutput($"您的預算 {budget:C} 不足以購買商品 [{pid}]。");
                CopilotNotify($"我有預算 {budget:C}, 想要拿來購買商品 (ID: {pid}, {product.Name})。不過目前購物車商品已經超出預算，最後沒有放任何商品進購物車");
                return;
            }

            // add product to cart as much as possible, until reach the budget
            int total = 0;
            while (cart.EstimatePrice() <= budget)
            {
                cart.AddProducts(pid, 1);
                total++;
                InfoOutput($"add {pid} x 1, estimate: {cart.EstimatePrice()}");
            }
            cart.AddProducts(pid, -1); // remove the last one
            total--;
            InfoOutput($"rmv  {pid} x 1, estimate: {cart.EstimatePrice()}");
            AssistantOutput($"您的預算 {budget:C} 可以再購買商品 [{pid}] {product.Name} x {total} 件, 總金額為 {cart.EstimatePrice():C}, 已為您加入購物車了。");
            CopilotNotify($"我有預算 {budget:C}, 想要拿來購買商品 (ID: {pid}, {product.Name})。經過計算，在預算範圍內多放了 {total} 件商品進購物車。");
        }

        private static void ShowMyItemsCommandProcessor(string[] args)
        {
            var cart = Cart.Get(_cartId);
            AssistantOutput($"您好, 你目前購物車內共有 {cart.LineItems.Count()} 件商品, 總共 {cart.EstimatePrice():C} 元.");
            CopilotNotify($"我查詢了購物車目前的內容，共有 {cart.LineItems.Count()} 種不同的商品，現在結帳的話我需要付 {cart.EstimatePrice():C} 元");

            if (cart.LineItems.Count() == 0)
            {
                Console.WriteLine($"您的購物車現在是空的，可以用 22 [product-id] 選購商品喔. 或是用 1 可以看我們的商品介紹~");
                return;
            }

            foreach (var item in cart.LineItems)
            {
                var product = Product.Database[item.ProductId];
                Console.WriteLine($"- 商品: [{product.Id}] {product.Name}\t{product.Price:C} x {item.Qty}");
            }

            foreach (var discount in cart.EstimateDiscounts())
            {
                Console.WriteLine($"- 折扣: [{discount.Name}], {discount.Description}\t{discount.DiscountAmount:C}");
            }
        }

        private static void ListProductsCommandProcessor(string[] args)
        {
            AssistantOutput("您好, 以下是我們店裡有賣的東西...");
            foreach (var product in Product.Database.Values)
            {
                Console.WriteLine($"- {product.Id}\t{product.Name}\t{product.Price:C}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(product.Description);
                Console.WriteLine();
                Console.ResetColor();
            }
            CopilotNotify($"我在查詢店裡有賣的東西，店裡總共有 {Product.Database.Values.Count()} 種商品販售。");
        }
        #endregion

    }

}