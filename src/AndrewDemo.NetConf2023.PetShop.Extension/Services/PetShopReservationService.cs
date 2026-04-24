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

        public PetShopReservationCancelHoldResult CancelHold(string reservationId, DateTime cancelledAt)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
            {
                return PetShopReservationCancelHoldResult.NotFound();
            }

            var reservation = _repository.FindReservation(reservationId);
            if (reservation == null)
            {
                return PetShopReservationCancelHoldResult.NotFound();
            }

            if (reservation.Status == PetShopReservationStatus.Cancelled)
            {
                return PetShopReservationCancelHoldResult.AlreadyCancelled();
            }

            if (reservation.Status == PetShopReservationStatus.Expired)
            {
                return PetShopReservationCancelHoldResult.HoldExpired();
            }

            if (reservation.Status != PetShopReservationStatus.Holding)
            {
                return PetShopReservationCancelHoldResult.ReservationNotCancellable();
            }

            if (reservation.HoldExpiresAt <= cancelledAt)
            {
                ExpireHold(reservation, cancelledAt);
                return PetShopReservationCancelHoldResult.HoldExpired();
            }

            reservation.Status = PetShopReservationStatus.Cancelled;
            reservation.UpdatedAt = cancelledAt;

            _repository.UpdateReservation(reservation);

            return PetShopReservationCancelHoldResult.CancelledNow();
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

        public PetShopReservationSnapshot? GetReservationSnapshot(
            string reservationId,
            DateTime evaluatedAt,
            bool applyLazyExpiration = true)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
            {
                return null;
            }

            var reservation = _repository.FindReservation(reservationId);
            if (reservation == null)
            {
                return null;
            }

            if (applyLazyExpiration)
            {
                NormalizeReservationForEvaluation(reservation, evaluatedAt);
            }

            return BuildSnapshot(reservation);
        }

        public IReadOnlyList<PetShopReservationSnapshot> GetReservationsByBuyer(int buyerMemberId, DateTime evaluatedAt)
        {
            return _repository.FindReservationsByBuyer(buyerMemberId)
                .Select(reservation =>
                {
                    NormalizeReservationForEvaluation(reservation, evaluatedAt);
                    return BuildSnapshot(reservation);
                })
                .Where(snapshot => snapshot != null)
                .Cast<PetShopReservationSnapshot>()
                .OrderBy(snapshot => snapshot.StartAt)
                .ThenBy(snapshot => snapshot.CreatedAt)
                .ToList();
        }

        public bool HasActiveReservationAtSlot(
            DateTime startAt,
            DateTime endAt,
            string venueId,
            string staffId,
            DateTime evaluatedAt)
        {
            return _repository.HasActiveReservationAtSlot(startAt, endAt, venueId, staffId, evaluatedAt);
        }

        public Product? GetReservationProduct(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return null;
            }

            return _repository.FindProduct(productId);
        }

        private void ExpireHold(PetShopReservationRecord reservation, DateTime expiredAt)
        {
            reservation.Status = PetShopReservationStatus.Expired;
            reservation.UpdatedAt = expiredAt;

            _repository.UpdateReservation(reservation);
        }

        private void NormalizeReservationForEvaluation(PetShopReservationRecord reservation, DateTime evaluatedAt)
        {
            ArgumentNullException.ThrowIfNull(reservation);

            if (reservation.Status == PetShopReservationStatus.Holding
                && reservation.HoldExpiresAt <= evaluatedAt)
            {
                ExpireHold(reservation, evaluatedAt);
            }
        }

        private PetShopReservationSnapshot BuildSnapshot(PetShopReservationRecord reservation)
        {
            ArgumentNullException.ThrowIfNull(reservation);

            var product = _repository.FindProduct(reservation.ProductId);

            return new PetShopReservationSnapshot
            {
                ReservationId = reservation.ReservationId,
                BuyerMemberId = reservation.BuyerMemberId,
                ServiceId = reservation.ServiceId,
                ServiceName = product?.Name ?? string.Empty,
                ServiceDescription = product?.Description,
                Price = product?.Price ?? 0m,
                StartAt = reservation.StartAt,
                EndAt = reservation.EndAt,
                VenueId = reservation.VenueId,
                StaffId = reservation.StaffId,
                Status = ToApiStatus(reservation.Status),
                HoldExpiresAt = reservation.HoldExpiresAt,
                CheckoutProductId = reservation.Status == PetShopReservationStatus.Holding ? reservation.ProductId : null,
                ConfirmedOrderId = reservation.ConfirmedOrderId,
                CreatedAt = reservation.CreatedAt,
                UpdatedAt = reservation.UpdatedAt
            };
        }

        private static string NewReservationId()
        {
            return $"pet-rsv-{Guid.NewGuid():N}";
        }

        private static string NewReservationProductId()
        {
            return $"pet-rsv-prod-{Guid.NewGuid():N}";
        }

        private static string ToApiStatus(PetShopReservationStatus status)
        {
            return status switch
            {
                PetShopReservationStatus.Holding => "holding",
                PetShopReservationStatus.Confirmed => "confirmed",
                PetShopReservationStatus.Expired => "expired",
                PetShopReservationStatus.Cancelled => "cancelled",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
    }
}
