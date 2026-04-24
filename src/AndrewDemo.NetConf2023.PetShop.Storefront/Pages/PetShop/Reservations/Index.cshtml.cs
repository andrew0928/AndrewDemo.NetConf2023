using AndrewDemo.NetConf2023.PetShop.Storefront.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.PetShop.Reservations;

public sealed class IndexModel : AuthenticatedPageModel
{
    private readonly PetShopApiClient _petShopApiClient;

    public IndexModel(StorefrontSessionAccessor sessionAccessor, PetShopApiClient petShopApiClient)
        : base(sessionAccessor)
    {
        _petShopApiClient = petShopApiClient;
    }

    public List<PetShopReservationDto> Reservations { get; private set; } = new();

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
            Reservations = (await _petShopApiClient.GetReservationsAsync(AccessToken!, cancellationToken))
                .OrderBy(reservation => reservation.StartAt)
                .ToList();
        }
        catch (PetShopApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
            return RedirectToLogin();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法載入預約清單，請稍後再試。");
        }

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

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }
}
