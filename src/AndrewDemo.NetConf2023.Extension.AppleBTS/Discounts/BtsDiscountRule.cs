using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Services;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Discounts
{
    public sealed class BtsDiscountRule : IDiscountRule
    {
        private readonly BtsOfferRepository _offerRepository;
        private readonly MemberEducationQualificationService _qualificationService;

        public BtsDiscountRule(
            BtsOfferRepository offerRepository,
            MemberEducationQualificationService qualificationService)
        {
            _offerRepository = offerRepository;
            _qualificationService = qualificationService;
        }

        public string RuleId => AppleBtsConstants.DiscountRuleId;

        public int Priority => 100;

        public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
        {
            throw new NotImplementedException();
        }
    }
}
