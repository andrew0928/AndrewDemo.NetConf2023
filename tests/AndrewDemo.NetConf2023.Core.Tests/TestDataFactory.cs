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
            int id = Interlocked.Increment(ref _productSeq);

            Product.Upsert(new Product
            {
                Id = id,
                Name = name ?? $"TestProduct-{id}",
                Description = description,
                Price = price
            });

            return id;
        }

        internal static (Member member, string token) RegisterMember()
        {
            string memberName = $"member-{Guid.NewGuid():N}";
            string? token = Member.Register(memberName);
            if (token == null)
            {
                throw new InvalidOperationException("failed to register test member");
            }

            var member = Member.GetCurrentMember(token);
            if (member == null)
            {
                throw new InvalidOperationException("registered member could not be loaded");
            }

            return (member, token);
        }
    }
}
