# DiscountRecord 擴充為 discount/hint 單一型別

## 狀態

- accepted
- 日期：2026-04-01

## 背景

目前 `DiscountEngine` 與 `IDiscountRule` 只會回傳 `DiscountRecord`，consumer 也直接把 `Amount` 加進總價。這對已成立折扣沒有問題，但無法表達：

- 還差一點就可成立的促銷提示
- BTS 活動已失效、資格不符等「應提示但不該改價」的狀態
- 一筆折扣或提示同時關聯哪些 cart line

若另外新增 `DiscountHintRecord` 或新的 evaluation result 型別，會擴大 `.Abstract` 與 consumer 的變更面。

## 決策

- 維持 `IDiscountRule.Evaluate(CartContext)` 的回傳型別不變
- 維持 `DiscountEngine.Evaluate(CartContext)` 的回傳型別不變
- 只擴充既有 `DiscountRecord`

`DiscountRecord` 應新增：

- `Kind`
  - `Discount`
  - `Hint`
- `RelatedLineIds`

語意規則：

- `Kind = Discount` 時：
  - `Amount` 會影響總價
  - 訂單折扣列只保存這類記錄
- `Kind = Hint` 時：
  - `Amount = 0`
  - 不影響總價
  - 可用來提示條件不足、活動失效等資訊
- `RelatedLineIds` 代表該記錄關聯的 `CartContext.LineItems[*].LineId`

## 影響

- `.Abstract.Discounts` 需要重開 Phase 1 調整 contract
- `.Core` 與 `.API` 消費 `DiscountRecord` 時，必須只把 `Kind = Discount` 視為有效金額
- cart estimate 可直接利用同一型別回傳 discount 與 hint
- BTS 可用 `Hint` 表達「已無 BTS 優惠」而不必先引入 checkout validation hook

## 替代方案

### 1. 新增 `DiscountHintRecord`

不採用。型別會更清楚，但 contract 與 consumer 的擴充面更大。

### 2. 新增 `DiscountEvaluationResult`

本輪不採用。這會牽動 `IDiscountRule`、`DiscountEngine` 與所有 consumer 的回傳型別。

## 後續工作

1. 更新 `/spec` 與 `/spec/testcases` 中的 discount contract。
2. 之後若進入實作，需同步更新 `CartsController`、`CheckoutService` 與所有 discount rules。
3. AppleBTS 之後應優先用 `Hint` 表達資格不符或活動失效，而非先擴充 checkout blocking hook。
