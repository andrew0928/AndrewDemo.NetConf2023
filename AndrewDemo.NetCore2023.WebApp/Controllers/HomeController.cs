using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetCore2023.WebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AndrewDemo.NetCore2023.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        
        public IActionResult SignIn(SignInRequest input)
        {
            if (input != null && 
                !string.IsNullOrWhiteSpace(input.name) && 
                !string.IsNullOrWhiteSpace(input.password) &&
                !string.IsNullOrWhiteSpace(input.client_id) &&
                !string.IsNullOrWhiteSpace(input.redirect_url))
            {
                
            }

            return View();
        }


        public IActionResult Authorize(string client_id, string redirect_url)
        {
            return View();
        }

        public IActionResult AccessToken(string client_id, string client_secret, string code)
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }






        public class SignInRequest
        {
            public string name { get; set; }
            public string password { get; set; }
            public string client_id { get; set; }
            public string redirect_url { get; set; }
        }
    }
}
