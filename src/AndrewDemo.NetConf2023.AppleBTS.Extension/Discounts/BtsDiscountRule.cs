using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Services;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Discounts
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
            ArgumentNullException.ThrowIfNull(context);

            if (context.LineItems.Count == 0)
            {
                return Array.Empty<DiscountRecord>();
            }

            if (_offerRepository.GetActiveCampaign(context.EvaluatedAt) == null)
            {
                return BuildCampaignInactiveHints(context);
            }

            var records = new List<DiscountRecord>();
            Models.EducationQualificationResult? qualification = null;

            foreach (var mainLine in context.LineItems.Where(IsRootLine))
            {
                var offer = _offerRepository.GetOffer(mainLine.ProductId, context.EvaluatedAt);
                if (offer.MainOffer == null)
                {
                    continue;
                }

                qualification ??= _qualificationService.Evaluate(context.ConsumerId ?? 0, context.EvaluatedAt);
                if (!qualification.IsQualified)
                {
                    records.Add(CreateHintRecord(
                        GetMainBundleLineIds(context, mainLine),
                        qualification.Reason ?? "教育資格不符"));
                    continue;
                }

                var mainDiscountAmount = CalculateMainProductDiscountAmount(mainLine, offer.MainOffer.BtsPrice);
                var childLines = GetGiftLines(context, mainLine.LineId);

                if (TryCreateGiftQuantityHint(mainLine, childLines, offer.MainOffer.MaxGiftQuantity, out var giftQuantityHint))
                {
                    if (mainDiscountAmount < 0m)
                    {
                        records.Add(CreateDiscountRecord(
                            new[] { mainLine.LineId },
                            mainDiscountAmount,
                            includeGiftSubsidy: false));
                    }

                    records.Add(giftQuantityHint!);
                    continue;
                }

                var giftSubsidyAmount = CalculateGiftSubsidyAmount(childLines, offer);
                var totalDiscountAmount = mainDiscountAmount - giftSubsidyAmount;
                if (totalDiscountAmount < 0m)
                {
                    var relatedLineIds = new List<string> { mainLine.LineId };
                    if (giftSubsidyAmount > 0m)
                    {
                        relatedLineIds.AddRange(GetEligibleGiftLineIds(childLines, offer));
                    }

                    records.Add(CreateDiscountRecord(
                        relatedLineIds,
                        totalDiscountAmount,
                        includeGiftSubsidy: giftSubsidyAmount > 0m));
                }
            }

            return records;
        }

        private IReadOnlyList<DiscountRecord> BuildCampaignInactiveHints(CartContext context)
        {
            var records = new List<DiscountRecord>();

            foreach (var mainLine in context.LineItems.Where(IsRootLine))
            {
                if (!_offerRepository.HasConfiguredMainOffer(mainLine.ProductId))
                {
                    continue;
                }

                records.Add(CreateHintRecord(
                    GetMainBundleLineIds(context, mainLine),
                    "BTS 活動目前未啟用或已過期"));
            }

            return records;
        }

        private static bool IsRootLine(LineItem line)
        {
            return !string.IsNullOrWhiteSpace(line.LineId)
                && string.IsNullOrWhiteSpace(line.ParentLineId)
                && line.Quantity > 0
                && line.UnitPrice.HasValue;
        }

        private static List<LineItem> GetGiftLines(CartContext context, string mainLineId)
        {
            return context.LineItems
                .Where(x => string.Equals(x.ParentLineId, mainLineId, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.Quantity > 0 && x.UnitPrice.HasValue)
                .ToList();
        }

        private static List<string> GetMainBundleLineIds(CartContext context, LineItem mainLine)
        {
            return context.LineItems
                .Where(x => string.Equals(x.LineId, mainLine.LineId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.ParentLineId, mainLine.LineId, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.LineId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static decimal CalculateMainProductDiscountAmount(LineItem mainLine, decimal btsPrice)
        {
            var unitPrice = mainLine.UnitPrice ?? 0m;
            var unitDelta = btsPrice - unitPrice;
            return unitDelta < 0m ? unitDelta * mainLine.Quantity : 0m;
        }

        private static bool TryCreateGiftQuantityHint(
            LineItem mainLine,
            IReadOnlyList<LineItem> childLines,
            int maxGiftQuantity,
            out DiscountRecord? hint)
        {
            hint = null;

            if (maxGiftQuantity <= 0 || childLines.Count == 0)
            {
                return false;
            }

            var selectedGiftQuantity = childLines.Sum(x => x.Quantity);
            if (selectedGiftQuantity <= maxGiftQuantity)
            {
                return false;
            }

            hint = new DiscountRecord
            {
                RuleId = AppleBtsConstants.DiscountRuleId,
                Kind = DiscountRecordKind.Hint,
                Name = AppleBtsConstants.DiscountName,
                Description = $"每個主商品最多只能選 {maxGiftQuantity} 個贈品",
                Amount = 0m,
                RelatedLineIds = childLines
                    .Select(x => x.LineId)
                    .Prepend(mainLine.LineId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };

            return true;
        }

        private static decimal CalculateGiftSubsidyAmount(
            IReadOnlyList<LineItem> childLines,
            Models.BtsOfferAggregate offer)
        {
            if (offer.MainOffer?.MaxGiftSubsidyAmount is not decimal maxGiftSubsidyAmount
                || maxGiftSubsidyAmount <= 0m
                || string.IsNullOrWhiteSpace(offer.MainOffer.GiftGroupId)
                || childLines.Count == 0)
            {
                return 0m;
            }

            var eligibleGiftProductIds = new HashSet<string>(
                offer.GiftOptions.Select(x => x.GiftProductId),
                StringComparer.OrdinalIgnoreCase);

            var eligibleGiftLineAmount = childLines
                .Where(x => eligibleGiftProductIds.Contains(x.ProductId))
                .Sum(x => (x.UnitPrice ?? 0m) * x.Quantity);

            if (eligibleGiftLineAmount <= 0m)
            {
                return 0m;
            }

            return Math.Min(maxGiftSubsidyAmount, eligibleGiftLineAmount);
        }

        private static IEnumerable<string> GetEligibleGiftLineIds(
            IReadOnlyList<LineItem> childLines,
            Models.BtsOfferAggregate offer)
        {
            if (string.IsNullOrWhiteSpace(offer.MainOffer?.GiftGroupId))
            {
                return Array.Empty<string>();
            }

            var eligibleGiftProductIds = new HashSet<string>(
                offer.GiftOptions.Select(x => x.GiftProductId),
                StringComparer.OrdinalIgnoreCase);

            return childLines
                .Where(x => eligibleGiftProductIds.Contains(x.ProductId))
                .Select(x => x.LineId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DiscountRecord CreateDiscountRecord(
            IEnumerable<string> relatedLineIds,
            decimal amount,
            bool includeGiftSubsidy)
        {
            return new DiscountRecord
            {
                RuleId = AppleBtsConstants.DiscountRuleId,
                Kind = DiscountRecordKind.Discount,
                Name = AppleBtsConstants.DiscountName,
                Description = includeGiftSubsidy
                    ? "主商品套用 BTS 價格，並補貼贈品售價"
                    : "主商品套用 BTS 價格",
                Amount = amount,
                RelatedLineIds = relatedLineIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }

        private static DiscountRecord CreateHintRecord(
            IEnumerable<string> relatedLineIds,
            string description)
        {
            return new DiscountRecord
            {
                RuleId = AppleBtsConstants.DiscountRuleId,
                Kind = DiscountRecordKind.Hint,
                Name = AppleBtsConstants.DiscountName,
                Description = description,
                Amount = 0m,
                RelatedLineIds = relatedLineIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };
        }
    }
}
