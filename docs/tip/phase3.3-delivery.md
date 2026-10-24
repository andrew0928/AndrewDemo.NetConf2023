# Phase 3.3：指定身分限制與結果出口

2026-10-19。已實作及驗證，待 review。沒有新增正式維護 endpoint，也沒有修改 Abstract。

| 操作／來源 | 實際行為 |
|---|---|
| 有一般權限的 normal identity 改價 | 維持原商業驗證後更新 |
| test identity 改價 | `TestIdentityForbiddenException`，OperationOutcome.Rejected；HTTP 403／`test_identity_forbidden`，沒有 product.update 略過 |
| test identity 合法修改 SKU 規格 | 維持驗證後略過 sku.update |
| test Checkout 扣庫存 | 通過所有必要檢查才略過 inventory.deduct |
| System 處理 test Order | 從保存用途控制效果；不能透過一般維護入口或 Clear context 產生實際效果 |

`ProductMaintenanceService.UpdatePrice` 先確認有效 identity 與一般授權，再套用 test identity 禁止政策。拒絕 log 記錄 ReasonCode、TargetType／TargetId、RequestedChange、MemberId。HTTP 測試探針驗證 403 body、test／rejected headers、商品重讀及後續 Checkout 仍為原價 100；normal 改價仍可更新為 80。

`TransactionContext` 在同一 scope 記住曾處理 test transaction，防止清除或替換公開快照後借用獨立維護入口。新的 request／event scope 不承接此狀態。這是 Core 的執行邊界保護，不是新身分用途或資料庫分類。

STORE-019～021 對應：ProtectedWriteTests、TransactionStorageTests.SystemCannotEscapeTestTransactionThroughMaintenance、ExecutionScopeTests.TestIdentityPriceChange_ReturnsExplicit403_AndLaterCheckoutKeepsOriginalPrice、OrderEventScopeTests.TestOrderSystemHandler_CannotEscapeThroughIndependentMaintenance。正常拒絕不記成成功略過；完整結果見 [3.6](phase3.6-delivery.md)。
