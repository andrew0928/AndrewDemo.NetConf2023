using System;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class MemberPersistenceTests
    {
        [Fact]
        public void RegisterAndGetCurrentMember_ReturnsPersistedMember()
        {
            var (member, token) = TestDataFactory.RegisterMember();

            var current = Member.GetCurrentMember(token);
            Assert.NotNull(current);
            Assert.Equal(member.Id, current!.Id);
            Assert.Equal(member.Name, current.Name);
        }

        [Fact]
        public void SetShopNotes_PersistsNotesToLiteDb()
        {
            var (_, token) = TestDataFactory.RegisterMember();
            string note = $"note-{Guid.NewGuid():N}";

            var updated = Member.SetShopNotes(token, note);
            Assert.NotNull(updated);
            Assert.Equal(note, updated!.ShopNotes);

            var reloaded = Member.GetCurrentMember(token);
            Assert.NotNull(reloaded);
            Assert.Equal(note, reloaded!.ShopNotes);
        }
    }
}
