﻿using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/checkout")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
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

            var member = Member.GetCurrentMember(accessToken);
            if (member == null)
            {
                return Unauthorized();
            }

            var transactionId = Checkout.Create(request.CartId, accessToken);            

            return new CheckoutCreateResponse()
            {
                TransactionId = transactionId,
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

            //var member = this.HttpContext.Items["Consumer"] as Member;
            var member = Member.GetCurrentMember(accessToken);
            if (member == null)
            {
                return Unauthorized();
            }

            var order = await Checkout.CompleteAsync(request.TransactionId, request.PaymentId, request.Satisfaction, request.ShopComments);

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





        public class CheckoutCreateRequest
        {
            public int CartId { get; set; }
            //public string AccessToken { get; set; }
        }

        public class CheckoutCreateResponse
        {
            public int TransactionId { get; set; }
            public DateTime TransactionStartAt { get; set; }
            public int ConsumerId { get; set; }
            public string ConsumerName { get; set; }
        }

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
            public string ShopComments { get; set; } = null;
        }

        public class CheckoutCompleteResponse
        {
            public int TransactionId { get; set; }
            public int PaymentId { get; set; }
            public DateTime TransactionCompleteAt { get; set; }
            public int ConsumerId { get; set; }
            public string ConsumerName { get; set; }
            
            public Order OrderDetail { get; set; }
        }
    }






    // ref: https://queue-it.com/how-does-queue-it-work/
}
