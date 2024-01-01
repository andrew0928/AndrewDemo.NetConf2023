using AndrewDemo.NetConf2023.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        /// <summary>
        /// 取得線上商店的所有商品資訊。
        /// </summary>
        /// <returns></returns>
        [HttpGet("", Name = "GetProducts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Product>> Get()
        {
            return Product.Database.Values.ToList();
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
