using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.PetShop.Extension.Reservations;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Discounts
{
    public sealed class PetShopReservationPurchaseThresholdDiscountRule : IDiscountRule
    {
        public const decimal ThresholdAmount = 1000m;
        public const decimal DiscountAmount = 100m;

        private readonly PetShopReservationRepository _repository;

        public PetShopReservationPurchaseThresholdDiscountRule(PetShopReservationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public string RuleId => PetShopConstants.ReservationPurchaseThresholdDiscountRuleId;

        public int Priority => 100;

        public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var reservationLines = context.LineItems
                .Where(line => IsEligibleReservationLine(line, context.EvaluatedAt))
                .ToList();
            if (reservationLines.Count == 0)
            {
                return Array.Empty<DiscountRecord>();
            }

            var productPurchaseLines = context.LineItems
                .Where(IsProductPurchaseLine)
                .ToList();
            var productPurchaseAmount = productPurchaseLines.Sum(line => line.UnitPrice!.Value * line.Quantity);
            if (productPurchaseAmount <= ThresholdAmount)
            {
                return Array.Empty<DiscountRecord>();
            }

            return new[]
            {
                new DiscountRecord
                {
                    RuleId = RuleId,
                    Kind = DiscountRecordKind.Discount,
                    Name = "PetShop 預約購買滿額折扣",
                    Description = "同次結帳含 PetShop 預約，且商品金額大於 1000 折 100",
                    Amount = -DiscountAmount,
                    RelatedLineIds = reservationLines
                        .Concat(productPurchaseLines)
                        .Select(line => line.LineId)
                        .Where(lineId => !string.IsNullOrWhiteSpace(lineId))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                }
            };
        }

        private bool IsEligibleReservationLine(LineItem line, DateTime evaluatedAt)
        {
            if (line.Quantity <= 0 || !line.UnitPrice.HasValue)
            {
                return false;
            }

            var reservation = _repository.FindReservationByProductId(line.ProductId);
            return reservation?.Status == PetShopReservationStatus.Holding
                && NormalizeUtc(reservation.HoldExpiresAt) > NormalizeUtc(evaluatedAt);
        }

        private bool IsProductPurchaseLine(LineItem line)
        {
            if (line.Quantity <= 0 || !line.UnitPrice.HasValue)
            {
                return false;
            }

            return _repository.FindReservationByProductId(line.ProductId) == null;
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }
    }
}
