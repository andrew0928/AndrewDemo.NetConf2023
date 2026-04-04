using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.Storefront.Shared.Authentication;

public abstract class AuthenticatedPageModel : StorefrontPageModel
{
    protected AuthenticatedPageModel(StorefrontSessionAccessor sessionAccessor)
        : base(sessionAccessor)
    {
    }

    protected string? AccessToken => SessionAccessor.GetAccessToken();

    protected int? CurrentCartId => SessionAccessor.GetCartId();

    protected IActionResult? EnsureAuthenticated()
    {
        return string.IsNullOrWhiteSpace(AccessToken)
            ? RedirectToLogin()
            : null;
    }
}
