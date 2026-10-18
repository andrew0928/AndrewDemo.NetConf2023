using AndrewDemo.NetConf2023.API.Controllers;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

public sealed class MemberFlowTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tip-member-web-{Guid.NewGuid():N}.db");
    private readonly ShopDatabaseContext _database;
    private readonly MemberService _members;

    public MemberFlowTests()
    {
        _database = new ShopDatabaseContext(new ShopDatabaseOptions { ConnectionString = $"Filename={_path};Connection=Direct" });
        _members = new MemberService(_database, new TestAllowlist(["test"], []), TimeProvider.System);
    }

    [Fact]
    public void OAuthRegistrationAndMemberNotes_UsePersistedTestPurpose()
    {
        var login = new LoginController(new TestEnvironment(), _members)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        Assert.IsType<RedirectResult>(login.PostAuthorize("test", null, "client", "https://example.test/callback", null));
        var member = Assert.Single(_database.Members.FindAll());
        var token = Assert.Single(_database.MemberTokens.FindAll());
        Assert.True(member.IsTest);
        Assert.True(token.IsTest);
        Assert.Equal(member.Id, token.MemberId);
        var controller = new MemberController(_database, _members)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Items["access-token"] = token.Token;
        Assert.True(controller.Get().Value!.IsTest);
        Assert.True(controller.SetShopNotes(new MemberController.MemberSetNotesRequest { ShopNotes = "note" }).Value!.IsTest);
        Assert.Equal("note", _database.Members.FindById(member.Id).ShopNotes);
        Assert.Null(typeof(MemberController.MemberSetNotesRequest).GetProperty("IsTest"));
        controller.HttpContext.Items["access-token"] = "unknown";
        Assert.IsType<UnauthorizedResult>(controller.Get().Result);
        Assert.IsType<UnauthorizedResult>(controller.GetOrders().Result);
        Assert.IsType<UnauthorizedResult>(controller.SetShopNotes(new MemberController.MemberSetNotesRequest { ShopNotes = "rejected" }).Result);
        Assert.Equal("note", _database.Members.FindById(member.Id).ShopNotes);
        Assert.IsType<RedirectResult>(login.PostAuthorize("test", null, "client", "https://example.test/callback", null));
        Assert.Single(_database.Members.FindAll());
        Assert.Equal(2, _database.MemberTokens.Count());
        Assert.All(_database.MemberTokens.FindAll(), t => Assert.True(t.IsTest));
    }

    [Fact]
    public void ContradictoryToken_StopsCheckoutBeforeAnyTransactionIsCreated()
    {
        var member = _members.GetOrCreate("test");
        _database.MemberTokens.Insert(new MemberAccessTokenRecord
        {
            Token = "contradictory", MemberId = member.Id, IsTest = false, Expire = DateTime.MaxValue
        });
        var checkout = new CheckoutService(_database, new DiscountEngine([]), new DefaultOrderEventDispatcher(),
            new DefaultProductService(_database), new ShopManifest { ShopId = "default" }, TimeProvider.System);
        var controller = new CheckoutController(checkout, _members)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Items["access-token"] = "contradictory";
        Assert.Throws<ExecutionContextInvalidException>(() => controller.Create(new CheckoutController.CheckoutCreateRequest { CartId = 1, PaymentId = 0 }));
        Assert.Empty(_database.CheckoutTransactions.FindAll());
        Assert.Empty(_database.Orders.FindAll());
    }

    public void Dispose() { _database.Dispose(); File.Delete(_path); }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "test";
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
