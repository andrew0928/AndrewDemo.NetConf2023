using System.Diagnostics;
using AndrewDemo.NetConf2023.Core.AdminQueries;
using AndrewDemo.NetConf2023.Core.Checkouts;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class StorageProcessTests
{
    [Fact]
    public async Task SeparateWriterProcess_CheckoutMovementAndOrderSummariesRemainCoherent()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tip-process-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var reader = new ShopDatabaseContext(new() { ConnectionString = $"Filename={directory}/shop.db;Connection=Shared" });
            reader.Members.Insert(new Member { Id = 1, Name = "normal" });
            foreach (var id in Enumerable.Range(1, 6)) reader.Transactions.InsertCheckout(new()
            { TransactionId = id, MemberId = 1, IsTest = id % 2 == 0, State = CheckoutState.Created, TotalPrice = 0 });
            using var writer = Start("tests", "StorageProbe", Path.Combine(directory, "shop.db"),
                Path.Combine(directory, "ready"), Path.Combine(directory, "go"));
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try
            {
                while (!File.Exists(Path.Combine(directory, "ready")))
                { Assert.False(writer.HasExited); await Task.Delay(10, timeout.Token); }
                File.WriteAllText(Path.Combine(directory, "go"), "go");
                var admin = new AdminQueryService(reader, TimeProvider.System);
                var observations = 0;
                while (!writer.HasExited)
                {
                    var checkouts = admin.GetCheckouts(new() { Purpose = "all", PageSize = 100 });
                    Assert.Equal(6, checkouts.TotalCount);
                    Assert.Equal(Enumerable.Range(1, 6), checkouts.Items.Select(x => x.TransactionId).Order());
                    var orders = admin.GetOrders(new() { Purpose = "all", PageSize = 100 });
                    Assert.Equal(orders.Page.TotalCount, orders.Page.Items.Count);
                    Assert.Equal(orders.Page.Items.Sum(x => x.TotalPrice ?? 0), orders.TotalAmount);
                    observations++;
                    await Task.Delay(10, timeout.Token);
                }
                Assert.Equal(0, writer.ExitCode); Assert.True(observations > 1);
                Assert.Empty(reader.Transactions.Checkouts("all"));
                Assert.Equal(6, reader.Transactions.Checkouts("all", true).Length);
                Assert.Equal(3, reader.Transactions.Orders("normal").Length);
                Assert.Equal(3, reader.Transactions.Orders("test").Length);
                reader.Dispose();
                using var reopened = new ShopDatabaseContext(new() { ConnectionString = $"Filename={directory}/shop.db;Connection=Shared" });
                var next = new CheckoutTransactionRecord { MemberId = 1, State = CheckoutState.Created, TotalPrice = 0 };
                reopened.Transactions.InsertCheckout(next);
                Assert.Equal(7, next.TransactionId);
            }
            finally { if (!writer.HasExited) writer.Kill(entireProcessTree: true); }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task MigrationCommand_BackupRestoreRepeatAndInvalidArgumentsPreserveData()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tip-command-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "legacy.db");
        try
        {
            const string original = "{\"_id\":7,\"IsTest\":true,\"extra\":\"保留\"}";
            using (var db = new LiteDatabase(path)) db.GetCollection("orders").Insert(JsonSerializer.Deserialize(original).AsDocument);
            using (var invalid = Start("src", "DatabaseInit", "--bad-option"))
            { await invalid.WaitForExitAsync(); Assert.Equal(2, invalid.ExitCode); }
            using (var first = Start("src", "DatabaseInit", "--migrate-tip-storage", "--database", path))
            { await first.WaitForExitAsync(); Assert.Equal(0, first.ExitCode); }
            var backup = Assert.Single(Directory.GetFiles(directory, "*.bak"));
            using (var db = new LiteDatabase(path))
            { Assert.Null(db.GetCollection("orders").FindById(7)); Assert.Equal("保留", db.GetCollection("test_orders").FindById(7)["extra"].AsString); }
            using (var repeat = Start("src", "DatabaseInit", "--migrate-tip-storage", "--database", path))
            { await repeat.WaitForExitAsync(); Assert.Equal(0, repeat.ExitCode); Assert.Contains("無需搬移", await repeat.StandardOutput.ReadToEndAsync()); }
            // 停止所有連線後還原備份；舊 layout 與原欄位存在，新 layout 版本不存在。
            File.Copy(backup, path, overwrite: true);
            using (var db = new LiteDatabase(path))
            {
                Assert.Equal(JsonSerializer.Deserialize(original).ToString(), db.GetCollection("orders").FindById(7).ToString());
                Assert.Null(db.GetCollection("transaction_storage").FindById(1));
                Assert.Empty(db.GetCollection("test_orders").FindAll());
            }
            var missingPath = Path.Combine(directory, "absent.db");
            using (var missing = Start("src", "DatabaseInit", "--migrate-tip-storage", "--database", missingPath))
            { await missing.WaitForExitAsync(); Assert.Equal(1, missing.ExitCode); Assert.False(File.Exists(missingPath)); }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static Process Start(string folder, string name, params string[] arguments)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "spec"))) root = root.Parent;
        Assert.NotNull(root);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var project = "AndrewDemo.NetConf2023." + name;
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(Path.Combine(root.FullName, folder, project, "bin", configuration, "net10.0", project + ".dll"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start)!;
    }
}
