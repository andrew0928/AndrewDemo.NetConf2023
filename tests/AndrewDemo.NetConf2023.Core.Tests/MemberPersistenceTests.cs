using System;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class MemberPersistenceTests : ShopDatabaseTestBase
    {
        [Fact]
        public void RegisterAndGetCurrentMember_ReturnsPersistedMember()
        {
            var (member, token) = TestDataFactory.RegisterMember();

            var tokenRecord = ShopDatabase.Current.MemberTokens.FindById(token);
            Assert.NotNull(tokenRecord);

            var current = ShopDatabase.Current.Members.FindById(tokenRecord!.MemberId);
            Assert.NotNull(current);
            Assert.Equal(member.Id, current!.Id);
            Assert.Equal(member.Name, current.Name);
        }

        [Fact]
        public void SetShopNotes_PersistsNotesToLiteDb()
        {
            var (_, token) = TestDataFactory.RegisterMember();
            string note = $"note-{Guid.NewGuid():N}";

            var tokenRecord = ShopDatabase.Current.MemberTokens.FindById(token);
            Assert.NotNull(tokenRecord);

            var member = ShopDatabase.Current.Members.FindById(tokenRecord!.MemberId);
            Assert.NotNull(member);

            member!.ShopNotes = note;
            ShopDatabase.Current.Members.Upsert(member);

            var reloaded = ShopDatabase.Current.Members.FindById(member.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(note, reloaded!.ShopNotes);
        }
    }
}
