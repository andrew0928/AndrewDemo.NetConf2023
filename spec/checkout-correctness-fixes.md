# Checkout Correctness 修正規格

## 狀態

- phase: 2
- status: draft-for-review
- 日期：2026-03-24；2026-10-17 同步 TiP 的封存要求。
- 本次補充為 [TiP Phase 1 基準](tip-phase1.md) 的規格變更，完整封存實作於 TiP Phase 2 驗收。

## 範圍

本規格只處理：

1. checkout transaction 與 order 建立的時序一致性
2. checkout complete 的 buyer 驗證

## 規格

### Transaction delete timing

`CheckoutService.CompleteAsync(...)` 必須符合以下順序：

1. 載入 checkout transaction
2. 驗證 request member 與 transaction buyer 一致
3. 確認已保存 PaymentId；舊紀錄缺少時回 409，要求重新建立
4. 讀取固定 Checkout 明細、buyer／products 並完成必要檢查，不重新計價
5. 在同一資料庫 transaction 內再次確認 Checkout 可完成，依用途處理庫存、建立 order、保存封存交易並移除作用中紀錄
6. commit 後執行 order event dispatch
7. 更新 fulfillment status

任一步資料庫寫入失敗須全部 rollback。封存延續原交易 ID，Order 透過 CheckoutTransactionId 關聯；重複完成回 400，不從封存重做。完整規則見 [TiP 標準流程](tip-standard-flow.md)。

### Buyer validation

若 `RequestMember.Id != transaction.MemberId`，系統必須：

- 不建立 order
- 不刪除 checkout transaction，也不產生封存
- 回傳 buyer mismatch result

`CheckoutController` 必須將此結果映射為 `403 Forbidden`

### Fulfillment failure

若 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)` 失敗：

- order 仍視為已建立成功
- `FulfillmentStatus` 更新為 `Failed`
- 封存紀錄保留，作用中的 checkout transaction 不應恢復

## 非目標

- retry / outbox
- transaction status 欄位
- payment gateway integration
- cancel flow
