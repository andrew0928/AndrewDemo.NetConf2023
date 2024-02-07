using AndrewDemo.NetConf2023.Core;
using System.Data;
using System.Runtime.CompilerServices;

namespace AndrewDemo.NetConf2023.ConsoleUI
{
    internal class Program
    {

        private delegate void CommandProcessor(string[] args);

        // command id => command processor mapping table
        private static Dictionary<string, CommandProcessor> commandProcessors = new Dictionary<string, CommandProcessor>()
        {
            //{ "0", ShowMenuCommandProcessor },
            { "1", ListProductsCommandProcessor },
            { "21", ShowMyItemsCommandProcessor },
            { "22", AddItemsCommandProcessor },
            { "23", RemoveItemsCommandProcessor },
            { "24", EmptyMyCartCommandProcessor },
            { "25", AddItemsWithBudgetCommandProcessor },
            { "3", CheckoutCommandProcessor },
            { "4", ShowMyInfoCommandProcessor },
            //{ "5", ExitCommandProcessor },
        };



        #region current login user context
        private static string _access_token = null;
        private static int _cartId = 0;
        #endregion

        static void Main(string[] args)
        {
            InitSystem();

            if (!UserAuthorize())
            {
                return;
            }

            var member = Member.GetCurrentMember(_access_token);
            Console.WriteLine($"Hello {member.Name}({member.Id}), Welcome to Andrew's Shop!");
            Console.WriteLine();

            bool exit = false;
            string commandline = "0";

            do
            {
                (string command, string[] parameters) = ParseCommand(commandline);
                if (command == "0")
                {
                    Console.WriteLine("\t0. show me (this menu)");
                    Console.WriteLine("\t1. list products");
                    Console.WriteLine("\t2. shopping cart commands");
                    Console.WriteLine("\t- 21. show my items");
                    Console.WriteLine("\t- 22. add items (patterns: 22 [pid] [qty])");
                    Console.WriteLine("\t- 23. remove items (patterns: 23 [pid] [qty])");
                    Console.WriteLine("\t- 24. empty my cart");
                    Console.WriteLine("\t- 25. special: add items with budget (patterns: 25 [pid] [budget])");
                    Console.WriteLine("\t3. checkout (patterns: 3 [payment-id])");
                    Console.WriteLine("\t4. my account info");
                    Console.WriteLine("\t5. exit");
                    Console.WriteLine();
                }
                else if (command == "5")
                {
                    exit = true;
                    continue;
                }
                //else
                //{
                //    Console.WriteLine("Invalid command");
                //}

                else if (commandProcessors.ContainsKey(command))
                {
                    try
                    {
                        commandProcessors[command](parameters);
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        SystemOutput($"Error: {ex.Message}");
                    }
                }
                else
                {
                    AssistantOutput("Invalid command. try [0] to get help, or [5] to exit...");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"command > ");
                Console.ResetColor();
                commandline = Console.ReadLine();
            } while (!exit);





        }

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
            while(cart.EstimatePrice() <= budget)
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

        #region console application helper methods
        private static void InitSystem()
        {

            Product.Database.Add(1, new Product()
            {
                Id = 1,
                Name = "18天台灣生啤酒 355ml",
                Description = "18天台灣生啤酒未經過巴氏德高溫殺菌，採用歐洲優質原料，全程0-7°C冷藏保鮮，猶如鮮奶與生魚片般珍貴，保留最多啤酒營養及麥香風味；這樣高品質、超新鮮、賞味期只有18天的台灣生啤酒，值得您搶鮮到手!",
                Price = 65m
            });
            Product.Database.Add(2, new Product()
            {
                Id = 2,
                Name = "可口可樂® 350ml",
                Description = "1886年，美國喬治亞州的亞特蘭大市，有位名叫約翰•潘伯頓（Dr. John S. Pemberton）的藥劑師，他挑選了幾種特別的成分，發明出一款美味的糖漿，沒想到清涼、暢快的「可口可樂」就奇蹟般的出現了！潘伯頓相信這產品可能具有商業價值，因此把它送到傑柯藥局（Jacobs' Pharmacy）販售，開始了「可口可樂」這個美國飲料的傳奇。而潘伯頓的事業合夥人兼會計師：法蘭克•羅賓森（Frank M. Robinson），認為兩個大寫C字母在廣告上可以有不錯的表現，所以創造了\"Coca‑Cola\"這個名字。但是讓「可口可樂」得以大展鋒頭的，卻是從艾薩•坎德勒（Asa G. Candler）這個具有行銷頭腦的企業家開始。",
                Price = 18m
            });
            Product.Database.Add(3, new Product()
            {
                Id = 3,
                Name = "御茶園 特撰冰釀綠茶 550ml",
                Description = "新升級!台灣在地茶葉入，冰釀回甘。台灣在地茶葉，原葉沖泡。如同現泡般的清新綠茶香。",
                Price = 25m
            });

        }

        private static void AssistantOutput(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"assistant > {message}");
            Console.ResetColor();
        }

        private static void SystemOutput(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"system > {message}");
            Console.ResetColor();
        }

        private static void InfoOutput(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"info > {message}");
            Console.ResetColor();
        }



        private static (string command, string[] args) ParseCommand(string commandline)
        {
            // format: command [arg1] [arg2] ...
            var parts = commandline.Split(' ');
            if (parts.Length == 0)
            {
                return ("0", new string[0]);
            }
            return (parts[0], parts[1..]);
        }

        private static bool UserAuthorize()
        {
            //Console.WriteLine($"Login / Register First:");
            //Console.Write($"- username:\t");
            //var username = Console.ReadLine();
            //Console.Write($"- password:\t");
            //var password = ReadPassword();

            var username = "andrew";
            var password = "123456";

            string token = Member.Login(username, password);
            if (token == null)
            {
                token = Member.Register(username);
            }
            if (token == null)
            {
                Console.WriteLine("Login / Register Failed");
                return false;
            }

            // init login user context
            _access_token = token;
            _cartId = Cart.Create().Id;

            return true;
        }


        // fully write by github copilot
        private static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return password;
        }
        #endregion
    }
}
