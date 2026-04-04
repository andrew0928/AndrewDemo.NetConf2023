using System;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using LiteDB;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Records
{
    public sealed class MemberEducationVerificationRecord
    {
        [BsonId]
        public string VerificationId { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public string Email { get; set; } = string.Empty;
        public EducationVerificationStatus Status { get; set; }
        public DateTime VerifiedAt { get; set; }
        public DateTime ExpireAt { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
