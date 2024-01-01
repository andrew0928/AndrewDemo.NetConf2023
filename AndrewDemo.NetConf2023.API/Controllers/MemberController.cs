using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{

    [Route("api/member")]
    [ApiController]
    public class MemberController : ControllerBase
    {


        /// <summary>
        /// 取得目前登入的使用者基本資訊。
        /// </summary>
        /// <remarks>
        /// API 執行時的登入使用者身分，是由 APIKEY 決定的。這個 API 只能取得目前登入使用者的資訊，無法取得其他會員的資訊。
        /// </remarks>
        /// <returns></returns>
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


        /// <summary>
        /// 取得目前登入的使用者已訂購過的訂單資訊。
        /// </summary>
        /// <returns></returns>
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
            /// <summary>
            /// 總訂單數量
            /// </summary>
            public int TotalOrders { get; set; }

            /// <summary>
            /// 總訂單金額
            /// </summary>
            public decimal TotalAmount { get; set; }

            /// <summary>
            /// 訂單列表
            /// </summary>
            public List<Order> Orders { get; set; }
        }
    }
}
