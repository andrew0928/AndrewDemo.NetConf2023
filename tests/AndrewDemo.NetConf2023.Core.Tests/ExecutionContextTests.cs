using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Core.Contexts;

namespace AndrewDemo.NetConf2023.Core.Tests;

[Trait("Feature", "TiP")]
public sealed class ExecutionContextTests
{
    [Fact]
    public void NewContexts_HaveAnonymousIdentityAndNoTransaction()
    {
        IIdentityContext identity = new IdentityContext();
        ITransactionContext transaction = new TransactionContext();

        Assert.Equal(IdentityKind.Anonymous, identity.Current.Kind);
        Assert.Null(identity.Current.MemberId);
        Assert.False(identity.Current.IsTest);
        Assert.Null(transaction.Current);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitializeIdentity_ExposesMemberPurposeWithoutCreatingTransaction(bool isTest)
    {
        var identity = new IdentityContext();
        ITransactionContext transaction = new TransactionContext();

        identity.Initialize(IdentitySnapshot.ForMember(17, isTest));

        IIdentityContext reader = identity;
        Assert.Equal(IdentityKind.Member, reader.Current.Kind);
        Assert.Equal(17, reader.Current.MemberId);
        Assert.Equal(isTest, reader.Current.IsTest);
        Assert.Null(transaction.Current);
    }

    [Fact]
    public void NewTransaction_HasNullCurrent()
    {
        ITransactionContext transaction = new TransactionContext();

        Assert.Null(transaction.Current);
    }

    [Theory]
    [InlineData(TransactionKind.Checkout, false)]
    [InlineData(TransactionKind.Checkout, true)]
    [InlineData(TransactionKind.Order, false)]
    [InlineData(TransactionKind.Order, true)]
    public void Bind_ExposesTransactionKindIdAndPurpose(TransactionKind kind, bool isTest)
    {
        var transaction = new TransactionContext();

        transaction.Bind(new TransactionSnapshot(kind, 23, isTest));

        ITransactionContext reader = transaction;
        Assert.NotNull(reader.Current);
        Assert.Equal(kind, reader.Current.Kind);
        Assert.Equal(23, reader.Current.Id);
        Assert.Equal(isTest, reader.Current.IsTest);
    }

    [Fact]
    public void ClearTransaction_PreservesIdentityAndPreviouslyReadSnapshot()
    {
        var identity = new IdentityContext();
        identity.Initialize(IdentitySnapshot.ForMember(17, false));
        var transaction = new TransactionContext();
        transaction.Bind(new TransactionSnapshot(TransactionKind.Checkout, 23, true));
        var snapshot = transaction.Current;

        transaction.Clear();

        Assert.Null(transaction.Current);
        Assert.Equal(17, identity.Current.MemberId);
        Assert.False(identity.Current.IsTest);
        Assert.NotNull(snapshot);
        Assert.Equal(23, snapshot.Id);
        Assert.True(snapshot.IsTest);
    }

    [Fact]
    public void EventContext_CanUseSystemIdentityAndPreserveTestOrderPurpose()
    {
        var identity = new IdentityContext();
        identity.Initialize(IdentitySnapshot.System);
        var transaction = new TransactionContext();
        transaction.Bind(new TransactionSnapshot(TransactionKind.Order, 42, true));

        Assert.Equal(IdentityKind.System, identity.Current.Kind);
        Assert.Null(identity.Current.MemberId);
        Assert.False(identity.Current.IsTest);
        Assert.NotNull(transaction.Current);
        Assert.True(transaction.Current.IsTest);
    }

    [Fact]
    public void SeparateInstances_DoNotInheritOrOverwriteEachOthersContext()
    {
        var outerIdentity = new IdentityContext();
        outerIdentity.Initialize(IdentitySnapshot.ForMember(17, true));
        var outerTransaction = new TransactionContext();
        outerTransaction.Bind(new TransactionSnapshot(TransactionKind.Checkout, 23, true));

        var innerIdentity = new IdentityContext();
        var innerTransaction = new TransactionContext();
        Assert.Equal(IdentityKind.Anonymous, innerIdentity.Current.Kind);
        Assert.Null(innerTransaction.Current);

        innerIdentity.Initialize(IdentitySnapshot.System);
        innerTransaction.Bind(new TransactionSnapshot(TransactionKind.Order, 42, false));
        innerTransaction.Clear();

        Assert.Equal(17, outerIdentity.Current.MemberId);
        Assert.True(outerIdentity.Current.IsTest);
        Assert.NotNull(outerTransaction.Current);
        Assert.Equal(23, outerTransaction.Current.Id);
        Assert.True(outerTransaction.Current.IsTest);
    }
}
