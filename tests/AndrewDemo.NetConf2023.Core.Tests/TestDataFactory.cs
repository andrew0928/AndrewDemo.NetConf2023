using System;
using System.Threading;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    internal static class TestDataFactory
    {
        private static int _productSeq = 1000;

        internal static string CreateProduct(IShopDatabaseContext context, decimal price, string? name = null, string? description = null)
        {
            int seq = Interlocked.Increment(ref _productSeq);

            var product = new Product
            {
                Id = seq.ToString(),
                SkuId = null,
                Name = name ?? $"TestProduct-{seq}",
                Description = description,
                Price = price,
                IsPublished = true
            };

            context.Products.Insert(product);
            return product.Id;
        }

        internal static (string productId, string skuId) CreateStockTrackedProduct(
            IShopDatabaseContext context,
            decimal price,
            int availableQuantity,
            string? name = null,
            string? description = null)
        {
            var skuId = $"SKU-{Guid.NewGuid():N}";

            int seq = Interlocked.Increment(ref _productSeq);
            var product = new Product
            {
                Id = seq.ToString(),
                SkuId = skuId,
                Name = name ?? $"TestProduct-{seq}",
                Description = description,
                Price = price,
                IsPublished = true
            };

            context.Products.Insert(product);

            context.Skus.Upsert(new SkuRecord
            {
                SkuId = skuId,
                ModelCode = skuId
            });

            context.InventoryRecords.Upsert(new InventoryRecord
            {
                SkuId = skuId,
                AvailableQuantity = availableQuantity,
                UpdatedAt = DateTime.UtcNow
            });

            return (product.Id, skuId);
        }

        internal static (Member member, string token) RegisterMember(IShopDatabaseContext context)
        {
            string memberName = $"member-{Guid.NewGuid():N}";
            var member = new Member
            {
                Name = memberName
            };

            context.Members.Insert(member);

            string token = Guid.NewGuid().ToString("N");
            context.MemberTokens.Upsert(new MemberAccessTokenRecord
            {
                Token = token,
                MemberId = member.Id,
                Expire = DateTime.MaxValue
            });

            return (member, token);
        }
    }
}
