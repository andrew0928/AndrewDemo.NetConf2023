# Line-based Cart 屬於 `.Core` 主線重構

## 狀態

- accepted
- 日期：2026-04-01

## 背景

在 Apple BTS 規劃過程中，發現目前 `.Core.Cart` 以 `ProdQtyMap<ProductId, Qty>` 作為 persistence model，會造成：

- 相同 `ProductId` 自動被合併
- line identity 消失
- line-to-line relation 無法保存

這些問題會影響 BTS，但本質上不只 BTS：

- 任何 bundle / attach / add-on / dependent line 類型都會受影響
- checkout 與 discount 也缺少穩定的 line-based input

## 決策

### 1. `Cart` 改為 line-based aggregate 是 `.Core` 主線重構

canonical 結論：

- 這不是 Apple BTS 專案專用修正
- 這是 `.Core` 的通用基礎重構
- 應先完成，再讓 AppleBTS Extension 建立在其上

### 2. `.Abstract.Carts` contract 應補齊 line identity 與 evaluation time

本階段接受的最小 contract 方向：

- `CartContext` 補 `EvaluatedAt`
- `LineItem` 補 `LineId`
- `LineItem` 補 `ParentLineId`
- `LineItem` 補 `AddedAt`

### 3. 本階段不先把 line role taxonomy 寫死進 `.Abstract`

canonical 結論：

- 先凍結 line identity 與 parent relation
- `LineRole` / `PromotionType` 後續若真的穩定，再重開 spec 討論

## 影響

- `.Abstract.Carts` 需要變更
- `.Core.Cart` persistence model 需要從 map 改為 line collection
- `CartContextFactory` 需要保留 line metadata
- `DiscountEngine` 與 `CheckoutService` 後續都可建立在同一套 line-based context 上
- AppleBTS Extension 應改為依賴這個能力，而不是反向驅動 `.Core`

## 替代方案

### 替代方案 A：維持 `ProdQtyMap`

缺點：

- 所有需要 line relation 的需求都會繼續失真
- `.Core` 會被迫把 promotion pairing 放到外層硬拼

結論：

- 不採用

### 替代方案 B：只在 AppleBTS Extension 內另外維護 line relation

缺點：

- 會把 `.Core` 的缺口藏到 extension
- 未來其他 promotion 類型仍會重複踩到同一個問題

結論：

- 不採用

## 後續工作

1. 在 `/spec` 與 `/spec/testcases` 正式寫下 line-based cart 規格。
2. 同步更新 `.Abstract.Carts` contract。
3. Phase 2 再重構 `.Core.Cart`、`CartContextFactory`、`CheckoutService`。
