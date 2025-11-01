using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private static IShopDatabaseContext Database => ShopDatabase.Current;

        private static Member? GetMemberByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var tokenRecord = Database.MemberTokens.FindById(token);
            if (tokenRecord == null) return null;
            if (tokenRecord.Expire <= DateTime.Now) return null;

            return Database.Members.FindById(tokenRecord.MemberId);
        }

        private static Member RequireCurrentMember()
        {
            var token = _access_token ?? throw new InvalidOperationException("access token not initialized");
            return GetMemberByToken(token) ?? throw new InvalidOperationException("member not found");
        }

        private static Cart CreateNewCart()
        {
            var cart = new Cart();
            Database.Carts.Insert(cart);
            return cart;
        }

        private static Cart? GetCurrentCart()
        {
            return Database.Carts.FindById(_cartId);
        }

        private static Product? GetProductById(int productId)
        {
            return Database.Products.FindById(productId);
        }

        private static IReadOnlyList<Product> GetAllProducts()
        {
            return Database.Products.FindAll().ToList();
        }

        private static IEnumerable<Order> GetOrdersForMember(int memberId)
        {
            return Database.Orders.Find(o => o.Buyer.Id == memberId);
        }

        private static string IssueToken(Member member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            string token = Guid.NewGuid().ToString("N");
            Database.MemberTokens.Upsert(new MemberAccessTokenRecord
            {
                Token = token,
                MemberId = member.Id,
                Expire = DateTime.MaxValue
            });

            return token;
        }

        private static int CreateCheckoutTransaction(int cartId, string token)
        {
            var cart = Database.Carts.FindById(cartId) ?? throw new ArgumentOutOfRangeException(nameof(cartId));
            if (!cart.LineItems.Any()) throw new InvalidOperationException("cart is empty");

            var member = GetMemberByToken(token) ?? throw new ArgumentOutOfRangeException(nameof(token));

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow
            };

            Database.CheckoutTransactions.Insert(transaction);
            return transaction.TransactionId;
        }

        private static async Task<Order> CompleteCheckoutTransactionAsync(int transactionId, int paymentId, int satisfaction = 0, string? comments = null)
        {
            var ticket = new WaitingRoomTicket();
            await ticket.WaitUntilCanRunAsync();

            var transaction = Database.CheckoutTransactions.FindById(transactionId) ?? throw new ArgumentOutOfRangeException(nameof(transactionId));
            Database.CheckoutTransactions.Delete(transactionId);

            var cart = Database.Carts.FindById(transaction.CartId) ?? throw new InvalidOperationException($"cart {transaction.CartId} not found");
            var consumer = Database.Members.FindById(transaction.MemberId) ?? throw new InvalidOperationException($"member {transaction.MemberId} not found");

            var order = new Order(transactionId)
            {
                Buyer = consumer
            };

            decimal total = 0m;

            foreach (var lineitem in cart.LineItems)
            {
                var product = Database.Products.FindById(lineitem.ProductId) ?? throw new InvalidOperationException($"product {lineitem.ProductId} not found");
                total += product.Price * lineitem.Qty;

                order.LineItems.Add(new Order.OrderLineItem
                {
                    Title = $"商品: {product.Name}, 單價: {product.Price} x {lineitem.Qty} 件 = {product.Price * lineitem.Qty:C}",
                    Price = product.Price * lineitem.Qty
                });
            }

            foreach (var discount in DiscountEngine.Calculate(cart, consumer))
            {
                order.LineItems.Add(new Order.OrderLineItem
                {
                    Title = $"優惠: {discount.Name}, 折扣 {-1 * discount.DiscountAmount:C}",
                    Price = discount.DiscountAmount
                });

                total += discount.DiscountAmount;
            }

            order.TotalPrice = total;
            order.ShopNotes = new Order.OrderShopNotes
            {
                BuyerSatisfaction = satisfaction,
                Comments = comments
            };

            Database.Orders.Upsert(order);
            return order;
        }

        private static void InitShop()
        {
            Database.Products.Upsert(new Product()
            {
                Id = 1,
                Name = "18天台灣生啤酒 355ml",
                Description = "18天台灣生啤酒未經過巴氏德高溫殺菌，採用歐洲優質原料，全程0-7°C冷藏保鮮，猶如鮮奶與生魚片般珍貴，保留最多啤酒營養及麥香風味；這樣高品質、超新鮮、賞味期只有18天的台灣生啤酒，值得您搶鮮到手! (未成年請勿飲酒)",
                Price = 65m
            });
            Database.Products.Upsert(new Product()
            {
                Id = 2,
                Name = "可口可樂® 350ml",
                Description = "1886年，美國喬治亞州的亞特蘭大市，有位名叫約翰•潘伯頓（Dr. John S. Pemberton）的藥劑師，他挑選了幾種特別的成分，發明出一款美味的糖漿，沒想到清涼、暢快的「可口可樂」就奇蹟般的出現了！潘伯頓相信這產品可能具有商業價值，因此把它送到傑柯藥局（Jacobs' Pharmacy）販售，開始了「可口可樂」這個美國飲料的傳奇。而潘伯頓的事業合夥人兼會計師：法蘭克•羅賓森（Frank M. Robinson），認為兩個大寫C字母在廣告上可以有不錯的表現，所以創造了\"Coca‑Cola\"這個名字。但是讓「可口可樂」得以大展鋒頭的，卻是從艾薩•坎德勒（Asa G. Candler）這個具有行銷頭腦的企業家開始。",
                Price = 18m
            });
            Database.Products.Upsert(new Product()
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




        private static (string? command, string[] args) ParseCommand(string? commandline)
        {
            // format: command [arg1] [arg2] ...
            if (commandline == null) return (null, Array.Empty<string>());

            var parts = commandline.Split(' ');
            if (parts.Length == 0)
            {
                return ("0", Array.Empty<string>());
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

            var member = Database.Members.FindOne(m => m.Name == (username ?? string.Empty));
            if (member == null)
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Login / Register Failed");
                    return false;
                }

                member = new Member
                {
                    Name = username
                };

                Database.Members.Insert(member);
            }

            // init login user context
            _access_token = IssueToken(member);
            _cartId = CreateNewCart().Id;

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