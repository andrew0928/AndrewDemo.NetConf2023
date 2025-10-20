namespace AndrewDemo.NetConf2023.Core
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description {  get; set; }
        public decimal Price { get; set; }





        private static Dictionary<int, Product> _database = new Dictionary<int, Product>()
        {
            //{ 1, new Product() { Id = 1, Name = "18天", Price = 65.00m } },
            //{ 2, new Product() { Id = 2, Name = "可樂", Price = 18.00m} }
        };

        [Obsolete("product: cross model data access!")]
        public static Dictionary<int, Product> Database { get { return _database; } }
    }
}