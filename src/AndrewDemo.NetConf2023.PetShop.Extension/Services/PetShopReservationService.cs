using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.PetShop.Extension.Records;
using AndrewDemo.NetConf2023.PetShop.Extension.Reservations;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Services
{
    public sealed class PetShopReservationService
    {
        private const string SlotUnavailable = "slot-unavailable";

        private readonly PetShopReservationRepository _repository;

        public PetShopReservationService(PetShopReservationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public PetShopReservationHoldResult CreateHold(CreatePetShopReservationHoldRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var reservationId = NewReservationId();
            var productId = NewReservationProductId();
            var holdExpiresAt = request.RequestedAt.Add(request.HoldDuration);

            var reservation = new PetShopReservationRecord
            {
                ReservationId = reservationId,
                BuyerMemberId = request.BuyerMemberId,
                ServiceId = request.ServiceId,
                StartAt = request.StartAt,
                EndAt = request.EndAt,
                VenueId = request.VenueId,
                StaffId = request.StaffId,
                Status = PetShopReservationStatus.Holding,
                HoldExpiresAt = holdExpiresAt,
                ProductId = productId,
                CreatedAt = request.RequestedAt,
                UpdatedAt = request.RequestedAt
            };

            var product = new Product
            {
                Id = productId,
                Name = request.ServiceName,
                Description = request.ServiceDescription,
                Price = request.Price,
                IsPublished = false
            };

            if (!_repository.TryCreateHold(reservation, product, request.RequestedAt))
            {
                return PetShopReservationHoldResult.CreateFailed(SlotUnavailable);
            }

            return PetShopReservationHoldResult.CreateSucceeded(reservationId, productId);
        }

        public bool CancelHold(string reservationId, DateTime cancelledAt)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
            {
                return false;
            }

            var reservation = _repository.FindReservation(reservationId);
            if (reservation == null)
            {
                return false;
            }

            if (reservation.Status == PetShopReservationStatus.Cancelled)
            {
                return true;
            }

            if (reservation.Status != PetShopReservationStatus.Holding)
            {
                return false;
            }

            if (reservation.HoldExpiresAt <= cancelledAt)
            {
                ExpireHold(reservation, cancelledAt);
                return false;
            }

            reservation.Status = PetShopReservationStatus.Cancelled;
            reservation.UpdatedAt = cancelledAt;

            _repository.UpdateReservation(reservation);

            return true;
        }

        public Product? ApplyReservationProductPolicy(Product product, DateTime evaluatedAt)
        {
            ArgumentNullException.ThrowIfNull(product);

            if (string.IsNullOrWhiteSpace(product.Id))
            {
                return null;
            }

            var reservation = _repository.FindReservationByProductId(product.Id);
            if (reservation == null)
            {
                return product.IsPublished ? product : null;
            }

            if (reservation.Status != PetShopReservationStatus.Holding)
            {
                return null;
            }

            if (reservation.HoldExpiresAt <= evaluatedAt)
            {
                ExpireHold(reservation, evaluatedAt);
                return null;
            }

            return product;
        }

        public PetShopReservationConfirmationResult ConfirmFromOrder(int orderId, string productId, DateTime confirmedAt)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return PetShopReservationConfirmationResult.NotApplicable();
            }

            var reservation = _repository.FindReservationByProductId(productId);
            if (reservation == null)
            {
                return PetShopReservationConfirmationResult.NotApplicable();
            }

            if (reservation.Status == PetShopReservationStatus.Confirmed
                && reservation.ConfirmedOrderId == orderId)
            {
                return PetShopReservationConfirmationResult.AlreadyConfirmed(reservation);
            }

            if (reservation.Status != PetShopReservationStatus.Holding)
            {
                return PetShopReservationConfirmationResult.NotApplicable();
            }

            if (reservation.HoldExpiresAt <= confirmedAt)
            {
                ExpireHold(reservation, confirmedAt);
                return PetShopReservationConfirmationResult.NotApplicable();
            }

            reservation.Status = PetShopReservationStatus.Confirmed;
            reservation.ConfirmedOrderId = orderId;
            reservation.UpdatedAt = confirmedAt;

            _repository.UpdateReservation(reservation);

            return PetShopReservationConfirmationResult.Confirmed(reservation);
        }

        private void ExpireHold(PetShopReservationRecord reservation, DateTime expiredAt)
        {
            reservation.Status = PetShopReservationStatus.Expired;
            reservation.UpdatedAt = expiredAt;

            _repository.UpdateReservation(reservation);
        }

        private static string NewReservationId()
        {
            return $"pet-rsv-{Guid.NewGuid():N}";
        }

        private static string NewReservationProductId()
        {
            return $"pet-rsv-prod-{Guid.NewGuid():N}";
        }
    }
}
