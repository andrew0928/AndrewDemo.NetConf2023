# TiP 標準流程規格

## 範圍與責任

本規格與 [物件定義](tip-core-object-definitions.md)、[白名單](tip-test-allowlist.md)、[Context 合約](tip-context-contracts.md)、[HTTP 與結果](tip-http-and-operation-results.md) 及 [Checkout 生命週期](tip-checkout-lifecycle.md) 共同構成本輪 Phase 1 基準。

適用標準 Core、API、CommonStorefront、CLI。AppleBTS／PetShop 的領域政策與到期整理不納入；因共用 API 合約而必須同步的呼叫參數仍須更新。既有 API 路徑維持。

本規格描述 Phase 2 完成後的行為。目前僅有資料定義、白名單、Context 基礎實作、Checkout 付款輸入搬移與舊交易 409 分支；其餘流程由 Phase 2 完成，不能以編譯成功代替完整 TiP 驗證。

## 會員註冊與 Token

1. 由 Core 共用建立流程配發最終 MemberId，依 Name 呼叫 TestAllowlist.IsTestMember 判定 IsTest。
2. ID 配發、用途初始化及會員首次保存必須在同一個資料庫交易邊界內完成，不可讓其他操作讀到先正式、後改成測試的會員。
3. 失敗重試或白名單調整不得改寫既有會員用途。已存在會員的登入直接使用保存值。
4. 白名單內的名稱決定首次建立的會員用途；以作用中資料庫 collation 比對 Name，不將數字名稱視為會員 ID。開發用測試會員可由受控初始化流程使用明確 ID 建立；公開 request 不接受任意 IsTest 或指定會員 ID 的新增能力。
5. Token 簽發以已保存 Member 為來源，複製 MemberId 與 IsTest。驗證須檢查 Token 存在、有效期限與 Member 存在；若兩者用途不一致，依用途矛盾處理，不能以白名單補救。
6. 既有會員與 Token 缺少 IsTest 時按已確認政策為 false，不重新分類。ID 配發機制、鎖定方式屬 Core 實作，驗收必須涵蓋併發及重試。

## Checkout 輸入合約

| 入口 | 欄位 |
|---|---|
| CheckoutCreateCommand | CartId、必填 PaymentId、必填 RequestMember。 |
| POST /api/checkout/create | cartId、必填 paymentId；API 由驗證結果組合 RequestMember。 |
| CheckoutCompleteCommand | TransactionId、Satisfaction?、ShopComments?、必填 RequestMember；沒有 PaymentId。 |
| POST /api/checkout/complete | transactionId、satisfaction?、shopComments?；沒有 paymentId。 |
| CheckoutCompleteResult／HTTP response | 保留 PaymentId，來源為該 Checkout 保存值。 |

PaymentId 沿用 int 數值語意，不建立 Payment 物件、不新增數值區間限制。必填表示「必須明確提供」，不是以 0 表示缺少；create JSON 未提供時由 API 回 400。舊 client 即使仍在 complete JSON 夾帶 paymentId，也不得覆寫保存值；不保留舊 command 欄位或相容入口。

Create／Complete 均保留明確 RequestMember，Core 不以 ambient identity 代替必填輸入。API 與 CLI 自行組合，Core 仍檢查買方與輸入有效性。

## 建立交易與用途固定

| Member.IsTest | PaymentId 命中 | Checkout.IsTest／後續 Order.IsTest |
|---|---|---|
| false | 否 | false |
| false | 是 | true |
| true | 否 | true |
| true | 是 | true |

建立時從 Cart／商品計價並執行折扣，保存 CartId、MemberId、PaymentId、CreatedAt、IsTest、State=Created，以及固定的 TotalPrice／ProductLines／DiscountLines。Cart 不承接會員、不保存 IsTest。Checkout 的 PaymentId、用途與買方建立後固定；白名單或 request 身分變動不得重新分類既有交易。

Checkout 與 Order 的用途可不同於 Buyer.IsTest。Create 照常計算價格／折扣並固定內容；complete 不再重算，必要檢查仍照常執行，test 不代表結帳一定成功。

## 完成交易與封存

1. 驗證身分並載入作用中的 Checkout；不存在時維持 TransactionNotFound／HTTP 400。
2. 驗證 RequestMember.Id 與交易 MemberId 相同；不符回 403，不得建立 Order、修改 Cart、封存或產生商業效果。
3. 交易未保存 PaymentId（欄位缺少或 null）時回 RecreateRequired／HTTP 409，要求重新 create；保留舊紀錄，不補值、不修改用途，也不產生副作用。
4. 確認 State=Created 與固定交易內容完整；舊格式缺資料依 409 要求重建。以保存的交易明細與金額完成，不依目前 Cart／現價重算；買方、商品可購買性、SKU 關聯與庫存檢查照常。正式交易扣庫存，test 略過扣減並記錄結果。
5. 在資料庫 transaction 內再次確認該 Checkout 仍可完成，確保併發 complete 不會重複建立 Order 或執行效果。
6. 同一 transaction 內完成適用的庫存異動、Order 建立、State=Completed／ClosedAt 設定、完整封存寫入、作用中紀錄移除；任一步失敗全部 rollback。
7. Order.IsTest 承接 Checkout；Order.CheckoutTransactionId 明確填入原 TransactionId，付款來源由封存紀錄取得。其他來源建立的 Order 可為 null 關聯；本輪不新增其他建立入口。
8. 封存保留所有原欄位，包含固定金額、明細、用途與付款來源，並保存終態 State 與本次 ClosedAt；同次建立的 Order.CreatedAt 採相同 UTC 業務完成時間。封存整筆凍結，成功後不再以封存紀錄執行 complete。
9. commit 後才派送訂單事件。Fulfillment 失敗不撤銷已成立的 Order 或封存；沿用既有 FulfillmentStatus 值。資料庫交易不宣稱能回滾外部服務效果。

重複 complete 維持最小規則：作用中區已沒有該交易就回 TransactionNotFound／400，不重送事件，不從封存還原成可完成交易。並行請求最多一個成功，其他請求不得留下部分異動。

TransactionId 在作用中與封存區間不可重用；Order ID 配發亦不得覆寫既有非 Checkout 訂單。具體 ID 配發實作留給 Phase 2，須有相應測試。一般成功回覆沿用現有 HTTP 200；不在此改版順帶改成 201。

## 受保護效果與用途來源

| 操作 | 用途來源與要求 |
|---|---|
| 一般會員操作、商品／SKU 維護 | current identity；test 時照常驗證，略過受保護寫入並記錄。 |
| Cart 新增、刪除、試算 | 身分仍可為 test；Cart 實際保存正常操作，不建立交易 context，不略過 Cart 寫入。 |
| Checkout create | 依上述四種組合建立固定用途。 |
| Checkout complete | 已保存 Checkout；完成前仍驗證買方，不能用當下白名單取代保存用途。 |
| 指向單筆 Order 的操作／事件 | 已保存 Order；系統 identity 不解除 test Order 的限制。 |
| 訂單集合查詢 | 沒有單一 current transaction，各筆 Order 保留用途。 |

Product、SKU、Inventory 是受保護資料；test 不可新增、修改或刪除。這不是跨所有應用的通用 OR 政策。標準流程以外的新來源組合須另定規格，不因沒有 context 而放行效果。

真實付款、出貨及商業統計若有介接，均須遵守 test 不產生真實效果的原則；目前未提供的能力標示不適用，不為通過測試虛構 adapter 或略過紀錄。查詢型正式報表排除 test Order；事件型統計若存在，test 事件略過更新並留 log。

## 事件與執行範圍

- HTTP request、CLI 每次操作、事件每次處理各有明確範圍；Context 與依賴它的 CheckoutService 採 scoped。
- 每個 scope 的讀取 interface 與 Core 初始化入口必須指向同一個對應 context 實例，identity／transaction 分別註冊。
- 同步 Dispatch 亦建立新的事件 scope，初始化系統 identity，依事件 OrderId 載入 Order 恢復交易用途，不沿用 HTTP request 的可變 context。
- 重送時仍由持久化 Order 恢復相同用途；缺少 Order／用途矛盾時終止效果並記錄錯誤，不將未明用途當正式。
- 不要求引入 queue、worker 或跨所有 handler 的 exactly-once 機制；本輪要求用途延續、scope 隔離與 Checkout 不重複完成。

## 既有資料與最小遷移

缺少 IsTest 的舊資料為 false，Order 缺少關聯為 null；不補造歷史封存。缺少 PaymentId 的作用中 Checkout 一律 409，要求重新建立。正式服務啟動或登入不得自動把舊會員改為 test。已完成舊 Order 即使沒有 Checkout 關聯，仍以自己的 IsTest 處理事件。

## 驗收

見 [標準流程測試案例](testcases/tip-standard-flow.md)。Phase 1 交付完整規則與案例；Phase 2 才以整合測試證明註冊、Checkout、封存、middleware 與事件流程已落實。

## 已建立物件的更新

一般更新入口不得接受用途重新分類；Member、Token、Checkout、Order 已有的 IsTest 在正式轉 test 與 test 轉正式兩個方向均不可覆寫。Checkout.PaymentId 與買方亦不得在完成時替換。Core 的持久化流程須保留原值或拒絕不符的更新，不能只依賴 C# init 限制；不為驗收新增公開用途修改 API。

## 業績／核帳追加原則

依 [時間欄位補充](tip-core-object-definitions.md)，新資料須填入交易結束／訂單建立時間，業績與核帳查詢預設排除各自 IsTest=true 的資料，明確要求才列出 test。查詢功能後續再做；Checkout 終態、獨立金額／明細及 create 時固定已依 [生命週期規格](tip-checkout-lifecycle.md) 定案，取消仍保留原交易內容但不計入成交。
