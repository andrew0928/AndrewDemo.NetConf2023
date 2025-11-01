using System;
using System.IO;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewDemo.NetConf2023.Core
{
    internal interface IShopDatabaseContext : IDisposable
    {
        ILiteDatabase Database { get; }
        ILiteCollection<Cart> Carts { get; }
        ILiteCollection<Product> Products { get; }
        ILiteCollection<Member> Members { get; }
        ILiteCollection<Order> Orders { get; }
        ILiteCollection<MemberAccessTokenRecord> MemberTokens { get; }
        ILiteCollection<CheckoutTransactionRecord> CheckoutTransactions { get; }
    }

    public sealed class ShopDatabaseContext : IShopDatabaseContext
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Cart> _carts;
        private readonly ILiteCollection<Product> _products;
        private readonly ILiteCollection<Member> _members;
        private readonly ILiteCollection<Order> _orders;
        private readonly ILiteCollection<MemberAccessTokenRecord> _memberTokens;
        private readonly ILiteCollection<CheckoutTransactionRecord> _checkoutTransactions;
        private bool _disposed;

        public ShopDatabaseContext(ShopDatabaseOptions? options = null)
        {
            var resolvedOptions = options ?? new ShopDatabaseOptions();
            var connection = ResolveConnection(resolvedOptions);
            _database = new LiteDatabase(connection);

            _carts = _database.GetCollection<Cart>("carts");
            _products = _database.GetCollection<Product>("products");
            _members = _database.GetCollection<Member>("members");
            _orders = _database.GetCollection<Order>("orders");
            _memberTokens = _database.GetCollection<MemberAccessTokenRecord>("member_tokens");
            _checkoutTransactions = _database.GetCollection<CheckoutTransactionRecord>("checkout_transactions");

            EnsureIndexes(_database);
        }
        public ILiteDatabase Database
        {
            get
            {
                ThrowIfDisposed();
                return _database;
            }
        }

        public ILiteCollection<Cart> Carts
        {
            get
            {
                ThrowIfDisposed();
                return _carts;
            }
        }

        public ILiteCollection<Product> Products
        {
            get
            {
                ThrowIfDisposed();
                return _products;
            }
        }

        public ILiteCollection<Member> Members
        {
            get
            {
                ThrowIfDisposed();
                return _members;
            }
        }

        public ILiteCollection<Order> Orders
        {
            get
            {
                ThrowIfDisposed();
                return _orders;
            }
        }

    public ILiteCollection<MemberAccessTokenRecord> MemberTokens
        {
            get
            {
                ThrowIfDisposed();
                return _memberTokens;
            }
        }

    public ILiteCollection<CheckoutTransactionRecord> CheckoutTransactions
        {
            get
            {
                ThrowIfDisposed();
                return _checkoutTransactions;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _database.Dispose();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ShopDatabaseContext));
            }
        }

        private static ConnectionString ResolveConnection(ShopDatabaseOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return new ConnectionString(options.ConnectionString);
            }

            var dataDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data");
            Directory.CreateDirectory(dataDirectory);
            var databasePath = Path.Combine(dataDirectory, "andrew-demo.db");

            return new ConnectionString
            {
                Filename = databasePath,
                Connection = ConnectionType.Shared
            };
        }

        private static void EnsureIndexes(LiteDatabase database)
        {
            database.GetCollection<Member>("members").EnsureIndex(x => x.Name, unique: true);
            database.GetCollection<Product>("products").EnsureIndex(x => x.Id);
            database.GetCollection<Order>("orders").EnsureIndex(x => x.Buyer.Id);
            database.GetCollection<MemberAccessTokenRecord>("member_tokens").EnsureIndex(x => x.MemberId);
            database.GetCollection<CheckoutTransactionRecord>("checkout_transactions").EnsureIndex(x => x.MemberId);
        }
    }

    internal static class ShopDatabase
    {
        private static readonly object SyncRoot = new();
        private static IShopDatabaseContext? _current;

        internal static IShopDatabaseContext Current
        {
            get
            {
                if (_current != null)
                {
                    return _current;
                }

                lock (SyncRoot)
                {
                    _current ??= new ShopDatabaseContext();

                    return _current;
                }
            }
        }

        internal static void Use(IShopDatabaseContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            lock (SyncRoot)
            {
                _current = context;
            }
        }

        internal static T Create<T>() where T : class, new()
        {
            return Create(new T());
        }

        internal static T Create<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var context = Current;
            var collection = GetCollection<T>(context);
            collection.Insert(entity);
            return entity;
        }

        private static ILiteCollection<T> GetCollection<T>(IShopDatabaseContext context) where T : class
        {
            if (typeof(T) == typeof(Cart)) return (ILiteCollection<T>)context.Carts;
            if (typeof(T) == typeof(Product)) return (ILiteCollection<T>)context.Products;
            if (typeof(T) == typeof(Member)) return (ILiteCollection<T>)context.Members;
            if (typeof(T) == typeof(Order)) return (ILiteCollection<T>)context.Orders;
            if (typeof(T) == typeof(MemberAccessTokenRecord)) return (ILiteCollection<T>)context.MemberTokens;
            if (typeof(T) == typeof(CheckoutTransactionRecord)) return (ILiteCollection<T>)context.CheckoutTransactions;

            return context.Database.GetCollection<T>();
        }
    }

    public static class ShopDatabaseServiceCollectionExtensions
    {
        public static IServiceCollection AddShopDatabase(this IServiceCollection services, Action<ShopDatabaseOptions>? configure = null)
        {
            services.AddSingleton<IShopDatabaseContext>(sp =>
            {
                var options = new ShopDatabaseOptions();
                configure?.Invoke(options);
                var context = new ShopDatabaseContext(options);
                ShopDatabase.Use(context);
                return context;
            });

            return services;
        }
    }

    public sealed class ShopDatabaseOptions
    {
        public string? ConnectionString { get; set; }
    }

    public class MemberAccessTokenRecord
    {
        [BsonId]
        public string Token { get; set; } = string.Empty;
        public DateTime Expire { get; set; }
        public int MemberId { get; set; }
    }

    public class CheckoutTransactionRecord
    {
        [BsonId(true)]
        public int TransactionId { get; set; }
        public int CartId { get; set; }
        public int MemberId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
