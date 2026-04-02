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
            throw new NotImplementedException();
        }
    }
}
