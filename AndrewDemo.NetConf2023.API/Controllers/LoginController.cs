using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/login")]
    [ApiController]
    [AllowAnonymous]
    public class LoginController : ControllerBase
    {
        private static Dictionary<string, string> _codes = new Dictionary<string, string>();

        [HttpGet("authorize", Name = "oauth_authorize")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public IActionResult Authorize(
            [FromQuery(Name = "client_id")] string clientId,
            [FromQuery(Name = "redirect_uri")] string redirectURL,
            [FromQuery(Name = "state")] string state)
        {
            if (Request.Query["name"].Count == 0)
            {
                return File("signin.html", "text/html");
            }
            else
            {
                string token = null;
                string name = Request.Query["name"][0];


                token = Member.Login(name, "0000");
                if (token == null)
                {
                    Console.WriteLine($"[/api/login/authorize] Login failed: {name}, try register...");
                    token = Member.Register(name);
                }

                Console.WriteLine($"[/api/login/authorize] Login success: {name}, token: {token}");
                string code = Guid.NewGuid().ToString("N");
                _codes[code] = token;

                Console.WriteLine($"[/api/login/authorize] Redirect to: {redirectURL}?code={code}&state={state}");
                return Redirect($"{redirectURL}?code={code}&state={state}");
            }
        }


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

        public class TokenRequest
        {
            public string code { get; set; }
            //public string grant_type { get; set; }
            //public string client_id { get; set; }
            //public string client_secret { get; set; }
            //public string redirect_uri { get; set; }
        }
    }
}
