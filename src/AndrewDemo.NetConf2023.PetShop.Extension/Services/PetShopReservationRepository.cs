using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.PetShop.Extension.Records;
using AndrewDemo.NetConf2023.PetShop.Extension.Reservations;
using LiteDB;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Services
{
    public sealed class PetShopReservationRepository
    {
        private readonly object _sync = new();
        private readonly IShopDatabaseContext _database;

        public PetShopReservationRepository(IShopDatabaseContext database)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
        }

        private ILiteCollection<PetShopReservationRecord> Reservations =>
            _database.Database.GetCollection<PetShopReservationRecord>(PetShopConstants.ReservationsCollectionName);

        public PetShopReservationRecord? FindReservation(string reservationId)
        {
            lock (_sync)
            {
                var reservation = Reservations.FindById(reservationId);
                return reservation == null ? null : CloneReservation(reservation);
            }
        }

        public PetShopReservationRecord? FindReservationByProductId(string productId)
        {
            lock (_sync)
            {
                var reservation = Reservations
                    .Query()
                    .Where(item => item.ProductId == productId)
                    .FirstOrDefault();

                return reservation == null ? null : CloneReservation(reservation);
            }
        }

        public IReadOnlyList<PetShopReservationRecord> FindReservationsByBuyer(int buyerMemberId)
        {
            lock (_sync)
            {
                return Reservations
                    .Query()
                    .Where(item => item.BuyerMemberId == buyerMemberId)
                    .ToList()
                    .Select(CloneReservation)
                    .ToList();
            }
        }

        public Product? FindProduct(string productId)
        {
            lock (_sync)
            {
                var product = _database.Products.FindById(productId);
                return product == null ? null : CloneProduct(product);
            }
        }

        public IReadOnlyList<Product> FindAllProducts()
        {
            lock (_sync)
            {
                return _database.Products.FindAll().Select(CloneProduct).ToList();
            }
        }

        public bool TryCreateHold(PetShopReservationRecord reservation, Product product, DateTime evaluatedAt)
        {
            ArgumentNullException.ThrowIfNull(reservation);
            ArgumentNullException.ThrowIfNull(product);

            if (!string.Equals(reservation.ProductId, product.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException("Reservation product id must match the hidden product id.", nameof(product));
            }

            lock (_sync)
            {
                if (HasActiveReservationAtSlot(
                    reservation.StartAt,
                    reservation.EndAt,
                    reservation.VenueId,
                    reservation.StaffId,
                    evaluatedAt))
                {
                    return false;
                }

                Reservations.Insert(CloneReservation(reservation));
                _database.Products.Insert(CloneProduct(product));
                return true;
            }
        }

        public bool HasActiveReservationAtSlot(
            DateTime startAt,
            DateTime endAt,
            string venueId,
            string staffId,
            DateTime evaluatedAt)
        {
            lock (_sync)
            {
                return HasActiveReservationAtSlotCore(startAt, endAt, venueId, staffId, evaluatedAt);
            }
        }

        public void UpdateReservation(PetShopReservationRecord reservation)
        {
            ArgumentNullException.ThrowIfNull(reservation);

            lock (_sync)
            {
                Reservations.Update(CloneReservation(reservation));
            }
        }

        private bool HasActiveReservationAtSlotCore(
            DateTime startAt,
            DateTime endAt,
            string venueId,
            string staffId,
            DateTime evaluatedAt)
        {
            var candidateStartAt = NormalizeUtc(startAt);
            var candidateEndAt = NormalizeUtc(endAt);
            var evaluationAt = NormalizeUtc(evaluatedAt);

            return Reservations
                .FindAll()
                .Select(CloneReservation)
                .Any(existing =>
                    NormalizeUtc(existing.StartAt) == candidateStartAt
                    && NormalizeUtc(existing.EndAt) == candidateEndAt
                    && existing.VenueId == venueId
                    && existing.StaffId == staffId
                    && (existing.Status == PetShopReservationStatus.Confirmed
                        || (existing.Status == PetShopReservationStatus.Holding && NormalizeUtc(existing.HoldExpiresAt) > evaluationAt)));
        }

        private static PetShopReservationRecord CloneReservation(PetShopReservationRecord reservation)
        {
            return new PetShopReservationRecord
            {
                ReservationId = reservation.ReservationId,
                BuyerMemberId = reservation.BuyerMemberId,
                ServiceId = reservation.ServiceId,
                StartAt = NormalizeUtc(reservation.StartAt),
                EndAt = NormalizeUtc(reservation.EndAt),
                VenueId = reservation.VenueId,
                StaffId = reservation.StaffId,
                Status = reservation.Status,
                HoldExpiresAt = NormalizeUtc(reservation.HoldExpiresAt),
                ProductId = reservation.ProductId,
                ConfirmedOrderId = reservation.ConfirmedOrderId,
                CreatedAt = NormalizeUtc(reservation.CreatedAt),
                UpdatedAt = NormalizeUtc(reservation.UpdatedAt)
            };
        }

        private static Product CloneProduct(Product product)
        {
            return new Product
            {
                Id = product.Id,
                SkuId = product.SkuId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                IsPublished = product.IsPublished
            };
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                : value.ToUniversalTime();
        }
    }
}
