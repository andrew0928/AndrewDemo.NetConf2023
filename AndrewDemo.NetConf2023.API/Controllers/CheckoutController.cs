using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/checkout")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {

        [HttpPost("create", Name = "CreateCheckout")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<CheckoutCreateResponse> Create([FromBody] CheckoutCreateRequest request)
        {
            var member = this.HttpContext.Items["Consumer"] as Member;
            if (member == null)
            {
                return Unauthorized();
            }

            var transactionId = Checkout.Create(request.CartId, member);            

            return new CheckoutCreateResponse()
            {
                TransactionId = transactionId,
                TransactionStartAt = DateTime.UtcNow,
                ConsumerId = member.Id,
                ConsumerName = member.Name
            };
        }


        [HttpPost("complete", Name = "CompleteCheckout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<CheckoutCompleteResponse>> CompleteAsync([FromBody] CheckoutCompleteRequest request)
        {
            var member = this.HttpContext.Items["Consumer"] as Member;
            if (member == null)
            {
                return Unauthorized();
            }

            var order = await Checkout.CompleteAsync(request.TransactionId, request.PaymentId);

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
            public int TransactionId { get; set; }
            public int PaymentId { get; set; }
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
}
