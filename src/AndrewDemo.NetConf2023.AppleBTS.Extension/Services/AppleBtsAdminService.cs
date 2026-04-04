using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Services
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
            _offerRepository.UpsertCampaign(record);
        }

        public void UpsertMainOffer(BtsMainOfferRecord record)
        {
            _offerRepository.UpsertMainOffer(record);
        }

        public void UpsertGiftOption(BtsGiftOptionRecord record)
        {
            _offerRepository.UpsertGiftOption(record);
        }

        public void DeleteGiftOption(string optionId)
        {
            _offerRepository.DeleteGiftOption(optionId);
        }

        public void UpsertMemberEducationVerification(MemberEducationVerificationRecord record)
        {
            _verificationRepository.Upsert(record);
        }
    }
}
