namespace AndrewDemo.NetConf2023.PetShop.Extension.Services
{
    public sealed class PetShopAvailabilityService
    {
        private readonly PetShopReservationService _reservationService;
        private readonly PetShopServiceCatalog _catalog;

        public PetShopAvailabilityService(
            PetShopReservationService reservationService,
            PetShopServiceCatalog catalog)
        {
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public IReadOnlyList<PetShopAvailabilitySlot> GetAvailableSlots(
            string serviceId,
            DateOnly date,
            DateTime evaluatedAt,
            string? venueId = null,
            string? staffId = null)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                return Array.Empty<PetShopAvailabilitySlot>();
            }

            var service = _catalog.GetService(serviceId);
            if (service == null)
            {
                return Array.Empty<PetShopAvailabilitySlot>();
            }

            var timeZone = _catalog.GetTimeZone();

            return _catalog.GetAvailabilityTemplates(serviceId, venueId, staffId)
                .SelectMany(template => template.StartTimes
                    .Select(startTime => CreateSlot(template, service, date, timeZone, startTime)))
                .Where(slot => slot != null)
                .Cast<PetShopAvailabilitySlot>()
                .Where(slot => !_reservationService.HasActiveReservationAtSlot(
                    slot.StartAt,
                    slot.EndAt,
                    slot.VenueId,
                    slot.StaffId,
                    evaluatedAt))
                .OrderBy(slot => slot.StartAt)
                .ThenBy(slot => slot.VenueId)
                .ThenBy(slot => slot.StaffId)
                .ToList();
        }

        public bool IsBookableSlot(
            string serviceId,
            DateTime startAt,
            string venueId,
            string staffId)
        {
            if (string.IsNullOrWhiteSpace(serviceId)
                || string.IsNullOrWhiteSpace(venueId)
                || string.IsNullOrWhiteSpace(staffId))
            {
                return false;
            }

            var service = _catalog.GetService(serviceId);
            if (service == null)
            {
                return false;
            }

            var normalizedStartAt = NormalizeUtc(startAt);
            var timeZone = _catalog.GetTimeZone();
            var localStartAt = TimeZoneInfo.ConvertTimeFromUtc(normalizedStartAt, timeZone);
            var date = DateOnly.FromDateTime(localStartAt);

            return _catalog.GetAvailabilityTemplates(serviceId, venueId, staffId)
                .SelectMany(template => template.StartTimes
                    .Select(startTime => CreateSlot(template, service, date, timeZone, startTime)))
                .Where(slot => slot != null)
                .Cast<PetShopAvailabilitySlot>()
                .Any(slot =>
                    slot.StartAt == normalizedStartAt
                    && string.Equals(slot.VenueId, venueId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(slot.StaffId, staffId, StringComparison.OrdinalIgnoreCase));
        }

        private static PetShopAvailabilitySlot? CreateSlot(
            PetShopAvailabilityTemplate template,
            PetShopServiceDefinition service,
            DateOnly date,
            TimeZoneInfo timeZone,
            string startTime)
        {
            if (!TimeOnly.TryParse(startTime, out var parsedStartTime))
            {
                return null;
            }

            var localStartAt = date.ToDateTime(parsedStartTime, DateTimeKind.Unspecified);
            var utcStartAt = TimeZoneInfo.ConvertTimeToUtc(localStartAt, timeZone);
            var utcEndAt = utcStartAt.AddMinutes(service.DurationMinutes);

            return new PetShopAvailabilitySlot
            {
                ServiceId = service.ServiceId,
                StartAt = utcStartAt,
                EndAt = utcEndAt,
                VenueId = template.VenueId,
                VenueName = template.VenueName,
                StaffId = template.StaffId,
                StaffName = template.StaffName
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
