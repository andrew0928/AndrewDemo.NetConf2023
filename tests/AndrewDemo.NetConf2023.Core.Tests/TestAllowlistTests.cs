using AndrewDemo.NetConf2023.Core.Testing;

namespace AndrewDemo.NetConf2023.Core.Tests;

[Trait("Feature", "TiP")]
public sealed class TestAllowlistTests
{
    [Theory]
    [InlineData(900001, true)]
    [InlineData(990001, false)]
    [InlineData(42, false)]
    public void DefaultList_ClassifiesMemberIds(int memberId, bool expected)
    {
        var allowlist = new TestAllowlist();

        Assert.Equal(expected, allowlist.IsTestMember(memberId));
    }

    [Theory]
    [InlineData(990001, true)]
    [InlineData(900001, false)]
    [InlineData(42, false)]
    public void DefaultList_ClassifiesPaymentIds(int paymentId, bool expected)
    {
        var allowlist = new TestAllowlist();

        Assert.Equal(expected, allowlist.IsTestPayment(paymentId));
    }

    [Fact]
    public void ExplicitLists_ReplaceDefaultsAndKeepIdKindsSeparate()
    {
        var allowlist = new TestAllowlist(memberIds: [17], paymentIds: [23]);

        Assert.True(allowlist.IsTestMember(17));
        Assert.False(allowlist.IsTestPayment(17));
        Assert.True(allowlist.IsTestPayment(23));
        Assert.False(allowlist.IsTestMember(23));
        Assert.False(allowlist.IsTestMember(900001));
        Assert.False(allowlist.IsTestPayment(990001));
    }

    [Fact]
    public void EmptyLists_ClassifyAllIdsAsRegular()
    {
        var allowlist = new TestAllowlist(memberIds: [], paymentIds: []);

        Assert.False(allowlist.IsTestMember(900001));
        Assert.False(allowlist.IsTestPayment(990001));
    }

    [Fact]
    public void ListsAreSnapshots_UnchangedByCallerMutation()
    {
        var memberIds = new HashSet<int> { 17 };
        var paymentIds = new HashSet<int> { 23 };
        var allowlist = new TestAllowlist(memberIds, paymentIds);

        memberIds.Clear();
        memberIds.Add(31);
        paymentIds.Clear();
        paymentIds.Add(37);

        Assert.True(allowlist.IsTestMember(17));
        Assert.False(allowlist.IsTestMember(31));
        Assert.True(allowlist.IsTestPayment(23));
        Assert.False(allowlist.IsTestPayment(37));
    }

    [Fact]
    public void DifferentInstances_DoNotShareCustomLists()
    {
        var first = new TestAllowlist(memberIds: [17], paymentIds: [23]);
        var second = new TestAllowlist(memberIds: [31], paymentIds: [37]);

        Assert.True(first.IsTestMember(17));
        Assert.False(second.IsTestMember(17));
        Assert.True(second.IsTestPayment(37));
        Assert.False(first.IsTestPayment(37));
    }
}
