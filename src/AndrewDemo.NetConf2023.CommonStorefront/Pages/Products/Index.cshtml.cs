using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

namespace AndrewDemo.NetConf2023.CommonStorefront.Pages.Products;

public sealed class IndexModel : StorefrontPageModel
{
    private readonly CoreApiClient _coreApiClient;

    public IndexModel(StorefrontSessionAccessor sessionAccessor, CoreApiClient coreApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
    }

    public List<ProductSummaryViewModel> Products { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var products = await _coreApiClient.GetProductsAsync(cancellationToken);
            Products = products
                .Select(x => new ProductSummaryViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Price = x.Price
                })
                .ToList();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ErrorSummary.Errors.Add("商品列表目前無法載入，請稍後再試。");
        }
    }
}
