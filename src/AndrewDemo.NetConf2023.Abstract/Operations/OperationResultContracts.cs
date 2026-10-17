using System;
using System.Collections.Generic;

namespace AndrewDemo.NetConf2023.Abstract.Operations
{
    public enum OperationOutcome
    {
        Succeeded = 0,
        Rejected = 1,
        Failed = 2
    }

    /// <summary>
    /// 實際因測試用途略過的效果。目標與原因以穩定代碼表達，不放 Token 或付款秘密。
    /// </summary>
    public sealed class SkippedEffect
    {
        public string EffectCode { get; }
        public string TargetType { get; }
        public string TargetId { get; }
        public string ReasonCode { get; }

        public SkippedEffect(string effectCode, string targetType, string targetId, string reasonCode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(effectCode);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetType);
            ArgumentException.ThrowIfNullOrWhiteSpace(targetId);
            ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
            EffectCode = effectCode;
            TargetType = targetType;
            TargetId = targetId;
            ReasonCode = reasonCode;
        }
    }

    /// <summary>
    /// 單次操作的結果快照。用途與成功與否分開表達，不合併 Identity／Transaction context。
    /// </summary>
    public sealed class OperationResult
    {
        public bool IsTestBehavior { get; }
        public OperationOutcome Outcome { get; }
        public IReadOnlyList<SkippedEffect> SkippedEffects { get; }

        public OperationResult(bool isTestBehavior, OperationOutcome outcome, IEnumerable<SkippedEffect> skippedEffects)
        {
            ArgumentNullException.ThrowIfNull(skippedEffects);
            var effects = new List<SkippedEffect>();
            foreach (var effect in skippedEffects)
            {
                ArgumentNullException.ThrowIfNull(effect);
                effects.Add(effect);
            }

            IsTestBehavior = isTestBehavior;
            Outcome = outcome;
            SkippedEffects = effects.AsReadOnly();
        }
    }
}
