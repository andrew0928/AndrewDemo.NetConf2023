using System;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Models
{
    public sealed class EducationQualificationResult
    {
        public bool IsQualified { get; init; }
        public string? Email { get; init; }
        public DateTime? VerifiedAt { get; init; }
        public DateTime? ExpireAt { get; init; }
        public string? Reason { get; init; }
    }
}
