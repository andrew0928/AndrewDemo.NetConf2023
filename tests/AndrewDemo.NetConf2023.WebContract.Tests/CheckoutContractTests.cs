using System.Net;
using System.Text;
using System.Text.Json;
using AndrewDemo.NetConf2023.API.Controllers;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

public sealed class CheckoutContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(typeof(CheckoutController.CheckoutCreateRequest))]
    [InlineData(typeof(CreateCheckoutRequestDto))]
    public void CreateRequest_RequiresPaymentIdInJson(Type requestType)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("{\"cartId\":1}", requestType, JsonOptions));
        Assert.NotNull(JsonSerializer.Deserialize("{\"cartId\":1,\"paymentId\":0}", requestType, JsonOptions));
    }

    [Fact]
    public async Task SharedClient_SendsPaymentOnlyDuringCreate()
    {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://local-contract.test") };
        var client = new CoreApiClient(http);

        await client.CreateCheckoutAsync("test-token", 11, 990001, CancellationToken.None);
        await client.CompleteCheckoutAsync("test-token", new CompleteCheckoutRequestDto { TransactionId = 42 }, CancellationToken.None);

        Assert.Equal("/api/checkout/create", handler.Requests[0].Path);
        var create = JsonDocument.Parse(handler.Requests[0].Body).RootElement;
        Assert.Equal(11, create.GetProperty("cartId").GetInt32());
        Assert.Equal(990001, create.GetProperty("paymentId").GetInt32());
        Assert.Equal("/api/checkout/complete", handler.Requests[1].Path);
        var complete = JsonDocument.Parse(handler.Requests[1].Body).RootElement;
        Assert.Equal(42, complete.GetProperty("transactionId").GetInt32());
        Assert.False(complete.TryGetProperty("paymentId", out _));
    }

    [Fact]
    public async Task CompleteLegacyCheckout_Returns409AndMachineReadableCode()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"tip-web-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            using var database = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = $"Filename={Path.Combine(directory, "test.db")};Connection=Direct"
            });
            var member = new Member { Name = "legacy-member" };
            database.Members.Insert(member);
            database.MemberTokens.Insert(new MemberAccessTokenRecord
            {
                Token = "test-token", MemberId = member.Id, Expire = DateTime.MaxValue
            });
            database.Database.GetCollection("checkout_transactions").Insert(new LiteDB.BsonDocument
            {
                ["_id"] = 42, ["MemberId"] = member.Id, ["CartId"] = 11
            });
            var service = new CheckoutService(database, new DiscountEngine([]), new DefaultOrderEventDispatcher(),
                new DefaultProductService(database), new ShopManifest { ShopId = "default" }, TimeProvider.System);
            var controller = new CheckoutController(service, new MemberService(database, new AndrewDemo.NetConf2023.Core.Testing.TestAllowlist(), TimeProvider.System))
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            controller.HttpContext.Items["access-token"] = "test-token";

            var response = await controller.CompleteAsync(new CheckoutController.CheckoutCompleteRequest { TransactionId = 42 });

            var conflict = Assert.IsType<ConflictObjectResult>(response.Result);
            Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
            var body = JsonSerializer.SerializeToElement(conflict.Value, JsonOptions);
            Assert.Equal("checkout_recreate_required", body.GetProperty("code").GetString());
            Assert.Contains("重新建立", body.GetProperty("message").GetString());
            Assert.NotNull(database.CheckoutTransactions.FindById(42));
            Assert.Empty(database.Orders.FindAll());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri!.AbsolutePath, await request.Content!.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        }
    }
}
