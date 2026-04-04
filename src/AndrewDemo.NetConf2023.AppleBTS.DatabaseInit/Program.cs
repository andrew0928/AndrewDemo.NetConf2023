using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Services;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Time;

namespace AndrewDemo.NetConf2023.AppleBTS.DatabaseInit
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("AndrewDemo AppleBTS Database Initializer");
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

            var adminService = new AppleBtsAdminService(
                new AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories.BtsOfferRepository(database),
                new AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories.MemberEducationVerificationRepository(database));

            SeedProducts(database, timeProvider);
            SeedCampaign(adminService);
            SeedMembers(database, adminService);

            Console.WriteLine();
            Console.WriteLine("AppleBTS database initialized successfully.");
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
            Console.WriteLine("Initializing Apple catalog...");

            foreach (var product in AppleBtsSeedData.Products)
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

        private static void SeedCampaign(AppleBtsAdminService adminService)
        {
            Console.WriteLine("Initializing Apple BTS campaign...");

            adminService.UpsertCampaign(AppleBtsSeedData.CreateCampaign());

            foreach (var offer in AppleBtsSeedData.MainOffers)
            {
                adminService.UpsertMainOffer(offer);
                Console.WriteLine($"  - main offer: {offer.MainProductId}");
            }

            foreach (var option in AppleBtsSeedData.GiftOptions)
            {
                adminService.UpsertGiftOption(option);
                Console.WriteLine($"  - gift option: {option.GiftProductId} ({option.GiftGroupId})");
            }
        }

        private static void SeedMembers(IShopDatabaseContext database, AppleBtsAdminService adminService)
        {
            Console.WriteLine("Initializing Apple BTS members...");

            foreach (var memberSeed in AppleBtsSeedData.Members)
            {
                var member = database.Members.FindOne(x => x.Name == memberSeed.Name)
                    ?? new Member { Name = memberSeed.Name };

                if (member.Id == 0)
                {
                    database.Members.Insert(member);
                }
                else
                {
                    database.Members.Update(member);
                }

                adminService.UpsertMemberEducationVerification(new MemberEducationVerificationRecord
                {
                    VerificationId = $"{member.Name}-seed",
                    MemberId = member.Id,
                    Email = memberSeed.Email,
                    Status = memberSeed.Status,
                    VerifiedAt = memberSeed.VerifiedAt,
                    ExpireAt = memberSeed.ExpireAt,
                    Source = "apple-bts-seed"
                });

                Console.WriteLine($"  - member: {member.Name} ({memberSeed.Status})");
            }
        }
    }
}
