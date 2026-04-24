namespace AndrewDemo.NetConf2023.PetShop.Extension.Services
{
    public sealed class PetShopCatalogOptions
    {
        public string TimeZoneId { get; set; } = "Asia/Taipei";
        public int HoldDurationMinutes { get; set; } = 30;
        public List<PetShopServiceDefinition> Services { get; set; } = new();
        public List<PetShopAvailabilityTemplate> AvailabilityTemplates { get; set; } = new();
    }

    public sealed class PetShopServiceDefinition
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
    }

    public sealed class PetShopAvailabilityTemplate
    {
        public string ServiceId { get; set; } = string.Empty;
        public string VenueId { get; set; } = string.Empty;
        public string? VenueName { get; set; }
        public string StaffId { get; set; } = string.Empty;
        public string? StaffName { get; set; }
        public List<string> StartTimes { get; set; } = new();
    }

    public sealed class PetShopAvailabilitySlot
    {
        public string ServiceId { get; init; } = string.Empty;
        public DateTime StartAt { get; init; }
        public DateTime EndAt { get; init; }
        public string VenueId { get; init; } = string.Empty;
        public string? VenueName { get; init; }
        public string StaffId { get; init; } = string.Empty;
        public string? StaffName { get; init; }
    }
}
