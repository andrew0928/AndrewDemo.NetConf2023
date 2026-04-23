using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.PetShop.Extension.Products;
using AndrewDemo.NetConf2023.PetShop.Extension.Reservations;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Tests
{
    public sealed class PetShopReservationLifecycleTests
    {
        private static readonly DateTime HoldRequestedAt = new(2026, 5, 1, 1, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ReservationStartAt = new(2026, 5, 1, 2, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ReservationEndAt = new(2026, 5, 1, 3, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void TC_PET_RSV_001_CreateHold_CreatesReservationAndHiddenProduct()
        {
            using var fixture = CreateFixture();

            var result = fixture.CreateHold();

            Assert.True(result.Succeeded);
            Assert.NotEmpty(result.ReservationId);
            Assert.NotEmpty(result.ProductId);

            var reservation = fixture.Repository.FindReservation(result.ReservationId);
            var product = fixture.Repository.FindProduct(result.ProductId);

            Assert.NotNull(reservation);
            Assert.Equal(PetShopReservationStatus.Holding, reservation!.Status);
            Assert.Equal(HoldRequestedAt.AddMinutes(30), reservation.HoldExpiresAt);
            Assert.Equal(result.ProductId, reservation.ProductId);

            Assert.NotNull(product);
            Assert.False(product!.IsPublished);
            Assert.Equal("基礎美容", product.Name);
            Assert.Equal(800m, product.Price);
            Assert.NotNull(fixture.LookupProduct(result.ProductId, HoldRequestedAt.AddMinutes(10)));
        }

        [Fact]
        public void TC_PET_RSV_002_CreateHold_WhenActiveOccupantExists_ReturnsSlotUnavailable()
        {
            using var fixture = CreateFixture();
            var existing = fixture.CreateHold();

            var rejected = fixture.CreateHold(buyerMemberId: 202, requestedAt: HoldRequestedAt.AddMinutes(10));

            Assert.True(existing.Succeeded);
            Assert.False(rejected.Succeeded);
            Assert.Equal("slot-unavailable", rejected.ErrorCode);
            Assert.Empty(rejected.ReservationId);
            Assert.Empty(rejected.ProductId);

            var reservation = fixture.Repository.FindReservation(existing.ReservationId);
            var product = fixture.Repository.FindProduct(existing.ProductId);

            Assert.Equal(PetShopReservationStatus.Holding, reservation!.Status);
            Assert.NotNull(product);
            Assert.False(product!.IsPublished);
            Assert.NotNull(fixture.LookupProduct(existing.ProductId, HoldRequestedAt.AddMinutes(10)));
        }

        [Fact]
        public void TC_PET_RSV_003_CancelHold_BeforeCheckout_HidesProductAndReleasesSlot()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold();

            var cancelled = fixture.Service.CancelHold(hold.ReservationId, HoldRequestedAt.AddMinutes(15));

            Assert.True(cancelled);

            var reservation = fixture.Repository.FindReservation(hold.ReservationId);
            var product = fixture.Repository.FindProduct(hold.ProductId);

            Assert.Equal(PetShopReservationStatus.Cancelled, reservation!.Status);
            Assert.NotNull(product);
            Assert.False(product!.IsPublished);
            Assert.Null(fixture.LookupProduct(hold.ProductId, HoldRequestedAt.AddMinutes(15)));

            var nextHold = fixture.CreateHold(buyerMemberId: 202, requestedAt: HoldRequestedAt.AddMinutes(16));
            Assert.True(nextHold.Succeeded);
        }

        [Fact]
        public void TC_PET_RSV_004_GetProductById_AfterHoldExpires_LazyExpiresReservationAndHidesProduct()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold();

            var product = fixture.LookupProduct(hold.ProductId, HoldRequestedAt.AddMinutes(31));

            Assert.Null(product);

            var reservation = fixture.Repository.FindReservation(hold.ReservationId);
            var storedProduct = fixture.Repository.FindProduct(hold.ProductId);

            Assert.Equal(PetShopReservationStatus.Expired, reservation!.Status);
            Assert.NotNull(storedProduct);
            Assert.False(storedProduct!.IsPublished);
        }

        [Fact]
        public void TC_PET_RSV_005_OrderCompletedEvent_WithinHold_ConfirmsReservationAndHidesProduct()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold();
            var product = fixture.LookupProduct(hold.ProductId, HoldRequestedAt.AddMinutes(12));

            Assert.NotNull(product);

            var dispatcher = new PetShopOrderEventDispatcher(fixture.Service);
            using var consoleOutput = new StringWriter();
            var originalOutput = Console.Out;

            try
            {
                Console.SetOut(consoleOutput);
                dispatcher.Dispatch(new OrderCompletedEvent
                {
                    OrderId = 9001,
                    ShopId = "petshop",
                    BuyerId = 101,
                    BuyerName = "buyer-101",
                    CompletedAt = HoldRequestedAt.AddMinutes(12),
                    Lines = new[]
                    {
                        new OrderProductLine
                        {
                            ProductId = hold.ProductId,
                            ProductName = product!.Name,
                            UnitPrice = product.Price,
                            Quantity = 1,
                            LineAmount = product.Price
                        }
                    }
                });
            }
            finally
            {
                Console.SetOut(originalOutput);
            }

            var reservation = fixture.Repository.FindReservation(hold.ReservationId);

            Assert.Equal(PetShopReservationStatus.Confirmed, reservation!.Status);
            Assert.Equal(9001, reservation.ConfirmedOrderId);
            Assert.Null(fixture.LookupProduct(hold.ProductId, HoldRequestedAt.AddMinutes(13)));
            Assert.Contains("notify staff and customer", consoleOutput.ToString(), StringComparison.Ordinal);
            Assert.Contains("staffId=staff-amy", consoleOutput.ToString(), StringComparison.Ordinal);
            Assert.Contains("buyerMemberId=101", consoleOutput.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void TC_PET_RSV_006_HistoricalExpiredOrCancelled_DoesNotBlockNewHold()
        {
            using var expiredFixture = CreateFixture();
            var expiredHold = expiredFixture.CreateHold();
            expiredFixture.LookupProduct(expiredHold.ProductId, HoldRequestedAt.AddMinutes(31));

            var afterExpired = expiredFixture.CreateHold(buyerMemberId: 202, requestedAt: HoldRequestedAt.AddMinutes(31));

            Assert.True(afterExpired.Succeeded);
            Assert.Equal(PetShopReservationStatus.Expired, expiredFixture.Repository.FindReservation(expiredHold.ReservationId)!.Status);
            Assert.Equal(PetShopReservationStatus.Holding, expiredFixture.Repository.FindReservation(afterExpired.ReservationId)!.Status);

            using var cancelledFixture = CreateFixture();
            var cancelledHold = cancelledFixture.CreateHold();
            cancelledFixture.Service.CancelHold(cancelledHold.ReservationId, HoldRequestedAt.AddMinutes(15));

            var afterCancelled = cancelledFixture.CreateHold(buyerMemberId: 202, requestedAt: HoldRequestedAt.AddMinutes(16));

            Assert.True(afterCancelled.Succeeded);
            Assert.Equal(PetShopReservationStatus.Cancelled, cancelledFixture.Repository.FindReservation(cancelledHold.ReservationId)!.Status);
            Assert.Equal(PetShopReservationStatus.Holding, cancelledFixture.Repository.FindReservation(afterCancelled.ReservationId)!.Status);
        }

        [Fact]
        public void TC_PET_RSV_007_DuplicateOrderCompletedEvent_IsIdempotent()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold();
            var dispatcher = new PetShopOrderEventDispatcher(fixture.Service);
            var orderEvent = new OrderCompletedEvent
            {
                OrderId = 9001,
                ShopId = "petshop",
                BuyerId = 101,
                BuyerName = "buyer-101",
                CompletedAt = HoldRequestedAt.AddMinutes(12),
                Lines = new[]
                {
                    new OrderProductLine
                    {
                        ProductId = hold.ProductId,
                        ProductName = "基礎美容",
                        UnitPrice = 800m,
                        Quantity = 1,
                        LineAmount = 800m
                    }
                }
            };

            using var consoleOutput = new StringWriter();
            var originalOutput = Console.Out;

            try
            {
                Console.SetOut(consoleOutput);
                dispatcher.Dispatch(orderEvent);
                dispatcher.Dispatch(orderEvent);
            }
            finally
            {
                Console.SetOut(originalOutput);
            }

            var reservation = fixture.Repository.FindReservation(hold.ReservationId);

            Assert.Equal(PetShopReservationStatus.Confirmed, reservation!.Status);
            Assert.Equal(9001, reservation.ConfirmedOrderId);
            Assert.Null(fixture.LookupProduct(hold.ProductId, HoldRequestedAt.AddMinutes(13)));
            Assert.Single(consoleOutput
                .ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        }

        private static PetShopFixture CreateFixture()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"andrew-demo-petshop-test-{Guid.NewGuid():N}.db");
            var database = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = $"Filename={databasePath};Connection=Direct"
            });
            var repository = new PetShopReservationRepository(database);
            var service = new PetShopReservationService(repository);
            var timeProvider = new MutableTimeProvider(HoldRequestedAt);
            var productService = new PetShopProductService(new DefaultProductService(database), service, timeProvider);
            return new PetShopFixture(databasePath, database, repository, service, productService, timeProvider);
        }

        private sealed class PetShopFixture : IDisposable
        {
            private readonly string _databasePath;
            private readonly ShopDatabaseContext _database;
            private readonly MutableTimeProvider _timeProvider;
            private readonly PetShopProductService _productService;

            public PetShopFixture(
                string databasePath,
                ShopDatabaseContext database,
                PetShopReservationRepository repository,
                PetShopReservationService service,
                PetShopProductService productService,
                MutableTimeProvider timeProvider)
            {
                _databasePath = databasePath;
                _database = database;
                Repository = repository;
                Service = service;
                _productService = productService;
                _timeProvider = timeProvider;
            }

            public PetShopReservationRepository Repository { get; }
            public PetShopReservationService Service { get; }

            public PetShopReservationHoldResult CreateHold(int buyerMemberId = 101, DateTime? requestedAt = null)
            {
                return Service.CreateHold(new CreatePetShopReservationHoldRequest
                {
                    BuyerMemberId = buyerMemberId,
                    ServiceId = "grooming-basic",
                    ServiceName = "基礎美容",
                    ServiceDescription = "基礎美容預約服務",
                    Price = 800m,
                    StartAt = ReservationStartAt,
                    EndAt = ReservationEndAt,
                    VenueId = "room-a",
                    StaffId = "staff-amy",
                    RequestedAt = requestedAt ?? HoldRequestedAt,
                    HoldDuration = TimeSpan.FromMinutes(30)
                });
            }

            public Product? LookupProduct(string productId, DateTime evaluatedAt)
            {
                _timeProvider.UtcNow = evaluatedAt;
                return _productService.GetProductById(productId);
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
                    // 測試清理失敗不應影響 lifecycle 行為驗證。
                }
            }
        }

        private sealed class MutableTimeProvider : TimeProvider
        {
            public MutableTimeProvider(DateTime utcNow)
            {
                UtcNow = utcNow;
            }

            public DateTime UtcNow { get; set; }

            public override DateTimeOffset GetUtcNow()
            {
                return new DateTimeOffset(UtcNow, TimeSpan.Zero);
            }
        }
    }
}
