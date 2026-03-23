# Checkout Correctness 修正測試案例

## 狀態

- phase: 2
- status: draft-for-review
- 日期：2026-03-24

## Core Service

### TC-CK-FIX-001 商品不存在時 transaction 保留

- Given: checkout transaction 存在
- And: cart 內引用的 product 不存在
- When: 呼叫 `CheckoutService.CompleteAsync(...)`
- Then: 回傳 `ProductNotFound`
- And: transaction 仍存在
- And: order 不存在

### TC-CK-FIX-002 buyer mismatch 時拒絕完成交易

- Given: checkout transaction 屬於 buyer A
- And: request member 為 buyer B
- When: 呼叫 `CheckoutService.CompleteAsync(...)`
- Then: 回傳 `BuyerMismatch`
- And: transaction 仍存在
- And: order 不存在

### TC-CK-FIX-003 成功建立 order 後才刪除 transaction

- Given: checkout transaction、cart、buyer、products 都存在
- When: 呼叫 `CheckoutService.CompleteAsync(...)`
- Then: order 先被建立
- And: transaction 最終被刪除

## API Boundary

### TC-CK-FIX-101 buyer mismatch 映射為 403 Forbidden

- Given: `CheckoutController`
- When: `CheckoutService` 回傳 `BuyerMismatch`
- Then: API response 為 `403 Forbidden`
