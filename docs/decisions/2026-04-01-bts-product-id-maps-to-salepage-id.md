# BTS 商業模型中，對外 `Product.Id` 對應 `SalePageId`

## 狀態

- accepted
- 日期：2026-04-01

## 背景

在重新討論 Apple BTS 商業需求後，已確認：

- BTS 不是獨立 `ShopId`
- BTS 是同一個 shop 內的限期 campaign
- 零售內部資料可區分：
  - `SKU`: 實際商品主檔、規格、庫存
  - `SalePage`: 陳列、入口、販售方式、價格
- `.Abstract` / API 不需要直接公開 `SKU`

因此必須先決定：對外公開的 `Product`，到底代表 `SKU` 還是 `SalePage`。

## 決策

### 1. 對外 `Product` 代表 `SalePage projection`

canonical 結論：

- `.Abstract.Product` 是對外可販售頁面的 projection
- 它不是內部 `SKU master`

### 2. `Product.Id = SalePageId`

對外公開的 `Product.Id` 必須對應 `SalePageId`。

因此：

- cart 內加入的 `ProductId` 是 `SalePageId`
- `IProductService.GetProductById(productId)` 查的是 `SalePageId`
- order product line 中保留的公開 product identity 也是 `SalePageId`

### 3. `SKU` 與庫存資訊不對外公開

`SKU` 與庫存屬於內部資料模型。

用途：

- 型號
- 規格
- 實際商品與庫存管理

但它們不應直接進入 `.Abstract.Product`。

### 4. `Member` 維持買家身分語意

公開 `Member` 仍代表買家。

BTS 相關資格則是 member-side 狀態：

- `.edu` 驗證
- 有效期限
- qualification 判定

### 5. `Product.Price` 對外代表 `SalePage` 當下販售價格投影

這讓系統可在不公開內部 `SKU` 的前提下，仍正確表達：

- 一般入口價格
- BTS 主商品價格

收據則可另外用折扣行表達 `BTS 優惠`。

## 影響

- 後續技術規格應以 `Product = SalePage projection` 為基準
- `SKU`、庫存、內部活動規則可留在內部 schema
- `.Abstract` 名詞與前端/API 名詞可維持簡潔

## 替代方案

### 替代方案 A：對外 `Product` 直接代表 `SKU`

缺點：

- 無法乾淨表達不同入口、價格與活動規則
- 會讓 API 直接暴露不需要的內部商品主檔語意

結論：

- 不採用

### 替代方案 B：同時對外公開 `SKU` 與 `SalePage`

缺點：

- `.Abstract` 與 API 名詞變複雜
- 前端與應用層需要理解過多內部模型

結論：

- 不採用

## 後續工作

1. 將 BTS 規格與 testcase 直接以 `Product = SalePage projection` 撰寫。
2. 後續技術規格再決定內部 `SKU` / `SalePage` / inventory schema。
3. `IProductService` 的查詢語意需明確以 `SalePageId` 為主。
