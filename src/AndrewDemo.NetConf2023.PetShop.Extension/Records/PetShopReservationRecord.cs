using AndrewDemo.NetConf2023.PetShop.Extension.Reservations;
using LiteDB;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Records
{
    public sealed class PetShopReservationRecord
    {
        [BsonId]
        public string ReservationId { get; set; } = string.Empty;
        public int BuyerMemberId { get; set; }
        public string ServiceId { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string VenueId { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        public PetShopReservationStatus Status { get; set; } = PetShopReservationStatus.Holding;
        public DateTime HoldExpiresAt { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int? ConfirmedOrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
