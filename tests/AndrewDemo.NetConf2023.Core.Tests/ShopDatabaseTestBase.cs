using System;
using System.IO;
using AndrewDemo.NetConf2023.Core;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    /// <summary>
    /// 建立隔離的 LiteDB 資料庫供每個測試使用，確保測試之間不互相污染。
    /// </summary>
    public abstract class ShopDatabaseTestBase : IDisposable
    {
        private readonly string _databasePath;
        private readonly ShopDatabaseContext _context;

        protected ShopDatabaseTestBase()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"andrew-demo-test-{Guid.NewGuid():N}.db");
            var connectionString = $"Filename={_databasePath};Connection=Direct";
            _context = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = connectionString
            });
        }

        public void Dispose()
        {
            _context.Dispose();
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
            catch
            {
                // ignore cleanup error in tests
            }
        }

    protected ShopDatabaseContext Context => _context;
    }
}
