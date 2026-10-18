using AndrewDemo.NetConf2023.Abstract.Operations;

namespace AndrewDemo.NetConf2023.Core.Checkouts;

/// <summary>單次操作實際略過的效果；Phase 2.6 由出口轉為 OperationResult。</summary>
public sealed class CheckoutEffects
{
    private readonly List<SkippedEffect> _skipped = new();
    public IReadOnlyList<SkippedEffect> Skipped => _skipped.AsReadOnly();
    internal void Clear() => _skipped.Clear();
    internal void RecordInventoryDeduction(string skuId) => _skipped.Add(
        new SkippedEffect("inventory.deduct", "inventory", skuId, "test_behavior"));
}
