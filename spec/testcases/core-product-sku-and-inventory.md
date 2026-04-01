# `.Core` Product SKU 與 Inventory 標準能力測試案例

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-04-01

## Product / SKU Mapping

### TC-SKU-001 實體商品可在內部對應 `SkuId`

- Given: 一個實體商品 `Product`
- When: `.Core` 載入其內部商品資料
- Then: 可取得對應的 `SkuId`

### TC-SKU-002 非實體商品允許 `SkuId = null`

- Given: 一個非實體商品 `Product`
- When: `.Core` 載入其內部商品資料
- Then: `SkuId` 可為 `null`

### TC-SKU-003 `SkuId` 不應透過產品查詢 API 預設公開

- Given: 一個對外公開的 `Product`
- When: API 或 application 取得 `.Abstract.Product`
- Then: 產品查詢 API 回應不應包含 `SkuId`

## Checkout Inventory Validation

### TC-SKU-101 有 `SkuId` 的商品在 checkout 時必須驗證庫存

- Given: cart line 對應的 product 有 `SkuId`
- When: 執行 checkout
- Then: `.Core` 必須先檢查庫存是否足夠

### TC-SKU-102 `SkuId = null` 的商品在 checkout 時不做實體庫存檢查

- Given: cart line 對應的 product 沒有 `SkuId`
- When: 執行 checkout
- Then: `.Core` 不需要做實體庫存檢查

### TC-SKU-103 庫存不足時 checkout 失敗且保留 transaction

- Given: checkout transaction 存在
- And: cart line 對應的 `SkuId` 庫存不足
- When: 執行 checkout
- Then: checkout 必須失敗
- And: 不得建立 order
- And: checkout transaction 仍保留

## Atomic Inventory Update

### TC-SKU-201 庫存扣減與 order 建立必須在同一個 transaction 內成功

- Given: checkout transaction、cart、buyer、products、inventory 都存在
- And: 庫存足夠
- When: 執行 checkout
- Then: inventory 扣減成功
- And: order 建立成功
- And: checkout transaction 被刪除

### TC-SKU-202 建立 order 前若失敗，inventory 不得部分扣減

- Given: checkout 已開始
- And: inventory 驗證與部分更新已進行
- And: 在 order 建立前發生失敗
- When: checkout rollback
- Then: inventory 不得殘留部分扣減結果
- And: checkout transaction 仍保留

### TC-SKU-203 inventory query 應在 transaction 內避免競爭更新

- Given: 兩個 checkout 同時競爭同一個 `SkuId`
- When: `.Core` 在 transaction 內讀取與更新 inventory
- Then: 不應同時成功扣減超過可用庫存
