using AndrewDemo.NetConf2023.Core;
using System.Data;
using System.Runtime.CompilerServices;

namespace AndrewDemo.NetConf2023.ConsoleUI
{
    internal partial class Program
    {

        private delegate void CommandProcessor(string[] args);

        // command id => command processor mapping table
        private static Dictionary<string, (CommandProcessor function, string intent)> commandProcessors = new Dictionary<string, (CommandProcessor, string)>()
        {
            //{ "0", ShowMenuCommandProcessor },
            { "1",  (ListProductsCommandProcessor, "查詢所有商品資訊") },
            { "21", (ShowMyItemsCommandProcessor, "查詢我的購物車內榮") },
            { "22", (AddItemsCommandProcessor, "將商品加入購物車") },
            { "23", (RemoveItemsCommandProcessor, "將商品從購物車移除") },
            { "24", (EmptyMyCartCommandProcessor, "清空我的購物車") },
            { "25", (AddItemsWithBudgetCommandProcessor, "在預算範圍內，盡可能的多將商品加入購物車") },
            { "3",  (CheckoutCommandProcessor, "結帳") },
            { "4",  (ShowMyInfoCommandProcessor, "查詢我的帳號資訊，以及購買紀錄") },
            //{ "5", ExitCommandProcessor },
        };



        #region current login user context
        private static string _access_token = null;
        private static int _cartId = 0;
        #endregion

        static void Main(string[] args)
        {
            InitSK();
            InitShop();

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
                    CopilotNotify("顯示操作指令");
                }
                else if (command == "5")
                {
                    exit = true;
                    continue;
                }
                else if (commandProcessors.ContainsKey(command))
                {
                    try
                    {
                        CopilotNotify(commandProcessors[command].intent);
                        commandProcessors[command].function(parameters);
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
                    CopilotAsk(commandline);
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"command > ");
                Console.ResetColor();
                commandline = Console.ReadLine();
            } while (!exit);
        }
    }
}
