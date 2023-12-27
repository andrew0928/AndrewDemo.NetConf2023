using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        [HttpGet(Name = "GetProducts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<Product> Get()
        {
            return Product.Database.Values;
        }


        [HttpGet("{id}", Name = "GetProduct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Product> Get(int id)
        {
            if (Product.Database.ContainsKey(id))
            {
                return Product.Database[id];
            }
            else
            {
                return NotFound();
            }
        }
    }


    
}
