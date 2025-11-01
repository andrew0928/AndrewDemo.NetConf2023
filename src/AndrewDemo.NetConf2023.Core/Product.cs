using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Product
    {
        [BsonId(true)]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }

        public static Product? GetById(int id)
        {
            return ShopDatabase.Current.Products.FindById(id);
        }

        public static IReadOnlyList<Product> GetAll()
        {
            return ShopDatabase.Current.Products.FindAll().ToList();
        }

        public static void Upsert(Product product)
        {
            ShopDatabase.Current.Products.Upsert(product);
        }

        public static bool Exists(int id)
        {
            return ShopDatabase.Current.Products.Exists(p => p.Id == id);
        }

        public static void Delete(int id)
        {
            ShopDatabase.Current.Products.Delete(id);
        }
    }
}