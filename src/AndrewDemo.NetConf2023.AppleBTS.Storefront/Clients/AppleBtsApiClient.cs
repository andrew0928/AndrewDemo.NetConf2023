using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Clients;

public sealed class AppleBtsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public AppleBtsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<BtsCatalogItemDto>> GetPublishedOffersAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<List<BtsCatalogItemDto>>("/bts-api/catalog", JsonOptions, cancellationToken)
            ?? new List<BtsCatalogItemDto>();
    }

    public async Task<BtsCatalogItemDto?> GetOfferDetailAsync(string mainProductId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"/bts-api/catalog/{Uri.EscapeDataString(mainProductId)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<BtsCatalogItemDto>(JsonOptions, cancellationToken);
    }

    public async Task<BtsQualificationResponseDto> GetCurrentQualificationAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/bts-api/qualification/me", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<BtsQualificationResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Qualification response is empty.");
    }

    public async Task<BtsQualificationResponseDto> VerifyQualificationAsync(string accessToken, string email, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/bts-api/qualification/verify", accessToken);
        request.Content = JsonContent.Create(new
        {
            Email = email
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<BtsQualificationResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Qualification verify response is empty.");
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

        throw new AppleBtsApiRequestException(response.StatusCode, body);
    }
}
