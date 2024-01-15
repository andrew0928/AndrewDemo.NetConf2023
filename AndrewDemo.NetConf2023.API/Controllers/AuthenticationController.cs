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
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthenticationController : ControllerBase
    {
        private IHttpClientFactory _httpClientFactory;
        public AuthenticationController(IHttpClientFactory httpClientFactory)
        {
            this._httpClientFactory = httpClientFactory;
        }

        // saved in notion
        private string _clientId = "51a88ad43f758bb868d2";
        private string _clientSecret = "0fbf213bc34c91603c31f0876d306653170fc243";
        private string _redirectUrl = "http://localhost:5108/api/authentication/github/callback";


        [HttpGet("signin")]
        public IActionResult SignIn()
        {
            return File("signin.html", "text/html");
            //return Ok("Hello World");
            
        }




        [HttpGet("github/login")]
        public IActionResult LoginWithGitHub()
        {
            return Redirect($"https://github.com/login/oauth/authorize?client_id={this._clientId}&redirect_url={this._redirectUrl}");
            // 构建并返回 GitHub 登录 URL
            // 例如：https://github.com/login/oauth/authorize?client_id=xxx&redirect_uri=yyy&scope=zzz

        }

        [HttpGet("github/callback")]
        public async Task<IActionResult> GitHubCallback(string code)
        {
            var client = this._httpClientFactory.CreateClient();
            string accessToken = null;
            GitHubUser user = null;

            { 
                var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = this._clientId,
                        ["client_secret"] = this._clientSecret,
                        ["code"] = code,
                        ["redirect_uri"] = this._redirectUrl
                    })
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                accessToken = payload.RootElement.GetProperty("access_token").GetString();
            }
            return Ok($"oauth2-access-token: {accessToken}");



            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AppName", "1.0")); // 替换为您的应用名称和版本

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return null; // 或处理错误
                }

                var content = await response.Content.ReadAsStringAsync();
                user = JsonSerializer.Deserialize<GitHubUser>(content);
            }

            

            return Ok($"access_token={Member.Register(user.Login)}");
        }



    }

    public class GitHubUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        // 添加其他您需要的字段
    }

}
