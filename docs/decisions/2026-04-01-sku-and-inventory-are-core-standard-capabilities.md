# SKU 與 Inventory 屬於 `.Core` 的標準能力

## 狀態

- accepted
- 日期：2026-04-01

## 背景

在 Apple BTS 討論過程中，已經確認：

- `Product.Id = SalePageId`
- `SKU` 與庫存是內部模型
- AppleBTS 只是同一個 shop 內的 campaign

接著需要決定：`SKU` / inventory 應該屬於 `.Core` 還是 AppleBTS Extension。

## 決策

### 1. SKU 與 inventory 屬於 `.Core` 的標準功能

canonical 結論：

- `SKU` / inventory 不是 AppleBTS 專屬能力
- 它們是 `.Core` 的通用商品能力
- 其他 future extension 也可依賴這套能力

### 2. `Product` 在內部應可關聯 nullable `SkuId`

規則：

- 實體商品應有 `SkuId`
- 非實體商品允許 `SkuId = null`
- `SkuId` 可直接存在於 `Product`
- 但不應作為產品查詢 API 的預設公開欄位

### 3. checkout 標準流程必須驗證庫存

若商品有 `SkuId`：

- checkout 必須確認庫存足夠
- 若不足，checkout 失敗

若商品沒有 `SkuId`：

- checkout 不做實體庫存檢查

### 4. inventory 扣減與 order 建立必須落在同一個資料庫 transaction 邊界

目前 repo 使用 `LiteDB 5.0.17`，可利用：

- `BeginTrans`
- `Commit`
- `Rollback`

並在必要時用 `ForUpdate` 進行 write-mode query，避免 checkout 併發時讀到不安全的舊庫存。

## 影響

- `.Core` 需要新增 `Sku` / `Inventory` 相關 internal model 與 repository
- `CheckoutService` 的責任會擴大為 inventory correctness owner
- `AppleBTS Extension` 不應自行擁有獨立的 SKU / inventory 機制
- `Product` 本體需能保存 nullable `SkuId`

## 替代方案

### 替代方案 A：將 SKU / inventory 放在 AppleBTS Extension

缺點：

- 會把通用商品能力誤建模成 campaign 專屬功能
- 未來其他 extension 仍會重複發明一套庫存機制

結論：

- 不採用

### 替代方案 B：所有 product 都強制必須有 `SkuId`

缺點：

- 無法自然支撐非實體商品
- 會讓「不需庫存管理」的產品型別被迫塞進實體商品模型

結論：

- 不採用

## 後續工作

1. 在 `/spec` 補上 `.Core` SKU / inventory 正式規格與 testcase。
2. 在 `.Core` 設計 line-based cart 與 transactional checkout。
3. 再讓 AppleBTS Extension 建立在這套標準能力之上。
