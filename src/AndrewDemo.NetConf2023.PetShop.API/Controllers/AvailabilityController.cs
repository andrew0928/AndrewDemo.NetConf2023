using AndrewDemo.NetConf2023.Core.Time;
using AndrewDemo.NetConf2023.PetShop.API.Models;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.API.Controllers
{
    [Route("petshop-api/availability")]
    [ApiController]
    public class AvailabilityController : ControllerBase
    {
        private readonly PetShopServiceCatalog _catalog;
        private readonly PetShopAvailabilityService _availabilityService;
        private readonly TimeProvider _timeProvider;

        public AvailabilityController(
            PetShopServiceCatalog catalog,
            PetShopAvailabilityService availabilityService,
            TimeProvider timeProvider)
        {
            _catalog = catalog;
            _availabilityService = availabilityService;
            _timeProvider = timeProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IReadOnlyList<AvailabilitySlotResponse>> Get(
            [FromQuery] string serviceId,
            [FromQuery] DateOnly date,
            [FromQuery] string? venueId,
            [FromQuery] string? staffId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
            {
                return BadRequest(ApiErrorResponse.Create(
                    "validation-failed",
                    "serviceId is required."));
            }

            if (_catalog.GetService(serviceId) == null)
            {
                return NotFound(ApiErrorResponse.Create(
                    "service-not-found",
                    $"Service {serviceId} was not found."));
            }

            var slots = _availabilityService.GetAvailableSlots(
                serviceId,
                date,
                _timeProvider.GetUtcDateTime(),
                venueId,
                staffId);

            return slots
                .Select(slot => new AvailabilitySlotResponse
                {
                    ServiceId = slot.ServiceId,
                    StartAt = slot.StartAt,
                    EndAt = slot.EndAt,
                    VenueId = slot.VenueId,
                    VenueName = slot.VenueName,
                    StaffId = slot.StaffId,
                    StaffName = slot.StaffName
                })
                .ToList();
        }

        public sealed class AvailabilitySlotResponse
        {
            public string ServiceId { get; set; } = string.Empty;
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
            public string VenueId { get; set; } = string.Empty;
            public string? VenueName { get; set; }
            public string StaffId { get; set; } = string.Empty;
            public string? StaffName { get; set; }
        }
    }
}
