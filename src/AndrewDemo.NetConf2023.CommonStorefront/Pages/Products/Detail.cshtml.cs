using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.CommonStorefront.Pages.Products;

public sealed class DetailModel : StorefrontPageModel
{
    private readonly CoreApiClient _coreApiClient;

    public DetailModel(StorefrontSessionAccessor sessionAccessor, CoreApiClient coreApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
    }

    public Product? Product { get; private set; }

    [BindProperty]
    public int Quantity { get; set; } = 1;

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        Product = await _coreApiClient.GetProductByIdAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id, CancellationToken cancellationToken)
    {
        Product = await _coreApiClient.GetProductByIdAsync(id, cancellationToken);
        if (Product == null)
        {
            return NotFound();
        }

        if (Quantity <= 0)
        {
            ErrorSummary.Errors.Add("數量必須大於 0。");
            return Page();
        }

        var accessToken = SessionAccessor.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToLogin($"/products/{id}");
        }

        var cartId = SessionAccessor.GetCartId();
        if (cartId == null)
        {
            var cart = await _coreApiClient.CreateCartAsync(accessToken, cancellationToken);
            cartId = cart.Id;
            SessionAccessor.SetCartId(cartId.Value);
        }

        await _coreApiClient.AddCartItemAsync(accessToken, cartId.Value, new AddCartItemRequestDto
        {
            ProductId = id,
            Qty = Quantity
        }, cancellationToken);

        TempData["NotificationTitle"] = "已加入購物車";
        TempData["NotificationMessage"] = $"{Product.Name} 已加入購物車。";
        TempData["NotificationTone"] = "success";
        return Redirect("/cart");
    }
}
