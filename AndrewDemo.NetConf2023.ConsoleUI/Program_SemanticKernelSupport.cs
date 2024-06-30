using AndrewDemo.NetConf2023.Core;
using Json.More;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

namespace AndrewDemo.NetConf2023.ConsoleUI
{
    internal partial class Program
    {
        #region semantic kernel context
        private static Kernel _kernel = null;
        private static ChatHistory _chatMessages = new ChatHistory();

        private static OpenAIPromptExecutionSettings _settings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        private static bool _enable_checkout_confirm = true;
        private static bool _enable_copilot_notify = true;
        private static bool _enable_copilot_ask = true;
        

        private static IChatCompletionService _chatCompletionService = null;
        #endregion


        private static void InitSK()
        {
            #region init semantic kernel
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            var builder = Kernel.CreateBuilder()
                //.AddAzureOpenAIChatCompletion("SKDemo_GPT4_Preview", "https://andrewskdemo.openai.azure.com/", config["azure-openai:apikey"]);
                .AddAzureOpenAIChatCompletion("SKDemo_GPT4o", "https://andrewskdemo.openai.azure.com/", config["azure-openai:apikey"]);



            //.AddOpenAIChatCompletion("fake-model", "fake-apikey", httpClient: new HttpClient(new LMStudioLocalServiceHandler(1234)));
            //.AddHuggingFaceTextGeneration("mistralai/Mixtral-8x7B-Instruct-v0.1");
            //.AddHuggingFaceTextGeneration("openchat/openchat-3.5-0106");


            builder.Services.AddLogging(logger => 
            {
                logger.AddDebug();
                //logger.AddConsole();
                logger.SetMinimumLevel(LogLevel.Information);
            });

            builder.Plugins.AddFromType<Program>();

            _kernel = builder.Build();

            _chatMessages.AddSystemMessage(
                """
                你是 "安德魯小舖" 的助理店長, 負責協助引導每個來商店購買的客人順利結帳。
                主要任務有三類:
                1. 結帳前的確認
                2. 選購過程的操作過程關注
                3. 回應客人的問題或是操作要求 (可呼叫 function call)

                以下是這三類任務的流程說明:                

                結帳前請檢查下列項目:
                1. 客人購買的東西是否適合他的期待? 請協助客人確認購買清單。
                2. 客人的購買行為是否安全? 請協助客人確認購買行為。有些商品有法律限制，或是有可能對客人造成危險。
                3. 客人的購買行為是否合理? 請協助客人確認購買行為。有些商品可能有更好的選擇，或是有更好的折扣。
                4. 檢查 FAQ 清單
                5. 確認方式: 客人提示訊息會用 "我要進行結帳確認: XXX" 開頭，並且附上購物內容資訊。沒問題就回覆 OK 並且不需要其他資訊, 有注意事項就回覆 HINT 開頭並附上注意事項。

                選購過程的操作過程關注:
                1. 如果購物車是空的，就進行結帳，代表客人可能遺漏操作步驟。請提醒客人留意，並在結帳前主動列出購物車內容再次確認。
                2. 如果客人連續加入/移除商品超過 5 次，可能是系統異常，或是需要諮詢才能決定。請直接詢問是否需要幫助。
                3. 如果客人加入購物車的商品數量超過 10 件，可能是操作異常，或是需要諮詢。請直接詢問是否需要幫助。
                4. 如果客人連續顯示操作指令清單 3 次，可能是不熟悉操作，或是找不到他要的功能。請直接詢問是否需要幫助。
                5. 確認方式: 客人提示訊息會用 "我已進行操作: XXX" 開頭，並附上操作內容。沒問題就回覆 OK 並且不需要其他資訊, 若有注意事項就回覆 HINT 開頭並附上注意事項。
                6. 客人操作都是自行執行，你只需要傾聽，只在需要提醒時回應客戶。請勿重複執行客戶已經進行的操作。


                客人開放性問題詢問或要求協助:
                1. 如果客人詢問的問題需要呼叫查詢的動作，你可以直接執行不必詢問
                2. 如果客人要求變更購物車內容，或是要你替他結帳，請再次確認，客人同意後才可執行
                3. 任何 function 呼叫，完成後都需要明確讓客人知道你呼叫了甚麼 function，並且告知執行結果
                4. 確認方式: 客人提示訊息會用 "店長請問: XXX" 開頭，，就歸類在開放性問題或是要求協助。


                以下是 FAQ 清單:
                1. 若購物車已經是空的，客人又嘗試清除購物車，可能碰到操作異常。請直接詢問是否需要幫助。
                2. 若購物車是空的，客人嘗試結帳，可能漏掉部分操作。請直接提醒客人留意，並在結帳前主動列出購物車內容再次確認。
                3. 購買含酒精飲料請提醒客人年齡限制，法律限制，避免酒駕。
                4. 購買含糖飲料請提醒客人注意醣類攝取。
                5. 購買含咖啡因飲料請提醒客人注意咖啡因攝取。
                6. 有預算要求，請留意折扣規則。部分優惠折扣可能導致買越多越便宜，請代替客人確認是否多買一件真的會超過預算。
                
                """);

            /*

                確認結帳的範例:

                客人: 
                我要開始結帳，沒問題請回覆 "OK: "，有注意事項請回覆 "HINT: "。其他建議可以在回覆之後接著說明

                以下是我購物車內的清單:
                - 18天生啤酒 x 5, 單價 $65
                - 可口可樂 x 10, 單價 $20

                購買註記:
                - 小孩辦10歲生日宴會使用，請附贈彩帶與氣球

                助理:
                HINT: 根據您提供的購買註記，您是為了一個10歲小孩的生日宴會購買這些商品。
                請注意，購買含酒精的飲料（如生啤酒）給未成年人是不合法的。我建議您考慮更換生啤酒為非酒精飲料，以符合法律規定並確保小孩的健康安全。
                另外，關於可口可樂，請注意醣類攝取量，尤其是在兒童聚會中。如果您需要幫助選擇適合的非酒精飲料或者有任何其他問題，我可以協助您。
                另外，我會為您準備彩帶與氣球作為贈品。您是否希望我為您更換生啤酒為其他飲料，或者有其他需要幫忙的地方？

             */

            _kernel.FunctionInvoking += (sender, args) =>
            {
                string argline = "";
                foreach (var arg in args.Arguments)
                {
                    argline = argline + $", {arg.Key}: {arg.Value}";
                }
                if (argline != "") argline = argline.Substring(2);

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"copilot > function call: {args.Function.Name}({argline})");
                Console.ResetColor();

            };

            //_kernel.FunctionInvoked += (sender, args) =>
            //{
            //    Console.WriteLine($" function invoked: {args.Function.Name}, {args.Result}");
            //};

            _chatCompletionService = _kernel.Services.GetRequiredService<IChatCompletionService>();
            #endregion
        }


        private static async Task<string> CallCopilotAsync(params string[] user_messages)
        {
            if (user_messages == null ||
                user_messages.Length == 0) return null;

            foreach(var message in user_messages)
            {
                if (string.IsNullOrWhiteSpace(message)) continue;

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"copilot > user: {message}");
                Console.ResetColor();
                _chatMessages.AddUserMessage(message);
            }

            var result = _chatCompletionService.GetChatMessageContentsAsync(
                _chatMessages,
                _settings,
                _kernel);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            try
            {
                while (!result.Wait(500)) Console.Write(".");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"copilot > error: {ex.Message}");
            }
            Console.WriteLine();
            Console.ResetColor();

            string response = "";
            foreach (var content in result.Result)
            {
                if (content == null) continue;
                if (content.Role != AuthorRole.Assistant) continue;
                if (content.Content == null) continue;

                _chatMessages.AddAssistantMessage(content.Content);
                //Console.WriteLine($"copilot > assistant: {content.Content}");
                response = response + content.Content + "\n";
            }

            return response; ;
        }

        private static void CopilotNotify(string message)
        {
            if (!_enable_copilot_notify) return;
            if (string.IsNullOrWhiteSpace(message)) return;

            var result = CallCopilotAsync($"我已進行操作: {message}").Result;

            //result.ContinueWith((task) =>
            //{
            //    var content = task.Result;
            //    if (!content.StartsWith("OK"))
            //    {
            //        Console.ForegroundColor = ConsoleColor.Yellow;
            //        Console.WriteLine($"copilot notify > {result}");
            //        Console.ResetColor();
            //    }
            //});

            if (result.StartsWith("OK"))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"copilot notify > {result}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"copilot notify > {result}");
                Console.ResetColor();
            }
        }


        private static (bool confirm, string message) CopilotCheckoutConfirm(string prompt)
        {
            if (!_enable_checkout_confirm) return (true, "");

            string items = "";
            foreach(var item in Cart.Get(_cartId).LineItems)
            {
                Product product = Product.Database[item.ProductId];
                items = items + $"\n- [{product.Id}] {product.Name}, 單價 {product.Price:C} x {item.Qty} 件";
            }

            var result = CallCopilotAsync(
               $"""
                我要進行結帳前確認
                我要開始結帳，沒問題請回覆 "OK: "，有注意事項請回覆 "HINT: "。
                其他建議可以在回覆之後接著說明。
                """,
                $"""
                以下是我購物車內的清單:{items}
                預估結帳金額: {Cart.Get(_cartId).EstimatePrice():C}
                """,
                $"""
                購買註記:
                - {prompt}
                """).Result;
            return (result.StartsWith("OK"), result);
        }

        private static string CopilotAsk(string prompt)
        {
            if (!_enable_copilot_ask) return "";

            if (string.IsNullOrWhiteSpace(prompt)) return "";

            var result = CallCopilotAsync($"店長請問: {prompt}").Result;
            return result;
        }



        #region SK: shop plugins

        // Cart_Empty
        [KernelFunction, Description("清空購物車。購物車內的代結帳商品清單會完全歸零回復原狀")]
        public static void ShopFunction_EmptyCart()
        {
            _cartId = Cart.Create().Id;
        }

        // Cart_AddItem
        [KernelFunction, Description("將指定的商品與指定的數量加入購物車。加入成功會傳回 TRUE，若加入失敗會傳回 FALSE，購物車內容會維持原狀不會改變")]
        public static bool ShopFunction_AddItemToCart(
            [Description("指定加入購物車的商品ID")] int productId,
            [Description("指定加入購物車的商品數量")] int quanty)
        {
            var cart = Cart.Get(_cartId);
            return cart.AddProducts(productId, quanty);
        }

        // Cart_RemoveItem
        [KernelFunction, Description("將指定的商品與指定的數量從購物車移除。移除成功會傳回 TRUE，若移除失敗會傳回 FALSE，購物車內容會維持原狀不會改變")]
        public static bool ShopFunction_RemoveItemToCart(
            [Description("指定要從購物車移除的商品ID")] int productId,
            [Description("指定要從購物車移除的商品數量")] int quanty)
        {
            var cart = Cart.Get(_cartId);
            return cart.AddProducts(productId, -quanty);
        }

        // Cart_EstimatePrice
        [KernelFunction, Description("試算目前購物車的結帳金額 (包含可能發生的折扣)")]
        public static decimal ShopFunction_EstimatePrice()
        {
            return Cart.Get(_cartId).EstimatePrice();
        }

        [KernelFunction, Description("傳回目前購物車的內容狀態")]
        public static Cart.CartLineItem[] ShopFunction_ShowMyCartItems()
        {
            return Cart.Get(_cartId).LineItems.ToArray();
        }


        // Cart_Checkout
        [KernelFunction, Description("購買目前購物車內的商品清單，提供支付代碼，完成結帳程序，傳回訂單內容")]
        public static Order ShopFunction_Checkout(
            [Description("支付代碼，此代碼代表客戶已經在外部系統完成付款")] int paymentId)
        {
            int checkout_id = Checkout.Create(_cartId, _access_token);
            var response = Checkout.CompleteAsync(checkout_id, paymentId);
            while (!response.Wait(1000))
            {
                InfoOutput("waiting for payment...");
            }

            if (response.Result != null)
            {
                _cartId = Cart.Create().Id;
                return response.Result;
            }

            return null;
        }

        // Product_List
        [KernelFunction, Description("傳回店內所有出售的商品品項資訊")]
        public static Product[] ShopFunction_ListProducts()
        {
            return Product.Database.Values.ToArray();
        }

        // Product_Get
        [KernelFunction, Description("傳回指定商品 ID 的商品內容")]
        public static Product ShopFunction_GetProduct(
            [Description("指定查詢的商品 ID")] int productId)
        {
            if (!Product.Database.ContainsKey(productId)) return null;
            return Product.Database[productId];
        }

        // Member_Get
        [KernelFunction, Description("傳回我 (目前登入) 的個人資訊")]
        public static Member ShopFunction_GetMyInfo()
        {
            return Member.GetCurrentMember(_access_token);
        }

        // Member_GetOrders
        [KernelFunction, Description("傳回我 (目前登入) 的過去訂購紀錄")]
        public static Order[] ShopFunction_GetMyOrders()
        {
            var member = Member.GetCurrentMember(_access_token);
            return Order.GetOrders(member.Id).ToArray();
        }

        #endregion
    }
}