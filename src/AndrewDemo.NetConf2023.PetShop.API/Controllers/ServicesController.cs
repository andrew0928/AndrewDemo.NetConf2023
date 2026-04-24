using AndrewDemo.NetConf2023.PetShop.Extension.Services;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.PetShop.API.Controllers
{
    [Route("petshop-api/services")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly PetShopServiceCatalog _catalog;

        public ServicesController(PetShopServiceCatalog catalog)
        {
            _catalog = catalog;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IReadOnlyList<ServiceResponse>> Get()
        {
            return _catalog.GetServices()
                .Select(service => new ServiceResponse
                {
                    ServiceId = service.ServiceId,
                    Name = service.Name,
                    Description = service.Description,
                    Price = service.Price,
                    DurationMinutes = service.DurationMinutes
                })
                .ToList();
        }

        public sealed class ServiceResponse
        {
            public string ServiceId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public int DurationMinutes { get; set; }
        }
    }
}
