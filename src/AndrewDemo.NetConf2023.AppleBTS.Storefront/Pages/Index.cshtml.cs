using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Pages;

public sealed class IndexModel : StorefrontPageModel
{
    public IndexModel(StorefrontSessionAccessor sessionAccessor)
        : base(sessionAccessor)
    {
    }

    public void OnGet()
    {
    }
}
