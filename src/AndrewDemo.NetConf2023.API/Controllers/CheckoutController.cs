using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/checkout")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly IShopDatabaseContext _database;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="database"></param>
        public CheckoutController(IShopDatabaseContext database)
        {
            _database = database;
        }

        /// <summary>
        /// 建立一個新的結帳交易。每次結帳都會有一個交易 ID (transactionId) 來識別。
        /// </summary>
        /// <remarks>
        /// 結帳交易的流程是：
        /// 1. 建立交易，須提供交易的消費者 ID，以及要結帳的購物車 ID。
        /// 2. 進行付款。付款由外部系統進行，這裡只需要提供付款的交易 ID。
        /// 3. 完成交易。完成交易時，會將購物車內容轉換成訂單，並且清空購物車。
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("create", Name = "CreateCheckout")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<CheckoutCreateResponse> Create(
            //[FromHeader(Name = "Authorization")] string token,
            [FromBody] CheckoutCreateRequest request)
        {
            var accessToken = this.HttpContext.Items["access-token"] as string;
            if (accessToken == null)
            {
                return Unauthorized();
            }

            var tokenRecord = _database.MemberTokens.FindById(accessToken);
            if (tokenRecord == null || tokenRecord.Expire <= DateTime.Now)
            {
                return Unauthorized();
            }

            var member = _database.Members.FindById(tokenRecord.MemberId);
            if (member == null)
            {
                return Unauthorized();
            }

            var cart = _database.Carts.FindById(request.CartId);
            if (cart == null)
            {
                return BadRequest("Cart not found");
            }

            var transaction = new CheckoutTransactionRecord
            {
                CartId = cart.Id,
                MemberId = member.Id,
                CreatedAt = DateTime.UtcNow
            };

            _database.CheckoutTransactions.Insert(transaction);

            return new CheckoutCreateResponse()
            {
                TransactionId = transaction.TransactionId,
                TransactionStartAt = DateTime.UtcNow,
                ConsumerId = member.Id,
                ConsumerName = member.Name
            };
        }

        /// <summary>
        /// 完成結帳。
        /// </summary>
        /// <remarks>
        /// 完成結帳時，會將購物車內容轉換成訂單。
        /// 呼叫這 API 時，必須提供付款的交易 ID，會同時完成交易的第二 及第三步驟。
        /// 成功後會建立訂單，並且回傳交易的內容 (包含訂單)。
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("complete", Name = "CompleteCheckout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CheckoutCompleteResponse>> CompleteAsync(
            //[FromHeader(Name = "Authorization")] string token,
            [FromBody] CheckoutCompleteRequest request)
        {
            var accessToken = this.HttpContext.Items["access-token"] as string;
            if (accessToken == null)
            {
                return Unauthorized();
            }

            var tokenRecord = _database.MemberTokens.FindById(accessToken);
            if (tokenRecord == null || tokenRecord.Expire <= DateTime.Now)
            {
                return Unauthorized();
            }

            var member = _database.Members.FindById(tokenRecord.MemberId);
            if (member == null)
            {
                return Unauthorized();
            }

            // Process checkout transaction
            var ticket = new WaitingRoomTicket();
            await ticket.WaitUntilCanRunAsync();

            var transaction = _database.CheckoutTransactions.FindById(request.TransactionId);
            if (transaction == null)
            {
                return BadRequest("Transaction not found");
            }

            _database.CheckoutTransactions.Delete(request.TransactionId);

            var cart = _database.Carts.FindById(transaction.CartId);
            if (cart == null)
            {
                return BadRequest("Cart not found");
            }

            var consumer = _database.Members.FindById(transaction.MemberId);
            if (consumer == null)
            {
                return BadRequest("Consumer not found");
            }

            var order = new Order(request.TransactionId)
            {
                Buyer = consumer
            };

            decimal total = 0m;

            foreach (var lineitem in cart.LineItems)
            {
                var product = _database.Products.FindById(lineitem.ProductId);
                if (product == null)
                {
                    return BadRequest($"Product {lineitem.ProductId} not found");
                }

                total += product.Price * lineitem.Qty;

                order.LineItems.Add(new Order.OrderLineItem
                {
                    Title = $"商品: {product.Name}, 單價: {product.Price} x {lineitem.Qty} 件 = {product.Price * lineitem.Qty:C}",
                    Price = product.Price * lineitem.Qty
                });
            }

            foreach (var discount in DiscountEngine.Calculate(cart, consumer, _database))
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
                BuyerSatisfaction = request.Satisfaction,
                Comments = request.ShopComments
            };

            _database.Orders.Upsert(order);

            return new CheckoutCompleteResponse()
            {
                TransactionId = request.TransactionId,
                PaymentId = request.PaymentId,

                TransactionCompleteAt = DateTime.UtcNow,
                ConsumerId = member.Id,
                ConsumerName = member.Name,

                OrderDetail = order
            };

        }





        /// <summary>
        /// 
        /// </summary>
        public class CheckoutCreateRequest
        {
            /// <summary>
            /// 
            /// </summary>
            public int CartId { get; set; }
            //public string AccessToken { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class CheckoutCreateResponse
        {
            /// <summary>
            /// 
            /// </summary>
            public int TransactionId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public DateTime TransactionStartAt { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int ConsumerId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string ConsumerName { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class CheckoutCompleteRequest
        {
            /// <summary>
            /// 指定要完成的交易 transactionId
            /// </summary>
            public int TransactionId { get; set; }
            //public string AccessToken { get; set; }

            /// <summary>
            /// 指定該交易的支付代碼, 由外部支付系統提供
            /// </summary>
            public int PaymentId { get; set; }

            /// <summary>
            /// 消費者對這次購物的滿意度評分, 1 ~ 10 分
            /// </summary>
            public int Satisfaction { get; set; } = 0;

            /// <summary>
            /// 消費者對這次交易的註記，包含對商店的評價、建議等。
            /// </summary>
            public string? ShopComments { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class CheckoutCompleteResponse
        {
            /// <summary>
            /// 
            /// </summary>
            public int TransactionId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int PaymentId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public DateTime TransactionCompleteAt { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int ConsumerId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string ConsumerName { get; set; }
            
            /// <summary>
            /// 
            /// </summary>
            public Order OrderDetail { get; set; }
        }
    }






    // ref: https://queue-it.com/how-does-queue-it-work/
}
