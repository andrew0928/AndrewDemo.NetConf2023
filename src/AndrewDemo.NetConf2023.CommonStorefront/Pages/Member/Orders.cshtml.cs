using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.CommonStorefront.Pages.Member;

public sealed class OrdersModel : AuthenticatedPageModel
{
    private readonly CoreApiClient _coreApiClient;

    public OrdersModel(StorefrontSessionAccessor sessionAccessor, CoreApiClient coreApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
    }

    public int TotalOrders { get; private set; }

    public decimal TotalAmount { get; private set; }

    public List<OrderDto> Orders { get; private set; } = new();

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        try
        {
            var response = await _coreApiClient.GetMemberOrdersAsync(AccessToken!, cancellationToken);
            TotalOrders = response.TotalOrders;
            TotalAmount = response.TotalAmount;
            Orders = response.Orders;
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("訂單列表目前無法載入。");
        }

        return Page();
    }
}
