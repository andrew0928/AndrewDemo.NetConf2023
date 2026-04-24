using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Time;
using AndrewDemo.NetConf2023.PetShop.API.Models;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.API.Controllers
{
    [Route("petshop-api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly IShopDatabaseContext _database;
        private readonly PetShopReservationService _reservationService;
        private readonly PetShopServiceCatalog _catalog;
        private readonly PetShopAvailabilityService _availabilityService;
        private readonly TimeProvider _timeProvider;

        public ReservationsController(
            IShopDatabaseContext database,
            PetShopReservationService reservationService,
            PetShopServiceCatalog catalog,
            PetShopAvailabilityService availabilityService,
            TimeProvider timeProvider)
        {
            _database = database;
            _reservationService = reservationService;
            _catalog = catalog;
            _availabilityService = availabilityService;
            _timeProvider = timeProvider;
        }

        [HttpPost("holds")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<ReservationResponse> CreateHold([FromBody] CreateReservationHoldRequest request)
        {
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized(ApiErrorResponse.Create(
                    "unauthorized",
                    "A valid member access token is required."));
            }

            if (string.IsNullOrWhiteSpace(request.ServiceId)
                || string.IsNullOrWhiteSpace(request.VenueId)
                || string.IsNullOrWhiteSpace(request.StaffId))
            {
                return BadRequest(ApiErrorResponse.Create(
                    "validation-failed",
                    "serviceId, venueId, and staffId are required."));
            }

            var service = _catalog.GetService(request.ServiceId);
            if (service == null)
            {
                return NotFound(ApiErrorResponse.Create(
                    "service-not-found",
                    $"Service {request.ServiceId} was not found."));
            }

            var now = _timeProvider.GetUtcDateTime();
            var startAt = NormalizeUtc(request.StartAt);
            if (!_availabilityService.IsBookableSlot(request.ServiceId, startAt, request.VenueId, request.StaffId))
            {
                return BadRequest(ApiErrorResponse.Create(
                    "validation-failed",
                    "The requested slot is not bookable for the selected service."));
            }

            var result = _reservationService.CreateHold(new CreatePetShopReservationHoldRequest
            {
                BuyerMemberId = member.Id,
                ServiceId = service.ServiceId,
                ServiceName = service.Name,
                ServiceDescription = service.Description,
                Price = service.Price,
                StartAt = startAt,
                EndAt = startAt.AddMinutes(service.DurationMinutes),
                VenueId = request.VenueId,
                StaffId = request.StaffId,
                RequestedAt = now,
                HoldDuration = _catalog.GetHoldDuration()
            });

            if (!result.Succeeded)
            {
                return Conflict(ApiErrorResponse.Create(
                    "slot-unavailable",
                    "The selected reservation slot is no longer available."));
            }

            var snapshot = _reservationService.GetReservationSnapshot(result.ReservationId, now);
            if (snapshot == null)
            {
                throw new InvalidOperationException($"created reservation not found: {result.ReservationId}");
            }

            return CreatedAtRoute(
                "GetPetShopReservation",
                new { reservationId = snapshot.ReservationId },
                ToResponse(snapshot));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<IReadOnlyList<ReservationResponse>> GetReservations()
        {
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized(ApiErrorResponse.Create(
                    "unauthorized",
                    "A valid member access token is required."));
            }

            var now = _timeProvider.GetUtcDateTime();
            return _reservationService.GetReservationsByBuyer(member.Id, now)
                .Select(ToResponse)
                .ToList();
        }

        [HttpGet("{reservationId}", Name = "GetPetShopReservation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ReservationResponse> GetReservation(string reservationId)
        {
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized(ApiErrorResponse.Create(
                    "unauthorized",
                    "A valid member access token is required."));
            }

            var snapshot = _reservationService.GetReservationSnapshot(reservationId, _timeProvider.GetUtcDateTime());
            if (snapshot == null)
            {
                return NotFound(ApiErrorResponse.Create(
                    "reservation-not-found",
                    $"Reservation {reservationId} was not found."));
            }

            if (snapshot.BuyerMemberId != member.Id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiErrorResponse.Create(
                    "reservation-owner-mismatch",
                    "The reservation does not belong to the current member."));
            }

            return ToResponse(snapshot);
        }

        [HttpPost("{reservationId}/cancel-hold")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<ReservationResponse> CancelHold(string reservationId)
        {
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized(ApiErrorResponse.Create(
                    "unauthorized",
                    "A valid member access token is required."));
            }

            var now = _timeProvider.GetUtcDateTime();
            var rawSnapshot = _reservationService.GetReservationSnapshot(reservationId, now, applyLazyExpiration: false);
            if (rawSnapshot == null)
            {
                return NotFound(ApiErrorResponse.Create(
                    "reservation-not-found",
                    $"Reservation {reservationId} was not found."));
            }

            if (rawSnapshot.BuyerMemberId != member.Id)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiErrorResponse.Create(
                    "reservation-owner-mismatch",
                    "The reservation does not belong to the current member."));
            }

            var result = _reservationService.CancelHold(reservationId, now);
            switch (result.Status)
            {
                case PetShopReservationCancelHoldStatus.CancelledNow:
                case PetShopReservationCancelHoldStatus.AlreadyCancelled:
                {
                    var snapshot = _reservationService.GetReservationSnapshot(reservationId, now, applyLazyExpiration: false);
                    if (snapshot == null)
                    {
                        return NotFound(ApiErrorResponse.Create(
                            "reservation-not-found",
                            $"Reservation {reservationId} was not found."));
                    }

                    return Ok(ToResponse(snapshot));
                }
                case PetShopReservationCancelHoldStatus.HoldExpired:
                    return Conflict(ApiErrorResponse.Create(
                        "hold-expired",
                        "The reservation hold has already expired."));
                case PetShopReservationCancelHoldStatus.ReservationNotCancellable:
                    return Conflict(ApiErrorResponse.Create(
                        "reservation-not-cancellable",
                        "The reservation is not in a cancellable holding state."));
                default:
                    return NotFound(ApiErrorResponse.Create(
                        "reservation-not-found",
                        $"Reservation {reservationId} was not found."));
            }
        }

        private Member? GetAuthenticatedMember()
        {
            var accessToken = HttpContext.Items["access-token"] as string;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var tokenRecord = _database.MemberTokens.FindById(accessToken);
            if (tokenRecord == null || tokenRecord.Expire <= _timeProvider.GetLocalDateTime())
            {
                return null;
            }

            return _database.Members.FindById(tokenRecord.MemberId);
        }

        private static ReservationResponse ToResponse(PetShopReservationSnapshot snapshot)
        {
            return new ReservationResponse
            {
                ReservationId = snapshot.ReservationId,
                Status = snapshot.Status,
                BuyerMemberId = snapshot.BuyerMemberId,
                ServiceId = snapshot.ServiceId,
                ServiceName = snapshot.ServiceName,
                Price = snapshot.Price,
                StartAt = snapshot.StartAt,
                EndAt = snapshot.EndAt,
                VenueId = snapshot.VenueId,
                StaffId = snapshot.StaffId,
                HoldExpiresAt = snapshot.HoldExpiresAt,
                CheckoutProductId = snapshot.CheckoutProductId,
                ConfirmedOrderId = snapshot.ConfirmedOrderId,
                CreatedAt = snapshot.CreatedAt,
                UpdatedAt = snapshot.UpdatedAt
            };
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }

        public sealed class CreateReservationHoldRequest
        {
            public string ServiceId { get; set; } = string.Empty;
            public DateTime StartAt { get; set; }
            public string VenueId { get; set; } = string.Empty;
            public string StaffId { get; set; } = string.Empty;
        }

        public sealed class ReservationResponse
        {
            public string ReservationId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int BuyerMemberId { get; set; }
            public string ServiceId { get; set; } = string.Empty;
            public string ServiceName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
            public string VenueId { get; set; } = string.Empty;
            public string StaffId { get; set; } = string.Empty;
            public DateTime HoldExpiresAt { get; set; }
            public string? CheckoutProductId { get; set; }
            public int? ConfirmedOrderId { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
    }
}
