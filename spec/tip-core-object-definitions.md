# TiP Core 物件定義

## 狀態與範圍

- 日期：2026-10-17
- 狀態：Phase 1 定案；交付與凍結範圍見 [Phase 1 基準](tip-phase1.md)。
- 依據：[Core 欄位與明確會員輸入決策](../docs/decisions/2026-10-17-tip-core-fields-and-explicit-member.md)。
- 依使用者指定，先修改 code，再同步本規格與 [測試案例](testcases/tip-core-object-definitions.md)。

本文件定義 `.Core` 持久化物件欄位與封存 collection。欄位、白名單查詢、Context 基礎行為與 Checkout command 的 PaymentId 搬移已實作；註冊分類、Token／Order 繼承、關聯填入、自動封存及 context／HTTP 出口接入，依 [標準流程](tip-standard-flow.md) 於 Phase 2 完成。新增欄位不代表 TiP 已能控制商業效果。

## 欄位

| 模型 | 新增欄位 | 型別 | 建立規則 |
|---|---|---|---|
| Member | IsTest | bool | 註冊時依 MemberId 白名單決定。 |
| MemberAccessTokenRecord | IsTest | bool | 簽發時複製 Member.IsTest。 |
| CheckoutTransactionRecord | IsTest | bool | create 時依 Member.IsTest 或 PaymentId 測試清單命中結果決定。 |
| CheckoutTransactionRecord | PaymentId | int | create 時提供並保存，供 complete 與封存使用。 |
| Order | IsTest | bool | 透過 Checkout 建立時複製 Checkout.IsTest。 |
| Order | CheckoutTransactionId | int? | 關聯對應 Checkout；其他來源或沒有可查證歷史紀錄時為 null。 |

上述欄位採 `get; init;`，供建立物件及持久化還原使用，一般 C# 呼叫端不能在物件建立後重新指派。此限制不等於資料庫禁止覆寫：建立來源驗證及持久化更新政策由後續服務流程保證。

`Order.Id` 為訂單識別，建構式參數命名為 `orderId`，不自動以該值填入 CheckoutTransactionId。關聯依明確欄位查詢，不要求兩者 ID 相等。不新增 Order.PaymentId。

Order.Buyer 保留會員快照，含 Member.IsTest；正式會員使用測試付款來源時，可同時有 Buyer.IsTest = false 與 Order.IsTest = true。

Cart 不新增 IsTest 或會員綁定。Product、SKU、Inventory 不新增用途欄位。OrderFulfillmentStatus 維持既有定義。

## 儲存區

同一 shop database 中使用兩個獨立 collection，沿用同一個 CheckoutTransactionRecord 型別：

| IShopDatabaseContext 屬性 | Collection | 用途 |
|---|---|---|
| CheckoutTransactions | checkout_transactions | 作用中的 Checkout。 |
| ArchivedCheckoutTransactions | archived_checkout_transactions | 已完成交易的封存紀錄。 |

封存保存原 TransactionId、CartId、MemberId、CreatedAt、IsTest 與 PaymentId。Order.CheckoutTransactionId 對應封存紀錄的 TransactionId。

這次提供獨立存取入口；CheckoutService 尚未自動搬移紀錄。成功結帳的原子封存、交易 ID 跨區唯一與重複 complete 防護，須在後續流程變更一起完成；不能因為新增 collection 就視為這些保證已成立。原有泛型 ShopDatabase.Create<CheckoutTransactionRecord> 仍指向作用中區，封存須明確使用封存入口。

## 既有資料

- 改版前缺少 IsTest 的 Member、Token、Checkout、Order，讀取時一律為 false；Order.Buyer 快照亦同。
- 缺少 CheckoutTransactionId 的舊 Order 讀取為 null，即使其 Id 與作用中 Checkout 相同，也不自動建立關聯。
- 不補造已被刪除的歷史 Checkout。
- 本次採讀取預設值相容，不批次改寫既有資料庫或 seed snapshot。
- 舊 Checkout 缺少 PaymentId 時，模型預設值不代表已知付款來源。Complete 在反序列化前檢查儲存欄位；缺少或 null 明確回 409／checkout_recreate_required，要求重新建立，不補值、不改寫舊紀錄。新交易明確保存的 0 不視為缺少。詳見 [HTTP 合約](tip-http-and-operation-results.md)。

## 驗證邊界

以原始 BSON 舊格式資料驗證讀取預設值，並驗證新增 init 欄位能經 LiteDB 寫入及還原、封存區與作用中區分離，以及 nullable 關聯與 Buyer 用途可獨立保存。

本次測試不宣稱白名單、完整封存流程或 TiP 商業效果隔離已完成。
