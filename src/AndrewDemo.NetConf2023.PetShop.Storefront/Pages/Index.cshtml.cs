using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages;

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
