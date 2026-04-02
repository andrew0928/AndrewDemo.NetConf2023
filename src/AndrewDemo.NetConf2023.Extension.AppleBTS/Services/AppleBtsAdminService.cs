using AndrewDemo.NetConf2023.Extension.AppleBTS.Records;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Services
{
    public sealed class AppleBtsAdminService
    {
        private readonly BtsOfferRepository _offerRepository;
        private readonly MemberEducationVerificationRepository _verificationRepository;

        public AppleBtsAdminService(
            BtsOfferRepository offerRepository,
            MemberEducationVerificationRepository verificationRepository)
        {
            _offerRepository = offerRepository;
            _verificationRepository = verificationRepository;
        }

        public void UpsertCampaign(BtsCampaignRecord record)
        {
            throw new NotImplementedException();
        }

        public void UpsertMainOffer(BtsMainOfferRecord record)
        {
            throw new NotImplementedException();
        }

        public void UpsertGiftOption(BtsGiftOptionRecord record)
        {
            throw new NotImplementedException();
        }

        public void DeleteGiftOption(string optionId)
        {
            throw new NotImplementedException();
        }

        public void UpsertMemberEducationVerification(MemberEducationVerificationRecord record)
        {
            throw new NotImplementedException();
        }
    }
}
