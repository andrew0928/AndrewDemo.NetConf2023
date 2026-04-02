using System;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Models;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Services
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
                    VerifiedAt = verification.VerifiedAt,
                    ExpireAt = verification.ExpireAt,
                    Reason = "教育驗證尚未通過"
                };
            }

            if (verification.VerifiedAt > at)
            {
                return new EducationQualificationResult
                {
                    IsQualified = false,
                    Email = verification.Email,
                    VerifiedAt = verification.VerifiedAt,
                    ExpireAt = verification.ExpireAt,
                    Reason = "教育驗證尚未生效"
                };
            }

            if (verification.ExpireAt < at)
            {
                return new EducationQualificationResult
                {
                    IsQualified = false,
                    Email = verification.Email,
                    VerifiedAt = verification.VerifiedAt,
                    ExpireAt = verification.ExpireAt,
                    Reason = "教育資格已過期"
                };
            }

            return new EducationQualificationResult
            {
                IsQualified = true,
                Email = verification.Email,
                VerifiedAt = verification.VerifiedAt,
                ExpireAt = verification.ExpireAt
            };
        }
    }
}
