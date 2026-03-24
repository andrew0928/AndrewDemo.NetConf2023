# Testcase Overview

這裡整理的是 phase 2 這個 commit 最具代表性的 4 條 testcase。

## 挑選原則

- 優先挑 phase 2 新增的 checkout service migration 與 correctness 修正，而不是重複 phase 1 已凍結的 shop runtime / product service 基礎行為。
- 以 `spec/checkout-service-phase2-migration.md`、`spec/checkout-correctness-fixes.md` 及其 testcase 為主，再對照 `CheckoutServiceTests` 與 controller 實作。
- testcase 的重點是 phase 2 的新邊界：`CheckoutService`、buyer 驗證、transaction delete timing、host 共用相同 checkout 主流程。

## 一覽表

| ID | 情境 | 對應規格 | 主要程式 |
| --- | --- | --- | --- |
| TC-P2-01 | create 流程搬進 `CheckoutService` | `TC-CK-001`, `TC-CK-101`, `TC-CK-201` | `CheckoutController.cs`, `CheckoutService.cs`, `CheckoutModels.cs` |
| TC-P2-02 | complete 成功建立 order，並以 phase 2 時序刪除 transaction | `TC-CK-002 ~ 003`, `TC-CK-FIX-003` | `CheckoutService.cs`, `CheckoutServiceTests.cs` |
| TC-P2-03 | buyer mismatch 被拒絕，API 映射為 `403 Forbidden` | `TC-CK-FIX-002`, `TC-CK-FIX-101` | `CheckoutController.cs`, `CheckoutService.cs`, `CheckoutModels.cs` |
| TC-P2-04 | 商品不存在時保留 transaction 供重試 | `TC-CK-FIX-001` | `CheckoutService.cs`, `CheckoutServiceTests.cs` |

## 文件連結

- [TC-P2-01 CheckoutCreate 由 CheckoutService 擁有](./tc-p2-01-checkout-create-service-boundary.md)
- [TC-P2-02 CheckoutComplete 成功路徑與 transaction delete timing](./tc-p2-02-checkout-complete-success.md)
- [TC-P2-03 Buyer mismatch 與 403 Forbidden 映射](./tc-p2-03-buyer-mismatch-and-forbidden.md)
- [TC-P2-04 商品不存在時保留 transaction 供重試](./tc-p2-04-product-missing-transaction-retention.md)

## 補充

- phase 2 雖然也更新了 ConsoleUI，但其主要價值在於「改走同一個 `CheckoutService`」，因此我把它視為上述 testcase 的 host 對齊，而不是另外拆成獨立案例。
- `FulfillmentStatus = Failed` 的 callback failure 行為在 phase 2 仍保留，但這輪 commit 的主要新增測試集中在 correctness，而不是 callback reliability。
