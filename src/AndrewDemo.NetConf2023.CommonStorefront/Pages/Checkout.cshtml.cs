using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.CommonStorefront.Pages;

public sealed class CheckoutModel : AuthenticatedPageModel
{
    private readonly CoreApiClient _coreApiClient;

    public CheckoutModel(StorefrontSessionAccessor sessionAccessor, CoreApiClient coreApiClient)
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

        await LoadCheckoutAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        await LoadCheckoutAsync(cancellationToken);
        if (!Lines.Any())
        {
            ErrorSummary.Errors.Add("目前沒有可結帳的商品。");
            return Page();
        }

        if (CurrentCartId == null || AccessToken == null)
        {
            return RedirectToLogin();
        }

        try
        {
            var createResult = await _coreApiClient.CreateCheckoutAsync(AccessToken, CurrentCartId.Value, cancellationToken);
            var paymentId = Random.Shared.Next(100000, 999999);

            await _coreApiClient.CompleteCheckoutAsync(AccessToken, new CompleteCheckoutRequestDto
            {
                TransactionId = createResult.TransactionId,
                PaymentId = paymentId,
                Satisfaction = null,
                ShopComments = null
            }, cancellationToken);

            SessionAccessor.ClearCartId();
            TempData["NotificationTitle"] = "訂單已建立";
            TempData["NotificationMessage"] = $"訂單 {createResult.TransactionId} 已完成建立。";
            TempData["NotificationTone"] = "success";
            return Redirect("/member/orders");
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("結帳目前無法完成，請稍後再試。");
            return Page();
        }
    }

    private async Task LoadCheckoutAsync(CancellationToken cancellationToken)
    {
        if (CurrentCartId == null || AccessToken == null)
        {
            return;
        }

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
}
