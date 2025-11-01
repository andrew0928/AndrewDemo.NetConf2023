using System;
using AndrewDemo.NetConf2023.Core;
using System.Data;
using System.Runtime.CompilerServices;

namespace AndrewDemo.NetConf2023.ConsoleUI
{
    internal partial class Program
    {

        private delegate void CommandProcessor(string[] args);

        // command id => command processor mapping table
    private static readonly Dictionary<string, CommandProcessor> commandProcessors = new Dictionary<string, CommandProcessor>()
        {
            //{ "0", ShowMenuCommandProcessor },
            { "1",  ListProductsCommandProcessor },
            { "21", ShowMyItemsCommandProcessor },
            { "22", AddItemsCommandProcessor },
            { "23", RemoveItemsCommandProcessor },
            { "24", EmptyMyCartCommandProcessor },
            { "25", AddItemsWithBudgetCommandProcessor },
            { "3",  CheckoutCommandProcessor },
            { "4",  ShowMyInfoCommandProcessor },
            //{ "5", ExitCommandProcessor },
        };



        #region current login user context
    private static string? _access_token;
        private static int _cartId = 0;
        #endregion

        static void Main(string[] args)
        {
            // 初始化資料庫
            Database = new ShopDatabaseContext();
            
            InitSK();
            InitShop();

            if (!UserAuthorize())
            {
                return;
            }

            var member = RequireCurrentMember();
            Console.WriteLine($"Hello {member.Name}({member.Id}), Welcome to Andrew's Shop!");
            Console.WriteLine();

            bool exit = false;
            string? commandline = null;

            do
            {
                (string? command, string[] parameters) = ParseCommand(commandline);
                if (command == null || command == "0")
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
                    if (commandline != null) CopilotNotify($"我查詢了目前商店提供的操作指令清單");
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
                    //AssistantOutput("Invalid command. try [0] to get help, or [5] to exit...");
                    AssistantOutput("請稍待，正在幫您詢問助理店長 (copilot) 中...");
                    var result = CopilotAsk(commandline);
                    Console.WriteLine($"copilot answer > {result}");
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"command > ");
                Console.ResetColor();
                commandline = Console.ReadLine();
            } while (!exit);
        }
    }
}
