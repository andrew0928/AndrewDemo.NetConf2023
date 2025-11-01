using System;
using System.Threading;
using AndrewDemo.NetConf2023.Core;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    internal static class TestDataFactory
    {
        private static int _productSeq = 1000;

        internal static int CreateProduct(IShopDatabaseContext context, decimal price, string? name = null, string? description = null)
        {
            int seq = Interlocked.Increment(ref _productSeq);

            var product = new Product
            {
                Id = seq,
                Name = name ?? $"TestProduct-{seq}",
                Description = description,
                Price = price
            };

            context.Products.Insert(product);
            return seq;
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
