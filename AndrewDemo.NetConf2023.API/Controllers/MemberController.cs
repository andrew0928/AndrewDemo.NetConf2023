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
        /// 註冊會員。
        /// </summary>
        /// <remarks>
        /// 若註冊成功，會傳回 201 Created，並且訊息會包含 AccessToken。
        /// 若 Name 已經存在，會回傳 400 Bad Request，並且訊息會說明 Name 已經存在。
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("register", Name = "RegisterMember")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<MemberAccessTokenResponse> Register([FromBody] MemberRegisterRequest request)
        {
            var token = Member.Register(request.Name);
            if (token == null)
            {
                return BadRequest($"Consumer's name: {request.Name} was existed.");
            }
            return new MemberAccessTokenResponse() { AccessToken = token };
        }

        /// <summary>
        /// 會員登入。
        /// </summary>
        /// <remarks>
        /// 若登入成功，會回傳 200 OK，並且訊息會包含 AccessToken。
        /// 若登入失敗，會回傳 401 Unauthorized。
        /// </remarks>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("login", Name = "LoginMember")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<MemberAccessTokenResponse> Login([FromBody] MemberLoginRequest request)
        {
            var token = Member.Login(request.Name, request.Password);
            if (token == null)
            {
                return Unauthorized();
            }
            return new MemberAccessTokenResponse() { AccessToken = token };
        }


        /// <summary>
        /// 取得目前登入的使用者基本資訊。
        /// </summary>
        /// <remarks>
        /// API 執行時的登入使用者身分，是由 APIKEY 決定的。這個 API 只能取得目前登入使用者的資訊，無法取得其他會員的資訊。
        /// </remarks>
        /// <returns></returns>
        [HttpGet(Name = "GetCurrentMember")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<Member> Get(
            //[FromHeader(Name = "Authorization")]string token
            )
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

            return member;
        }


        /// <summary>
        /// 取得目前登入的使用者已訂購過的訂單資訊。
        /// </summary>
        /// <returns></returns>
        [HttpGet("orders", Name = "GetMemberOrders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<MemberOrdersResponse> GetOrders(
            //[FromHeader(Name = "Authorization")] string token
            )
        {
            var accessToken = this.HttpContext.Items["access-token"] as string;
            if (accessToken == null)
            {
                return Unauthorized();
            }


            var member = Member.GetCurrentMember(accessToken); 
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



        public class MemberRegisterRequest
        {
            public string Name { get; set; }
        }

        public class MemberLoginRequest
        {
            public string Name { get; set; }
            public string Password { get; set; }
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

        public class MemberAccessTokenResponse
        {
            public string AccessToken { get; set; }
            //public DateTime ExpiredAt { get; set; }
        }
    }
}
