# Apple BTS Campaign 技術邊界與 Cart/Projection 重構方向

## 狀態

- superseded
- 日期：2026-04-01
- superseded-by:
  - [2026-04-01-bts-single-salepage-and-price-delta-discount.md](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-01-bts-single-salepage-and-price-delta-discount.md)
  - [2026-04-01-discount-record-kind-and-related-lines.md](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-01-discount-record-kind-and-related-lines.md)

## 背景

已確認的商業決策如下：

- BTS 不是獨立 `ShopId`
- BTS 是同一個 shop 內的限期 campaign
- 對外 `Product.Id = SalePageId`
- `SKU` 與庫存不對外公開，但屬於 `.Core` 的標準能力

在這個前提下，現行 repo 還有三個明顯落差：

1. `.Core` 雖然已有 `products` collection，但還缺少 sidecar merge 與 extension repository 邊界
2. `Cart` 仍使用 `ProdQtyMap<string, int>`
3. `IDiscountRule` 的輸入 `CartContext` 還缺少 line-level identity 與 evaluation time

## 決策

### 1. `IProductService` 應保留公開 contract，`Product` 直接保存 nullable `SkuId`

canonical 方向：

- 對外仍維持 `IProductService`
- 對外仍回傳 `.Abstract.Product`
- `products` collection 先視為 `SalePage` 主資料
- `SkuId` 直接存在於 `Product`
- 其他 BTS-specific 規則再由 sidecar collections 補足

### 2. `Cart` 應從 `ProdQtyMap` 改為 line-based aggregate

canonical 方向：

- 每個加入購物車的 line 都應有自己的 identity
- gift line 應可明確連回 main line
- cart persistence 應保存加入時間與 line relation

### 3. `AppleBTS Extension` 應集中 BTS-specific records / repositories / rules

canonical 方向：

- `.Core` 只保留通用 cart / checkout / product service 擴充點
- `.Core` 同時擁有通用 `SKU` / inventory 能力
- `AppleBTS Extension` 定義 BTS sidecar records
- `AppleBTS Extension` 定義 qualification service 與 `BtsDiscountRule`

### 4. 若 BTS 折扣仍由 `IDiscountRule` 承接，則 `CartContext` 必須補足 line-based context

目前建議：

- `CartContext` 補 `EvaluatedAt`
- `LineItem` 補 `LineId`
- `LineItem` 補 `ParentLineId`

這些欄位不是為了把 cart contract 做成 BTS 專用，而是讓折扣引擎有足夠資訊處理 line-based promotion。

### 5. `.Core` checkout 應負責 transactional inventory correctness

canonical 方向：

- 若 product 有 `SkuId`，checkout 必須檢查庫存
- inventory 扣減、order 建立、checkout transaction 刪除，應落在同一個 transaction 邊界
- 非實體商品允許 `SkuId = null`

### 6. correctness-critical 資料應維持 typed sidecar，不採 schema-free blob

必須 typed 的資料包括：

- `bts-price`
- 活動時間窗
- member 驗證有效期限
- main / gift relation

## 影響

- Phase 1 很可能需要重新確認 `.Abstract.Carts` contract
- `.Core` cart line-based refactor 已被視為 BTS 之前置工作，不再算作 BTS 專屬修正
- Phase 2 的 `.Core` 需要優先重構 `ShopDatabaseContext`、`Cart`、`IProductService`、inventory transaction 行為
- `AppleBTS Extension` 會是獨立 project 或至少獨立 module
- API host 暫不列入這一輪主線

## 替代方案

### 替代方案 A：維持目前 `products` collection + `ProdQtyMap`

缺點：

- 無法正確表示多組 BTS 主商品 / 贈品組合
- pairing 只能靠推測
- checkout 與 estimate 容易出現規則漂移

結論：

- 不建議

### 替代方案 B：不讓 BTS 走 `IDiscountRule`，只在 checkout service 內直接改價

缺點：

- cart estimate 與 checkout 會有兩套計價邏輯
- 收據的 `BTS 優惠` 也會缺乏單一來源

結論：

- 不建議

## 後續工作

1. 確認是否在 Phase 1 重開 `.Abstract.Carts` 的 contract 調整。
2. 確認 `.Core` 的 `Product.SkuId?`、`skus`、`inventory_records` 正式 spec。
3. 確認 `bts_main_offers`、`bts_gift_options`、`member_education_verifications` 的正式 spec。
4. 先進入 `.Core` 與 `AppleBTS Extension` 設計與重構，再回頭看 API host 是否需要調整。
