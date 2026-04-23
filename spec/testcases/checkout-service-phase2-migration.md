# Checkout Service Phase 2 搬移測試案例

## 狀態

- phase: 2
- status: draft-for-review
- 日期：2026-03-24

## 後續補充

- checkout correctness 修正案例另見 [checkout-correctness-fixes.md](/Users/andrew/code-work/andrewshop.apidemo/spec/testcases/checkout-correctness-fixes.md)

## Core Service

### TC-CK-001 CheckoutCreate 流程移入 CheckoutService

- Given: member 與 cart 都存在
- When: 呼叫 `CheckoutService.Create(...)`
- Then: 建立 `CheckoutTransactionRecord`
- And: 回傳 transaction result

### TC-CK-002 CheckoutComplete 流程移入 CheckoutService

- Given: checkout transaction 存在
- And: cart、member、products 都存在
- When: 呼叫 `CheckoutService.CompleteAsync(...)`
- Then: 建立 order
- And: 回傳 complete result

### TC-CK-003 Waiting room 由 CheckoutService 控制

- Given: checkout complete 流程啟動
- When: 執行 complete
- Then: waiting room 的執行位置在 `CheckoutService`
- And: controller 不直接 new / await `WaitingRoomTicket`

## Controller Boundary

### TC-CK-101 CheckoutController 不再直接操作 CheckoutTransactions

- Given: `CheckoutController`
- When: 檢查 create / complete 實作
- Then: 不直接存取 `_database.CheckoutTransactions`

### TC-CK-102 CheckoutController 不再直接組裝 order

- Given: `CheckoutController`
- When: 檢查 complete 實作
- Then: 不直接建立 order product lines / discount lines

### TC-CK-103 CheckoutController 不再直接觸發 order event callback

- Given: `CheckoutController`
- When: 檢查 complete 實作
- Then: 不直接呼叫 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)`

## Behavior Preservation

### TC-CK-201 搬移後仍維持既有 create API 行為

- Given: 原本的 `/api/checkout/create`
- When: 搬移到 `CheckoutService`
- Then: 對外 response contract 不改變

### TC-CK-202 搬移後仍維持既有 complete API 行為

- Given: 原本的 `/api/checkout/complete`
- When: 搬移到 `CheckoutService`
- Then: 對外 response contract 不改變

### TC-CK-203 本階段不修正已知 transaction 缺失

- Given: 目前已知 checkout transaction 流程存在設計缺失
- When: 執行 Phase 2 搬移
- Then: 本階段不以修正該行為為目標
- And: 只要求責任位置從 controller 移到 `.Core`
