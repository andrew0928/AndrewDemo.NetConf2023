using System.Net;
using System.Text;
using AndrewDemo.NetConf2023.API.Controllers;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

public sealed class OAuthRouteContractTests
{
    [Fact]
    public void LoginController_DeclaresOAuthNamespace()
    {
        var route = typeof(LoginController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("oauth", route.Template);
    }

    [Fact]
    public void LoginController_DeclaresAuthorizeAndTokenEndpoints()
    {
        var authorizeGet = typeof(LoginController).GetMethod(nameof(LoginController.GetAuthorize))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>()
            .Single();
        var authorizePost = typeof(LoginController).GetMethod(nameof(LoginController.PostAuthorize))!
            .GetCustomAttributes(typeof(HttpPostAttribute), inherit: false)
            .Cast<HttpPostAttribute>()
            .Single();
        var tokenPost = typeof(LoginController).GetMethod(nameof(LoginController.PostToken))!
            .GetCustomAttributes(typeof(HttpPostAttribute), inherit: false)
            .Cast<HttpPostAttribute>()
            .Single();

        Assert.Equal("authorize", authorizeGet.Template);
        Assert.Equal("authorize", authorizePost.Template);
        Assert.Equal("token", tokenPost.Template);
    }

    [Fact]
    public void StorefrontAuthService_BuildAuthorizeRedirect_UsesCurrentOriginOAuthNamespace()
    {
        var session = new TestSession();
        var httpContext = CreateHttpContext(session);
        var service = CreateAuthService(httpContext, new CoreApiOptions
        {
            OAuthClientId = "storefront-client"
        });

        var redirectUrl = service.BuildAuthorizeRedirect(httpContext, "/cart");
        var redirectUri = new Uri(redirectUrl);
        var query = QueryHelpers.ParseQuery(redirectUri.Query);

        Assert.Equal("https://shop.example.test/oauth/authorize", redirectUri.GetLeftPart(UriPartial.Path));
        Assert.Equal("storefront-client", query["client_id"]);
        Assert.Equal("https://shop.example.test/auth/callback", query["redirect_uri"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("openid", query["scope"]);
        Assert.False(string.IsNullOrWhiteSpace(query["state"]));
    }

    [Fact]
    public void StorefrontAuthService_BuildAuthorizeRedirect_UsesConfiguredPublicOAuthBaseUrl()
    {
        var session = new TestSession();
        var httpContext = CreateHttpContext(session);
        var service = CreateAuthService(httpContext, new CoreApiOptions
        {
            PublicOAuthBaseUrl = "https://login.example.test/",
            OAuthClientId = "storefront-client"
        });

        var redirectUrl = service.BuildAuthorizeRedirect(httpContext, "/member");
        var redirectUri = new Uri(redirectUrl);

        Assert.Equal("https://login.example.test/oauth/authorize", redirectUri.GetLeftPart(UriPartial.Path));
    }

    [Fact]
    public async Task CoreApiClient_ExchangeTokenAsync_PostsToOAuthTokenEndpoint()
    {
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://internal-core-api.test")
        };
        var client = new CoreApiClient(httpClient);

        var response = await client.ExchangeTokenAsync("code-123", CancellationToken.None);

        Assert.Equal("token-123", response.AccessToken);
        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("/oauth/token", handler.Request?.RequestUri?.PathAndQuery);
        Assert.Equal("code=code-123", handler.RequestBody);
    }

    private static HttpContext CreateHttpContext(ISession session)
    {
        var httpContext = new DefaultHttpContext
        {
            Session = session
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("shop.example.test");
        return httpContext;
    }

    private static StorefrontAuthService CreateAuthService(HttpContext httpContext, CoreApiOptions options)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };
        var sessionAccessor = new StorefrontSessionAccessor(accessor);
        var coreApiClient = new CoreApiClient(new HttpClient(new RecordingHttpMessageHandler())
        {
            BaseAddress = new Uri("https://internal-core-api.test")
        });

        return new StorefrontAuthService(sessionAccessor, coreApiClient, Options.Create(options));
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler, IDisposable
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"access_token":"token-123","token_type":"Bearer","expires_in":3600}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;

        public string Id { get; } = Guid.NewGuid().ToString("N");

        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
