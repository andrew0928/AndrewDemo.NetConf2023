using AndrewDemo.NetConf2023.Core.AdminQueries;

namespace AndrewDemo.NetConf2023.API.Admin;

/// <summary>管理路徑的獨立認證與錯誤出口；不經會員身分解析。</summary>
public sealed class AdminQueryMiddleware(RequestDelegate next)
{
    /// <summary>處理管理憑證，不接受其他位置代傳，也不向會員認證 fallback。</summary>
    public async Task InvokeAsync(HttpContext http, AdminAccessToken access, ILogger<AdminQueryMiddleware> logger)
    {
        if (!http.Request.Path.StartsWithSegments("/api/admin")) { await next(http); return; }
        http.Response.Headers.CacheControl = "no-store";
        if (!access.Enabled)
        {
            await Error(http, 404, "admin_disabled", "管理查核未啟用。");
            return;
        }
        var headers = http.Request.Headers.Authorization;
        var authorization = headers.Count == 1 ? headers[0] : null;
        if (authorization == null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            || !access.Matches(authorization[7..]))
        {
            await Error(http, 401, "admin_unauthorized", "管理認證無效。");
            return;
        }
        try { await next(http); }
        catch (AdminQueryInvalidException) when (!http.Response.HasStarted)
        {
            await Error(http, 400, "admin_query_invalid", "查詢參數無效。");
        }
        catch (AdminDataInvalidException) when (!http.Response.HasStarted)
        {
            logger.LogError("管理查核資料無效，TraceId={TraceId}", http.TraceIdentifier);
            await Error(http, 500, "admin_data_invalid", "資料無法可靠判讀。");
        }
        catch (Exception error) when (!http.Response.HasStarted && !http.RequestAborted.IsCancellationRequested)
        {
            // 不記錄 exception 內容，避免連線字串或資料值進入 log。
            logger.LogError("管理查核失敗，ExceptionType={ExceptionType}, TraceId={TraceId}", error.GetType().Name, http.TraceIdentifier);
            await Error(http, 500, "admin_query_failed", "管理查詢失敗。");
        }
    }

    private static Task Error(HttpContext http, int status, string code, string message)
    {
        http.Response.StatusCode = status;
        return http.Response.WriteAsJsonAsync(new { code, message });
    }
}
