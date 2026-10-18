using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
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

    [BindProperty]
    [Required(ErrorMessage = "請輸入 PaymentID（模擬卡號）。")]
    public int? PaymentId { get; set; }

    public ApiOperationSummary? Operation { get; private set; }

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

        if (!ModelState.IsValid || !PaymentId.HasValue)
        {
            const string message = "請輸入有效的整數 PaymentID（模擬卡號）。";
            if (ModelState.TryGetValue(nameof(PaymentId), out var entry)) entry.Errors.Clear();
            ModelState.AddModelError(nameof(PaymentId), message);
            ErrorSummary.Errors.Add(message);
            return Page();
        }

        try
        {
            var createResult = await _coreApiClient.CreateCheckoutAsync(AccessToken, CurrentCartId.Value, PaymentId.Value, cancellationToken);

            var completeResult = await _coreApiClient.CompleteCheckoutAsync(AccessToken, new CompleteCheckoutRequestDto
            {
                TransactionId = createResult.TransactionId,
                Satisfaction = null,
                ShopComments = null
            }, cancellationToken);

            Operation = completeResult.Operation;
            TempData["CheckoutOperation"] = JsonSerializer.Serialize(Operation);
            SessionAccessor.ClearCartId();
            TempData["NotificationTitle"] = "訂單已建立";
            TempData["NotificationMessage"] = $"訂單 {completeResult.OrderDetail.Id} 已完成建立。";
            TempData["NotificationTone"] = "success";
            return Redirect("/member/orders");
        }
        catch (CoreApiRequestException error)
        {
            Operation = error.Operation;
            ErrorSummary.Errors.Add(error.StatusCode == HttpStatusCode.Conflict && IsRecreateRequired(error.ResponseBody)
                ? "這筆結帳資料已不適用，請重新確認購物車與 PaymentID，再送出以建立新的結帳。"
                : error.StatusCode == HttpStatusCode.BadRequest
                    ? "結帳遭拒絕，請確認商品與庫存後再試。"
                    : "結帳目前無法完成，請稍後再試。");
            return Page();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("結帳目前無法完成，請稍後再試。");
            return Page();
        }
    }

    private static bool IsRecreateRequired(string? body)
    {
        try
        {
            using var json = JsonDocument.Parse(body ?? "{}");
            return json.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String && code.GetString() == "checkout_recreate_required";
        }
        catch (JsonException) { return false; }
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
