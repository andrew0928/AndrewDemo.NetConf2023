using AndrewDemo.NetConf2023.PetShop.Storefront.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.PetShop;

public sealed class IndexModel : StorefrontPageModel
{
    private readonly PetShopApiClient _petShopApiClient;

    public IndexModel(StorefrontSessionAccessor sessionAccessor, PetShopApiClient petShopApiClient)
        : base(sessionAccessor)
    {
        _petShopApiClient = petShopApiClient;
    }

    public List<PetShopServiceDto> Services { get; private set; } = new();

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Services = (await _petShopApiClient.GetServicesAsync(cancellationToken)).ToList();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("PetShop 美容服務目前無法載入，請稍後再試。");
        }
    }
}
