using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.PetShop.Extension.Discounts;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Tests
{
    public sealed class PetShopReservationPurchaseThresholdDiscountRuleTests
    {
        private static readonly DateTime HoldRequestedAt = new(2026, 5, 1, 1, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ReservationStartAt = new(2026, 5, 1, 2, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ReservationEndAt = new(2026, 5, 1, 3, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void D1_WithoutReservation_DoesNotDiscountProductPurchaseAboveThreshold()
        {
            using var fixture = CreateFixture();
            var rule = new PetShopReservationPurchaseThresholdDiscountRule(fixture.Repository);
            var context = CreateCartContext(ProductLine("line-product-1500", 1500m));

            var records = rule.Evaluate(context);

            Assert.Empty(records);
            Assert.Equal(1500m, CalculateCheckoutTotal(context, records));
        }

        [Fact]
        public void D2_WithOneReservationAndProductPurchaseAboveThreshold_ReturnsSingleDiscount()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold(price: 2000m);
            var rule = new PetShopReservationPurchaseThresholdDiscountRule(fixture.Repository);
            var context = CreateCartContext(
                ReservationLine(hold, "line-reservation-1", 2000m),
                ProductLine("line-product-1500", 1500m));

            var records = rule.Evaluate(context);

            var record = AssertSingleDiscount(records);
            Assert.Equal(new[] { "line-reservation-1", "line-product-1500" }, record.RelatedLineIds);
            Assert.Equal(3400m, CalculateCheckoutTotal(context, records));
        }

        [Fact]
        public void D3_WithMultipleReservationsAndProductPurchaseAboveThreshold_ReturnsSingleDiscount()
        {
            using var fixture = CreateFixture();
            var firstHold = fixture.CreateHold(price: 2000m, staffId: "staff-amy");
            var secondHold = fixture.CreateHold(
                price: 2000m,
                staffId: "staff-ben",
                startAt: ReservationStartAt.AddHours(2),
                endAt: ReservationEndAt.AddHours(2));
            var rule = new PetShopReservationPurchaseThresholdDiscountRule(fixture.Repository);
            var context = CreateCartContext(
                ReservationLine(firstHold, "line-reservation-1", 2000m),
                ReservationLine(secondHold, "line-reservation-2", 2000m),
                ProductLine("line-product-3000", 3000m));

            var records = rule.Evaluate(context);

            var record = AssertSingleDiscount(records);
            Assert.Equal(new[] { "line-reservation-1", "line-reservation-2", "line-product-3000" }, record.RelatedLineIds);
            Assert.Equal(6900m, CalculateCheckoutTotal(context, records));
        }

        [Fact]
        public void D4_WithReservationAndProductPurchaseAtThreshold_DoesNotDiscount()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold(price: 2000m);
            var rule = new PetShopReservationPurchaseThresholdDiscountRule(fixture.Repository);
            var context = CreateCartContext(
                ReservationLine(hold, "line-reservation-1", 2000m),
                ProductLine("line-product-1000", 1000m));

            var records = rule.Evaluate(context);

            Assert.Empty(records);
            Assert.Equal(3000m, CalculateCheckoutTotal(context, records));
        }

        [Fact]
        public void D5_WithReservationOnly_DoesNotDiscount()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold(price: 2000m);
            var rule = new PetShopReservationPurchaseThresholdDiscountRule(fixture.Repository);
            var context = CreateCartContext(ReservationLine(hold, "line-reservation-1", 2000m));

            var records = rule.Evaluate(context);

            Assert.Empty(records);
            Assert.Equal(2000m, CalculateCheckoutTotal(context, records));
        }

        [Fact]
        public void D6_WithExpiredReservationAndProductPurchaseAboveThreshold_DoesNotDiscount()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold(price: 2000m);
            var rule = new PetShopReservationPurchaseThresholdDiscountRule(fixture.Repository);
            var context = CreateCartContext(
                HoldRequestedAt.AddMinutes(31),
                ReservationLine(hold, "line-expired-reservation", 2000m),
                ProductLine("line-product-1500", 1500m));

            var records = rule.Evaluate(context);

            Assert.Empty(records);
            Assert.Equal(3500m, CalculateCheckoutTotal(context, records));
        }

        private static DiscountRecord AssertSingleDiscount(IReadOnlyList<DiscountRecord> records)
        {
            var record = Assert.Single(records);
            Assert.Equal(PetShopConstants.ReservationPurchaseThresholdDiscountRuleId, record.RuleId);
            Assert.Equal(DiscountRecordKind.Discount, record.Kind);
            Assert.Equal("PetShop 預約購買滿額折扣", record.Name);
            Assert.Equal("同次結帳含 PetShop 預約，且商品金額大於 1000 折 100", record.Description);
            Assert.Equal(-100m, record.Amount);
            return record;
        }

        private static LineItem ReservationLine(PetShopReservationHoldResult hold, string lineId, decimal price)
        {
            return new LineItem
            {
                LineId = lineId,
                ProductId = hold.ProductId,
                ProductName = "基礎美容",
                UnitPrice = price,
                Quantity = 1
            };
        }

        private static LineItem ProductLine(string lineId, decimal price)
        {
            return new LineItem
            {
                LineId = lineId,
                ProductId = $"product-{lineId}",
                ProductName = "一般商品",
                UnitPrice = price,
                Quantity = 1
            };
        }

        private static CartContext CreateCartContext(params LineItem[] lines)
        {
            return CreateCartContext(HoldRequestedAt, lines);
        }

        private static CartContext CreateCartContext(DateTime evaluatedAt, params LineItem[] lines)
        {
            return new CartContext
            {
                ShopId = "petshop",
                ConsumerId = 101,
                ConsumerName = "buyer-101",
                EvaluatedAt = evaluatedAt,
                LineItems = lines
            };
        }

        private static decimal CalculateCheckoutTotal(CartContext context, IReadOnlyList<DiscountRecord> records)
        {
            return context.LineItems.Sum(line => line.UnitPrice!.Value * line.Quantity)
                + records.Where(record => record.Kind == DiscountRecordKind.Discount).Sum(record => record.Amount);
        }

        private static PetShopFixture CreateFixture()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"andrew-demo-petshop-discount-test-{Guid.NewGuid():N}.db");
            var database = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = $"Filename={databasePath};Connection=Direct"
            });
            var repository = new PetShopReservationRepository(database);
            var service = new PetShopReservationService(repository);
            return new PetShopFixture(databasePath, database, repository, service);
        }

        private sealed class PetShopFixture : IDisposable
        {
            private readonly string _databasePath;
            private readonly ShopDatabaseContext _database;
            private readonly PetShopReservationService _service;

            public PetShopFixture(
                string databasePath,
                ShopDatabaseContext database,
                PetShopReservationRepository repository,
                PetShopReservationService service)
            {
                _databasePath = databasePath;
                _database = database;
                Repository = repository;
                _service = service;
            }

            public PetShopReservationRepository Repository { get; }

            public PetShopReservationHoldResult CreateHold(
                decimal price,
                string staffId = "staff-amy",
                string venueId = "room-a",
                DateTime? startAt = null,
                DateTime? endAt = null)
            {
                var result = _service.CreateHold(new CreatePetShopReservationHoldRequest
                {
                    BuyerMemberId = 101,
                    ServiceId = "grooming-basic",
                    ServiceName = "基礎美容",
                    ServiceDescription = "基礎美容預約服務",
                    Price = price,
                    StartAt = startAt ?? ReservationStartAt,
                    EndAt = endAt ?? ReservationEndAt,
                    VenueId = venueId,
                    StaffId = staffId,
                    RequestedAt = HoldRequestedAt,
                    HoldDuration = TimeSpan.FromMinutes(30)
                });

                Assert.True(result.Succeeded, result.ErrorCode);
                return result;
            }

            public void Dispose()
            {
                _database.Dispose();

                try
                {
                    if (File.Exists(_databasePath))
                    {
                        File.Delete(_databasePath);
                    }
                }
                catch
                {
                    // 測試清理失敗不應影響 discount rule 行為驗證。
                }
            }
        }
    }
}
