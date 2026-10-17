using System;
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

        // 由 Checkout 建立時承接其用途，與 Buyer 的會員用途分開保存。
        public bool IsTest { get; init; }

        // 對應封存的 Checkout；其他來源或缺少歷史交易紀錄時為 null。
        public int? CheckoutTransactionId { get; init; }

        // 訂單成立時的 UTC 時間；舊資料無可查證時間時為 null，不以讀取時間補值。
        public DateTime? CreatedAt { get; init; }

        public Member Buyer { get; set; } = null!;

        public List<OrderProductLine> ProductLines { get; set; } = new List<OrderProductLine>();

        public List<OrderDiscountLine> DiscountLines { get; set; } = new List<OrderDiscountLine>();

        public decimal TotalPrice { get; set; }

        public OrderFulfillmentStatus FulfillmentStatus { get; set; } = OrderFulfillmentStatus.Pending;

        public OrderShopNotes? ShopNotes { get; set; }

        public Order(int orderId)
        {
            Id = orderId;
        }

        // LiteDB 還原資料時需要無參數建構式。
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
