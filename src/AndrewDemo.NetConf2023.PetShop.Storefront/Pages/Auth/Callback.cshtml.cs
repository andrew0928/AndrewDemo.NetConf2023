using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.Auth;

public sealed class CallbackModel : StorefrontPageModel
{
    private readonly StorefrontAuthService _authService;

    public CallbackModel(StorefrontSessionAccessor sessionAccessor, StorefrontAuthService authService)
        : base(sessionAccessor)
    {
        _authService = authService;
    }

    public async Task<IActionResult> OnGetAsync(string code, string? state, CancellationToken cancellationToken)
    {
        try
        {
            var returnUrl = await _authService.HandleOAuthCallbackAsync(code, state, cancellationToken);
            TempData["NotificationTitle"] = "登入成功";
            TempData["NotificationMessage"] = "已完成登入。";
            TempData["NotificationTone"] = "success";
            return Redirect(returnUrl);
        }
        catch
        {
            TempData["NotificationTitle"] = "登入失敗";
            TempData["NotificationMessage"] = "無法完成登入流程，請重新再試。";
            TempData["NotificationTone"] = "warning";
            return Redirect("/");
        }
    }
}
