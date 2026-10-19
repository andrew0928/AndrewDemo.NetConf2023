# TiP Core 物件定義測試案例

2026-10-19 修訂：與 [交易隔離案例](tip-transaction-storage.md) 共同驗收 Phase 3。下方 Phase 1／2 證據是當時版本；本次新增／變更要求尚未實作或驗證。

Phase 3 的模型 round-trip fixture 須明確選擇相符用途的 collection；舊 BSON 缺 IsTest 僅在 normal 區視為 false，test 區缺值為異常。MODEL-001／002 的 true 交易保存於 test 區，Member／Token 仍共用；配合 STORE-001／007 驗證，不只更新測試中的 collection 字串。

## 狀態

- 日期：2026-10-17
- 範圍：[物件定義規格](../tip-core-object-definitions.md)。
- 自動化測試：[TipObjectPersistenceTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/TipObjectPersistenceTests.cs)。

## TC-TIP-MODEL-001 用途與付款來源可持久化

- Given：明確指定用途的 Member、Token、Checkout、Order，分別以 true 與 false 執行。
- When：透過 LiteDB 寫入並重新讀取。
- Then：四種物件與 Order.Buyer 的 IsTest 正確還原；原始 BSON 確實保存 IsTest。
- And：Checkout.PaymentId 與 Order.CheckoutTransactionId 正確還原。
- 對應：`PurposeAndPaymentFields_RoundTripThroughLiteDb`，2 筆案例。

## TC-TIP-MODEL-002 封存區分離與訂單關聯

- Given：測試 Checkout 與正式會員 Buyer，Order.Id 不同於 TransactionId。
- When：測試直接寫入封存區、移除作用中紀錄，並以明確關聯保存 Order。
- Then：作用中區查不到原紀錄；可透過 Order.CheckoutTransactionId 查回封存紀錄，所有交易欄位保留。
- And：Order.IsTest = true，Buyer.IsTest = false。
- 對應：`ArchivedCheckout_IsStoredSeparatelyAndReferencedByOrder`。
- 邊界：只驗證儲存定義，不代表 CheckoutService 已自動或原子地封存。

## TC-TIP-MODEL-003 非 Checkout 訂單允許空關聯

- Given：建立 Order 時未提供 CheckoutTransactionId。
- When：寫入並重新讀取。
- Then：關聯維持 null，不能使用 Order.Id 自動填入；未指定的 IsTest 為 false。
- 對應：`OrderWithoutCheckout_PreservesNullRelationship`。

## TC-TIP-MODEL-004 舊資料用途與關聯預設值

- Given：直接寫入缺少新增欄位的原始 BSON 文件；舊 Order.Id 與作用中 Checkout.TransactionId 恰好相同。
- When：以新模型讀取 Member、Token、Checkout、Order 與 Buyer 快照。
- Then：所有 IsTest 為 false，Order.CheckoutTransactionId、Order.CreatedAt、Checkout.ClosedAt 為 null，封存區仍為空。
- 對應：`LegacyDocuments_DefaultToRegularPurposeWithoutInventingOrderRelationship`。

## TC-TIP-MODEL-005 核帳時間與訂單快照可持久化

- Given：直接保存具有 CreatedAt／ClosedAt 的封存 Checkout，以及有 CreatedAt、明確關聯、金額與商品／折扣明細的 Order。
- When：透過 LiteDB 重新讀取，將時間換算為 UTC 比較。
- Then：起訖時間與 Order 建立時間正確；即使 Cart 不存在，Order 仍有原金額及明細。
- 對應：`AccountingTimestamps_RoundTripWithoutDependingOnCurrentCart`。
- 邊界：只驗證儲存結構，不代表 Checkout 有獨立金額快照或服務已填入時間。

## TC-TIP-MODEL-006 作用中交易沒有結束時間

- Given：新 Checkout 未設定 ClosedAt。
- When：保存後重新讀取。
- Then：ClosedAt=null，不自動使用現在時間。
- 對應：`ActiveCheckout_HasNoClosingTimestamp`。

## TC-TIP-MODEL-007 正式 collection 排除 test Order

- Given：同一正式 Buyer 的兩筆 Order，正式金額 100 保存於 orders、test 金額 900 保存於 test_orders。
- When：透過受控入口讀取 normal Order collection，再明確要求 all。
- Then：normal 僅含正式 Order、金額 100；all 含兩筆，不依 Buyer.IsTest 分類。
- 歷史證據：StoredPurpose_CanExcludeTestOrdersForTheSameRegularBuyer 只驗證舊 mixed collection 的 row filtering，不代表新 storage 要求通過；Phase 3 依 STORE-015 補實際分區驗證。

## Phase 2／後續查詢驗收要求

- 正常 complete：Order.CreatedAt、封存 ClosedAt 與完成回應一致；失敗 rollback 不留下結束時間，履約失敗不改寫成立時間。
- 作用中檢查失敗或 409：ClosedAt 仍為 null。
- 未來取消流程依 [已定案生命週期規格](../tip-checkout-lifecycle.md) 填 ClosedAt；本批不新增 cancel API。
- 查詢功能交付時驗證預設排除 test、明確列 test、正式 Buyer 的 test Order、不重複加總關聯資料、未知時間與歷史 IsTest 缺欄位。
