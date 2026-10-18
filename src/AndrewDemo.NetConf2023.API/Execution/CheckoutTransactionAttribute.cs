using AndrewDemo.NetConf2023.API.Controllers;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AndrewDemo.NetConf2023.API.Execution;

/// <summary>明確宣告交易為 Checkout，ID 來自 MVC 綁定的 complete request.TransactionId。</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CheckoutTransactionAttribute : Attribute;

/// <summary>沿用 MVC 唯一一次 JSON 解析；不重讀 body、不猜測其他端點的 id。</summary>
public sealed class CheckoutTransactionFilter(CheckoutContextLoader loader) : IAsyncActionFilter
{
    /// <summary>在模型驗證成功後載入已授權的交易，再執行 action。</summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<CheckoutTransactionAttribute>() != null)
        {
            var request = context.ActionArguments.Values.OfType<CheckoutController.CheckoutCompleteRequest>().SingleOrDefault()
                ?? throw new ExecutionContextInvalidException("Checkout endpoint 缺少宣告的 request。");
            switch (loader.Bind(request.TransactionId))
            {
                case CheckoutContextStatus.NotFound:
                    context.Result = new BadRequestObjectResult("Transaction not found");
                    return;
                case CheckoutContextStatus.Forbidden:
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
            }
        }
        await next();
    }
}
