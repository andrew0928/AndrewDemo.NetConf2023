using System.Text.Json.Serialization;

namespace AndrewDemo.NetConf2023.Core
{

    public class Order
    {
        public static Dictionary<int, Order> _database = new Dictionary<int, Order>();

        public Order(int transactionId)
        {
            this.Id = transactionId;
            _database.Add(transactionId, this);
        }



        public int Id { get; private set; }

        public Member buyer { get; set; }

        public List<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();

        public decimal TotalPrice { get; set; }


        public static IEnumerable<Order> GetOrders(int memberId)
        {
            return _database.Values.Where(x => x.buyer.Id == memberId);
        }


        public class OrderLineItem
        {
            public string Title { get; set; }
            public decimal Price { get; set; }
        }
    }
}