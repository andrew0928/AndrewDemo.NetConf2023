using AndrewDemo.NetConf2023.PetShop.Extension.Records;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Services
{
    public sealed class CreatePetShopReservationHoldRequest
    {
        public int BuyerMemberId { get; init; }
        public string ServiceId { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public string? ServiceDescription { get; init; }
        public decimal Price { get; init; }
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public string VenueId { get; init; } = string.Empty;
        public string StaffId { get; init; } = string.Empty;
        public DateTime RequestedAt { get; init; }
        public TimeSpan HoldDuration { get; init; } = TimeSpan.FromMinutes(30);
    }

    public sealed class PetShopReservationHoldResult
    {
        public bool Succeeded { get; init; }
        public string? ErrorCode { get; init; }
        public string ReservationId { get; init; } = string.Empty;
        public string ProductId { get; init; } = string.Empty;

        public static PetShopReservationHoldResult CreateSucceeded(string reservationId, string productId)
        {
            return new PetShopReservationHoldResult
            {
                Succeeded = true,
                ReservationId = reservationId,
                ProductId = productId
            };
        }

        public static PetShopReservationHoldResult CreateFailed(string errorCode)
        {
            return new PetShopReservationHoldResult
            {
                Succeeded = false,
                ErrorCode = errorCode
            };
        }
    }

    public enum PetShopReservationCancelHoldStatus
    {
        NotFound = 0,
        CancelledNow = 1,
        AlreadyCancelled = 2,
        HoldExpired = 3,
        ReservationNotCancellable = 4
    }

    public sealed class PetShopReservationCancelHoldResult
    {
        public PetShopReservationCancelHoldStatus Status { get; init; }

        public static PetShopReservationCancelHoldResult NotFound()
        {
            return new PetShopReservationCancelHoldResult
            {
                Status = PetShopReservationCancelHoldStatus.NotFound
            };
        }

        public static PetShopReservationCancelHoldResult CancelledNow()
        {
            return new PetShopReservationCancelHoldResult
            {
                Status = PetShopReservationCancelHoldStatus.CancelledNow
            };
        }

        public static PetShopReservationCancelHoldResult AlreadyCancelled()
        {
            return new PetShopReservationCancelHoldResult
            {
                Status = PetShopReservationCancelHoldStatus.AlreadyCancelled
            };
        }

        public static PetShopReservationCancelHoldResult HoldExpired()
        {
            return new PetShopReservationCancelHoldResult
            {
                Status = PetShopReservationCancelHoldStatus.HoldExpired
            };
        }

        public static PetShopReservationCancelHoldResult ReservationNotCancellable()
        {
            return new PetShopReservationCancelHoldResult
            {
                Status = PetShopReservationCancelHoldStatus.ReservationNotCancellable
            };
        }
    }

    public sealed class PetShopReservationConfirmationResult
    {
        public bool IsConfirmedNow { get; init; }
        public bool IsAlreadyConfirmed { get; init; }
        public string ReservationId { get; init; } = string.Empty;
        public string ProductId { get; init; } = string.Empty;
        public int BuyerMemberId { get; init; }
        public string StaffId { get; init; } = string.Empty;

        public static PetShopReservationConfirmationResult NotApplicable()
        {
            return new PetShopReservationConfirmationResult();
        }

        public static PetShopReservationConfirmationResult Confirmed(PetShopReservationRecord reservation)
        {
            ArgumentNullException.ThrowIfNull(reservation);

            return new PetShopReservationConfirmationResult
            {
                IsConfirmedNow = true,
                ReservationId = reservation.ReservationId,
                ProductId = reservation.ProductId,
                BuyerMemberId = reservation.BuyerMemberId,
                StaffId = reservation.StaffId
            };
        }

        public static PetShopReservationConfirmationResult AlreadyConfirmed(PetShopReservationRecord reservation)
        {
            ArgumentNullException.ThrowIfNull(reservation);

            return new PetShopReservationConfirmationResult
            {
                IsAlreadyConfirmed = true,
                ReservationId = reservation.ReservationId,
                ProductId = reservation.ProductId,
                BuyerMemberId = reservation.BuyerMemberId,
                StaffId = reservation.StaffId
            };
        }
    }

    public sealed class PetShopReservationSnapshot
    {
        public string ReservationId { get; init; } = string.Empty;
        public int BuyerMemberId { get; init; }
        public string ServiceId { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public string? ServiceDescription { get; init; }
        public decimal Price { get; init; }
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public string VenueId { get; init; } = string.Empty;
        public string StaffId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime HoldExpiresAt { get; init; }
        public string? CheckoutProductId { get; init; }
        public int? ConfirmedOrderId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
