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

            //return new MemberOrdersResponse()
            //{
            //    TotalOrders = count,
            //    TotalAmount = amount,
            //    Orders = orders
            //};

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
        }

        private static void EmptyMyCartCommandProcessor(string[] args)
        {
            _cartId = Cart.Create().Id;
            AssistantOutput("Your cart is empty now.");
        }

        private static void CheckoutCommandProcessor(string[] args)
        {
            int checkout_id = Checkout.Create(_cartId, _access_token);
            int payment_id = int.Parse(args[0]);

            var result = Checkout.CompleteAsync(checkout_id, payment_id);
            while (!result.Wait(1000))
            {
                Console.WriteLine("waiting for payment...");
            }

            if (result.Result != null)
            {
                AssistantOutput($"Checkout completed successfully. your order number is: [{result.Result.Id}]");
            }
        }

        private static void RemoveItemsCommandProcessor(string[] args)
        {
            int pid = int.Parse(args[0]);
            if (args.Length < 2 || !int.TryParse(args[1], out int qty)) qty = 1;

            var cart = Cart.Get(_cartId);
            cart.AddProducts(pid, -qty);
            AssistantOutput($"商品 [{pid}] 已經從您的購物車中移除了 {qty} 件.");
        }

        private static void AddItemsCommandProcessor(string[] args)
        {
            int pid = int.Parse(args[0]);
            if (args.Length < 2 || !int.TryParse(args[1], out int qty)) qty = 1;

            var cart = Cart.Get(_cartId);
            cart.AddProducts(pid, qty);
            AssistantOutput($"商品 [{pid}] 已經加入您的購物車了 {qty} 件.");
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
        }

        private static void ShowMyItemsCommandProcessor(string[] args)
        {
            var cart = Cart.Get(_cartId);
            AssistantOutput($"您好, 你目前購物車內共有 {cart.LineItems.Count()} 件商品, 總共 {cart.EstimatePrice():C} 元.");

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
        }
        #endregion

    }

}