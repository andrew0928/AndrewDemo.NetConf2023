using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Clients;

public sealed class PetShopApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public PetShopApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PetShopServiceDto>> GetServicesAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<List<PetShopServiceDto>>("/petshop-api/services", JsonOptions, cancellationToken)
            ?? new List<PetShopServiceDto>();
    }

    public async Task<IReadOnlyList<PetShopAvailabilitySlotDto>> GetAvailabilityAsync(
        string serviceId,
        DateOnly date,
        string? venueId,
        string? staffId,
        CancellationToken cancellationToken)
    {
        var query = new List<string>
        {
            $"serviceId={Uri.EscapeDataString(serviceId)}",
            $"date={date:yyyy-MM-dd}"
        };

        if (!string.IsNullOrWhiteSpace(venueId))
        {
            query.Add($"venueId={Uri.EscapeDataString(venueId)}");
        }

        if (!string.IsNullOrWhiteSpace(staffId))
        {
            query.Add($"staffId={Uri.EscapeDataString(staffId)}");
        }

        return await _httpClient.GetFromJsonAsync<List<PetShopAvailabilitySlotDto>>(
            $"/petshop-api/availability?{string.Join('&', query)}",
            JsonOptions,
            cancellationToken) ?? new List<PetShopAvailabilitySlotDto>();
    }

    public async Task<PetShopReservationDto> CreateReservationHoldAsync(
        string accessToken,
        CreatePetShopReservationHoldRequestDto requestModel,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/petshop-api/reservations/holds", accessToken);
        request.Content = JsonContent.Create(requestModel);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PetShopReservationDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Reservation hold response is empty.");
    }

    public async Task<IReadOnlyList<PetShopReservationDto>> GetReservationsAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/petshop-api/reservations", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<PetShopReservationDto>>(JsonOptions, cancellationToken)
            ?? new List<PetShopReservationDto>();
    }

    public async Task<PetShopReservationDto?> GetReservationAsync(
        string accessToken,
        string reservationId,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/petshop-api/reservations/{Uri.EscapeDataString(reservationId)}",
            accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PetShopReservationDto>(JsonOptions, cancellationToken);
    }

    public async Task<PetShopReservationDto> CancelHoldAsync(
        string accessToken,
        string reservationId,
        CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/petshop-api/reservations/{Uri.EscapeDataString(reservationId)}/cancel-hold",
            accessToken);
        request.Content = JsonContent.Create(new { });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PetShopReservationDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Cancel hold response is empty.");
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string uri, string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = response.Content == null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        throw new PetShopApiRequestException(response.StatusCode, body);
    }
}
