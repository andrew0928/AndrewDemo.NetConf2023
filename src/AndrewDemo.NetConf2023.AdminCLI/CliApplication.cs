using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;

namespace AndrewDemo.NetConf2023.AdminCLI;

internal static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, Func<string, string?> environment,
        TextWriter stdout, TextWriter stderr, CancellationToken cancellation = default, HttpClient? httpClient = null)
    {
        try
        {
            var description = SelfDescription.Read(args);
            if (description != null) { await stdout.WriteAsync(description); return 0; }
            var options = CliOptions.Parse(args);
            var configuration = CliConfiguration.Load(options, environment);
            using var ownedClient = httpClient == null ? AdminClient.CreateHttpClient() : null;
            System.Text.Json.Nodes.JsonObject result;
            try { result = await new AdminClient(httpClient ?? ownedClient!, configuration).QueryAsync(options, cancellation); }
            catch (IOException) { throw new CliException(4, "讀取 HTTP 回應時傳輸失敗。"); }
            var rendered = OutputRenderer.Render(result, options);
            if (rendered.Contains(configuration.AccessToken, StringComparison.Ordinal)) throw ResponseSchema.Invalid();
            cancellation.ThrowIfCancellationRequested();
            await PublishAsync(rendered, options.Get("output"), stdout, cancellation);
            return 0;
        }
        catch (CliException error) { return await Fail(error.ExitCode, error.Message, stderr); }
        catch (OperationCanceledException) { return await Fail(cancellation.IsCancellationRequested ? 130 : 4,
            cancellation.IsCancellationRequested ? "使用者取消查詢。" : "查詢逾時或傳輸取消。", stderr); }
        catch (HttpRequestException) { return await Fail(4, "網路或 TLS 傳輸失敗。", stderr); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { return await Fail(7, "輸出失敗；請檢查目標是否存在、路徑、權限與磁碟。", stderr); }
    }

    private static async Task<int> Fail(int code, string message, TextWriter stderr)
    {
        try { await stderr.WriteLineAsync(message); }
        catch (IOException) { }
        return code;
    }

    private static async Task PublishAsync(string content, string? destination, TextWriter stdout, CancellationToken cancellation)
    {
        if (destination == null) { await stdout.WriteAsync(content.AsMemory(), cancellation); return; }
        var full = Path.GetFullPath(destination);
        if (File.Exists(full) || Directory.Exists(full)) throw new CliException(7, "輸出目標已存在，不覆寫。");
        var temporary = Path.Combine(Path.GetDirectoryName(full)!, $".shop-admin-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var file = CreatePrivateTemporary(temporary))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                await file.WriteAsync(bytes, cancellation);
                await file.FlushAsync(cancellation);
            }
            cancellation.ThrowIfCancellationRequested();
            File.Move(temporary, full, overwrite: false);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static FileStream CreatePrivateTemporary(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var user = identity.User ?? throw new UnauthorizedAccessException();
            var security = new FileSecurity();
            security.SetOwner(user);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
            return new FileInfo(path).Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.Asynchronous, security);
        }
        return new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
            Options = FileOptions.Asynchronous, UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
        });
    }
}
