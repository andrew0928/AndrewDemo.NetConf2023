using System.Net.Http.Headers;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AndrewDemo.NetConf2023.API.Execution;

/// <summary>Routing 之後建立 request 身分，Items 與 DI 共用同一份 scoped context。</summary>
public sealed class ShopExecutionMiddleware(RequestDelegate next)
{
    /// <summary>驗證 Token 並初始化同一 request scope 的身分。</summary>
    public async Task InvokeAsync(HttpContext http, IdentityContext identity, TransactionContext transaction,
        MemberService members, ILogger<ShopExecutionMiddleware> logger)
    {
        http.Items[typeof(IdentityContext)] = identity;
        http.Items[typeof(TransactionContext)] = transaction;
        try
        {
            var endpoint = http.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>() != null && http.Request.Path.StartsWithSegments("/api"))
            {
                var anonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null;
                var authorization = http.Request.Headers.Authorization;
                if (authorization.Count == 0 && !anonymous)
                {
                    http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                if (authorization.Count != 0)
                {
                    if (authorization.Count != 1 || !AuthenticationHeaderValue.TryParse(authorization[0], out var header)
                        || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrWhiteSpace(header.Parameter)
                        || header.Parameter.StartsWith("admin_", StringComparison.OrdinalIgnoreCase))
                    {
                        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                    var member = members.ValidateToken(header.Parameter);
                    if (member == null)
                    {
                        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                    identity.Initialize(IdentitySnapshot.ForMember(member.Id, member.IsTest));
                    http.Items["access-token"] = header.Parameter;
                }
            }
            await next(http);
        }
        catch (ExecutionContextInvalidException error) when (!http.Response.HasStarted)
        {
            transaction.Clear();
            logger.LogError(error, "execution_context_invalid: 執行用途無效");
            http.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await http.Response.WriteAsJsonAsync(new { code = "execution_context_invalid", message = "執行用途資料無效。" });
        }
    }
}
