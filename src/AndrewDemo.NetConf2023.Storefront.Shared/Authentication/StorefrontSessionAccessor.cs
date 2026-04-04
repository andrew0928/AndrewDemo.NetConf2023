using Microsoft.AspNetCore.Http;

namespace AndrewDemo.NetConf2023.Storefront.Shared.Authentication;

public sealed class StorefrontSessionAccessor
{
    private const string AccessTokenKey = "storefront.access_token";
    private const string CartIdKey = "storefront.cart_id";
    private const string AuthStateKey = "storefront.auth_state";
    private const string AuthReturnUrlKey = "storefront.auth_return_url";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public StorefrontSessionAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetAccessToken() => GetSession().GetString(AccessTokenKey);

    public bool HasAccessToken() => !string.IsNullOrWhiteSpace(GetAccessToken());

    public void SetAccessToken(string accessToken)
    {
        GetSession().SetString(AccessTokenKey, accessToken);
    }

    public void ClearAccessToken()
    {
        GetSession().Remove(AccessTokenKey);
    }

    public int? GetCartId() => GetSession().GetInt32(CartIdKey);

    public void SetCartId(int cartId)
    {
        GetSession().SetInt32(CartIdKey, cartId);
    }

    public void ClearCartId()
    {
        GetSession().Remove(CartIdKey);
    }

    public void SetPendingAuth(string state, string returnUrl)
    {
        var session = GetSession();
        session.SetString(AuthStateKey, state);
        session.SetString(AuthReturnUrlKey, returnUrl);
    }

    public string? GetPendingAuthState() => GetSession().GetString(AuthStateKey);

    public string? GetPendingAuthReturnUrl() => GetSession().GetString(AuthReturnUrlKey);

    public void ClearPendingAuth()
    {
        var session = GetSession();
        session.Remove(AuthStateKey);
        session.Remove(AuthReturnUrlKey);
    }

    public void ClearAll()
    {
        ClearPendingAuth();
        ClearCartId();
        ClearAccessToken();
    }

    private ISession GetSession()
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required.");

        return httpContext.Session;
    }
}
