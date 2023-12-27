using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        [HttpGet("{id}", Name = "GetCart")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Cart> Get(int id)
        {
            var cart = Cart.Get(id);

            if (cart != null)
            {
                return cart;
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost(Name = "CreateCart")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<Cart> Post()
        {
            var cart = Cart.Create();

            return CreatedAtRoute("GetCart", new { id = cart.Id }, cart);
        }

        [HttpPost("{id}/items", Name = "AddItemToCart")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Cart> Post(int id, [FromBody] AddItemToCartRequest request)
        {
            var cart = Cart.Get(id);

            if (cart != null)
            {
                cart.AddProducts(request.ProductId, request.Qty);
                return CreatedAtRoute("GetCart", new { id = cart.Id }, cart);
            }
            else
            {
                return NotFound();
            }
        }

        public class AddItemToCartRequest
        {
            public int ProductId { get; set; }
            public int Qty { get; set; }
        }
    }
}
