using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Discounts;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Services;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Tests
{
    public sealed class AppleBtsIntegrationSkeletonTests : ShopDatabaseTestBase
    {
        [Fact]
        public void DiscountPipeline_WithMainAndParentGift_ComposesIntoCoreDiscountEngine()
        {
            Context.Products.Upsert(new Abstract.Products.Product
            {
                Id = "macbook-air",
                Name = "MacBook Air",
                Price = 35900m,
                IsPublished = true
            });

            Context.Products.Upsert(new Abstract.Products.Product
            {
                Id = "airpods-4",
                Name = "AirPods 4",
                Price = 5990m,
                IsPublished = true
            });

            var member = new Member
            {
                Name = "student"
            };
            Context.Members.Insert(member);

            var mainLineId = "main-line-001";
            var giftLineId = "gift-line-001";
            var addedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

            var cart = new Cart
            {
                LineItems = new List<LineItem>
                {
                    new()
                    {
                        LineId = mainLineId,
                        ProductId = "macbook-air",
                        Quantity = 1,
                        AddedAt = addedAt
                    },
                    new()
                    {
                        LineId = giftLineId,
                        ParentLineId = mainLineId,
                        ProductId = "airpods-4",
                        Quantity = 1,
                        AddedAt = addedAt
                    }
                }
            };
            Context.Carts.Insert(cart);

            Context.Database.GetCollection<BtsCampaignRecord>(AppleBtsConstants.CampaignsCollectionName).Upsert(new BtsCampaignRecord
            {
                CampaignId = "bts-2026",
                Name = "Apple BTS 2026",
                StartAt = addedAt.AddDays(-1),
                EndAt = addedAt.AddDays(30),
                IsEnabled = true
            });

            Context.Database.GetCollection<BtsMainOfferRecord>(AppleBtsConstants.MainOffersCollectionName).Upsert(new BtsMainOfferRecord
            {
                OfferId = "offer-macbook-air",
                CampaignId = "bts-2026",
                MainProductId = "macbook-air",
                BtsPrice = 31400m,
                GiftGroupId = "gift-group-macbook",
                MaxGiftSubsidyAmount = 5990m
            });

            Context.Database.GetCollection<BtsGiftOptionRecord>(AppleBtsConstants.GiftOptionsCollectionName).Upsert(new BtsGiftOptionRecord
            {
                OptionId = "gift-airpods-4",
                CampaignId = "bts-2026",
                GiftGroupId = "gift-group-macbook",
                GiftProductId = "airpods-4"
            });

            Context.Database.GetCollection<MemberEducationVerificationRecord>(AppleBtsConstants.MemberEducationVerificationsCollectionName).Upsert(new MemberEducationVerificationRecord
            {
                VerificationId = "verify-student-001",
                MemberId = member.Id,
                Email = "student@campus.edu.tw",
                Status = EducationVerificationStatus.Verified,
                VerifiedAt = addedAt.AddHours(-1),
                ExpireAt = addedAt.AddDays(7),
                Source = "UNiDAYS"
            });

            var qualificationService = new MemberEducationQualificationService(new MemberEducationVerificationRepository(Context));
            var discountRule = new BtsDiscountRule(new BtsOfferRepository(Context), qualificationService);
            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                ProductServiceId = DefaultProductService.ServiceId,
                EnabledDiscountRuleIds = { AppleBtsConstants.DiscountRuleId }
            };

            var enabledRules = new IDiscountRule[] { discountRule };
            var engine = new DiscountEngine(enabledRules);
            var cartContext = CartContextFactory.Create(manifest, cart, member, new DefaultProductService(Context), FixedTimeProvider);

            var discounts = engine.Evaluate(cartContext);

            Assert.Single(discounts);

            var discount = discounts[0];
            Assert.Equal(DiscountRecordKind.Discount, discount.Kind);
            Assert.Equal(AppleBtsConstants.DiscountRuleId, discount.RuleId);
            Assert.Equal(AppleBtsConstants.DiscountName, discount.Name);
            Assert.Equal(-10490m, discount.Amount);
            Assert.Equal(2, discount.RelatedLineIds.Count);
            Assert.Contains(mainLineId, discount.RelatedLineIds);
            Assert.Contains(giftLineId, discount.RelatedLineIds);

            var total = cartContext.LineItems.Sum(x => (x.UnitPrice ?? 0m) * x.Quantity) + discounts.Sum(x => x.Amount);
            Assert.Equal(31400m, total);
        }
    }
}
