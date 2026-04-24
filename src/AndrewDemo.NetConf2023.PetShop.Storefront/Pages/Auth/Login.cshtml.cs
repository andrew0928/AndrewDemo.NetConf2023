using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.Auth;

public sealed class LoginModel : StorefrontPageModel
{
    private readonly StorefrontAuthService _authService;

    public LoginModel(StorefrontSessionAccessor sessionAccessor, StorefrontAuthService authService)
        : base(sessionAccessor)
    {
        _authService = authService;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        var redirectUrl = _authService.BuildAuthorizeRedirect(HttpContext, returnUrl ?? "/");
        return Redirect(redirectUrl);
    }
}
