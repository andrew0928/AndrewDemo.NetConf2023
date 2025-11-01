using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{

    public class Order
    {
        [BsonId]
        public int Id { get; private set; }

        public Member Buyer { get; set; } = null!;

        public List<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();

        public decimal TotalPrice { get; set; }

        public OrderShopNotes? ShopNotes { get; set; }

        public Order(int transactionId)
        {
            Id = transactionId;
        }

        // parameterless constructor required by LiteDB
        private Order()
        {
        }

        [Obsolete("請改用 IShopDatabaseContext.Orders 查詢會員訂單。")]
        public static IEnumerable<Order> GetOrders(int memberId)
        {
            return ShopDatabase.Current.Orders.Find(o => o.Buyer.Id == memberId);
        }

        [Obsolete("請改用 IShopDatabaseContext.Orders.Upsert 儲存訂單。")]
        internal static void Upsert(Order order)
        {
            ShopDatabase.Current.Orders.Upsert(order);
        }


        public class OrderLineItem
        {
            public string Title { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        public class OrderShopNotes
        {
            public int BuyerSatisfaction { get; set; }
            public string? Comments { get; set; }
        }
    }
}