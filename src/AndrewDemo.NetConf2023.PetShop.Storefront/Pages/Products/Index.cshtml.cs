using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.Products;

public sealed class IndexModel : StorefrontPageModel
{
    private readonly CoreApiClient _coreApiClient;

    public IndexModel(StorefrontSessionAccessor sessionAccessor, CoreApiClient coreApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
    }

    public List<Product> Products { get; private set; } = new();

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Products = (await _coreApiClient.GetProductsAsync(cancellationToken)).ToList();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("商品列表目前無法載入，請稍後再試。");
        }
    }
}
