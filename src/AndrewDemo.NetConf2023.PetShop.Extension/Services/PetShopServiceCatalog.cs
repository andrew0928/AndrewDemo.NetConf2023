namespace AndrewDemo.NetConf2023.PetShop.Extension.Services
{
    public sealed class PetShopServiceCatalog
    {
        private readonly PetShopCatalogOptions _options;
        private readonly TimeZoneInfo _timeZone;

        public PetShopServiceCatalog(PetShopCatalogOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);
        }

        public IReadOnlyList<PetShopServiceDefinition> GetServices()
        {
            return _options.Services
                .Select(CloneService)
                .ToList();
        }

        public PetShopServiceDefinition? GetService(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                return null;
            }

            var service = _options.Services
                .FirstOrDefault(item => string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));

            return service == null ? null : CloneService(service);
        }

        public IReadOnlyList<PetShopAvailabilityTemplate> GetAvailabilityTemplates(
            string serviceId,
            string? venueId = null,
            string? staffId = null)
        {
            return _options.AvailabilityTemplates
                .Where(item => string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase))
                .Where(item => string.IsNullOrWhiteSpace(venueId)
                    || string.Equals(item.VenueId, venueId, StringComparison.OrdinalIgnoreCase))
                .Where(item => string.IsNullOrWhiteSpace(staffId)
                    || string.Equals(item.StaffId, staffId, StringComparison.OrdinalIgnoreCase))
                .Select(CloneTemplate)
                .ToList();
        }

        public TimeSpan GetHoldDuration()
        {
            return TimeSpan.FromMinutes(_options.HoldDurationMinutes);
        }

        public TimeZoneInfo GetTimeZone()
        {
            return _timeZone;
        }

        private static PetShopServiceDefinition CloneService(PetShopServiceDefinition service)
        {
            return new PetShopServiceDefinition
            {
                ServiceId = service.ServiceId,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes
            };
        }

        private static PetShopAvailabilityTemplate CloneTemplate(PetShopAvailabilityTemplate template)
        {
            return new PetShopAvailabilityTemplate
            {
                ServiceId = template.ServiceId,
                VenueId = template.VenueId,
                VenueName = template.VenueName,
                StaffId = template.StaffId,
                StaffName = template.StaffName,
                StartTimes = template.StartTimes.ToList()
            };
        }
    }
}
