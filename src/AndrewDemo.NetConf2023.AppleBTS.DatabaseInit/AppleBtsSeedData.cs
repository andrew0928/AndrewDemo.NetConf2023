using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;

namespace AndrewDemo.NetConf2023.AppleBTS.DatabaseInit
{
    internal static class AppleBtsSeedData
    {
        public const string CampaignId = "apple-bts-2026";

        public static IReadOnlyList<ProductSeed> Products => new[]
        {
            new ProductSeed("macbook-air", "SKU-MACBOOK-AIR", "MacBook Air", "Apple 筆記型電腦主力機型。", 35900m, 50),
            new ProductSeed("macbook-pro", "SKU-MACBOOK-PRO", "MacBook Pro", "Apple 專業級筆記型電腦。", 54900m, 30),
            new ProductSeed("imac", "SKU-IMAC", "iMac", "Apple 一體式桌上型電腦。", 42900m, 20),
            new ProductSeed("mac-mini", "SKU-MAC-MINI", "Mac mini", "Apple 精巧桌上型主機。", 19900m, 30),
            new ProductSeed("mac-studio", "SKU-MAC-STUDIO", "Mac Studio", "Apple 高效能桌上型工作站。", 67900m, 10),
            new ProductSeed("mac-pro", "SKU-MAC-PRO", "Mac Pro", "Apple 旗艦工作站。", 229900m, 5),
            new ProductSeed("ipad-pro", "SKU-IPAD-PRO", "iPad Pro", "Apple 高階平板。", 32900m, 40),
            new ProductSeed("ipad-air", "SKU-IPAD-AIR", "iPad Air", "Apple 輕薄平板。", 19900m, 50),
            new ProductSeed("ipad", "SKU-IPAD", "iPad", "Apple 入門平板。", 11900m, 60),
            new ProductSeed("ipad-mini", "SKU-IPAD-MINI", "iPad mini", "Apple 輕巧平板。", 15900m, 40),
            new ProductSeed("iphone-17-pro", "SKU-IPHONE-17-PRO", "iPhone 17 Pro", "Apple 旗艦手機。", 36900m, 60),
            new ProductSeed("iphone-air", "SKU-IPHONE-AIR", "iPhone Air", "Apple 輕薄手機。", 29900m, 60),
            new ProductSeed("iphone-17", "SKU-IPHONE-17", "iPhone 17", "Apple 主流手機。", 25900m, 60),
            new ProductSeed("iphone-17e", "SKU-IPHONE-17E", "iPhone 17e", "Apple 入門手機。", 21900m, 60),
            new ProductSeed("iphone-16", "SKU-IPHONE-16", "iPhone 16", "Apple 前代手機。", 25900m, 40),
            new ProductSeed("apple-watch-series-11", "SKU-WATCH-S11", "Apple Watch Series 11", "Apple 智慧手錶。", 13900m, 50),
            new ProductSeed("apple-watch-se-3", "SKU-WATCH-SE3", "Apple Watch SE 3", "Apple 入門智慧手錶。", 8900m, 50),
            new ProductSeed("apple-watch-ultra-3", "SKU-WATCH-ULTRA3", "Apple Watch Ultra 3", "Apple 專業戶外智慧手錶。", 27900m, 20),
            new ProductSeed("airpods-4", "SKU-AIRPODS-4", "AirPods 4", "Apple 無線耳機。", 5990m, 100),
            new ProductSeed("airpods-pro-3", "SKU-AIRPODS-PRO-3", "AirPods Pro 3", "Apple 主動降噪耳機。", 7990m, 80),
            new ProductSeed("airpods-max", "SKU-AIRPODS-MAX", "AirPods Max", "Apple 頭戴式耳機。", 17900m, 20),
            new ProductSeed("apple-pencil-pro", "SKU-APPLE-PENCIL-PRO", "Apple Pencil Pro", "Apple 進階手寫筆。", 4500m, 80),
            new ProductSeed("apple-pencil-usb-c", "SKU-APPLE-PENCIL-USB-C", "Apple Pencil (USB-C)", "Apple 基礎手寫筆。", 2690m, 80),
            new ProductSeed("magic-trackpad", "SKU-MAGIC-TRACKPAD", "Magic Trackpad", "Apple 觸控板配件。", 4500m, 40)
        };

        public static IReadOnlyList<BtsMainOfferRecord> MainOffers => new[]
        {
            new BtsMainOfferRecord { OfferId = "offer-macbook-air", CampaignId = CampaignId, MainProductId = "macbook-air", BtsPrice = 31400m, GiftGroupId = "gift-group-mac", MaxGiftQuantity = 1, MaxGiftSubsidyAmount = 5990m },
            new BtsMainOfferRecord { OfferId = "offer-macbook-pro", CampaignId = CampaignId, MainProductId = "macbook-pro", BtsPrice = 50400m, GiftGroupId = "gift-group-mac", MaxGiftQuantity = 1, MaxGiftSubsidyAmount = 5990m },
            new BtsMainOfferRecord { OfferId = "offer-imac", CampaignId = CampaignId, MainProductId = "imac", BtsPrice = 38400m, GiftGroupId = "gift-group-mac", MaxGiftQuantity = 1, MaxGiftSubsidyAmount = 5990m },
            new BtsMainOfferRecord { OfferId = "offer-mac-mini", CampaignId = CampaignId, MainProductId = "mac-mini", BtsPrice = 18400m, GiftGroupId = null, MaxGiftQuantity = 0, MaxGiftSubsidyAmount = null },
            new BtsMainOfferRecord { OfferId = "offer-ipad-pro", CampaignId = CampaignId, MainProductId = "ipad-pro", BtsPrice = 28400m, GiftGroupId = "gift-group-ipad-pro", MaxGiftQuantity = 1, MaxGiftSubsidyAmount = 4500m },
            new BtsMainOfferRecord { OfferId = "offer-ipad-air", CampaignId = CampaignId, MainProductId = "ipad-air", BtsPrice = 18400m, GiftGroupId = "gift-group-ipad-pro", MaxGiftQuantity = 1, MaxGiftSubsidyAmount = 4500m },
            new BtsMainOfferRecord { OfferId = "offer-ipad", CampaignId = CampaignId, MainProductId = "ipad", BtsPrice = 10400m, GiftGroupId = "gift-group-ipad-basic", MaxGiftQuantity = 1, MaxGiftSubsidyAmount = 4500m },
            new BtsMainOfferRecord { OfferId = "offer-ipad-mini", CampaignId = CampaignId, MainProductId = "ipad-mini", BtsPrice = 14400m, GiftGroupId = "gift-group-ipad-basic", MaxGiftQuantity = 1, MaxGiftSubsidyAmount = 4500m }
        };

        public static IReadOnlyList<BtsGiftOptionRecord> GiftOptions => new[]
        {
            new BtsGiftOptionRecord { OptionId = "gift-mac-airpods4", CampaignId = CampaignId, GiftGroupId = "gift-group-mac", GiftProductId = "airpods-4" },
            new BtsGiftOptionRecord { OptionId = "gift-mac-airpodspro3", CampaignId = CampaignId, GiftGroupId = "gift-group-mac", GiftProductId = "airpods-pro-3" },
            new BtsGiftOptionRecord { OptionId = "gift-mac-trackpad", CampaignId = CampaignId, GiftGroupId = "gift-group-mac", GiftProductId = "magic-trackpad" },
            new BtsGiftOptionRecord { OptionId = "gift-ipadpro-airpods4", CampaignId = CampaignId, GiftGroupId = "gift-group-ipad-pro", GiftProductId = "airpods-4" },
            new BtsGiftOptionRecord { OptionId = "gift-ipadpro-pencil-pro", CampaignId = CampaignId, GiftGroupId = "gift-group-ipad-pro", GiftProductId = "apple-pencil-pro" },
            new BtsGiftOptionRecord { OptionId = "gift-ipad-basic-airpods4", CampaignId = CampaignId, GiftGroupId = "gift-group-ipad-basic", GiftProductId = "airpods-4" },
            new BtsGiftOptionRecord { OptionId = "gift-ipad-basic-pencil-usbc", CampaignId = CampaignId, GiftGroupId = "gift-group-ipad-basic", GiftProductId = "apple-pencil-usb-c" }
        };

        public static BtsCampaignRecord CreateCampaign()
        {
            var timeZone = ResolveTaipeiTimeZone();
            var startAt = ConvertTaipeiLocalToUtc(timeZone, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Unspecified));
            var endAt = ConvertTaipeiLocalToUtc(timeZone, new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Unspecified));

            return new BtsCampaignRecord
            {
                CampaignId = CampaignId,
                Name = "Apple BTS 2026",
                StartAt = startAt,
                EndAt = endAt,
                IsEnabled = true
            };
        }

        public static IReadOnlyList<MemberSeed> Members => new[]
        {
            new MemberSeed(
                "bts-expired-user",
                "expired-user@campus.edu.tw",
                EducationVerificationStatus.Verified,
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 31, 15, 59, 59, DateTimeKind.Utc))
        };

        private static TimeZoneInfo ResolveTaipeiTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            }
        }

        private static DateTime ConvertTaipeiLocalToUtc(TimeZoneInfo timeZone, DateTime localTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
        }
    }

    internal sealed record ProductSeed(
        string ProductId,
        string SkuId,
        string Name,
        string Description,
        decimal Price,
        int InventoryQuantity);

    internal sealed record MemberSeed(
        string Name,
        string Email,
        EducationVerificationStatus Status,
        DateTime VerifiedAt,
        DateTime ExpireAt);
}
