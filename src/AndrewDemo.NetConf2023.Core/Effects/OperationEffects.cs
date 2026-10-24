using AndrewDemo.NetConf2023.Abstract.Operations;

namespace AndrewDemo.NetConf2023.Core.Effects;

/// <summary>單次操作實際略過的效果；出口建立不可變 OperationResult。</summary>
public sealed class OperationEffects
{
    private readonly List<SkippedEffect> _skipped = new();
    public IReadOnlyList<SkippedEffect> Skipped => _skipped.AsReadOnly();
    private bool _operationActive;
    internal void BeginOperation() => _operationActive = true;
    internal void Clear() { if (!_operationActive) _skipped.Clear(); }
    internal void Record(SkippedEffect effect) => _skipped.Add(effect);
}
