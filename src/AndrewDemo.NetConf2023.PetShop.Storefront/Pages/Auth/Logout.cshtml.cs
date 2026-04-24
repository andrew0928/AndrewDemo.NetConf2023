using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.Auth;

public sealed class LogoutModel : StorefrontPageModel
{
    private readonly StorefrontAuthService _authService;

    public LogoutModel(StorefrontSessionAccessor sessionAccessor, StorefrontAuthService authService)
        : base(sessionAccessor)
    {
        _authService = authService;
    }

    public void OnGet()
    {
        _authService.Logout();
    }
}
