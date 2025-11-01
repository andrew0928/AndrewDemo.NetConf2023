using System;
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

        [Obsolete("請改用 IShopDatabaseContext.Products.FindById 取得商品資料。")]
        public static Product? GetById(int id)
        {
            return ShopDatabase.Current.Products.FindById(id);
        }

        [Obsolete("請改用 IShopDatabaseContext.Products.FindAll 查詢商品。")]
        public static IReadOnlyList<Product> GetAll()
        {
            return ShopDatabase.Current.Products.FindAll().ToList();
        }

        [Obsolete("請改用 IShopDatabaseContext.Products.Upsert 進行寫入。")]
        public static void Upsert(Product product)
        {
            ShopDatabase.Current.Products.Upsert(product);
        }

        [Obsolete("請改用 IShopDatabaseContext.Products.Exists 或 Query。")]
        public static bool Exists(int id)
        {
            return ShopDatabase.Current.Products.Exists(p => p.Id == id);
        }

        [Obsolete("請改用 IShopDatabaseContext.Products.Delete。")]
        public static void Delete(int id)
        {
            ShopDatabase.Current.Products.Delete(id);
        }
    }
}