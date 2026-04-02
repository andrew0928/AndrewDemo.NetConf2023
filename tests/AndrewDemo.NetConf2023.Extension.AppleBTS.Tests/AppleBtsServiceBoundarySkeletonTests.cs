using System;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Discounts;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Records;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Services;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Tests
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

            Assert.Throws<NotImplementedException>(() => catalogService.GetOfferDetail("macbook-air", DateTime.UtcNow));
            Assert.Throws<NotImplementedException>(() => adminService.UpsertCampaign(new BtsCampaignRecord
            {
                CampaignId = "bts-2026",
                Name = "Apple BTS 2026",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddDays(30),
                IsEnabled = true
            }));
            Assert.NotNull(discountRule);
        }
    }
}
