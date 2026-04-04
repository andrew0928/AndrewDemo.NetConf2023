using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;

namespace AndrewDemo.NetConf2023.Storefront.Shared.Clients;

public sealed class CoreApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public CoreApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<List<Product>>("/api/products", JsonOptions, cancellationToken)
            ?? new List<Product>();
    }

    public async Task<Product?> GetProductByIdAsync(string productId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"/api/products/{Uri.EscapeDataString(productId)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<Product>(JsonOptions, cancellationToken);
    }

    public async Task<Member> GetMemberProfileAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/member", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<Member>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Member profile response is empty.");
    }

    public async Task<MemberOrdersResponseDto> GetMemberOrdersAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, "/api/member/orders", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<MemberOrdersResponseDto>(JsonOptions, cancellationToken)
            ?? new MemberOrdersResponseDto();
    }

    public async Task<Cart> CreateCartAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/carts/create", accessToken);
        request.Content = JsonContent.Create(new { });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<Cart>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Cart create response is empty.");
    }

    public async Task<Cart?> GetCartAsync(string accessToken, int cartId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, $"/api/carts/{cartId}", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<Cart>(JsonOptions, cancellationToken);
    }

    public async Task<Cart> AddCartItemAsync(string accessToken, int cartId, AddCartItemRequestDto requestModel, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/carts/{cartId}/items", accessToken);
        request.Content = JsonContent.Create(requestModel);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<Cart>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Add cart item response is empty.");
    }

    public async Task<Cart> RemoveCartLineAsync(string accessToken, int cartId, string lineId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Delete, $"/api/carts/{cartId}/lines/{Uri.EscapeDataString(lineId)}", accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<Cart>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Remove cart line response is empty.");
    }

    public async Task<CartEstimateResponseDto> EstimateCartAsync(string accessToken, int cartId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, $"/api/carts/{cartId}/estimate", accessToken);
        request.Content = JsonContent.Create(new { });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<CartEstimateResponseDto>(JsonOptions, cancellationToken)
            ?? new CartEstimateResponseDto();
    }

    public async Task<CheckoutCreateResponseDto> CreateCheckoutAsync(string accessToken, int cartId, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/checkout/create", accessToken);
        request.Content = JsonContent.Create(new CreateCheckoutRequestDto { CartId = cartId });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<CheckoutCreateResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Checkout create response is empty.");
    }

    public async Task<CheckoutCompleteResponseDto> CompleteCheckoutAsync(string accessToken, CompleteCheckoutRequestDto requestModel, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, "/api/checkout/complete", accessToken);
        request.Content = JsonContent.Create(requestModel);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<CheckoutCompleteResponseDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Checkout complete response is empty.");
    }

    public async Task<TokenExchangeResponse> ExchangeTokenAsync(string code, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/login/token");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<TokenExchangeResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("OAuth token response is empty.");
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

        throw new CoreApiRequestException(response.StatusCode, body);
    }
}
