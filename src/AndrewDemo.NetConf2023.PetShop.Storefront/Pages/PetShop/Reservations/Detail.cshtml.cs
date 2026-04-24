using AndrewDemo.NetConf2023.PetShop.Storefront.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.PetShop.Reservations;

public sealed class DetailModel : AuthenticatedPageModel
{
    private readonly CoreApiClient _coreApiClient;
    private readonly PetShopApiClient _petShopApiClient;

    public DetailModel(
        StorefrontSessionAccessor sessionAccessor,
        CoreApiClient coreApiClient,
        PetShopApiClient petShopApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
        _petShopApiClient = petShopApiClient;
    }

    public PetShopReservationDto? Reservation { get; private set; }

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        await LoadReservationAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(string id, CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        await LoadReservationAsync(id, cancellationToken);
        if (Reservation == null)
        {
            return Page();
        }

        if (!string.Equals(Reservation.Status, "holding", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(Reservation.CheckoutProductId))
        {
            ErrorSummary.Errors.Add("這筆預約目前不能加入購物車。");
            return Page();
        }

        try
        {
            var cartId = await EnsureCartAsync(cancellationToken);
            await _coreApiClient.AddCartItemAsync(AccessToken!, cartId, new AddCartItemRequestDto
            {
                ProductId = Reservation.CheckoutProductId,
                Qty = 1
            }, cancellationToken);

            TempData["NotificationTitle"] = "已加入購物車";
            TempData["NotificationMessage"] = $"{Reservation.ServiceName} 已加入購物車。";
            TempData["NotificationTone"] = "success";
            return Redirect("/cart");
        }
        catch (CoreApiRequestException)
        {
            ErrorSummary.Errors.Add("這筆預約已失效或目前無法加入購物車，請重新查詢預約狀態。");
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法加入購物車，請稍後再試。");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelHoldAsync(string id, CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        try
        {
            Reservation = await _petShopApiClient.CancelHoldAsync(AccessToken!, id, cancellationToken);
            TempData["NotificationTitle"] = "已取消預約保留";
            TempData["NotificationMessage"] = "這筆預約保留已取消。";
            TempData["NotificationTone"] = "success";
            return Redirect($"/petshop/reservations/{id}");
        }
        catch (PetShopApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
            return RedirectToLogin();
        }
        catch (PetShopApiRequestException)
        {
            ErrorSummary.Errors.Add("這筆預約目前不能取消，請重新查詢預約狀態。");
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法取消預約保留，請稍後再試。");
        }

        await LoadReservationAsync(id, cancellationToken);
        return Page();
    }

    public string FormatDateTime(DateTime value)
    {
        return NormalizeUtc(value).ToString("yyyy-MM-dd HH:mm 'UTC'");
    }

    public string DisplayStatus(string status)
    {
        return status switch
        {
            "holding" => "預約確認中",
            "confirmed" => "已預約",
            "expired" => "已逾期",
            "cancelled" => "已取消",
            _ => status
        };
    }

    private async Task LoadReservationAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            Reservation = await _petShopApiClient.GetReservationAsync(AccessToken!, id, cancellationToken);
        }
        catch (PetShopApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
            ErrorSummary.Errors.Add("登入狀態已失效，請重新登入。");
        }
        catch (PetShopApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            ErrorSummary.Errors.Add("你沒有權限查看這筆預約。");
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法載入預約明細，請稍後再試。");
        }
    }

    private async Task<int> EnsureCartAsync(CancellationToken cancellationToken)
    {
        var cartId = SessionAccessor.GetCartId();
        if (cartId != null)
        {
            return cartId.Value;
        }

        var cart = await _coreApiClient.CreateCartAsync(AccessToken!, cancellationToken);
        SessionAccessor.SetCartId(cart.Id);
        return cart.Id;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }
}
