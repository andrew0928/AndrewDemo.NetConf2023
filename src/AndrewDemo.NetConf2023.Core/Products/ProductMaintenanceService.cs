using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Core.Effects;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Products;

/// <summary>
/// 受控的 Core 維護入口，沒有對外 API。呼叫端必須提供權限檢查；test 不略過授權或資料驗證。
/// 資料庫的原始集合僅供基礎設施、初始化及明確排除本輪的擴充模組使用。
/// </summary>
public sealed class ProductMaintenanceService(IShopDatabaseContext database, ProtectedWriteExecutor writes,
    OperationEffects effects)
{
    public void UpdatePrice(string productId, decimal price, Action<IdentitySnapshot> authorize)
    {
        effects.Clear();
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentNullException.ThrowIfNull(authorize);
        writes.ForPrice(productId, price, authorize, () =>
        {
            ArgumentOutOfRangeException.ThrowIfNegative(price);
            if (database.Products.FindById(productId) == null) throw new KeyNotFoundException("商品不存在。");
        }, () => Update("products", productId, "{ Price: @0 }", new BsonValue(price)));
    }

    public void UpdateSpecification(string skuId, string specification, Action<IdentitySnapshot> authorize)
    {
        effects.Clear();
        ArgumentException.ThrowIfNullOrWhiteSpace(skuId);
        ArgumentNullException.ThrowIfNull(authorize);
        writes.ForIdentity("sku.update", "sku", skuId, specification, identity =>
        {
            authorize(identity);
            ArgumentException.ThrowIfNullOrWhiteSpace(specification);
            if (database.Skus.FindById(skuId) == null) throw new KeyNotFoundException("SKU 不存在。");
        }, () => Update("skus", skuId, "{ SpecificationSummary: @0 }", new BsonValue(specification)));
    }

    private void Update(string collection, string id, string expression, BsonValue value)
    {
        // 僅修改要求欄位，保留其他資料；目標若已移除就拒絕，不以 upsert 重建。
        if (database.Database.GetCollection(collection).UpdateMany(BsonExpression.Create(expression, value),
            BsonExpression.Create("_id = @0", new BsonValue(id))) != 1)
            throw new KeyNotFoundException("維護目標已不存在。");
    }
}
