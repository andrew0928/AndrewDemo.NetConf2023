using System.Net;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;
using AndrewDemo.NetConf2023.CommonStorefront.Pages;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

public sealed class StorefrontOperationTests
{
    [Theory]
    [InlineData(0, "normal")]
    [InlineData(123456, "normal")]
    [InlineData(990001, "test")]
    [InlineData(0, "test")]
    [InlineData(int.MinValue, "normal")]
    [InlineData(int.MaxValue, "normal")]
    public async Task Checkout_UsesExplicitPaymentAndApiPurpose_AndShowsActualOrderId(int payment, string behavior)
    {
        using var handler = new ApiHandler(HttpStatusCode.OK, behavior);
        using var http = new HttpClient(handler) { BaseAddress = new("https://test.invalid") };
        var (page, session) = Page(new CoreApiClient(http)); page.PaymentId = payment;
        Assert.Equal("/member/orders", Assert.IsType<RedirectResult>(await page.OnPostAsync(default)).Url);
        Assert.Null(session.GetCartId());
        Assert.Contains("訂單 99", (string)page.TempData["NotificationMessage"]!);
        var summary = JsonSerializer.Deserialize<ApiOperationSummary>((string)page.TempData["CheckoutOperation"]!)!;
        Assert.Equal(behavior == "test", summary.IsTestBehavior);
        Assert.Equal("succeeded", summary.Outcome); Assert.Equal(42, summary.Transaction!.Id);
        var create = JsonSerializer.Deserialize<JsonElement>(handler.Requests.Single(x => x.Path == "/api/checkout/create").Body);
        Assert.Equal(payment, create.GetProperty("paymentId").GetInt32());
        var complete = JsonSerializer.Deserialize<JsonElement>(handler.Requests.Single(x => x.Path == "/api/checkout/complete").Body);
        Assert.False(complete.TryGetProperty("paymentId", out _));
        Assert.Equal(behavior == "test" ? new[] { "inventory.deduct" } : [], summary.SkippedEffectCodes);
    }

    [Theory]
    [InlineData(409, "rejected", "重新確認購物車")]
    [InlineData(400, "rejected", "商品與庫存")]
    [InlineData(500, "failed", "稍後再試")]
    public async Task CheckoutError_KeepsCartAndOutcome_WithoutAutomaticRetry(int status, string outcome, string message)
    {
        using var handler = new ApiHandler((HttpStatusCode)status, status == 500 ? null : "test");
        using var http = new HttpClient(handler) { BaseAddress = new("https://test.invalid") };
        var (page, session) = Page(new CoreApiClient(http)); page.PaymentId = 990001;
        Assert.IsType<PageResult>(await page.OnPostAsync(default));
        Assert.Equal(11, session.GetCartId());
        Assert.Equal(990001, page.PaymentId);
        Assert.Contains(message, Assert.Single(page.ErrorSummary.Errors));
        Assert.Equal(outcome, page.Operation!.Outcome);
        if (status == 500) Assert.Null(page.Operation.IsTestBehavior);
        Assert.Single(handler.Requests, x => x.Path == "/api/checkout/complete");
        Assert.False(page.TempData.ContainsKey("CheckoutOperation"));
    }

    [Fact]
    public void IncompleteOrMissingHeaders_DoNotInventNormalOrTransaction()
    {
        using var response = new HttpResponseMessage();
        var missing = ApiOperationSummary.FromHeaders(response.Headers);
        Assert.Null(missing.IsTestBehavior); Assert.Null(missing.Outcome); Assert.Null(missing.Transaction);
        response.Headers.Add("X-Shop-Outcome", "failed");
        response.Headers.Add("X-Shop-Transaction-Type", "checkout");
        response.Headers.Add("X-Shop-Transaction-Id", "42");
        var partial = ApiOperationSummary.FromHeaders(response.Headers);
        Assert.Null(partial.Transaction); Assert.Null(partial.IsTestBehavior); Assert.Equal("failed", partial.Outcome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    public async Task InvalidOrMissingPaymentId_DoesNotCallCheckout(string? input)
    {
        using var handler = new ApiHandler(HttpStatusCode.OK, "test");
        using var http = new HttpClient(handler) { BaseAddress = new("https://test.invalid") };
        var (page, session) = Page(new CoreApiClient(http));
        var metadata = new EmptyModelMetadataProvider().GetMetadataForProperty(typeof(CheckoutModel), nameof(CheckoutModel.PaymentId));
        var form = new Dictionary<string, StringValues>();
        if (input != null) form.Add("PaymentId", input);
        var binding = DefaultModelBindingContext.CreateBindingContext(page.PageContext,
            new FormValueProvider(BindingSource.Form, new FormCollection(form), CultureInfo.InvariantCulture), metadata, null, "PaymentId");
        await new SimpleTypeModelBinder(typeof(int?), NullLoggerFactory.Instance).BindModelAsync(binding);
        page.PaymentId = binding.Result.Model as int?;
        Assert.IsType<PageResult>(await page.OnPostAsync(default));
        Assert.DoesNotContain(handler.Requests, x => x.Path.StartsWith("/api/checkout"));
        Assert.Equal(11, session.GetCartId());
        Assert.Contains("PaymentID", Assert.Single(page.ErrorSummary.Errors));
        Assert.Contains("整數", Assert.Single(page.ModelState["PaymentId"]!.Errors).ErrorMessage);
        if (input != null) Assert.Equal(input, page.ModelState["PaymentId"]!.AttemptedValue);
    }

    private static (CheckoutModel, StorefrontSessionAccessor) Page(CoreApiClient client)
    {
        var context = new DefaultHttpContext { Session = new MemorySession() };
        var accessor = new StorefrontSessionAccessor(new HttpContextAccessor { HttpContext = context });
        accessor.SetAccessToken("fixture-token"); accessor.SetCartId(11);
        var page = new CheckoutModel(accessor, client)
        {
            PageContext = new PageContext { HttpContext = context, ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) },
            TempData = new TempDataDictionary(context, new TempProvider())
        };
        return (page, accessor);
    }

    private sealed class ApiHandler(HttpStatusCode status, string? behavior) : HttpMessageHandler
    {
        public List<(string Path, string Body)> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            Requests.Add((path, request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
            var body = path switch
            {
                "/api/carts/11" => "{\"id\":11,\"lineItems\":[{\"lineId\":\"line\",\"productId\":\"p\",\"quantity\":1}]}",
                "/api/products/p" => "{\"id\":\"p\",\"name\":\"商品\",\"price\":100}",
                "/api/carts/11/estimate" => "{\"totalPrice\":100,\"discounts\":[]}",
                "/api/checkout/create" => "{\"transactionId\":42}",
                "/api/checkout/complete" when status == HttpStatusCode.OK => "{\"transactionId\":42,\"orderDetail\":{\"id\":99}}",
                _ => "{\"code\":\"checkout_recreate_required\"}"
            };
            var response = new HttpResponseMessage(path.EndsWith("/complete") ? status : HttpStatusCode.OK)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            if (path.StartsWith("/api/checkout"))
            {
                if (behavior != null)
                {
                    response.Headers.Add("X-Shop-Behavior", behavior);
                    response.Headers.Add("X-Shop-Transaction-Type", "checkout");
                    response.Headers.Add("X-Shop-Transaction-Id", "42");
                    response.Headers.Add("X-Shop-Transaction-Is-Test", behavior == "test" ? "true" : "false");
                }
                response.Headers.Add("X-Shop-Outcome", response.StatusCode == HttpStatusCode.OK ? "succeeded" : status == HttpStatusCode.InternalServerError ? "failed" : "rejected");
                if (path.EndsWith("/complete") && status == HttpStatusCode.OK && behavior == "test")
                    response.Headers.Add("X-Shop-Skipped-Effects", "inventory.deduct");
            }
            return response;
        }
    }

    private sealed class TempProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
    private sealed class MemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> _items = new();
        public bool IsAvailable => true;
        public string Id => "test";
        public IEnumerable<string> Keys => _items.Keys;
        public void Clear() => _items.Clear();
        public Task CommitAsync(CancellationToken token = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken token = default) => Task.CompletedTask;
        public void Remove(string key) => _items.Remove(key);
        public void Set(string key, byte[] value) => _items[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _items.TryGetValue(key, out value!);
    }
}
