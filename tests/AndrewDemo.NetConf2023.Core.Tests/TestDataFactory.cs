using System;
using System.Threading;
using AndrewDemo.NetConf2023.Core;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    internal static class TestDataFactory
    {
        private static int _productSeq = 1000;

        internal static int CreateProduct(decimal price, string? name = null, string? description = null)
        {
            int seq = Interlocked.Increment(ref _productSeq);

            var product = new Product
            {
                Id = seq,
                Name = name ?? $"TestProduct-{seq}",
                Description = description,
                Price = price
            };

            ShopDatabase.Create(product);
            return seq;
        }

        internal static (Member member, string token) RegisterMember()
        {
            string memberName = $"member-{Guid.NewGuid():N}";
            var member = new Member
            {
                Name = memberName
            };

            ShopDatabase.Create(member);

            string token = Guid.NewGuid().ToString("N");
            ShopDatabase.Current.MemberTokens.Upsert(new MemberAccessTokenRecord
            {
                Token = token,
                MemberId = member.Id,
                Expire = DateTime.MaxValue
            });

            return (member, token);
        }
    }
}
