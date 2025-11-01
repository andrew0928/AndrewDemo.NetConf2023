using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    /// <summary>
    /// 負責提供商品查詢介面。
    /// </summary>
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IShopDatabaseContext _database;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="database"></param>
        public ProductsController(IShopDatabaseContext database)
        {
            _database = database;
        }

        /// <summary>
        /// 取得線上商店的所有商品資訊。
        /// </summary>
        /// <returns></returns>
        [HttpGet("", Name = "GetProducts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Product>> Get()
        {
            return _database.Products.FindAll().ToList();
        }

        /// <summary>
        /// 取得線上商店的指定商品資訊。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}", Name = "GetProduct")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Product> Get(int id)
        {
            var product = _database.Products.FindById(id);
            if (product == null)
            {
                return NotFound();
            }

            return product;
        }
    }


    
}
