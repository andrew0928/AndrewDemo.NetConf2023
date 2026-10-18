using AndrewDemo.NetConf2023.API.Admin;
using AndrewDemo.NetConf2023.Core.AdminQueries;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers;

/// <summary>唯讀管理查核；認證由獨立的 admin middleware 負責。</summary>
[ApiController]
[Route("api/admin")]
public sealed class AdminQueryController(AdminQueryService queries) : ControllerBase
{
    /// <summary>訂單清單及所有符合條件的總額，不受當頁筆數影響。</summary>
    [HttpGet("orders")]
    public IActionResult Orders()
    {
        var options = AdminQueryParameters.Parse(Request.Query, "orders");
        var result = queries.GetOrders(options);
        return Ok(new
        {
            result.Page.QueriedAt, Filters = AdminQueryParameters.Filters(options, "orders"), options.Page, options.PageSize,
            result.Page.TotalCount, result.Page.ExcludedUnknownTimeCount, result.Page.Items,
            result.TotalAmount, result.KnownTotalAmount, result.UnknownAmountCount
        });
    }

    /// <summary>合併作用中與封存的結帳清單。</summary>
    [HttpGet("checkouts")]
    public IActionResult Checkouts()
    {
        var options = AdminQueryParameters.Parse(Request.Query, "checkouts");
        return Ok(Envelope(queries.GetCheckouts(options), options, "checkouts"));
    }

    /// <summary>只回傳會員識別、名稱與保存用途。</summary>
    [HttpGet("members")]
    public IActionResult Members()
    {
        var options = AdminQueryParameters.Parse(Request.Query, "members");
        return Ok(Envelope(queries.GetMembers(options), options, "members"));
    }

    private static object Envelope<T>(AdminPage<T> page, AdminQueryOptions options, string resource) => new
    {
        page.QueriedAt, Filters = AdminQueryParameters.Filters(options, resource), options.Page, options.PageSize,
        page.TotalCount, page.ExcludedUnknownTimeCount, page.Items
    };
}
