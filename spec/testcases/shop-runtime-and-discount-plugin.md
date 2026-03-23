# Shop Runtime 與 Discount Plugin 測試案例

## 狀態

- phase: 1
- status: draft-for-freeze
- 日期：2026-03-23

## Shop Runtime

### TC-RT-001 預設 shop 啟動

- Given: 未指定 `SHOP_ID`，appsettings 內 `DefaultShopId=default`
- When: 啟動 API
- Then: 使用 `default` 對應的 `DatabaseFilePath`

### TC-RT-002 指定 shop-id 啟動

- Given: `SHOP_ID=shop-b`
- When: 啟動 API
- Then: 使用 `shop-b` 對應的 manifest 與資料庫路徑

### TC-RT-003 未知 shop-id 啟動失敗

- Given: `SHOP_ID=missing-shop`
- When: 啟動 API
- Then: 啟動階段直接丟出例外，不可悄悄 fallback 到其他 shop

### TC-RT-004 相對資料庫路徑解析

- Given: manifest 的 `DatabaseFilePath=shop-database.db`
- When: 啟動 API
- Then: 以 `AppContext.BaseDirectory` 為基準解析最終路徑

## Discount Engine

### TC-DC-001 manifest 啟用規則會被執行

- Given: manifest 啟用 `product-1-second-item-40-off`
- And: 購物車內商品 1 數量為 2
- When: 執行 `IDiscountEngine`
- Then: 回傳 1 筆折扣
- And: 折扣金額為 `單價 * -0.4`

### TC-DC-002 manifest 未啟用規則不執行

- Given: manifest 不含 `product-1-second-item-40-off`
- And: 購物車內商品 1 數量為 2
- When: 執行 `IDiscountEngine`
- Then: 回傳 0 筆折扣

### TC-DC-003 多件商品可重複套用

- Given: manifest 啟用 `product-1-second-item-40-off`
- And: 購物車內商品 1 數量為 4
- When: 執行 `IDiscountEngine`
- Then: 回傳 2 筆折扣

### TC-DC-004 manifest 指向不存在的規則

- Given: manifest 啟用 `missing-rule`
- When: 執行 `IDiscountEngine`
- Then: 不丟例外
- And: 只忽略該規則

## API 整合

### TC-API-001 購物車試算改走新引擎

- Given: `CartsController` 已注入 `IDiscountEngine`
- When: 呼叫 `/api/carts/{id}/estimate`
- Then: 回傳的折扣來自 plugin engine

### TC-API-002 checkout 改走新引擎

- Given: `CheckoutController` 已注入 `IDiscountEngine`
- When: 完成結帳
- Then: 訂單內優惠列來自 plugin engine
