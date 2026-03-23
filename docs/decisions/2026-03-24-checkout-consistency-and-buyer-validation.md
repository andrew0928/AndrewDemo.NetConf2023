# Checkout 交易一致性與 Buyer 驗證修正

## 狀態

- accepted
- 日期：2026-03-24

## 背景

`CheckoutService` 已經在 Phase 2 搬移完成，但搬移階段刻意保留了兩個既有缺失：

- checkout transaction 會在建立 order 前先被刪除
- access token 對應的 member 未驗證是否等於 transaction 的 buyer

這兩點都會直接影響 checkout correctness：

- 若建立 order 前失敗，transaction 已消失，無法安全重試
- 若 buyer 驗證缺失，其他已登入使用者可能完成不屬於自己的 transaction

## 決策

### 1. checkout transaction 必須在 order 建立成功後才刪除

新的時序為：

1. 載入並驗證 transaction
2. 載入 cart / buyer / products
3. 建立並持久化 order
4. 刪除 checkout transaction
5. 執行 product fulfillment callback
6. 更新 fulfillment status

這代表：

- 若 order 尚未成功建立，transaction 必須保留
- 若 fulfillment callback 失敗，order 仍成立，transaction 不再保留

### 2. transaction buyer 必須等於 access token 對應 member

`CheckoutService.CompleteAsync(...)` 必須驗證：

- `transaction.MemberId == RequestMember.Id`

若不相符：

- 不建立 order
- 不刪除 transaction
- 回傳 buyer mismatch 結果

API 層將其映射為 `403 Forbidden`

### 3. fulfillment failure 不推翻 order complete

這項既有規則保留不變：

- 只要 order 已建立成功，checkout complete 視為成功
- `IProductService` callback 若失敗，只更新 fulfillment status 為 `Failed`

## 影響

- `CheckoutService` 的刪除 transaction 時機需調整
- checkout complete result 需新增 buyer mismatch 狀態
- `CheckoutController` 需將 buyer mismatch 映射為 `403 Forbidden`
- `.Core.Tests` 需新增：
  - transaction 在 product not found 時保留
  - buyer mismatch 時拒絕完成交易

## 不處理事項

- outbox / retry 機制
- transaction status 欄位
- payment gateway 整合
- cancel flow
