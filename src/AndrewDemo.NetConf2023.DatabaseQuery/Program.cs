using LiteDB;

if (args.Length != 2)
{
    Console.Error.WriteLine("用法：DatabaseQuery <資料庫路徑> <SELECT 查詢>；執行前請停止資料庫寫入端。");
    return 2;
}

try
{
    var path = Path.GetFullPath(args[0]);
    if (!File.Exists(path)) throw new FileNotFoundException("找不到資料庫，不會自動建立。", path);
    var query = args[1].Trim();
    // 小工具只接受單一查詢；避免引擎只執行第一句卻讓呼叫端誤認整段成功。
    if (query.TrimEnd(';').Contains(';'))
        throw new ArgumentException("一次只接受一個查詢；分號只可放在結尾（字串內的分號亦不支援）。");
    if (!query.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase) &&
        !query.StartsWith("SELECT\n", StringComparison.OrdinalIgnoreCase) &&
        !query.StartsWith("SELECT\t", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("此工具只接受 SELECT 查詢。");

    Console.Error.WriteLine($"資料庫：{path}");
    Console.Error.WriteLine($"檔案修改時間（UTC）：{File.GetLastWriteTimeUtc(path):O}");
    // 每次啟動建立新連線，離開即釋放；不經 Core 初始化或建立索引。
    using var database = new LiteDatabase(new ConnectionString
    {
        Filename = path,
        ReadOnly = true,
        Connection = ConnectionType.Direct
    });
    using var reader = database.Execute(query);
    var count = 0;
    while (reader.Read())
    {
        Console.WriteLine(JsonSerializer.Serialize(reader.Current));
        count++;
    }
    Console.Error.WriteLine($"共 {count} 筆結果（唯讀、新連線）。");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"查詢失敗：{error.Message}");
    return 1;
}
