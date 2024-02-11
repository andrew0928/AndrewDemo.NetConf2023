using AndrewDemo.NetConf2023.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Data;
using System.Runtime.CompilerServices;

namespace AndrewDemo.NetConf2023.ConsoleUI
{
    internal partial class Program
    {
        #region console application helper methods
        private static void InitShop()
        {
            Product.Database.Add(1, new Product()
            {
                Id = 1,
                Name = "18天台灣生啤酒 355ml",
                Description = "18天台灣生啤酒未經過巴氏德高溫殺菌，採用歐洲優質原料，全程0-7°C冷藏保鮮，猶如鮮奶與生魚片般珍貴，保留最多啤酒營養及麥香風味；這樣高品質、超新鮮、賞味期只有18天的台灣生啤酒，值得您搶鮮到手! (未成年請勿飲酒)",
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
            if (commandline == null) return (null, new string[0]);

            var parts = commandline.Split(' ');
            if (parts.Length == 0)
            {
                return ("0", new string[0]);
            }
            return (parts[0], parts[1..]);
        }

        private static bool UserAuthorize()
        {
            Console.WriteLine($"Login / Register First:");
            Console.Write($"- username:\t");
            var username = Console.ReadLine();
            Console.Write($"- password:\t");
            var password = ReadPassword();

            //var username = "andrew";
            //var password = "123456";

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