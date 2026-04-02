using System;
using System.Collections.Generic;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Records;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories
{
    public sealed class MemberEducationVerificationRepository
    {
        private readonly IShopDatabaseContext _database;

        public MemberEducationVerificationRepository(IShopDatabaseContext database)
        {
            _database = database;
        }

        public IReadOnlyList<MemberEducationVerificationRecord> GetVerificationHistory(int memberId)
        {
            throw new NotImplementedException();
        }

        public MemberEducationVerificationRecord? GetLatestVerification(int memberId)
        {
            throw new NotImplementedException();
        }

        public void Upsert(MemberEducationVerificationRecord record)
        {
            throw new NotImplementedException();
        }
    }
}
