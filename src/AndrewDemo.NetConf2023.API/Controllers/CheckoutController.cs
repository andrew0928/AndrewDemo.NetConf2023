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
        /// 1. 建立交易，提供購物車 ID 與 PaymentId，會員由 API 驗證後明確傳入。
        /// 2. 交易保存 PaymentId；此 PoC 不代表已接入真實金流。
        /// 3. 完成交易時，使用保存的 PaymentId，將購物車內容轉換成訂單。
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
                PaymentId = request.PaymentId,
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
        /// 呼叫時提供 TransactionId；付款來源使用 create 時保存的 PaymentId。
        /// 成功後會建立訂單，並且回傳交易的內容 (包含訂單)。
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("complete", Name = "CompleteCheckout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

                if (result.Status == CheckoutCompleteStatus.RecreateRequired)
                {
                    return Conflict(new { code = "checkout_recreate_required", message = result.ErrorMessage });
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

            /// <summary>建立時指定並保存的付款來源代碼。</summary>
            public required int PaymentId { get; set; }
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
