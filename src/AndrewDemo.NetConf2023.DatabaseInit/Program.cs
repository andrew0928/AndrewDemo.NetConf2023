using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;

namespace AndrewDemo.NetConf2023.DatabaseInit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("AndrewDemo Shop Database Initializer");
            Console.WriteLine("=====================================");
            Console.WriteLine();

            // 設定資料庫檔案路徑
            var outputDir = AppContext.BaseDirectory;
            var dbFilePath = Path.Combine(outputDir, "shop-database.db");
            
            Console.WriteLine($"Database file: {dbFilePath}");
            
            // 如果檔案已存在，先刪除
            if (File.Exists(dbFilePath))
            {
                Console.WriteLine("Existing database file found, removing...");
                File.Delete(dbFilePath);
            }

            // 建立資料庫連線
            var connectionString = $"Filename={dbFilePath};Connection=Direct";
            var database = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = connectionString
            });

            Console.WriteLine("Initializing products...");

            // 初始化產品資料 (從 API 專案遷移)
            database.Products.Upsert(new Product()
            {
                Id = "1",
                SkuId = "SKU-BEER-18D",
                Name = "18天台灣生啤酒 355ml",
                Description = "18天台灣生啤酒未經過巴氏德高溫殺菌，採用歐洲優質原料，全程0-7°C冷藏保鮮，猶如鮮奶與生魚片般珍貴，保留最多啤酒營養及麥香風味；這樣高品質、超新鮮、賞味期只有18天的台灣生啤酒，值得您搶鮮到手!",
                Price = 65m,
                IsPublished = true
            });
            database.Skus.Upsert(new SkuRecord { SkuId = "SKU-BEER-18D", ModelCode = "BEER-18D-355ML", SpecificationSummary = "18天台灣生啤酒 355ml" });
            database.InventoryRecords.Upsert(new InventoryRecord { SkuId = "SKU-BEER-18D", AvailableQuantity = 100, UpdatedAt = DateTime.UtcNow });
            Console.WriteLine("  - Product 1: 18天台灣生啤酒 355ml");

            database.Products.Upsert(new Product()
            {
                Id = "2",
                SkuId = "SKU-COKE-350",
                Name = "可口可樂® 350ml",
                Description = "1886年，美國喬治亞州的亞特蘭大市，有位名叫約翰•潘伯頓（Dr. John S. Pemberton）的藥劑師，他挑選了幾種特別的成分，發明出一款美味的糖漿，沒想到清涼、暢快的「可口可樂」就奇蹟般的出現了！潘伯頓相信這產品可能具有商業價值，因此把它送到傑柯藥局（Jacobs' Pharmacy）販售，開始了「可口可樂」這個美國飲料的傳奇。而潘伯頓的事業合夥人兼會計師：法蘭克•羅賓森（Frank M. Robinson），認為兩個大寫C字母在廣告上可以有不錯的表現，所以創造了\"Coca‑Cola\"這個名字。但是讓「可口可樂」得以大展鋒頭的，卻是從艾薩•坎德勒（Asa G. Candler）這個具有行銷頭腦的企業家開始。",
                Price = 18m,
                IsPublished = true
            });
            database.Skus.Upsert(new SkuRecord { SkuId = "SKU-COKE-350", ModelCode = "COKE-350ML", SpecificationSummary = "可口可樂 350ml" });
            database.InventoryRecords.Upsert(new InventoryRecord { SkuId = "SKU-COKE-350", AvailableQuantity = 100, UpdatedAt = DateTime.UtcNow });
            Console.WriteLine("  - Product 2: 可口可樂® 350ml");

            database.Products.Upsert(new Product()
            {
                Id = "3",
                SkuId = "SKU-GREEN-TEA-550",
                Name = "御茶園 特撰冰釀綠茶 550ml",
                Description = "新升級!台灣在地茶葉入，冰釀回甘。台灣在地茶葉，原葉沖泡。如同現泡般的清新綠茶香。",
                Price = 25m,
                IsPublished = true
            });
            database.Skus.Upsert(new SkuRecord { SkuId = "SKU-GREEN-TEA-550", ModelCode = "GREEN-TEA-550ML", SpecificationSummary = "御茶園 特撰冰釀綠茶 550ml" });
            database.InventoryRecords.Upsert(new InventoryRecord { SkuId = "SKU-GREEN-TEA-550", AvailableQuantity = 100, UpdatedAt = DateTime.UtcNow });
            Console.WriteLine("  - Product 3: 御茶園 特撰冰釀綠茶 550ml");

            Console.WriteLine();
            Console.WriteLine($"Database initialized successfully!");
            Console.WriteLine($"Total products: {database.Products.Count()}");
            Console.WriteLine();
            Console.WriteLine($"Database file created at: {dbFilePath}");

            database.Dispose();
        }
    }
}
