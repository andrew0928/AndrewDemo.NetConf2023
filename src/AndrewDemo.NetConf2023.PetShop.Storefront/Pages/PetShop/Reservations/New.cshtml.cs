using AndrewDemo.NetConf2023.PetShop.Storefront.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Pages.PetShop.Reservations;

public sealed class NewModel : StorefrontPageModel
{
    private readonly PetShopApiClient _petShopApiClient;
    private readonly TimeProvider _timeProvider;

    public NewModel(
        StorefrontSessionAccessor sessionAccessor,
        PetShopApiClient petShopApiClient,
        TimeProvider timeProvider)
        : base(sessionAccessor)
    {
        _petShopApiClient = petShopApiClient;
        _timeProvider = timeProvider;
    }

    [BindProperty(SupportsGet = true)]
    public string? ServiceId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Date { get; set; }

    public List<PetShopServiceDto> Services { get; private set; } = new();

    public PetShopServiceDto? SelectedService { get; private set; }

    public List<PetShopAvailabilitySlotDto> AvailabilitySlots { get; private set; } = new();

    public bool ShouldShowSlots => !string.IsNullOrWhiteSpace(ServiceId);

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateHoldAsync(
        string serviceId,
        string date,
        DateTime startAt,
        string venueId,
        string staffId,
        CancellationToken cancellationToken)
    {
        ServiceId = serviceId;
        Date = date;

        var accessToken = SessionAccessor.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return RedirectToLogin($"/petshop/reservations/new?serviceId={Uri.EscapeDataString(serviceId)}&date={Uri.EscapeDataString(date)}");
        }

        if (string.IsNullOrWhiteSpace(serviceId)
            || string.IsNullOrWhiteSpace(venueId)
            || string.IsNullOrWhiteSpace(staffId))
        {
            ErrorSummary.Errors.Add("請選擇完整的預約時段。");
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        try
        {
            var reservation = await _petShopApiClient.CreateReservationHoldAsync(
                accessToken,
                new CreatePetShopReservationHoldRequestDto
                {
                    ServiceId = serviceId,
                    StartAt = NormalizeUtc(startAt),
                    VenueId = venueId,
                    StaffId = staffId
                },
                cancellationToken);

            TempData["NotificationTitle"] = "已保留預約時段";
            TempData["NotificationMessage"] = "預約目前為「預約確認中」，請在保留時間內加入購物車並完成結帳。";
            TempData["NotificationTone"] = "success";
            return Redirect($"/petshop/reservations/{reservation.ReservationId}");
        }
        catch (PetShopApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            ErrorSummary.Errors.Add("這個時段剛剛已被保留，請重新選擇其他時段。");
        }
        catch (PetShopApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
            return RedirectToLogin();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法建立預約，請稍後再試。");
        }

        await LoadPageAsync(cancellationToken);
        return Page();
    }

    public string FormatDateTime(DateTime value)
    {
        return NormalizeUtc(value).ToString("yyyy-MM-dd HH:mm 'UTC'");
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken)
    {
        ResolveDate();

        try
        {
            Services = (await _petShopApiClient.GetServicesAsync(cancellationToken)).ToList();
            SelectedService = Services.FirstOrDefault(service => string.Equals(service.ServiceId, ServiceId, StringComparison.OrdinalIgnoreCase));

            if (SelectedService == null || string.IsNullOrWhiteSpace(ServiceId))
            {
                return;
            }

            var selectedDate = ResolveDate();
            AvailabilitySlots = (await _petShopApiClient.GetAvailabilityAsync(
                    ServiceId,
                    selectedDate,
                    venueId: null,
                    staffId: null,
                    cancellationToken))
                .ToList();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法載入可預約時段，請稍後再試。");
        }
    }

    private DateOnly ResolveDate()
    {
        if (!string.IsNullOrWhiteSpace(Date)
            && DateOnly.TryParse(Date, out var parsedDate))
        {
            Date = parsedDate.ToString("yyyy-MM-dd");
            return parsedDate;
        }

        var defaultDate = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime.AddDays(1));
        Date = defaultDate.ToString("yyyy-MM-dd");
        return defaultDate;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
    }
}
