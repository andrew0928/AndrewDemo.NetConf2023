using System;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Services
{
    public sealed class MemberEducationQualificationService
    {
        private readonly MemberEducationVerificationRepository _verificationRepository;

        public MemberEducationQualificationService(MemberEducationVerificationRepository verificationRepository)
        {
            _verificationRepository = verificationRepository;
        }

        public EducationQualificationResult Evaluate(int memberId, DateTime at)
        {
            var evaluationAt = NormalizeUtc(at);

            if (memberId <= 0)
            {
                return new EducationQualificationResult
                {
                    IsQualified = false,
                    Reason = "找不到有效的教育驗證資料"
                };
            }

            var verification = _verificationRepository.GetLatestVerification(memberId);
            if (verification == null)
            {
                return new EducationQualificationResult
                {
                    IsQualified = false,
                    Reason = "找不到有效的教育驗證資料"
                };
            }

            if (verification.Status != EducationVerificationStatus.Verified)
            {
                return new EducationQualificationResult
                {
                    IsQualified = false,
                    Email = verification.Email,
                    VerifiedAt = NormalizeUtc(verification.VerifiedAt),
                    ExpireAt = NormalizeUtc(verification.ExpireAt),
                    Reason = "教育驗證尚未通過"
                };
            }

            var verifiedAt = NormalizeUtc(verification.VerifiedAt);
            var expireAt = NormalizeUtc(verification.ExpireAt);

            if (verifiedAt > evaluationAt)
            {
                return new EducationQualificationResult
                {
                    IsQualified = false,
                    Email = verification.Email,
                    VerifiedAt = verifiedAt,
                    ExpireAt = expireAt,
                    Reason = "教育驗證尚未生效"
                };
            }

            if (expireAt < evaluationAt)
            {
                return new EducationQualificationResult
                {
                    IsQualified = false,
                    Email = verification.Email,
                    VerifiedAt = verifiedAt,
                    ExpireAt = expireAt,
                    Reason = "教育資格已過期"
                };
            }

            return new EducationQualificationResult
            {
                IsQualified = true,
                Email = verification.Email,
                VerifiedAt = verifiedAt,
                ExpireAt = expireAt
            };
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : value.ToUniversalTime();
        }
    }
}
