using System;
using System.IO;
using AndrewDemo.NetConf2023.Core;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Tests
{
    public abstract class ShopDatabaseTestBase : IDisposable
    {
        private readonly string _databasePath;
        private readonly ShopDatabaseContext _context;

        protected ShopDatabaseTestBase()
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"andrew-demo-bts-test-{Guid.NewGuid():N}.db");
            var connectionString = $"Filename={_databasePath};Connection=Direct";
            _context = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = connectionString
            });
        }

        protected ShopDatabaseContext Context => _context;

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
    }
}
