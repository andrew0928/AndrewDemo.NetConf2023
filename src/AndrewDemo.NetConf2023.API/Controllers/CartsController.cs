using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{

    /// <summary>
    /// 
    /// </summary>
    [Route("api/carts")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly IShopDatabaseContext _database;
        private readonly IDiscountEngine _discountEngine;
        private readonly IShopRuntimeContext _shopRuntime;

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="database">商店資料庫內容。</param>
        /// <param name="discountEngine">折扣計算引擎。</param>
        /// <param name="shopRuntime">目前啟動中的商店 runtime。</param>
        public CartsController(IShopDatabaseContext database, IDiscountEngine discountEngine, IShopRuntimeContext shopRuntime)
        {
            _database = database;
            _discountEngine = discountEngine;
            _shopRuntime = shopRuntime;
        }

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
            var cart = _database.Carts.FindById(id);

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
            var cart = new Cart();
            _database.Carts.Insert(cart);

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
            var cart = _database.Carts.FindById(id);

            if (cart != null)
            {
                cart.AddProducts(request.ProductId, request.Qty);
                _database.Carts.Update(cart);
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
            var cart = _database.Carts.FindById(id);

            if (cart != null)
            {
                return new CartEstimateResponse()
                {
                    TotalPrice = cart.EstimatePrice(_database, _discountEngine, _shopRuntime.ShopId),
                    Discounts = cart.EstimateDiscounts(_database, _discountEngine, _shopRuntime.ShopId).ToList()
                };
                
            }
            else
            {
                return NotFound();
            }
        }





        /// <summary>
        /// 
        /// </summary>
        public class AddItemToCartRequest
        {
            /// <summary>
            /// 
            /// </summary>
            public int ProductId { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int Qty { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class CartEstimateResponse
        {
            /// <summary>
            /// 
            /// </summary>
            public decimal TotalPrice { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public List<Cart.CartDiscountHint> Discounts { get; set; }
        }
    }
}
