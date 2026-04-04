using System;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Discounts;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Services;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Tests
{
    public sealed class AppleBtsServiceBoundarySkeletonTests : ShopDatabaseTestBase
    {
        [Fact]
        public void DefaultProductService_RemainsProductOwner_WhileAppleBtsServicesOwnSidecarOperations()
        {
            Context.Products.Upsert(new Product
            {
                Id = "macbook-air",
                Name = "MacBook Air",
                Price = 35900m,
                IsPublished = true
            });

            var productService = new DefaultProductService(Context);
            var product = productService.GetProductById("macbook-air");

            Assert.NotNull(product);
            Assert.Equal("MacBook Air", product!.Name);
            Assert.Equal(35900m, product.Price);

            var offerRepository = new BtsOfferRepository(Context);
            var verificationRepository = new MemberEducationVerificationRepository(Context);
            var catalogService = new AppleBtsCatalogService(offerRepository);
            var adminService = new AppleBtsAdminService(offerRepository, verificationRepository);
            var qualificationService = new MemberEducationQualificationService(verificationRepository);
            var discountRule = new BtsDiscountRule(offerRepository, qualificationService);

            var now = DateTime.UtcNow;

            adminService.UpsertCampaign(new BtsCampaignRecord
            {
                CampaignId = "bts-2026",
                Name = "Apple BTS 2026",
                StartAt = now.AddDays(-1),
                EndAt = now.AddDays(30),
                IsEnabled = true
            });
            adminService.UpsertMainOffer(new BtsMainOfferRecord
            {
                OfferId = "offer-macbook-air",
                CampaignId = "bts-2026",
                MainProductId = "macbook-air",
                BtsPrice = 31400m,
                GiftGroupId = "gift-group-macbook",
                MaxGiftSubsidyAmount = 5990m
            });
            adminService.UpsertGiftOption(new BtsGiftOptionRecord
            {
                OptionId = "gift-airpods-4",
                CampaignId = "bts-2026",
                GiftGroupId = "gift-group-macbook",
                GiftProductId = "airpods-4"
            });
            adminService.UpsertMemberEducationVerification(new MemberEducationVerificationRecord
            {
                VerificationId = "verify-member-001",
                MemberId = 1001,
                Email = "student@campus.edu.tw",
                Status = EducationVerificationStatus.Verified,
                VerifiedAt = now.AddDays(-2),
                ExpireAt = now.AddDays(5),
                Source = "UNiDAYS"
            });

            var offerDetail = catalogService.GetOfferDetail("macbook-air", now);
            var qualification = qualificationService.Evaluate(1001, now);

            Assert.NotNull(offerDetail.Campaign);
            Assert.NotNull(offerDetail.MainOffer);
            Assert.Single(offerDetail.GiftOptions);
            Assert.True(qualification.IsQualified);
            Assert.NotNull(discountRule);
        }
    }
}
