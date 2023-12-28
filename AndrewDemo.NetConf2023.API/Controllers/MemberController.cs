using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/member")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        [HttpGet("", Name = "GetCurrentMember")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<Member> Get()
        {
            var member = this.HttpContext.Items["Consumer"] as Member;
            if (member == null)
            {
                return NotFound();
            }

            return member;
        }

        [HttpGet("orders", Name = "GetMemberOrders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<MemberOrdersResponse> GetOrders()
        {
            var member = this.HttpContext.Items["Consumer"] as Member;
            if (member == null)
            {
                return NotFound();
            }

            int count = 0;
            decimal amount = 0;
            var orders = Order.GetOrders(member.Id).ToList();
            
            foreach (var order in orders)
            {
                count++;
                amount += order.TotalPrice;
            }

            return new MemberOrdersResponse()
            {
                TotalOrders = count,
                TotalAmount = amount,
                Orders = orders
            };
        }


        public class MemberOrdersResponse
        {
            public int TotalOrders { get; set; }
            public decimal TotalAmount { get; set; }
            public List<Order> Orders { get; set; }
        }
    }
}
