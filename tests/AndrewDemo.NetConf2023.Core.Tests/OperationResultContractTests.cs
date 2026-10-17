using AndrewDemo.NetConf2023.Abstract.Operations;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class OperationResultContractTests
{
    [Fact]
    public void Result_CopiesSkippedEffectsAndKeepsSnapshotStable()
    {
        var effect = new SkippedEffect("inventory.deduct", "inventory", "SKU-1", "test_behavior");
        var source = new List<SkippedEffect> { effect };
        var result = new OperationResult(true, OperationOutcome.Succeeded, source);

        source.Clear();

        Assert.True(result.IsTestBehavior);
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        var saved = Assert.Single(result.SkippedEffects);
        Assert.Equal("inventory.deduct", saved.EffectCode);
        Assert.Equal("SKU-1", saved.TargetId);
        Assert.Throws<NotSupportedException>(() => ((IList<SkippedEffect>)result.SkippedEffects).Clear());
    }

    [Theory]
    [InlineData(OperationOutcome.Succeeded)]
    [InlineData(OperationOutcome.Rejected)]
    [InlineData(OperationOutcome.Failed)]
    public void TestPurpose_DoesNotImplySuccessOrInventSkippedEffects(OperationOutcome outcome)
    {
        var result = new OperationResult(true, outcome, []);

        Assert.True(result.IsTestBehavior);
        Assert.Equal(outcome, result.Outcome);
        Assert.Empty(result.SkippedEffects);
    }
}
