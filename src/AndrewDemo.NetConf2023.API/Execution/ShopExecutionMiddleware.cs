using System.Globalization;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.Core.Operations;
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
        MemberService members, OperationExecution operation, ILogger<ShopExecutionMiddleware> logger)
    {
        if (http.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>() == null)
        {
            await next(http);
            return;
        }
        operation.Begin(http.TraceIdentifier);
        http.Response.OnStarting(() =>
        {
            var result = operation.Complete(Outcome(http.Response.StatusCode));
            http.Response.Headers["X-Shop-Outcome"] = operation.Outcome!.Value.ToString().ToLowerInvariant();
            if (result != null)
                http.Response.Headers["X-Shop-Behavior"] = result.IsTestBehavior ? "test" : "normal";
            var codes = result?.SkippedEffects.Select(x => x.EffectCode).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (codes?.Length > 0) http.Response.Headers["X-Shop-Skipped-Effects"] = string.Join(",", codes);
            if (operation.Transaction is { } snapshot)
            {
                http.Response.Headers["X-Shop-Transaction-Type"] = snapshot.Kind.ToString().ToLowerInvariant();
                http.Response.Headers["X-Shop-Transaction-Id"] = snapshot.Id.ToString(CultureInfo.InvariantCulture);
                http.Response.Headers["X-Shop-Transaction-Is-Test"] = snapshot.IsTest ? "true" : "false";
            }
            return Task.CompletedTask;
        });
        http.Items[typeof(IdentityContext)] = identity;
        http.Items[typeof(TransactionContext)] = transaction;
        var purposeReady = false;
        try
        {
            var endpoint = http.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>() != null && http.Request.Path.StartsWithSegments("/api"))
            {
                var anonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null;
                var authorization = http.Request.Headers.Authorization;
                if (authorization.Count == 0 && !anonymous)
                {
                    operation.InvalidatePurpose();
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
                        operation.InvalidatePurpose();
                        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                    var member = members.ValidateToken(header.Parameter);
                    if (member == null)
                    {
                        operation.InvalidatePurpose();
                        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                    identity.Initialize(IdentitySnapshot.ForMember(member.Id, member.IsTest));
                    http.Items["access-token"] = header.Parameter;
                }
            }
            purposeReady = true;
            await next(http);
        }
        catch (ExecutionContextInvalidException) when (!http.Response.HasStarted)
        {
            transaction.Clear();
            operation.InvalidatePurpose();
            logger.LogError("execution_context_invalid: 執行用途無效 TraceId={TraceId}", operation.TraceId);
            http.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await http.Response.WriteAsJsonAsync(new { code = "execution_context_invalid", message = "執行用途資料無效。" });
        }
        catch (BadHttpRequestException error) when (!http.Response.HasStarted)
        {
            http.Response.StatusCode = error.StatusCode;
            await http.Response.WriteAsJsonAsync(new { message = "Request 格式或大小不符合要求。" });
        }
        catch (Exception error) when (!http.Response.HasStarted)
        {
            var outcome = OperationExecution.Classify(error);
            // 不輸出可能含 request 秘密的例外訊息或完整物件。
            logger.Log(outcome == OperationOutcome.Failed ? LogLevel.Error : LogLevel.Information,
                "操作異常 TraceId={TraceId} ErrorType={ErrorType}", operation.TraceId, error.GetType().Name);
            http.Response.StatusCode = outcome == OperationOutcome.Rejected
                ? (error is UnauthorizedAccessException ? 403 : 400) : 500;
            if (!purposeReady) operation.InvalidatePurpose();
            await http.Response.WriteAsJsonAsync(new { message = outcome == OperationOutcome.Rejected ? "操作遭拒絕。" : "操作執行失敗。" });
        }
        finally
        {
            // 空 body（例如 401）也需要結果與診斷紀錄。
            operation.Complete(Outcome(http.Response.StatusCode));
        }
    }

    private static OperationOutcome Outcome(int status) => status >= 500 ? OperationOutcome.Failed
        : status >= 400 ? OperationOutcome.Rejected : OperationOutcome.Succeeded;
}
