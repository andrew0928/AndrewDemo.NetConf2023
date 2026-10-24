using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Storage;

/// <summary>只供同步 storage 區段使用；避免 LiteDB Shared mode 重入 BeginTrans 而洩漏 mutex。</summary>
internal sealed class StorageTransaction : IDisposable
{
    [ThreadStatic] private static HashSet<ILiteDatabase>? _active;
    private readonly ILiteDatabase _database;
    private readonly bool _owner;
    private bool _completed;

    private StorageTransaction(ILiteDatabase database, bool owner) { _database = database; _owner = owner; }

    internal static StorageTransaction Begin(ILiteDatabase database, bool requireNew = false)
    {
        _active ??= new(ReferenceEqualityComparer.Instance);
        if (_active.Contains(database))
        {
            if (requireNew) throw new InvalidOperationException("此操作不可參與外層交易。");
            return new(database, false);
        }
        if (!database.BeginTrans()) throw new InvalidOperationException("交易必須由受控 storage 邊界建立。");
        _active.Add(database);
        return new(database, true);
    }

    internal void Commit()
    {
        if (!_owner) return;
        _database.Commit();
        _completed = true;
    }

    internal static void RequireActive(ILiteDatabase database)
    {
        if (_active?.Contains(database) != true)
            throw new InvalidOperationException("此關鍵步驟必須在外層已啟動的 storage transaction 內執行。");
    }

    public void Dispose()
    {
        if (!_owner) return;
        try { if (!_completed) _database.Rollback(); }
        finally { _active!.Remove(_database); }
    }
}
