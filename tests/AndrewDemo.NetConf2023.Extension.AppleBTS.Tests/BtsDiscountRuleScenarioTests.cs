using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Discounts;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Models;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Records;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Services;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Tests
{
    public sealed class BtsDiscountRuleScenarioTests : ShopDatabaseTestBase
    {
        private static readonly DateTime EvaluatedAt = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void M01_WhenCampaignAndQualificationAreValid_MainUsesBtsPrice()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(memberId, CreateMainLine("main-001", "macbook-air", 35900m));

            var discount = AssertSingleDiscount(discounts, "main-001");
            Assert.Equal(-4500m, discount.Amount);
            Assert.Equal(31400m, ApplyToTotal(35900m, discounts));
        }

        [Fact]
        public void M02_WhenQualificationIsInvalid_ReturnsHintAndKeepsRegularPrice()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            var memberId = SeedVerification(EducationVerificationStatus.Rejected, EvaluatedAt.AddDays(-2), EvaluatedAt.AddDays(5));

            var discounts = Evaluate(memberId, CreateMainLine("main-001", "macbook-air", 35900m));

            var hint = AssertSingleHint(discounts, "main-001");
            Assert.Contains("教育驗證", hint.Description);
            Assert.Equal(35900m, ApplyToTotal(35900m, discounts));
        }

        [Fact]
        public void M03_WhenCampaignIsInactive_BtsIsDisabledAndReturnsHint()
        {
            SeedInactiveCampaign();
            SeedMainOffer();

            var discounts = Evaluate(consumerId: null, CreateMainLine("main-001", "macbook-air", 35900m));

            var hint = AssertSingleHint(discounts, "main-001");
            Assert.Contains("未啟用", hint.Description);
            Assert.Equal(35900m, ApplyToTotal(35900m, discounts));
        }

        [Fact]
        public void M04_WhenMainOfferDoesNotExist_ReturnsNoBtsRecord()
        {
            SeedActiveCampaign();
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(memberId, CreateMainLine("main-001", "macbook-air", 35900m));

            Assert.Empty(discounts);
        }

        [Fact]
        public void G01_WhenNoGiftSelected_MainStillUsesBtsPrice()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(memberId, CreateMainLine("main-001", "macbook-air", 35900m));

            var discount = AssertSingleDiscount(discounts, "main-001");
            Assert.Equal(-4500m, discount.Amount);
        }

        [Fact]
        public void G02_WhenGiftHasParentAndBelongsToGroup_AppliesGiftSubsidy()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "airpods-4", 5990m));

            var discount = AssertSingleDiscount(discounts, "main-001", "gift-001");
            Assert.Equal(-10490m, discount.Amount);
        }

        [Fact]
        public void G03_WhenGiftHasNoParent_DoesNotReceiveGiftSubsidy()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateMainLine("gift-root-001", "airpods-4", 5990m));

            var discount = AssertSingleDiscount(discounts, "main-001");
            Assert.Equal(-4500m, discount.Amount);
        }

        [Fact]
        public void G04_WhenGiftIsOutsideGiftGroup_DoesNotReceiveGiftSubsidy()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "apple-pencil", 4500m));

            var discount = AssertSingleDiscount(discounts, "main-001");
            Assert.Equal(-4500m, discount.Amount);
        }

        [Fact]
        public void G05_WhenMainOfferHasNoGiftGroup_GiftLogicIsIgnored()
        {
            SeedActiveCampaign();
            SeedMainOffer(giftGroupId: null, maxGiftSubsidyAmount: null);
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "airpods-4", 5990m));

            var discount = AssertSingleDiscount(discounts, "main-001");
            Assert.Equal(-4500m, discount.Amount);
        }

        [Fact]
        public void P01_MacbookAirWithAirPods4_TotalIs31400()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "airpods-4", 5990m));

            Assert.Equal(31400m, ApplyToTotal(35900m + 5990m, discounts));
        }

        [Fact]
        public void P02_MacbookAirWithApplePencil_TotalIs31400()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("apple-pencil");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "apple-pencil", 4500m));

            Assert.Equal(31400m, ApplyToTotal(35900m + 4500m, discounts));
        }

        [Fact]
        public void P03_MacbookAirWithAirPodsPro3_UserPaysGiftPriceDifference()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-pro-3");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "airpods-pro-3", 7990m));

            Assert.Equal(33400m, ApplyToTotal(35900m + 7990m, discounts));
        }

        [Fact]
        public void P04_MacbookAirWithoutGift_DoesNotTransferUnusedGiftSubsidy()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(memberId, CreateMainLine("main-001", "macbook-air", 35900m));

            Assert.Equal(31400m, ApplyToTotal(35900m, discounts));
            var discount = AssertSingleDiscount(discounts, "main-001");
            Assert.Equal(-4500m, discount.Amount);
        }

        [Fact]
        public void C03_WhenCampaignExpiresAtEvaluationTime_ReturnsHintAndFallsBackToRegularPrice()
        {
            SeedExpiredCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "airpods-4", 5990m));

            var hint = AssertSingleHint(discounts, "main-001", "gift-001");
            Assert.Contains("已過期", hint.Description);
            Assert.Equal(41890m, ApplyToTotal(35900m + 5990m, discounts));
        }

        [Fact]
        public void C04_WhenQualificationExpiresAtEvaluationTime_ReturnsHintAndFallsBackToRegularPrice()
        {
            SeedActiveCampaign();
            SeedMainOffer();
            SeedGiftOption("airpods-4");
            var memberId = SeedVerification(EducationVerificationStatus.Verified, EvaluatedAt.AddDays(-10), EvaluatedAt.AddDays(-1));

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "airpods-4", 5990m));

            var hint = AssertSingleHint(discounts, "main-001", "gift-001");
            Assert.Contains("已過期", hint.Description);
            Assert.Equal(41890m, ApplyToTotal(35900m + 5990m, discounts));
        }

        [Fact]
        public void C05_WhenSelectingMoreThanOneGift_ReturnsHintAndSkipsGiftSubsidy()
        {
            SeedActiveCampaign();
            SeedMainOffer(maxGiftQuantity: 1);
            SeedGiftOption("airpods-4");
            SeedGiftOption("apple-pencil");
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(
                memberId,
                CreateMainLine("main-001", "macbook-air", 35900m),
                CreateGiftLine("gift-001", "main-001", "airpods-4", 5990m),
                CreateGiftLine("gift-002", "main-001", "apple-pencil", 4500m));

            Assert.Equal(2, discounts.Count);

            var discount = discounts.Single(x => x.Kind == DiscountRecordKind.Discount);
            Assert.Equal(-4500m, discount.Amount);
            Assert.Equal(new[] { "main-001" }, discount.RelatedLineIds);

            var hint = discounts.Single(x => x.Kind == DiscountRecordKind.Hint);
            Assert.Contains("最多只能選 1 個贈品", hint.Description);
            Assert.Equal(new[] { "main-001", "gift-001", "gift-002" }, hint.RelatedLineIds);
        }

        [Fact]
        public void C06_WhenMainOfferHasPriceOnlyWithoutGift_OnlyBtsPriceIsApplied()
        {
            SeedActiveCampaign();
            SeedMainOffer(giftGroupId: null, maxGiftSubsidyAmount: null);
            var memberId = SeedVerifiedMember();

            var discounts = Evaluate(memberId, CreateMainLine("main-001", "macbook-air", 35900m));

            var discount = AssertSingleDiscount(discounts, "main-001");
            Assert.Equal(-4500m, discount.Amount);
            Assert.Equal(31400m, ApplyToTotal(35900m, discounts));
        }

        private IReadOnlyList<DiscountRecord> Evaluate(int? consumerId, params LineItem[] lines)
        {
            var repository = new BtsOfferRepository(Context);
            var qualificationService = new MemberEducationQualificationService(new MemberEducationVerificationRepository(Context));
            var rule = new BtsDiscountRule(repository, qualificationService);

            var context = new CartContext
            {
                ShopId = "default",
                ConsumerId = consumerId,
                EvaluatedAt = EvaluatedAt,
                LineItems = lines
            };

            return rule.Evaluate(context);
        }

        private void SeedActiveCampaign()
        {
            Context.Database
                .GetCollection<BtsCampaignRecord>(AppleBtsConstants.CampaignsCollectionName)
                .Upsert(new BtsCampaignRecord
            {
                CampaignId = "bts-2026",
                Name = "Apple BTS 2026",
                StartAt = EvaluatedAt.AddDays(-1),
                EndAt = EvaluatedAt.AddDays(30),
                IsEnabled = true
            });
        }

        private void SeedInactiveCampaign()
        {
            Context.Database
                .GetCollection<BtsCampaignRecord>(AppleBtsConstants.CampaignsCollectionName)
                .Upsert(new BtsCampaignRecord
            {
                CampaignId = "bts-2026",
                Name = "Apple BTS 2026",
                StartAt = EvaluatedAt.AddDays(-30),
                EndAt = EvaluatedAt.AddDays(-5),
                IsEnabled = true
            });
        }

        private void SeedExpiredCampaign()
        {
            SeedInactiveCampaign();
        }

        private void SeedMainOffer(
            string mainProductId = "macbook-air",
            decimal btsPrice = 31400m,
            string? giftGroupId = "gift-group-macbook",
            decimal? maxGiftSubsidyAmount = 5990m,
            int maxGiftQuantity = 1)
        {
            Context.Database
                .GetCollection<BtsMainOfferRecord>(AppleBtsConstants.MainOffersCollectionName)
                .Upsert(new BtsMainOfferRecord
            {
                OfferId = $"offer-{mainProductId}",
                CampaignId = "bts-2026",
                MainProductId = mainProductId,
                BtsPrice = btsPrice,
                GiftGroupId = giftGroupId,
                MaxGiftQuantity = maxGiftQuantity,
                MaxGiftSubsidyAmount = maxGiftSubsidyAmount
            });
        }

        private void SeedGiftOption(string giftProductId, string giftGroupId = "gift-group-macbook")
        {
            Context.Database
                .GetCollection<BtsGiftOptionRecord>(AppleBtsConstants.GiftOptionsCollectionName)
                .Upsert(new BtsGiftOptionRecord
            {
                OptionId = $"gift-{giftProductId}",
                CampaignId = "bts-2026",
                GiftGroupId = giftGroupId,
                GiftProductId = giftProductId
            });
        }

        private int SeedVerifiedMember()
        {
            return SeedVerification(
                EducationVerificationStatus.Verified,
                EvaluatedAt.AddDays(-2),
                EvaluatedAt.AddDays(5));
        }

        private int SeedVerification(
            EducationVerificationStatus status,
            DateTime verifiedAt,
            DateTime expireAt)
        {
            var member = new AndrewDemo.NetConf2023.Core.Member
            {
                Name = $"member-{Guid.NewGuid():N}"
            };
            Context.Members.Insert(member);

            Context.Database
                .GetCollection<MemberEducationVerificationRecord>(AppleBtsConstants.MemberEducationVerificationsCollectionName)
                .Upsert(new MemberEducationVerificationRecord
            {
                VerificationId = $"verify-{member.Id}",
                MemberId = member.Id,
                Email = "student@campus.edu.tw",
                Status = status,
                VerifiedAt = verifiedAt,
                ExpireAt = expireAt,
                Source = "UNiDAYS"
            });

            return member.Id;
        }

        private static LineItem CreateMainLine(string lineId, string productId, decimal unitPrice)
        {
            return new LineItem
            {
                LineId = lineId,
                ProductId = productId,
                UnitPrice = unitPrice,
                Quantity = 1,
                AddedAt = EvaluatedAt
            };
        }

        private static LineItem CreateGiftLine(string lineId, string parentLineId, string productId, decimal unitPrice)
        {
            return new LineItem
            {
                LineId = lineId,
                ParentLineId = parentLineId,
                ProductId = productId,
                UnitPrice = unitPrice,
                Quantity = 1,
                AddedAt = EvaluatedAt
            };
        }

        private static DiscountRecord AssertSingleDiscount(IReadOnlyList<DiscountRecord> discounts, params string[] relatedLineIds)
        {
            var discount = Assert.Single(discounts);
            Assert.Equal(DiscountRecordKind.Discount, discount.Kind);
            Assert.Equal(relatedLineIds, discount.RelatedLineIds);
            return discount;
        }

        private static DiscountRecord AssertSingleHint(IReadOnlyList<DiscountRecord> discounts, params string[] relatedLineIds)
        {
            var hint = Assert.Single(discounts);
            Assert.Equal(DiscountRecordKind.Hint, hint.Kind);
            Assert.Equal(0m, hint.Amount);
            Assert.Equal(relatedLineIds, hint.RelatedLineIds);
            return hint;
        }

        private static decimal ApplyToTotal(decimal originalTotal, IReadOnlyList<DiscountRecord> discounts)
        {
            return originalTotal + discounts
                .Where(x => x.Kind == DiscountRecordKind.Discount)
                .Sum(x => x.Amount);
        }
    }
}
