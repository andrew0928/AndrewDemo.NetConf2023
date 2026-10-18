using System;
using System.Collections.Generic;
using System.IO;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Orders;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.Core
{
    public interface IShopDatabaseContext : IDisposable
    {
        ILiteDatabase Database { get; }
        ILiteCollection<Cart> Carts { get; }
        // 基礎儲存入口；標準維護走 ProductMaintenanceService，扣庫存走 CheckoutService 的效果邊界。
        ILiteCollection<Product> Products { get; }
        ILiteCollection<SkuRecord> Skus { get; }
        ILiteCollection<InventoryRecord> InventoryRecords { get; }
        // 基礎儲存入口；標準應用的會員／Token 寫入必須經過 MemberService。
        ILiteCollection<Member> Members { get; }
        ILiteCollection<Order> Orders { get; }
        ILiteCollection<MemberAccessTokenRecord> MemberTokens { get; }
        ILiteCollection<CheckoutTransactionRecord> CheckoutTransactions { get; }
        ILiteCollection<CheckoutTransactionRecord> ArchivedCheckoutTransactions { get; }
    }

    public sealed class ShopDatabaseContext : IShopDatabaseContext
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Cart> _carts;
        private readonly ILiteCollection<Product> _products;
        private readonly ILiteCollection<SkuRecord> _skus;
        private readonly ILiteCollection<InventoryRecord> _inventoryRecords;
        private readonly ILiteCollection<Member> _members;
        private readonly ILiteCollection<Order> _orders;
        private readonly ILiteCollection<MemberAccessTokenRecord> _memberTokens;
        private readonly ILiteCollection<CheckoutTransactionRecord> _checkoutTransactions;
        private readonly ILiteCollection<CheckoutTransactionRecord> _archivedCheckoutTransactions;
        private bool _disposed;

        public ShopDatabaseContext(ShopDatabaseOptions? options = null, ILogger<ShopDatabaseContext>? logger = null)
        {
            var resolvedOptions = options ?? new ShopDatabaseOptions();
            var connection = ResolveConnection(resolvedOptions);
            _database = new LiteDatabase(connection);

            _carts = _database.GetCollection<Cart>("carts");
            _products = _database.GetCollection<Product>("products");
            _skus = _database.GetCollection<SkuRecord>("skus");
            _inventoryRecords = _database.GetCollection<InventoryRecord>("inventory_records");
            _members = _database.GetCollection<Member>("members");
            _orders = OrderCollectionGuard.Wrap(_database.GetCollection<Order>("orders"), logger);
            _memberTokens = _database.GetCollection<MemberAccessTokenRecord>("member_tokens");
            _checkoutTransactions = CheckoutCollectionGuard.Wrap(_database.GetCollection<CheckoutTransactionRecord>("checkout_transactions"),
                _database.GetCollection<CheckoutTransactionRecord>("archived_checkout_transactions"), logger);
            _archivedCheckoutTransactions = CheckoutCollectionGuard.Wrap(_database.GetCollection<CheckoutTransactionRecord>("archived_checkout_transactions"), logger: logger);

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

        public ILiteCollection<SkuRecord> Skus
        {
            get
            {
                ThrowIfDisposed();
                return _skus;
            }
        }

        public ILiteCollection<InventoryRecord> InventoryRecords
        {
            get
            {
                ThrowIfDisposed();
                return _inventoryRecords;
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

        public ILiteCollection<CheckoutTransactionRecord> ArchivedCheckoutTransactions
        {
            get
            {
                ThrowIfDisposed();
                return _archivedCheckoutTransactions;
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
            database.GetCollection<SkuRecord>("skus").EnsureIndex(x => x.SkuId, unique: true);
            database.GetCollection<InventoryRecord>("inventory_records").EnsureIndex(x => x.SkuId, unique: true);
            database.GetCollection<Order>("orders").EnsureIndex(x => x.Buyer.Id);
            database.GetCollection<MemberAccessTokenRecord>("member_tokens").EnsureIndex(x => x.MemberId);
            database.GetCollection<CheckoutTransactionRecord>("checkout_transactions").EnsureIndex(x => x.MemberId);
        }
    }

    [Obsolete("請使用 IShopDatabaseContext 取代 ShopDatabase.Current，避免靜態依賴")]
    public static class ShopDatabase
    {
        private static readonly object SyncRoot = new();
        private static IShopDatabaseContext? _current;

        public static IShopDatabaseContext Current
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

        public static void Use(IShopDatabaseContext context)
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

        public static T Create<T>() where T : class, new()
        {
            return Create(new T());
        }

        public static T Create<T>(T entity) where T : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (entity is Member or MemberAccessTokenRecord or CheckoutTransactionRecord or Order
                or Product or SkuRecord or InventoryRecord)
                throw new InvalidOperationException("用途固定或受保護物件必須透過對應 Core 服務／受控初始化建立。");
            var context = Current;
            var collection = GetCollection<T>(context);
            collection.Insert(entity);
            return entity;
        }

        private static ILiteCollection<T> GetCollection<T>(IShopDatabaseContext context) where T : class
        {
            if (typeof(T) == typeof(Cart)) return (ILiteCollection<T>)context.Carts;
            if (typeof(T) == typeof(Product)) return (ILiteCollection<T>)context.Products;
            if (typeof(T) == typeof(SkuRecord)) return (ILiteCollection<T>)context.Skus;
            if (typeof(T) == typeof(InventoryRecord)) return (ILiteCollection<T>)context.InventoryRecords;
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
                var context = new ShopDatabaseContext(options, sp.GetService<ILogger<ShopDatabaseContext>>());
                ShopDatabase.Use(context);
                return context;
            });

            return services;
        }

        public static IServiceCollection AddShopDatabase(this IServiceCollection services, Func<IServiceProvider, ShopDatabaseOptions> configure)
        {
            services.AddSingleton<IShopDatabaseContext>(sp =>
            {
                var options = configure(sp) ?? throw new InvalidOperationException("shop database options cannot be null");
                var context = new ShopDatabaseContext(options, sp.GetService<ILogger<ShopDatabaseContext>>());
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
        // 簽發時由 Member.IsTest 複製；舊資料缺少欄位時為 false。
        public bool IsTest { get; init; }
    }

    public class CheckoutTransactionRecord
    {
        [BsonId(true)]
        public int TransactionId { get; set; }
        public int CartId { get; set; }
        public int MemberId { get; set; }
        public DateTime CreatedAt { get; set; }

        // 完成或取消時記錄的 UTC 結束時間；作用中或舊資料未知時為 null。
        public DateTime? ClosedAt { get; set; }

        public CheckoutState State { get; set; } = CheckoutState.Unknown;

        // create 時計價並固定；null 代表舊資料沒有金額，不等同免費交易的 0。
        public decimal? TotalPrice { get; init; }
        public List<CheckoutProductLine> ProductLines { get; init; } = new();
        public List<CheckoutDiscountLine> DiscountLines { get; init; } = new();

        // 建立時依 Member 與 PaymentId 白名單判定，封存時保留原值。
        public bool IsTest { get; init; }

        // 建立 Checkout 時指定，完成交易及封存後沿用同一付款來源。
        public int PaymentId { get; init; }
    }
}
