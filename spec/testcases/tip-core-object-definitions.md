# TiP Core 物件定義測試案例

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
- Then：所有 IsTest 為 false，Order.CheckoutTransactionId 為 null，封存區仍為空。
- 對應：`LegacyDocuments_DefaultToRegularPurposeWithoutInventingOrderRelationship`。
