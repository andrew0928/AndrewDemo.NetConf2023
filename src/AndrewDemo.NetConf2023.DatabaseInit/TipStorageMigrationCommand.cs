using AndrewDemo.NetConf2023.Core.Storage;
using LiteDB;

namespace AndrewDemo.NetConf2023.DatabaseInit;

internal static class TipStorageMigrationCommand
{
    internal static int Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--migrate-tip-storage" || args[1] != "--database")
        {
            Console.Error.WriteLine("用法：--migrate-tip-storage --database <既有資料庫路徑>；請先停止所有 writers。");
            return 2;
        }
        try
        {
            var path = Path.GetFullPath(args[2]);
            if (!File.Exists(path)) throw new FileNotFoundException("指定資料庫不存在，不建立空資料庫。", path);
            // Direct connection 持有檔案寫入權；完成 checkpoint 後才複製一致備份。
            using var database = new LiteDatabase(new ConnectionString { Filename = path, Connection = ConnectionType.Direct });
            database.Checkpoint();
            var backup = path + $".before-tip-{TimeProvider.System.GetUtcNow():yyyyMMddHHmmss}-{Guid.NewGuid():N}.bak";
            File.Copy(path, backup, overwrite: false);
            Console.WriteLine($"備份：{backup}");
            var result = TransactionStorageMigrator.Migrate(database);
            Console.WriteLine(result.AlreadyCurrent ? "交易儲存版本已符合且資料一致，無需搬移。"
                : $"遷移完成：test Order={result.MovedOrders}、active Checkout={result.MovedActive}、archived Checkout={result.MovedArchived}。");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"遷移失敗（{error.GetType().Name}）：{error.Message}");
            return 1;
        }
    }
}
