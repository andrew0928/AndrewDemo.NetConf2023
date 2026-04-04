using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;
using LiteDB;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories
{
    public sealed class MemberEducationVerificationRepository
    {
        private readonly IShopDatabaseContext _database;

        public MemberEducationVerificationRepository(IShopDatabaseContext database)
        {
            _database = database;
        }

        private ILiteCollection<MemberEducationVerificationRecord> Collection =>
            _database.Database.GetCollection<MemberEducationVerificationRecord>(
                AppleBtsConstants.MemberEducationVerificationsCollectionName);

        public IReadOnlyList<MemberEducationVerificationRecord> GetVerificationHistory(int memberId)
        {
            return Collection
                .Query()
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.VerifiedAt)
                .ToList();
        }

        public MemberEducationVerificationRecord? GetLatestVerification(int memberId)
        {
            return Collection
                .Query()
                .Where(x => x.MemberId == memberId)
                .OrderByDescending(x => x.VerifiedAt)
                .FirstOrDefault();
        }

        public void Upsert(MemberEducationVerificationRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            Collection.Upsert(record);
        }
    }
}
