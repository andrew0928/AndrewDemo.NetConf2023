using System;
using System.IO;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    internal static class LiteDbContext
    {
        private static readonly Lazy<LiteDatabase> _database = new Lazy<LiteDatabase>(CreateDatabase);

        internal static LiteDatabase Database => _database.Value;

    internal static ILiteCollection<Cart> Carts => Database.GetCollection<Cart>("carts");
    internal static ILiteCollection<Product> Products => Database.GetCollection<Product>("products");
    internal static ILiteCollection<Member> Members => Database.GetCollection<Member>("members");
    internal static ILiteCollection<Order> Orders => Database.GetCollection<Order>("orders");
    internal static ILiteCollection<MemberAccessTokenRecord> MemberTokens => Database.GetCollection<MemberAccessTokenRecord>("member_tokens");
    internal static ILiteCollection<CheckoutTransactionRecord> CheckoutTransactions => Database.GetCollection<CheckoutTransactionRecord>("checkout_transactions");

        private static LiteDatabase CreateDatabase()
        {
            var dataDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data");
            Directory.CreateDirectory(dataDirectory);

            var databasePath = Path.Combine(dataDirectory, "andrew-demo.db");

            var connectionString = new ConnectionString
            {
                Filename = databasePath,
                Connection = ConnectionType.Shared
            };

            var database = new LiteDatabase(connectionString);

            // minimal index setup used by current flows
            LiteDbContextExtensions.EnsureIndexes(database);

            return database;
        }
    }

    internal static class LiteDbContextExtensions
    {
        internal static void EnsureIndexes(LiteDatabase database)
        {
            database.GetCollection<Member>("members").EnsureIndex(x => x.Name, unique: true);
            database.GetCollection<Product>("products").EnsureIndex(x => x.Id);
            database.GetCollection<Order>("orders").EnsureIndex(x => x.Buyer.Id);
            database.GetCollection<MemberAccessTokenRecord>("member_tokens").EnsureIndex(x => x.MemberId);
            database.GetCollection<CheckoutTransactionRecord>("checkout_transactions").EnsureIndex(x => x.MemberId);
        }
    }

    internal class MemberAccessTokenRecord
    {
        [BsonId]
        public string Token { get; set; } = string.Empty;
        public DateTime Expire { get; set; }
        public int MemberId { get; set; }
    }

    internal class CheckoutTransactionRecord
    {
        [BsonId(true)]
        public int TransactionId { get; set; }
        public int CartId { get; set; }
        public int MemberId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
