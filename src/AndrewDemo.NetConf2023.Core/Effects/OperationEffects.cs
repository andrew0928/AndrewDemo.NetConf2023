using AndrewDemo.NetConf2023.Abstract.Operations;

namespace AndrewDemo.NetConf2023.Core.Effects;

/// <summary>單次操作實際略過的效果；Phase 2.6 由各出口轉為 OperationResult。</summary>
public sealed class OperationEffects
{
    private readonly List<SkippedEffect> _skipped = new();
    public IReadOnlyList<SkippedEffect> Skipped => _skipped.AsReadOnly();
    internal void Clear() => _skipped.Clear();
    internal void Record(SkippedEffect effect) => _skipped.Add(effect);
}
