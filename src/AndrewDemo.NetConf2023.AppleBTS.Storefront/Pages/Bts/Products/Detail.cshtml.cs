using AndrewDemo.NetConf2023.AppleBTS.Storefront.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Pages.Bts.Products;

public sealed class DetailModel : StorefrontPageModel
{
    private readonly CoreApiClient _coreApiClient;
    private readonly AppleBtsApiClient _appleBtsApiClient;

    public DetailModel(
        StorefrontSessionAccessor sessionAccessor,
        CoreApiClient coreApiClient,
        AppleBtsApiClient appleBtsApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
        _appleBtsApiClient = appleBtsApiClient;
    }

    public BtsCatalogItemDto? Offer { get; private set; }

    public BtsGiftOptionDto? PendingGift { get; private set; }

    public BtsQualificationResponseDto? Qualification { get; private set; }

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task<IActionResult> OnGetAsync(string id, string? confirmGiftProductId, CancellationToken cancellationToken)
    {
        await LoadPageAsync(id, cancellationToken);
        if (Offer == null)
        {
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(confirmGiftProductId))
        {
            PendingGift = Offer.GiftOptions.FirstOrDefault(x => string.Equals(x.ProductId, confirmGiftProductId, StringComparison.OrdinalIgnoreCase));
            if (PendingGift == null)
            {
                ErrorSummary.Errors.Add("指定的贈品不存在於這個 BTS 商品組合內。");
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddMainAsync(string id, CancellationToken cancellationToken)
    {
        await LoadPageAsync(id, cancellationToken);
        if (Offer == null)
        {
            return NotFound();
        }

        var accessToken = SessionAccessor.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToLogin($"/bts/products/{id}");
        }

        try
        {
            var cartId = await EnsureCartAsync(accessToken, cancellationToken);
            await _coreApiClient.AddCartItemAsync(accessToken, cartId, new AddCartItemRequestDto
            {
                ProductId = Offer.MainProductId,
                Qty = 1
            }, cancellationToken);

            TempData["NotificationTitle"] = "已加入購物車";
            TempData["NotificationMessage"] = $"{Offer.MainProductName} 已加入購物車。";
            TempData["NotificationTone"] = "success";
            return Redirect("/cart");
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法將主商品加入購物車。");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddBundleAsync(string id, string giftProductId, CancellationToken cancellationToken)
    {
        await LoadPageAsync(id, cancellationToken);
        if (Offer == null)
        {
            return NotFound();
        }

        PendingGift = Offer.GiftOptions.FirstOrDefault(x => string.Equals(x.ProductId, giftProductId, StringComparison.OrdinalIgnoreCase));
        if (PendingGift == null)
        {
            ErrorSummary.Errors.Add("指定的贈品不存在於這個 BTS 商品組合內。");
            return Page();
        }

        var accessToken = SessionAccessor.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToLogin($"/bts/products/{id}?confirmGiftProductId={Uri.EscapeDataString(giftProductId)}");
        }

        try
        {
            var cartId = await EnsureCartAsync(accessToken, cancellationToken);
            var cart = await _coreApiClient.AddCartItemAsync(accessToken, cartId, new AddCartItemRequestDto
            {
                ProductId = Offer.MainProductId,
                Qty = 1
            }, cancellationToken);

            var mainLine = cart.LineItems.LastOrDefault();
            if (mainLine == null || !string.Equals(mainLine.ProductId, Offer.MainProductId, StringComparison.OrdinalIgnoreCase))
            {
                ErrorSummary.Errors.Add("目前無法建立主商品與贈品的綁定關係。");
                return Page();
            }

            await _coreApiClient.AddCartItemAsync(accessToken, cartId, new AddCartItemRequestDto
            {
                ProductId = PendingGift.ProductId,
                Qty = 1,
                ParentLineId = mainLine.LineId
            }, cancellationToken);

            TempData["NotificationTitle"] = "已加入 BTS 組合";
            TempData["NotificationMessage"] = $"{Offer.MainProductName} 與 {PendingGift.ProductName} 已加入購物車。";
            TempData["NotificationTone"] = "success";
            return Redirect("/cart");
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法將 BTS 商品組合加入購物車。");
            return Page();
        }
    }

    private async Task LoadPageAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            Offer = await _appleBtsApiClient.GetOfferDetailAsync(id, cancellationToken);
            if (Offer == null)
            {
                return;
            }
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("BTS 商品詳細資料目前無法載入。");
            return;
        }

        var accessToken = SessionAccessor.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        try
        {
            Qualification = await _appleBtsApiClient.GetCurrentQualificationAsync(accessToken, cancellationToken);
        }
        catch (AppleBtsApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法讀取教育資格狀態。");
        }
    }

    private async Task<int> EnsureCartAsync(string accessToken, CancellationToken cancellationToken)
    {
        var cartId = SessionAccessor.GetCartId();
        if (cartId != null)
        {
            return cartId.Value;
        }

        var cart = await _coreApiClient.CreateCartAsync(accessToken, cancellationToken);
        SessionAccessor.SetCartId(cart.Id);
        return cart.Id;
    }
}
