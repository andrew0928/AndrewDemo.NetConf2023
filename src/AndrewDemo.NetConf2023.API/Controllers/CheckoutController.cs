using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Time;
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
        private readonly CheckoutService _checkoutService;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="database">商店資料庫內容。</param>
        /// <param name="checkoutService">結帳流程服務。</param>
        /// <param name="timeProvider">目前系統的時間提供者。</param>
        public CheckoutController(IShopDatabaseContext database, CheckoutService checkoutService, TimeProvider timeProvider)
        {
            _database = database;
            _checkoutService = checkoutService;
            _timeProvider = timeProvider;
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
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized();
            }

            var result = _checkoutService.Create(new CheckoutCreateCommand
            {
                CartId = request.CartId,
                RequestMember = member
            });

            if (result.Status != CheckoutCreateStatus.Succeeded)
            {
                return BadRequest(result.ErrorMessage);
            }

            return new CheckoutCreateResponse
            {
                TransactionId = result.TransactionId,
                TransactionStartAt = result.TransactionStartAt,
                ConsumerId = result.ConsumerId,
                ConsumerName = result.ConsumerName
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
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<CheckoutCompleteResponse>> CompleteAsync(
            //[FromHeader(Name = "Authorization")] string token,
            [FromBody] CheckoutCompleteRequest request)
        {
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized();
            }

            var result = await _checkoutService.CompleteAsync(new CheckoutCompleteCommand
            {
                TransactionId = request.TransactionId,
                PaymentId = request.PaymentId,
                Satisfaction = request.Satisfaction,
                ShopComments = request.ShopComments,
                RequestMember = member
            });

            if (result.Status != CheckoutCompleteStatus.Succeeded)
            {
                if (result.Status == CheckoutCompleteStatus.BuyerMismatch)
                {
                    return Forbid();
                }

                return BadRequest(result.ErrorMessage);
            }

            return new CheckoutCompleteResponse
            {
                TransactionId = result.TransactionId,
                PaymentId = result.PaymentId,
                TransactionCompleteAt = result.TransactionCompleteAt,
                ConsumerId = result.ConsumerId,
                ConsumerName = result.ConsumerName,
                OrderDetail = result.OrderDetail!
            };
        }

        private Member? GetAuthenticatedMember()
        {
            var accessToken = HttpContext.Items["access-token"] as string;
            if (accessToken == null)
            {
                return null;
            }

            var tokenRecord = _database.MemberTokens.FindById(accessToken);
            if (tokenRecord == null || tokenRecord.Expire <= _timeProvider.GetLocalDateTime())
            {
                return null;
            }

            return _database.Members.FindById(tokenRecord.MemberId);
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
            /// 消費者對這次購物的滿意度評分。`null` 代表沒有提供或沒有判讀結果。
            /// </summary>
            public int? Satisfaction { get; set; }

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
