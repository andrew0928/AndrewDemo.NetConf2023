using System.Collections.Generic;
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