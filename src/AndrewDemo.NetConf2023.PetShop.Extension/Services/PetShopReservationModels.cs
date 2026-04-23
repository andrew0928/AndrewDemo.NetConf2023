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
}
