using AndrewDemo.NetConf2023.PetShop.API.Controllers;
using AndrewDemo.NetConf2023.PetShop.API.Models;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.API.Tests
{
    public sealed class PetShopApiControllerTests
    {
        private static readonly DateTime Now = new(2026, 5, 1, 1, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime SlotStartAt = new(2026, 5, 1, 2, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AlternativeSlotStartAt = new(2026, 5, 1, 2, 30, 0, DateTimeKind.Utc);

        [Fact]
        public void GetServices_ReturnsConfiguredCatalog()
        {
            using var fixture = CreateFixture();
            var controller = fixture.CreateServicesController();

            var result = controller.Get();

            var services = Assert.IsAssignableFrom<IReadOnlyList<ServicesController.ServiceResponse>>(result.Value);
            Assert.Equal(2, services.Count);
            Assert.Contains(services, service => service.ServiceId == "grooming-basic" && service.Price == 2000m);
        }

        [Fact]
        public void GetAvailability_WhenSlotOccupied_OmitsUnavailableSlot()
        {
            using var fixture = CreateFixture();
            fixture.CreateHold(buyerMemberId: 101, requestedAt: Now, startAt: SlotStartAt, venueId: "room-a", staffId: "staff-amy");
            var controller = fixture.CreateAvailabilityController();

            var result = controller.Get("grooming-basic", DateOnly.FromDateTime(SlotStartAt), null, null);

            var slots = Assert.IsAssignableFrom<IReadOnlyList<AvailabilityController.AvailabilitySlotResponse>>(result.Value);
            Assert.DoesNotContain(slots, slot => slot.StartAt == SlotStartAt && slot.VenueId == "room-a" && slot.StaffId == "staff-amy");
            Assert.Contains(slots, slot => slot.StartAt == AlternativeSlotStartAt && slot.VenueId == "room-b" && slot.StaffId == "staff-ben");
        }

        [Fact]
        public void CreateHold_WithoutAccessToken_ReturnsUnauthorized()
        {
            using var fixture = CreateFixture();
            var controller = fixture.CreateReservationsController();

            var result = controller.CreateHold(new ReservationsController.CreateReservationHoldRequest
            {
                ServiceId = "grooming-basic",
                StartAt = SlotStartAt,
                VenueId = "room-a",
                StaffId = "staff-amy"
            });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var error = Assert.IsType<ApiErrorResponse>(unauthorized.Value);
            Assert.Equal("unauthorized", error.Code);
        }

        [Fact]
        public void CreateHold_WithAuthenticatedMember_ReturnsCreatedReservation()
        {
            using var fixture = CreateFixture();
            var controller = fixture.CreateReservationsController("token-101");

            var result = controller.CreateHold(new ReservationsController.CreateReservationHoldRequest
            {
                ServiceId = "grooming-basic",
                StartAt = SlotStartAt,
                VenueId = "room-a",
                StaffId = "staff-amy"
            });

            var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
            Assert.Equal("GetPetShopReservation", created.RouteName);

            var response = Assert.IsType<ReservationsController.ReservationResponse>(created.Value);
            Assert.Equal("holding", response.Status);
            Assert.Equal(101, response.BuyerMemberId);
            Assert.NotNull(response.CheckoutProductId);
            Assert.Equal(2000m, response.Price);
        }

        [Fact]
        public void GetReservations_ReturnsOnlyCurrentMembersReservationsAndLazyExpiresHold()
        {
            using var fixture = CreateFixture(now: Now.AddMinutes(31));
            fixture.CreateHold(buyerMemberId: 101, requestedAt: Now, startAt: SlotStartAt, venueId: "room-a", staffId: "staff-amy");
            fixture.CreateHold(buyerMemberId: 202, requestedAt: Now, startAt: AlternativeSlotStartAt, venueId: "room-b", staffId: "staff-ben");
            var controller = fixture.CreateReservationsController("token-101");

            var result = controller.GetReservations();

            var reservations = Assert.IsAssignableFrom<IReadOnlyList<ReservationsController.ReservationResponse>>(result.Value);
            var reservation = Assert.Single(reservations);
            Assert.Equal(101, reservation.BuyerMemberId);
            Assert.Equal("expired", reservation.Status);
            Assert.Null(reservation.CheckoutProductId);
        }

        [Fact]
        public void GetReservation_WhenOwnerMismatch_ReturnsForbidden()
        {
            using var fixture = CreateFixture();
            var hold = fixture.CreateHold(buyerMemberId: 101, requestedAt: Now, startAt: SlotStartAt, venueId: "room-a", staffId: "staff-amy");
            var controller = fixture.CreateReservationsController("token-202");

            var result = controller.GetReservation(hold.ReservationId);

            var forbidden = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            var error = Assert.IsType<ApiErrorResponse>(forbidden.Value);
            Assert.Equal("reservation-owner-mismatch", error.Code);
        }

        [Fact]
        public void CancelHold_WhenConfirmed_ReturnsConflict()
        {
            using var fixture = CreateFixture(now: Now.AddMinutes(10));
            var hold = fixture.CreateHold(buyerMemberId: 101, requestedAt: Now, startAt: SlotStartAt, venueId: "room-a", staffId: "staff-amy");
            fixture.Service.ConfirmFromOrder(9001, hold.ProductId, Now.AddMinutes(10));
            var controller = fixture.CreateReservationsController("token-101");

            var result = controller.CancelHold(hold.ReservationId);

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            var error = Assert.IsType<ApiErrorResponse>(conflict.Value);
            Assert.Equal("reservation-not-cancellable", error.Code);
        }

        private static PetShopApiFixture CreateFixture(DateTime? now = null)
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"andrew-demo-petshop-api-test-{Guid.NewGuid():N}.db");
            var database = new ShopDatabaseContext(new ShopDatabaseOptions
            {
                ConnectionString = $"Filename={databasePath};Connection=Direct"
            });

            var timeProvider = new MutableTimeProvider(now ?? Now);
            var repository = new PetShopReservationRepository(database);
            var service = new PetShopReservationService(repository);
            var catalog = new PetShopServiceCatalog(new PetShopCatalogOptions
            {
                TimeZoneId = "Asia/Taipei",
                HoldDurationMinutes = 30,
                Services =
                {
                    new PetShopServiceDefinition
                    {
                        ServiceId = "grooming-basic",
                        Name = "基礎美容",
                        Description = "基礎美容預約服務",
                        Price = 2000m,
                        DurationMinutes = 60
                    },
                    new PetShopServiceDefinition
                    {
                        ServiceId = "grooming-deluxe",
                        Name = "精緻美容",
                        Description = "精緻美容預約服務",
                        Price = 3200m,
                        DurationMinutes = 90
                    }
                },
                AvailabilityTemplates =
                {
                    new PetShopAvailabilityTemplate
                    {
                        ServiceId = "grooming-basic",
                        VenueId = "room-a",
                        VenueName = "美容室 A",
                        StaffId = "staff-amy",
                        StaffName = "Amy",
                        StartTimes = { "10:00" }
                    },
                    new PetShopAvailabilityTemplate
                    {
                        ServiceId = "grooming-basic",
                        VenueId = "room-b",
                        VenueName = "美容室 B",
                        StaffId = "staff-ben",
                        StaffName = "Ben",
                        StartTimes = { "10:30" }
                    }
                }
            });
            var availability = new PetShopAvailabilityService(service, catalog);

            SeedMember(database, 101, "buyer-101", "token-101", timeProvider.UtcNow.AddYears(1));
            SeedMember(database, 202, "buyer-202", "token-202", timeProvider.UtcNow.AddYears(1));

            return new PetShopApiFixture(databasePath, database, timeProvider, service, catalog, availability);
        }

        private static void SeedMember(
            ShopDatabaseContext database,
            int memberId,
            string memberName,
            string token,
            DateTime expireAt)
        {
            database.Members.Upsert(new Member
            {
                Id = memberId,
                Name = memberName
            });

            database.MemberTokens.Upsert(new MemberAccessTokenRecord
            {
                Token = token,
                MemberId = memberId,
                Expire = expireAt
            });
        }

        private sealed class PetShopApiFixture : IDisposable
        {
            private readonly string _databasePath;
            private readonly ShopDatabaseContext _database;
            private readonly MutableTimeProvider _timeProvider;
            private readonly PetShopServiceCatalog _catalog;
            private readonly PetShopAvailabilityService _availability;

            public PetShopApiFixture(
                string databasePath,
                ShopDatabaseContext database,
                MutableTimeProvider timeProvider,
                PetShopReservationService service,
                PetShopServiceCatalog catalog,
                PetShopAvailabilityService availability)
            {
                _databasePath = databasePath;
                _database = database;
                _timeProvider = timeProvider;
                Service = service;
                _catalog = catalog;
                _availability = availability;
            }

            public PetShopReservationService Service { get; }

            public ServicesController CreateServicesController()
            {
                return new ServicesController(_catalog);
            }

            public AvailabilityController CreateAvailabilityController()
            {
                return new AvailabilityController(_catalog, _availability, _timeProvider);
            }

            public ReservationsController CreateReservationsController(string? accessToken = null)
            {
                var controller = new ReservationsController(_database, Service, _catalog, _availability, _timeProvider)
                {
                    ControllerContext = new ControllerContext
                    {
                        HttpContext = new DefaultHttpContext()
                    }
                };

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    controller.HttpContext.Items["access-token"] = accessToken;
                }

                return controller;
            }

            public PetShopReservationHoldResult CreateHold(
                int buyerMemberId,
                DateTime requestedAt,
                DateTime startAt,
                string venueId,
                string staffId)
            {
                return Service.CreateHold(new CreatePetShopReservationHoldRequest
                {
                    BuyerMemberId = buyerMemberId,
                    ServiceId = "grooming-basic",
                    ServiceName = "基礎美容",
                    ServiceDescription = "基礎美容預約服務",
                    Price = 2000m,
                    StartAt = startAt,
                    EndAt = startAt.AddHours(1),
                    VenueId = venueId,
                    StaffId = staffId,
                    RequestedAt = requestedAt,
                    HoldDuration = TimeSpan.FromMinutes(30)
                });
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
                    // 測試清理失敗不應影響 API controller 驗證。
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
