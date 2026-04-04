using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AndrewDemo.NetConf2023.Storefront.Shared.Authentication;

public abstract class StorefrontPageModel : PageModel
{
    protected StorefrontPageModel(StorefrontSessionAccessor sessionAccessor)
    {
        SessionAccessor = sessionAccessor;
    }

    protected StorefrontSessionAccessor SessionAccessor { get; }

    protected string BuildCurrentRelativeUrl()
    {
        var path = Request.Path.HasValue ? Request.Path.Value : "/";
        var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return $"{path}{query}";
    }

    protected RedirectToPageResult RedirectToLogin(string? returnUrl = null)
    {
        return RedirectToPage("/Auth/Login", new { returnUrl = returnUrl ?? BuildCurrentRelativeUrl() });
    }
}
