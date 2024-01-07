using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/carts")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        /// <summary>
        /// 取得指定ID的購物車內容
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 建立一個新的購物車
        /// </summary>
        /// <returns></returns>
        [HttpPost("create", Name = "CreateCart")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<Cart> Post()
        {
            var cart = Cart.Create();

            return CreatedAtRoute("GetCart", new { id = cart.Id }, cart);
        }

        /// <summary>
        /// 將商品加入購物車。
        /// </summary>
        /// <remarks>
        /// Qty 參數為加入的數量，若要移除可以用負數。
        /// </remarks>
        /// <param name="id">指定的購物車ID</param>
        /// <param name="request"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 試算購物車內商品結帳時應支付的總金額 (包含折扣)。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
                    TotalPrice = cart.EstimatePrice(),
                    Discounts = cart.EstimateDiscounts().ToList()
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
            public List<Cart.CartDiscountHint> Discounts { get; set; }
        }
    }
}
