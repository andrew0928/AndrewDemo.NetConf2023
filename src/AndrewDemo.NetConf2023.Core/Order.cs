using System.Collections.Generic;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public enum OrderFulfillmentStatus
    {
        Pending = 0,
        Succeeded = 1,
        Failed = 2
    }

    public class Order
    {
        [BsonId]
        public int Id { get; private set; }

        public Member Buyer { get; set; } = null!;

        public List<OrderProductLine> ProductLines { get; set; } = new List<OrderProductLine>();

        public List<OrderDiscountLine> DiscountLines { get; set; } = new List<OrderDiscountLine>();

        public decimal TotalPrice { get; set; }

        public OrderFulfillmentStatus FulfillmentStatus { get; set; } = OrderFulfillmentStatus.Pending;

        public OrderShopNotes? ShopNotes { get; set; }

        public Order(int transactionId)
        {
            Id = transactionId;
        }

        // parameterless constructor required by LiteDB
        private Order()
        {
        }


        public class OrderProductLine
        {
            public string ProductId { get; set; } = string.Empty;
            public string? SkuId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal LineAmount { get; set; }
        }

        public class OrderDiscountLine
        {
            public string RuleId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
        }

        public class OrderShopNotes
        {
            public int? BuyerSatisfaction { get; set; }
            public string? Comments { get; set; }
        }
    }
}
