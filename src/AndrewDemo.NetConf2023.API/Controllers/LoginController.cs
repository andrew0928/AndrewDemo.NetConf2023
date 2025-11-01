using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    /// <summary>
    /// 處理登入與 OAuth2 授權流程。
    /// </summary>
    [Route("api/login")]
    [ApiController]
    [AllowAnonymous]
    public class LoginController : ControllerBase
    {
        private static Dictionary<string, string> _codes = new Dictionary<string, string>();


        /// <summary>
        /// GET, 顯示登入表單
        /// </summary>
        /// <returns>表單 HTML 內容</returns>
        [HttpGet("authorize", Name = "signin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetAuthorize()
        {
            return File("signin.html", "text/html");
        }

        /// <summary>
        /// POST, 接收登入表單，並且進行登入。按照 OAuth2 規範，驗證成功後會產生 grant_code 並且導向 redirect_uri。
        /// </summary>
        /// <remarks>這個版本的實做, 只要有填 name 一定會登入成功。密碼會被忽略，如果帳號不存在會自動註冊。</remarks>
        /// <param name="name">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="clientId">Oauth2: client-id</param>
        /// <param name="redirectURL">Oauth2: redirect-uri</param>
        /// <param name="state">Oauth2: state</param>
        /// <returns></returns>
        [HttpPost("authorize", Name = "oauth_authorize")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public IActionResult PostAuthorize(
            [FromForm(Name = "name")] string ?name,
            [FromForm(Name = "password")] string? password,
            [FromForm(Name = "client_id"), Required] string clientId,
            [FromForm(Name = "redirect_uri"), Required] string redirectURL,
            [FromForm(Name = "state")] string? state)
        {
            string token = Member.Login(name ?? string.Empty, password ?? string.Empty);
            if (token == null)
            {
                Console.WriteLine($"[/api/login/authorize] Login failed: {name}, try register...");
                token = Member.Register(name ?? string.Empty);
            }

            string code = Guid.NewGuid().ToString("N");
            _codes[code] = token;
            Console.WriteLine($"[/api/login/authorize] Authorize success: {name}, code: {code}, token: {token}");

            Console.WriteLine($"[/api/login/authorize] Redirect to: {redirectURL}?code={code}&state={state}");
            return Redirect($"{redirectURL}?code={code}&state={state}");
        }


        /// <summary>
        /// 支援 OAuth2 規範, 讓 application 對 authorizer 進行 token 的交換 (拿 code 換 access-token, 只能執行一次)。
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("token", Name = "oauth_token_exchange")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Consumes("application/x-www-form-urlencoded")]
        public IActionResult PostToken([FromForm]TokenRequest request)
        {
            Console.WriteLine($"[/api/login/token] Request Form: [{request}]");

            if (!_codes.ContainsKey(request.code))
            {
                Console.WriteLine($"[/api/login/token] Invalid code: {request.code}");
                return BadRequest();
            }

            string token = _codes[request.code];
            _codes.Remove(request.code);

            Console.WriteLine($"[/api/login/token] Return access-token: {token}");

            return Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = 3600
            });
        }

        /// <summary>
        /// OAuth2 代碼交換請求。
        /// </summary>
        public class TokenRequest
        {
            /// <summary>
            /// 授權碼。
            /// </summary>
            public string code { get; set; } = string.Empty;
            //public string grant_type { get; set; }
            //public string client_id { get; set; }
            //public string client_secret { get; set; }
            //public string redirect_uri { get; set; }
        }
    }
}
