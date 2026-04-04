using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Pages.Auth;

public sealed class LogoutModel : StorefrontPageModel
{
    private readonly StorefrontAuthService _authService;

    public LogoutModel(StorefrontSessionAccessor sessionAccessor, StorefrontAuthService authService)
        : base(sessionAccessor)
    {
        _authService = authService;
    }

    public IActionResult OnGet()
    {
        _authService.Logout();
        TempData["NotificationTitle"] = "已登出";
        TempData["NotificationMessage"] = "登入狀態已清除。";
        TempData["NotificationTone"] = "success";
        return Redirect("/");
    }
}
