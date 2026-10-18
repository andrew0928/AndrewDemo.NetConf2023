using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AndrewDemo.NetConf2023.AdminCLI;

namespace AndrewDemo.NetConf2023.AdminCLI.Tests;

public sealed class ProcessTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "admin-cli-process-" + Guid.NewGuid().ToString("N"));
    public ProcessTests() => Directory.CreateDirectory(directory);
    public void Dispose() => Directory.Delete(directory, true);
    private async Task<(int Code, string Out, string Error)> Execute(string[] args, string? token = null)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(CliOptions).Assembly.Location);
        foreach (var argument in args) start.ArgumentList.Add(argument);
        start.Environment.Remove("SHOP_ADMIN_ACCESS_TOKEN"); start.Environment.Remove("SHOP_ADMIN_BASE_URL");
        if (token != null) start.Environment["SHOP_ADMIN_ACCESS_TOKEN"] = token;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(true); throw; }
        return (process.ExitCode, await stdout, await stderr);
    }

    [Fact]
    public async Task DefaultDotenvUsesWorkingDirectoryAndOfflineCommandsIgnoreIt()
    {
        File.WriteAllText(Path.Combine(directory, ".env"), "BROKEN_SECRET");
        var invalid = await Execute(["stats", "--source", "member"]);
        Assert.Equal(3, invalid.Code); Assert.Contains("第 1 行", invalid.Error); Assert.DoesNotContain("BROKEN_SECRET", invalid.Error);
        foreach (var args in new[] { new[] { "--help" }, new[] { "--version" }, new[] { "--skill", "configure" } })
        {
            var offline = await Execute(args); Assert.Equal(0, offline.Code); Assert.NotEmpty(offline.Out); Assert.Empty(offline.Error);
        }
        File.Delete(Path.Combine(directory, ".env"));
        var missing = await Execute(["stats", "--source", "member"]);
        Assert.Equal(3, missing.Code); Assert.Contains("ACCESS_TOKEN", missing.Error); Assert.DoesNotContain("dotenv", missing.Error);
        File.WriteAllText(Path.Combine(directory, ".env"), "BROKEN_SECRET");
        File.WriteAllText(Path.Combine(directory, "chosen.env"), "SHOP_ADMIN_ACCESS_TOKEN=bad\n");
        var chosen = await Execute(["stats", "--source", "member", "--env-file", "chosen.env"]);
        Assert.Equal(3, chosen.Code); Assert.Contains("ACCESS_TOKEN", chosen.Error); Assert.DoesNotContain("第 1 行", chosen.Error);
    }

    [Fact]
    public async Task RealHttpUsesDotenvAndRejectsRedirectWithoutFollowingIt()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        File.WriteAllText(Path.Combine(directory, ".env"), $"SHOP_ADMIN_BASE_URL=http://127.0.0.1:{port}\nSHOP_ADMIN_ACCESS_TOKEN=admin_processfixture\n");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var response = Task.Run(async () =>
        {
            using var connection = await listener.AcceptTcpClientAsync(timeout.Token);
            await using var stream = connection.GetStream();
            using var reader = new StreamReader(stream, leaveOpen: true);
            var headers = new StringBuilder();
            while (await reader.ReadLineAsync(timeout.Token) is { Length: > 0 } line) headers.AppendLine(line);
            Assert.Contains("GET /api/admin/members?purpose=normal&page=1&pageSize=1 HTTP/1.1", headers.ToString());
            Assert.Contains("Authorization: Bearer admin_processfixture", headers.ToString());
            var bytes = Encoding.ASCII.GetBytes($"HTTP/1.1 302 Found\r\nLocation: http://127.0.0.1:{port}/unexpected\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(bytes, timeout.Token);
        });
        var result = await Execute(["stats", "--source", "member"]);
        await response;
        Assert.Equal(5, result.Code); Assert.Contains("302", result.Error); Assert.Empty(result.Out); Assert.False(listener.Pending());
    }

    [Fact]
    public void ProductionClientHasThirtySecondTimeout()
    { using var client = AdminClient.CreateHttpClient(); Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout); }
}
