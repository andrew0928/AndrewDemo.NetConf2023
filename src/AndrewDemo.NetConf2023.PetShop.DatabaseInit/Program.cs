using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Time;

namespace AndrewDemo.NetConf2023.PetShop.DatabaseInit
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("AndrewDemo PetShop Database Initializer");
            Console.WriteLine("========================================");
            Console.WriteLine();

            var dbFilePath = ResolveDatabaseFilePath();
            Console.WriteLine($"Database file: {dbFilePath}");

            var dbDirectory = Path.GetDirectoryName(dbFilePath);
            if (!string.IsNullOrWhiteSpace(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            if (File.Exists(dbFilePath))
            {
                Console.WriteLine("Existing database file found, removing...");
                File.Delete(dbFilePath);
            }

            using var database = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = $"Filename={dbFilePath};Connection=Direct"
            });
            var timeProvider = TimeProviderFactory.Create(new TimeOptions());

            SeedProducts(database, timeProvider);

            Console.WriteLine();
            Console.WriteLine("PetShop database initialized successfully.");
            Console.WriteLine($"Products: {database.Products.Count()}");
            Console.WriteLine($"Members: {database.Members.Count()}");
            Console.WriteLine($"Orders: {database.Orders.Count()}");
            Console.WriteLine($"Database file created at: {dbFilePath}");
        }

        private static string ResolveDatabaseFilePath()
        {
            var configuredPath = Environment.GetEnvironmentVariable("SHOP_DATABASE_FILEPATH");
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(AppContext.BaseDirectory, configuredPath);
            }

            return Path.Combine(AppContext.BaseDirectory, "shop-database.db");
        }

        private static void SeedProducts(IShopDatabaseContext database, TimeProvider timeProvider)
        {
            Console.WriteLine("Initializing PetShop catalog...");

            foreach (var product in PetShopSeedData.Products)
            {
                database.Products.Upsert(new Product
                {
                    Id = product.ProductId,
                    SkuId = product.SkuId,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    IsPublished = true
                });

                database.Skus.Upsert(new SkuRecord
                {
                    SkuId = product.SkuId,
                    ModelCode = product.SkuId,
                    SpecificationSummary = product.Name
                });

                database.InventoryRecords.Upsert(new InventoryRecord
                {
                    SkuId = product.SkuId,
                    AvailableQuantity = product.InventoryQuantity,
                    UpdatedAt = timeProvider.GetUtcDateTime()
                });

                Console.WriteLine($"  - {product.ProductId}: {product.Name}");
            }
        }
    }
}
