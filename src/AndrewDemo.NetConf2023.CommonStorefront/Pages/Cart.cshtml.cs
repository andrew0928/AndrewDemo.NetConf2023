using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.CommonStorefront.Pages;

public sealed class CartModel : AuthenticatedPageModel
{
    private readonly CoreApiClient _coreApiClient;

    public CartModel(StorefrontSessionAccessor sessionAccessor, CoreApiClient coreApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
    }

    public List<CartLineViewModel> Lines { get; private set; } = new();

    public PriceSummaryViewModel PriceSummary { get; private set; } = new();

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        await LoadCartAsync(cancellationToken);
        return Page();
    }

    private async Task LoadCartAsync(CancellationToken cancellationToken)
    {
        if (AccessToken == null || CurrentCartId == null)
        {
            return;
        }

        try
        {
            var cart = await _coreApiClient.GetCartAsync(AccessToken, CurrentCartId.Value, cancellationToken);
            if (cart == null)
            {
                SessionAccessor.ClearCartId();
                return;
            }

            var products = new Dictionary<string, AndrewDemo.NetConf2023.Abstract.Products.Product?>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in cart.LineItems)
            {
                if (!products.ContainsKey(line.ProductId))
                {
                    products[line.ProductId] = await _coreApiClient.GetProductByIdAsync(line.ProductId, cancellationToken);
                }
            }

            Lines = cart.LineItems
                .Select(line =>
                {
                    var product = products[line.ProductId];
                    var unitPrice = product?.Price ?? 0m;
                    return new CartLineViewModel
                    {
                        LineId = line.LineId,
                        ParentLineId = line.ParentLineId,
                        ProductId = line.ProductId,
                        ProductName = product?.Name ?? line.ProductId,
                        UnitPrice = unitPrice,
                        Quantity = line.Quantity,
                        LineAmount = unitPrice * line.Quantity
                    };
                })
                .ToList();

            var estimate = await _coreApiClient.EstimateCartAsync(AccessToken, CurrentCartId.Value, cancellationToken);
            PriceSummary = new PriceSummaryViewModel
            {
                Subtotal = Lines.Sum(x => x.LineAmount),
                Total = estimate.TotalPrice,
                Adjustments = estimate.Discounts
                    .Select(x => new PriceAdjustmentViewModel
                    {
                        Kind = x.Kind,
                        Name = x.Name,
                        Description = x.Description,
                        Amount = x.Amount,
                        RelatedLineIds = x.RelatedLineIds
                    })
                    .ToList()
            };
        }
        catch (CoreApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
            ErrorSummary.Errors.Add("登入狀態已失效，請重新登入。");
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("購物車目前無法載入，請稍後再試。");
        }
    }
}
