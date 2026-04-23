# Checkout Correctness 修正規格

## 狀態

- phase: 2
- status: draft-for-review
- 日期：2026-03-24

## 範圍

本規格只處理：

1. checkout transaction 與 order 建立的時序一致性
2. checkout complete 的 buyer 驗證

## 規格

### Transaction delete timing

`CheckoutService.CompleteAsync(...)` 必須符合以下順序：

1. 載入 checkout transaction
2. 驗證 request member 與 transaction buyer 一致
3. 載入 cart / buyer / products
4. 建立並持久化 order
5. 刪除 checkout transaction
6. 執行 order event dispatch
7. 更新 fulfillment status

### Buyer validation

若 `RequestMember.Id != transaction.MemberId`，系統必須：

- 不建立 order
- 不刪除 checkout transaction
- 回傳 buyer mismatch result

`CheckoutController` 必須將此結果映射為 `403 Forbidden`

### Fulfillment failure

若 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)` 失敗：

- order 仍視為已建立成功
- `FulfillmentStatus` 更新為 `Failed`
- checkout transaction 不應恢復

## 非目標

- retry / outbox
- transaction status 欄位
- payment gateway integration
- cancel flow
