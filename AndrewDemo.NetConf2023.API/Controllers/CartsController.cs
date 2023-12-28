using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/carts")]
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

        [HttpPost("create", Name = "CreateCart")]
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


        [HttpPost("{id}/estimate", Name = "EstimatePrice")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<CartEstimateResponse> Post(int id)
        {
            var cart = Cart.Get(id);

            if (cart != null)
            {
                return new CartEstimateResponse()
                {
                    TotalPrice = cart.EstimatePrice()
                };
                
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

        public class CartEstimateResponse
        {
            public decimal TotalPrice { get; set; }
            //public List<DiscountRecord> Discounts { get; set; }
        }
    }
}
