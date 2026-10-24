# TiP Core 物件定義

2026-10-19 修訂：交易 collection 隔離與指定身分限制已同步至本規格並獲使用者確認，見 [Phase 3 review](../docs/tip/phase3-spec-review.md)。業務原則已確認；新 storage 與改價拒絕尚未實作。既有 Phase 1／2 的交付數字及紀錄僅代表當時版本。

## 狀態與範圍

- 日期：2026-10-17
- 狀態：Phase 1 定案；交付與凍結範圍見 [Phase 1 基準](tip-phase1.md)。
- 依據：[Core 欄位與明確會員輸入決策](../docs/decisions/2026-10-17-tip-core-fields-and-explicit-member.md)。
- 依使用者指定，先修改 code，再同步本規格與 [測試案例](testcases/tip-core-object-definitions.md)。

本文件定義 `.Core` 持久化物件欄位與封存 collection。欄位、白名單查詢、Context 基礎行為與 Checkout command 的 PaymentId 搬移已實作；註冊分類、Token／Order 繼承、關聯填入、自動封存及 context／HTTP 出口接入，依 [標準流程](tip-standard-flow.md) 於 Phase 2 完成。新增欄位不代表 TiP 已能控制商業效果。

## 欄位

| 模型 | 新增欄位 | 型別 | 建立規則 |
|---|---|---|---|
| Member | IsTest | bool | 註冊時依 Member.Name 白名單決定。 |
| MemberAccessTokenRecord | IsTest | bool | 簽發時複製 Member.IsTest。 |
| CheckoutTransactionRecord | IsTest | bool | create 時依 Member.IsTest 或 PaymentId 測試清單命中結果決定。 |
| CheckoutTransactionRecord | PaymentId | int | create 時提供並保存，供 complete 與封存使用。 |
| Order | IsTest | bool | 透過 Checkout 建立時複製 Checkout.IsTest。 |
| Order | CheckoutTransactionId | int? | 關聯對應 Checkout；其他來源或沒有可查證歷史紀錄時為 null。 |
| CheckoutTransactionRecord | ClosedAt | DateTime? | complete 或 cancel 時的 UTC 結束時間；作用中為 null。 |
| Order | CreatedAt | DateTime? | 訂單成立時的 UTC 時間；新訂單由建立流程明確指定。 |
| CheckoutTransactionRecord | State | CheckoutState | Created → Completed／Cancelled；Unknown 僅表示舊資料未知。 |
| CheckoutTransactionRecord | TotalPrice | decimal? | create 時計算並固定；新資料必填，舊資料缺值為 null。 |
| CheckoutTransactionRecord | ProductLines | List<CheckoutProductLine> | 獨立交易識別、數量、單價與行金額，不含商品資訊快照。 |
| CheckoutTransactionRecord | DiscountLines | List<CheckoutDiscountLine> | create 時已套用的折扣結果。 |

用途、付款、訂單關聯與 Order.CreatedAt 採 `get; init;`；Checkout.State／ClosedAt 採 `get; set;`，由服務在結束交易時填入，結束後不得改寫。前述 init 欄位供建立物件及持久化還原使用，一般 C# 呼叫端不能在物件建立後重新指派。此限制不等於資料庫禁止覆寫：建立來源驗證及持久化更新政策由後續服務流程保證。

`Order.Id` 為訂單識別，建構式參數命名為 `orderId`，不自動以該值填入 CheckoutTransactionId。關聯依明確欄位查詢，不要求兩者 ID 相等。不新增 Order.PaymentId。

Order.Buyer 保留會員快照，含 Member.IsTest；正式會員使用測試付款來源時，可同時有 Buyer.IsTest = false 與 Order.IsTest = true。

Cart 不新增 IsTest 或會員綁定。Product、SKU、Inventory 不新增用途欄位。OrderFulfillmentStatus 維持既有定義。

## 儲存區

同一 shop database 中將 Order、作用中 Checkout、封存 Checkout 各拆 normal／test，合計六個 collections；完整對照與路由以 [交易儲存規格](tip-transaction-storage.md) 為唯一基準。沿用原模型及 IsTest，不新增 test 模型；Member／Token 與其他非交易資料共用。

封存完整保存原 TransactionId、CartId、MemberId、CreatedAt、IsTest、PaymentId、TotalPrice、ProductLines、DiscountLines，以及終態 State／ClosedAt；不縮減欄位。Order.CheckoutTransactionId 對應同用途封存紀錄。

Core 集中提供受控讀寫與單筆定位；IShopDatabaseContext 屬性屬 Core 內部設計，不作為讓業務端直接寫 raw collection 的契約。泛型 ShopDatabase.Create 不得成為交易繞道；既有用途防寫／凍結與原子封存必須擴及六區。Phase 3 待實作，不能以 collection 名稱已定義宣稱隔離成立。

## 既有資料

- 改版前缺少 IsTest 的 Member、Token、Checkout、Order，讀取時一律為 false；Order.Buyer 快照亦同。
- 缺少 CheckoutTransactionId 的舊 Order 讀取為 null，即使其 Id 與作用中 Checkout 相同，也不自動建立關聯。
- 不補造已被刪除的歷史 Checkout。
- Phase 1 採讀取預設值相容；Phase 3 依交易儲存規格執行明確的離線 storage 遷移，保留 raw BSON，缺 IsTest 僅在 normal 分區視為 false，不補造歷史欄位。此次文件更新不操作既有 DB。
- 舊 Checkout 缺少 PaymentId 時，模型預設值不代表已知付款來源。Complete 在反序列化前檢查儲存欄位；缺少或 null 明確回 409／checkout_recreate_required，要求重新建立，不補值、不改寫舊紀錄。新交易明確保存的 0 不視為缺少。詳見 [HTTP 合約](tip-http-and-operation-results.md)。

## 驗證邊界

以原始 BSON 舊格式資料驗證讀取預設值，並驗證新增 init 欄位能經 LiteDB 寫入及還原、封存區與作用中區分離，以及 nullable 關聯與 Buyer 用途可獨立保存。

本節的模型測試不代表完整流程通過；Phase 2 流程交付另有證據，Phase 3 分區要求須依新增案例重新驗收。

## 2026-10-17 核帳時間補充

依 [追加決策](../docs/decisions/2026-10-17-tip-accounting-timestamps.md)，第一批追加兩個時間欄位與查詢用途原則；後續使用者再確認 State、獨立交易內容與 create 時固定，詳見 [生命週期規格](tip-checkout-lifecycle.md)。

- ClosedAt 表示 complete／cancel 的結束時間；必要檢查失敗、409 要求重建等未結束交易維持 null。尚未提供 cancel 入口，不因新增欄位而新增取消流程。
- Order.CreatedAt 在建立時確定，與履約完成時間分開；履約失敗或重試不改寫。
- 新資料的時間由 TimeProvider 取得 UTC。完成 Checkout 並建立 Order 時，ClosedAt、Order.CreatedAt 與該次完成回應採同一個業務完成時間，與 Order／封存一併提交；rollback 不留下結束時間。Phase 2 已接入，Phase 3 須在同用途分區維持此保證。
- 舊 Checkout 缺少 ClosedAt、舊 Order 缺少 CreatedAt，均讀取 null。不以目前時間或 DateTime.MinValue 補值；沒有可查證來源時，不回填時間。
- 業績／核帳查詢預設依各物件自己的 IsTest 排除 test，只有明確要求才列 test。不能依 Buyer.IsTest、Token 或當下白名單代替。歷史缺欄位仍依既有 false 政策處理，查詢須驗證原始 BSON 的缺值語意。
- 本次沒有新增時間索引、查詢服務或結果 DTO；後續查詢須把時間未知的舊資料明確區隔，不能任意歸入日期。

State、獨立金額與明細已納入 schema，create 時固定、終態完整封存後凍結。完整規則見 [生命週期規格](tip-checkout-lifecycle.md) 與 [測試案例](testcases/tip-checkout-lifecycle.md)；模型欄位與服務流程的驗證必須分開，Phase 3 另驗證 collection 分流與既有資料遷移。
