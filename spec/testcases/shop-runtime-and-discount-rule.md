# Shop Runtime 與 Discount Rule 測試案例

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-03-23

## Shop Runtime

### TC-RT-001 預設 shop 啟動

- Given: 未指定 `SHOP_ID`，appsettings 內 `DefaultShopId=default`
- When: 啟動 API
- Then: 解析出 `default` 的 `ShopManifest`
- And: 使用該 manifest 的 `DatabaseFilePath`

### TC-RT-002 指定 shop-id 啟動

- Given: `SHOP_ID=shop-b`
- When: 啟動 API
- Then: 使用 `shop-b` 對應的 `ShopManifest` 與資料庫路徑

### TC-RT-003 未知 shop-id 啟動失敗

- Given: `SHOP_ID=missing-shop`
- When: 啟動 API
- Then: 啟動階段直接丟出例外，不可悄悄 fallback 到其他 shop

### TC-RT-004 相對資料庫路徑解析

- Given: manifest 的 `DatabaseFilePath=shop-database.db`
- When: 啟動 API
- Then: 以 `AppContext.BaseDirectory` 為基準解析最終路徑

## Cart Context

### TC-CT-001 可由 cart 與 manifest 建立 CartContext

- Given: cart 內有商品與數量
- And: 資料庫可查到對應商品價格與名稱
- When: 執行 `CartContextFactory.Create(...)`
- Then: 回傳 `CartContext`
- And: `ShopId` 來自 `ShopManifest`
- And: `CartContext` 內含 `EvaluatedAt`
- And: `LineItems` 內含 `LineId`、`ParentLineId`、`AddedAt`、`ProductId`、`ProductName`、`UnitPrice`、`Quantity`

### TC-CT-002 Cart 與 CartContext 共用唯讀 LineItem 型別

- Given: `Cart.LineItems` 與 `CartContext.LineItems`
- When: 檢查其型別
- Then: 兩者共用同一個 `LineItem`
- And: raw `Cart.LineItems` 可不帶 `ProductName` / `UnitPrice`
- And: raw `Cart.LineItems` 至少保留 `LineId` / `ParentLineId` / `AddedAt`
- And: `CartContext.LineItems` 必須帶完整價格快照

## Discount Engine

### TC-DC-001 已組裝進 engine 的規則會被執行

- Given: 啟用規則清單包含 `product-1-second-item-40-off`
- And: 購物車內商品 1 數量為 2
- When: 執行 `DiscountEngine`
- Then: 回傳 1 筆折扣
- And: 該記錄 `Kind = Discount`
- And: 折扣金額為 `單價 * -0.4`

### TC-DC-002 未組裝進 engine 的規則不執行

- Given: 啟用規則清單不含 `product-1-second-item-40-off`
- And: 購物車內商品 1 數量為 2
- When: 執行 `DiscountEngine`
- Then: 回傳 0 筆折扣

### TC-DC-003 多件商品可重複套用

- Given: 啟用規則清單包含 `product-1-second-item-40-off`
- And: 購物車內商品 1 數量為 4
- When: 執行 `DiscountEngine`
- Then: 回傳 2 筆折扣

### TC-DC-004 manifest 指向不存在的規則

- Given: manifest 啟用 `missing-rule`
- When: API 啟動時組裝 engine
- Then: 不丟例外
- And: 只忽略該規則

### TC-DC-005 discount rule 可回傳 hint record

- Given: 某條 discount rule 需要滿足額外門檻才成立
- And: 購物車尚未達成折扣條件
- When: 執行 `DiscountEngine`
- Then: 可回傳 `DiscountRecord`
- And: 該記錄 `Kind = Hint`
- And: `Amount = 0`
- And: 若要提示成立後的優惠內容，應寫在 `Name` 或 `Description`

### TC-DC-006 hint record 不影響總價

- Given: `DiscountEngine` 回傳一筆 `Kind = Hint` 的 `DiscountRecord`
- When: API 試算購物車總價
- Then: 不得把該記錄的 `Amount` 加入總價

### TC-DC-006A hint record 若帶非零 amount 應視為 rule 實作錯誤

- Given: 某條 discount rule 回傳一筆 `Kind = Hint` 的 `DiscountRecord`
- And: 該記錄 `Amount != 0`
- When: 執行 `DiscountEngine`
- Then: 應直接丟出錯誤
- And: 不得把該記錄視為有效輸出

### TC-DC-007 discount record 可標記關聯 line ids

- Given: 某條折扣規則同時關聯主商品 line 與贈品 line
- When: 規則回傳 `DiscountRecord`
- Then: `RelatedLineIds` 可同時包含多個 `LineId`
- And: 這些 `LineId` 必須存在於 `CartContext.LineItems`

## API 整合

### TC-API-001 購物車試算改走 CartContextFactory 與新引擎

- Given: `CartsController` 已注入 `ShopManifest` 與 `DiscountEngine`
- When: 呼叫 `/api/carts/{id}/estimate`
- Then: 先建立 `CartContext`
- And: 回傳的折扣來自 plugin engine
- And: response 可區分 `Discount` 與 `Hint`
- And: response 可帶出 `RelatedLineIds`
- And: `TotalPrice` 只加總 `Kind = Discount` 的記錄

### TC-API-002 checkout 改走 CartContextFactory 與新引擎

- Given: `CheckoutController` 已注入 `ShopManifest` 與 `DiscountEngine`
- When: 完成結帳
- Then: 先建立 `CartContext`
- And: 訂單內優惠列來自 plugin engine
- And: 只有 `Kind = Discount` 的記錄可進入訂單折扣列
