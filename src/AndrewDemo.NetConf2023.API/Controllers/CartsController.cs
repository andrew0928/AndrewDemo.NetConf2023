using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Time;
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
        private readonly DiscountEngine _discountEngine;
        private readonly IProductService _productService;
        private readonly ShopManifest _shopManifest;
        private readonly TimeProvider _timeProvider;

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="database">商店資料庫內容。</param>
        /// <param name="discountEngine">折扣計算引擎。</param>
        /// <param name="productService">商品服務。</param>
        /// <param name="shopManifest">目前啟動中的商店 manifest。</param>
        /// <param name="timeProvider">目前系統的時間提供者。</param>
        public CartsController(IShopDatabaseContext database, DiscountEngine discountEngine, IProductService productService, ShopManifest shopManifest, TimeProvider timeProvider)
        {
            _database = database;
            _discountEngine = discountEngine;
            _productService = productService;
            _shopManifest = shopManifest;
            _timeProvider = timeProvider;
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
                var product = _productService.GetProductById(request.ProductId);
                if (product == null)
                {
                    return BadRequest($"Product {request.ProductId} not found");
                }

                cart.AddProducts(request.ProductId, request.Qty, _timeProvider.GetUtcDateTime(), request.ParentLineId);
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
                var consumer = GetAuthenticatedMember();
                var cartContext = CartContextFactory.Create(_shopManifest, cart, consumer, _productService, _timeProvider);
                var discountRecords = _discountEngine.Evaluate(cartContext);

                return new CartEstimateResponse()
                {
                    TotalPrice = cartContext.LineItems.Sum(x =>
                        (x.UnitPrice ?? throw new InvalidOperationException($"unit price is required for product {x.ProductId}")) * x.Quantity)
                        + discountRecords
                            .Where(x => x.Kind == DiscountRecordKind.Discount)
                            .Sum(x => x.Amount),
                    Discounts = discountRecords
                        .Select(x => new CartDiscountRecord
                        {
                            Kind = x.Kind,
                            Name = x.Name,
                            Description = x.Description,
                            Amount = x.Amount,
                            RelatedLineIds = x.RelatedLineIds.ToList()
                        })
                        .ToList()
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
            public string ProductId { get; set; } = string.Empty;
            /// <summary>
            /// 
            /// </summary>
            public int Qty { get; set; }

            /// <summary>
            /// gift line 對應的主商品 line id。
            /// </summary>
            public string? ParentLineId { get; set; }
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
            public List<CartDiscountRecord> Discounts { get; set; } = new List<CartDiscountRecord>();
        }

        /// <summary>
        /// 
        /// </summary>
        public class CartDiscountRecord
        {
            /// <summary>
            /// 
            /// </summary>
            public DiscountRecordKind Kind { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public decimal Amount { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public List<string> RelatedLineIds { get; set; } = new List<string>();
        }

        private Member? GetAuthenticatedMember()
        {
            var accessToken = HttpContext.Items["access-token"] as string;
            if (accessToken == null)
            {
                return null;
            }

            var tokenRecord = _database.MemberTokens.FindById(accessToken);
            if (tokenRecord == null || tokenRecord.Expire <= _timeProvider.GetLocalDateTime())
            {
                return null;
            }

            return _database.Members.FindById(tokenRecord.MemberId);
        }
    }
}
