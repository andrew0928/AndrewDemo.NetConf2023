# `.Core` Product SKU 與 Inventory 標準能力規格

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-04-01；2026-10-17 同步 [TiP Phase 1](tip-phase1.md) 的用途與封存要求。

## 範圍

本規格定義 `.Core` 的標準商品主檔能力：

1. `Product` 對內可關聯 `SkuId`
2. `SkuId` 屬於內部資料，不應成為對外 API 預設公開欄位
3. checkout 標準流程必須驗證庫存
4. checkout 對庫存的扣減必須在資料庫 transaction 內完成
5. 非實體商品允許 `SkuId = null`

本規格暫不涵蓋：

- 對外 API 是否公開 `SkuId`
- UI / API 層如何顯示庫存
- Apple BTS 的 qualification 與活動規則
- 倉庫、批次、預留庫存、跨倉調撥

## Canonical 術語

- `Product`: 對外公開的可販售頁面 projection；`Product.Id = SalePageId`
- `SKU`: 內部商品主檔與實體庫存管理單位
- `SkuId`: 內部識別碼，可為 null
- `InventoryRecord`: 內部庫存記錄
- `StockTracked Product`: 有 `SkuId` 的商品
- `NonStock Product`: `SkuId = null` 的商品

## 規格

### 1. SKU 是 `.Core` 的標準能力，不是 AppleBTS Extension 專屬能力

canonical 結論：

- `SKU` / inventory 是 `.Core` 的通用商品能力
- `AppleBTS Extension` 可以依賴它，但不擁有它

### 2. `Product` 對內應可關聯 nullable `SkuId`

規則：

- 每個 `Product` 在內部可對應一個 `SkuId`
- 若商品屬於實體商品，`SkuId` 應有值
- 若商品屬於非實體商品或不需庫存管理的項目，`SkuId` 可為 `null`

### 3. `SkuId` 可存在於 `.Abstract.Product`，但不應作為對外 API 預設公開欄位

規則：

- `SkuId` 是內部資料模型
- 可直接保存於 `Product`
- 但產品查詢 API 預設不應輸出 `SkuId`

### 4. checkout 標準流程必須檢查庫存

若 cart line 對應的 product 有 `SkuId`：

- checkout 必須檢查該 `SkuId` 是否有足夠庫存
- 若庫存不足，checkout 必須失敗
- 不得建立 order
- 不得刪除 checkout transaction 或產生成功封存
- test 交易仍須檢查庫存，不因略過扣減就直接成功

若 cart line 對應的 product 沒有 `SkuId`：

- checkout 不需要做實體庫存檢查

### 5. 庫存扣減必須與 order 建立使用同一個 transaction 邊界

checkout 標準正確性要求：

1. 載入 checkout transaction
2. 驗證 buyer
3. 載入 cart 與 products
4. 驗證有 `SkuId` 的商品庫存足夠
5. 正式交易寫入 inventory 扣減；test 交易略過扣減並記錄實際略過
6. 建立 order，保存 IsTest 與 CheckoutTransactionId
7. 保存原 checkout transaction 到封存區
8. 移除作用中 checkout transaction
9. commit

庫存驗證及所有資料異動須在一致的資料庫 transaction 邊界內；同時再次確認 Checkout 尚未完成。完整 TiP 規則與實作驗收階段見 [標準流程](tip-standard-flow.md)。

若任一步失敗：

- rollback
- transaction 保留
- order 不成立，封存不留下部分紀錄
- inventory 不得被部分扣減

### 6. LiteDB 實作前提

repo 目前使用 `LiteDB 5.0.17`。

本規格要求 `.Core` 的庫存操作建立在 LiteDB 提供的標準 transaction 能力上：

- `ILiteDatabase.BeginTrans()`
- `ILiteDatabase.Commit()`
- `ILiteDatabase.Rollback()`

若需要避免讀寫競爭，inventory query 應在 transaction 內使用 write-mode query，例如：

- `LiteQueryable.ForUpdate()`

## 資料模型方向

本階段建議的內部資料形狀：

- `products`
  - 公開 product / salepage 主資料
- `skus`
  - `SkuId`
  - 型號、規格、實體商品主檔資訊
- `inventory_records`
  - `SkuId`
  - `AvailableQuantity`
  - 必要時可加 `UpdatedAt`

## 非目標

- 本規格不要求先實作 inventory reservation / hold
- 本規格不定義多倉庫與配送演算法
