using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    /// <summary>
    /// 提供會員資料與交易紀錄的查詢與更新功能。
    /// </summary>
    [Route("api/member")]
    [ApiController]
    public class MemberController : ControllerBase
    {
        private readonly IShopDatabaseContext _database;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="database"></param>
        public MemberController(IShopDatabaseContext database)
        {
            _database = database;
        }

        // remove useless api for register and login

        /*
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
        */


        /// <summary>
        /// 更新會員註記 (商店端)
        /// 商店可在交易過程中，對會員做一些註記，例如：該會員的偏好、特殊需求等等。
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("shop_notes", Name = "SetShopNotes")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<Member> SetShopNotes([FromBody] MemberSetNotesRequest request)
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

            member.ShopNotes = request.ShopNotes;
            _database.Members.Update(member);

            return member;
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


            var tokenRecord = _database.MemberTokens.FindById(accessToken);
            if (tokenRecord == null || tokenRecord.Expire <= DateTime.Now)
            {
                return Unauthorized();
            }

            var member = _database.Members.FindById(tokenRecord.MemberId);
            if (member == null)
            {
                return NotFound();
            }

            int count = 0;
            decimal amount = 0;
            var orders = _database.Orders.Find(o => o.Buyer.Id == member.Id).ToList();
            
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



    /// <summary>
    /// 會員註冊請求資料。
    /// </summary>
    public class MemberRegisterRequest
        {
            /// <summary>
            /// 會員名稱
            /// </summary>
            public string Name { get; set; } = string.Empty;
        }

    /// <summary>
    /// 會員登入請求資料。
    /// </summary>
    public class MemberLoginRequest
        {
            /// <summary>
            /// 登入帳號
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 登入密碼
            /// </summary>
            public string Password { get; set; } = string.Empty;
        }

    /// <summary>
    /// 更新會員註記的請求資料。
    /// </summary>
    public class MemberSetNotesRequest
        {
            /// <summary>
            /// 會員註記
            /// </summary>
            public string ShopNotes { get; set; } = string.Empty;
        }

    /// <summary>
    /// 會員訂單查詢回應資料。
    /// </summary>
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
            public List<Order> Orders { get; set; } = new List<Order>();
        }

        /// <summary>
        /// 會員存取權杖回應資料。
        /// </summary>
        public class MemberAccessTokenResponse
        {
            /// <summary>
            /// 存取權杖。
            /// </summary>
            public string AccessToken { get; set; } = string.Empty;
            //public DateTime ExpiredAt { get; set; }
        }


    }
}
