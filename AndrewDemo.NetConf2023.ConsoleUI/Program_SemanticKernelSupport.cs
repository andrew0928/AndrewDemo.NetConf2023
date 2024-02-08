using AndrewDemo.NetConf2023.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

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


        private static IChatCompletionService _chatCompletionService = null;
        #endregion


        private static void InitSK()
        {
            #region init semantic kernel
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

            var builder = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion("SKDemo_GPT4_Preview", "https://andrewskdemo.openai.azure.com/", config["azure-openai:apikey"]);
            //.AddAzureOpenAIChatCompletion("SKDemo_GPT4_ShopCopilot", "https://andrewskdemo.openai.azure.com/", config["azure-openai:apikey"]);
            //.AddOpenAIChatCompletion("fake-model", "fake-apikey", httpClient: new HttpClient(new LMStudioLocalServiceHandler(1234)));
            //.AddHuggingFaceTextGeneration("mistralai/Mixtral-8x7B-Instruct-v0.1");//, "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");
            //.AddHuggingFaceTextGeneration("openchat/openchat-3.5-0106");//, "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");

            builder.Services.AddLogging(logger => 
            {
                logger.AddDebug();
                logger.SetMinimumLevel(LogLevel.Trace);
            });

            builder.Plugins.AddFromType<Program>();
            //builder.Plugins.AddFromType<SystemCallPlugin>();
            //builder.Plugins.AddFromType<ProductsQueryPlugin>();
            //builder.Plugins.AddFromType<CalculateProductsPricePlanner>();

            _kernel = builder.Build();

            _chatMessages.AddSystemMessage(
                """
                你現在是助理店長, 負責協助引導每個來商店購買的客人順利結帳。

                結帳前請檢查下列項目:
                1. 客人購買的東西是否適合他的期待? 請協助客人確認購買清單。
                2. 客人的購買行為是否安全? 請協助客人確認購買行為。有些商品有法律限制，或是有可能對客人造成危險。
                3. 客人的購買行為是否合理? 請協助客人確認購買行為。有些商品可能有更好的選擇，或是有更好的折扣。
                4. 檢查 FAQ 清單

                如果訊息是用 "確認:" 開始，請你按照上述規則檢驗。沒問題請回覆 "OK."，有問題請回覆你的建議。
                如果訊息是用 "操作紀錄:" 開始，代表客人已經可以自己處理，只是系統讓你知道客人做了什麼。記住歷程就好，若沒任何問題只要回覆 "OK." 即可

                如果你需要呼叫 function 來幫助客人，請遵守下列原則:
                1. 查詢的動作你可以直接執行不必詢問
                2. 未經客人同意，請勿直接變更購物車內容，或是進行結帳
                3. 任何 function 呼叫，都需要明確讓客人知道，並且告知執行結果

                以下是 FAQ 清單:
                1. 若購物車已經是空的，客人又嘗試清除購物車，可能碰到操作異常。請直接詢問是否需要幫助。
                2. 若購物車是空的，客人嘗試結帳，可能漏掉部分操作。請直接提醒客人留意，並在結帳前主動列出購物車內容再次確認。
                3. 購買含酒精飲料請提醒客人年齡限制，法律限制，避免酒駕。
                4. 購買含糖飲料請提醒客人注意醣類攝取。
                5. 購買含咖啡因飲料請提醒客人注意咖啡因攝取。
                6. 有預算要求，請留意折扣規則。部分優惠折扣可能導致買越多越便宜，請代替客人確認是否多買一件真的會超過預算。
                """);

            _chatCompletionService = _kernel.Services.GetRequiredService<IChatCompletionService>();
            #endregion
        }


        private static void CopilotNotify(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"copilot notify > {message}");
            Console.ResetColor();

            _chatMessages.AddUserMessage("操作紀錄: " + message);
            var result = _chatCompletionService.GetChatMessageContentsAsync(
                _chatMessages,
                _settings,
                _kernel).Result;

            foreach (var content in result)
            {
                _chatMessages.AddAssistantMessage(content.Content);

                if (!content.Content.StartsWith("OK"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"copilot hint > {content.Content}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"copilot hint > {content.Content}");
                }
                Console.ResetColor();
            }
            Console.ResetColor();
        }


        private static bool CopilotConfirm(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return true;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"copilot confirm > {prompt}");
            Console.ResetColor();

            _chatMessages.AddUserMessage("確認: " + prompt);
            var result = _chatCompletionService.GetChatMessageContentsAsync(
                _chatMessages,
                _settings,
                _kernel).Result;

            bool safe = true;
            foreach(var content in result)
            {
                _chatMessages.AddAssistantMessage(content.Content);

                if (!content.Content.StartsWith("OK"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"copilot hint > {content.Content}");
                    safe = false;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"copilot hint > {content.Content}");
                }
                Console.ResetColor();
            }
            Console.ResetColor();
            return safe;
        }

        private static void CopilotAsk(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"copilot ask > {prompt}");

            _chatMessages.AddUserMessage(prompt);
            var result = _chatCompletionService.GetChatMessageContentsAsync(
                _chatMessages,
                _settings,
                _kernel).Result;

            foreach (var content in result)
            {
                _chatMessages.AddAssistantMessage(content.Content);
                Console.WriteLine($"copilot answer > {content.Content}");
            }
            Console.ResetColor();
        }



        #region SK: shop plugins

        // Cart_Empty
        [KernelFunction, Description("清空購物車。購物車內的代結帳商品清單會完全歸零回復原狀")]
        public static void ShopFunction_EmptyCart()
        {
            //Console.WriteLine($"copilot call > EmptyCart()");

            _cartId = Cart.Create().Id;
        }

        // Cart_AddItem
        [KernelFunction, Description("將指定的商品與指定的數量加入購物車。加入成功會傳回 TRUE，若加入失敗會傳回 FALSE，購物車內容會維持原狀不會改變")]
        public static bool ShopFunction_AddItemToCart(
            [Description("指定加入購物車的商品ID")] int productId,
            [Description("指定加入購物車的商品數量")] int quanty)
        {
            //Console.WriteLine($"copilot call > AddItemToCart(productId: {productId}, quanty: {quanty})");

            var cart = Cart.Get(_cartId);
            return cart.AddProducts(productId, quanty);
        }

        // Cart_EstimatePrice
        [KernelFunction, Description("試算目前購物車的結帳金額 (包含可能發生的折扣)")]
        public static decimal ShopFunction_EstimatePrice()
        {
            //Console.WriteLine($"copilot call > EstimatePrice()");

            return Cart.Get(_cartId).EstimatePrice();
        }

        [KernelFunction, Description("傳回目前購物車的內容狀態")]
        public static Cart ShopFunction_ShowMyCartItems()
        {
            //Console.WriteLine($"copilot call > ShowMyCartItems()");

            return Cart.Get(_cartId);
            //AssistantOutput($"您好, 你目前購物車內共有 {cart.LineItems.Count()} 件商品, 總共 {cart.EstimatePrice():C} 元.");

            //if (cart.LineItems.Count() == 0)
            //{
            //    Console.WriteLine($"您的購物車現在是空的，可以用 22 [product-id] 選購商品喔. 或是用 1 可以看我們的商品介紹~");
            //    return;
            //}

            //foreach (var item in cart.LineItems)
            //{
            //    var product = Product.Database[item.ProductId];
            //    Console.WriteLine($"- 商品: [{product.Id}] {product.Name}\t{product.Price:C} x {item.Qty}");
            //}

            //foreach (var discount in cart.EstimateDiscounts())
            //{
            //    Console.WriteLine($"- 折扣: [{discount.Name}], {discount.Description}\t{discount.DiscountAmount:C}");
            //}
        }

        // Cart_AddItemWithBudget
        //private static bool ShopFunction_AddItemToCartWithBudget(int productId, decimal budget)
        //{
        //    var cart = Cart.Get(_cartId);
        //    var product = Product.Database[productId];

        //    if (cart.EstimatePrice() > budget)
        //    {
        //        //AssistantOutput($"您的預算 {budget:C} 不足以購買商品 [{pid}]。");
        //        //return;
        //        return false;
        //    }

        //    // add product to cart as much as possible, until reach the budget
        //    int total = 0;
        //    while (cart.EstimatePrice() <= budget)
        //    {
        //        cart.AddProducts(productId, 1);
        //        total++;
        //        InfoOutput($"add {productId} x 1, estimate: {cart.EstimatePrice()}");
        //    }
        //    cart.AddProducts(productId, -1); // remove the last one
        //    total--;
        //    InfoOutput($"rmv  {productId} x 1, estimate: {cart.EstimatePrice()}");
        //    //AssistantOutput($"您的預算 {budget:C} 可以再購買商品 [{pid}] {product.Name} x {total} 件, 總金額為 {cart.EstimatePrice():C}, 已為您加入購物車了。");
        //    return true;
        //}

        // Cart_Checkout
        [KernelFunction, Description("購買目前購物車內的商品清單，提供支付代碼，完成結帳程序，傳回訂單內容")]
        public static Order ShopFunction_Checkout(
            [Description("支付代碼，此代碼代表客戶已經在外部系統完成付款")] int paymentId)
        {
            //Console.WriteLine($"copilot call > Checkout(paymentId: {paymentId})");

            int checkout_id = Checkout.Create(_cartId, _access_token);
            var response = Checkout.CompleteAsync(checkout_id, paymentId);
            while (!response.Wait(1000))
            {
                //Console.WriteLine("waiting for payment...");
                InfoOutput("waiting for payment...");
            }

            if (response.Result != null)
            {
                //AssistantOutput($"Checkout completed successfully. your order number is: [{result.Result.Id}]");
                _cartId = Cart.Create().Id;
                return response.Result;
            }

            return null;
        }

        // Product_List
        [KernelFunction, Description("傳回店內所有出售的商品品項資訊")]
        public static Product[] ShopFunction_ListProducts()
        {
            //Console.WriteLine($"copilot call > ListProducts()");

            return Product.Database.Values.ToArray();
        }

        // Product_Get
        [KernelFunction, Description("傳回指定商品 ID 的商品內容")]
        public static Product ShopFunction_GetProduct(
            [Description("指定查詢的商品 ID")] int productId)
        {
            //Console.WriteLine($"copilot call > GetProduct(productId: {productId})");

            if (!Product.Database.ContainsKey(productId)) return null;
            return Product.Database[productId];
        }

        // Member_Get
        [KernelFunction, Description("傳回我 (目前登入) 的個人資訊")]
        public static Member ShopFunction_GetMyInfo()
        {
            //Console.WriteLine($"copilot call > GetMyInfo()");

            return Member.GetCurrentMember(_access_token);
        }

        // Member_GetOrders
        [KernelFunction, Description("傳回我 (目前登入) 的過去訂購紀錄")]
        public static Order[] ShopFunction_GetMyOrders()
        {
            //Console.WriteLine($"copilot call > GetMyOrders()");

            var member = Member.GetCurrentMember(_access_token);
            return Order.GetOrders(member.Id).ToArray();
        }

        #endregion
    }
}