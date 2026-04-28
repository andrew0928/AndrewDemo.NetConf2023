using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AndrewDemo.NetConf2023.Storefront.Shared.Authentication;

public sealed class StorefrontAuthService
{
    private readonly StorefrontSessionAccessor _sessionAccessor;
    private readonly CoreApiClient _coreApiClient;
    private readonly CoreApiOptions _options;

    public StorefrontAuthService(
        StorefrontSessionAccessor sessionAccessor,
        CoreApiClient coreApiClient,
        IOptions<CoreApiOptions> options)
    {
        _sessionAccessor = sessionAccessor;
        _coreApiClient = coreApiClient;
        _options = options.Value;
    }

    public string BuildAuthorizeRedirect(HttpContext httpContext, string? returnUrl)
    {
        var normalizedReturnUrl = NormalizeReturnUrl(returnUrl);
        var state = Guid.NewGuid().ToString("N");
        _sessionAccessor.SetPendingAuth(state, normalizedReturnUrl);

        var callbackUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/auth/callback";
        var publicOAuthBaseUrl = string.IsNullOrWhiteSpace(_options.PublicOAuthBaseUrl)
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : _options.PublicOAuthBaseUrl.TrimEnd('/');
        var authorizeUrl = $"{publicOAuthBaseUrl}/oauth/authorize";

        return QueryHelpers.AddQueryString(authorizeUrl, new Dictionary<string, string?>
        {
            ["client_id"] = _options.OAuthClientId,
            ["redirect_uri"] = callbackUrl,
            ["response_type"] = "code",
            ["scope"] = "openid",
            ["state"] = state
        });
    }

    public async Task<string> HandleOAuthCallbackAsync(string code, string? state, CancellationToken cancellationToken)
    {
        var pendingState = _sessionAccessor.GetPendingAuthState();
        var pendingReturnUrl = _sessionAccessor.GetPendingAuthReturnUrl();
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("OAuth code is required.");
        }

        if (string.IsNullOrWhiteSpace(pendingState) || !string.Equals(pendingState, state, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("OAuth state mismatch.");
        }

        var tokenResponse = await _coreApiClient.ExchangeTokenAsync(code, cancellationToken);
        _sessionAccessor.SetAccessToken(tokenResponse.AccessToken);
        _sessionAccessor.ClearPendingAuth();

        return NormalizeReturnUrl(pendingReturnUrl);
    }

    public void Logout()
    {
        _sessionAccessor.ClearAll();
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (!returnUrl.StartsWith('/'))
        {
            return "/";
        }

        return returnUrl;
    }
}
