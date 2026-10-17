# TiP 標準流程驗收案例

依據：[標準流程](../tip-standard-flow.md)。除明列自動化證據外，下列為 Phase 2 必須實作及執行的案例，不代表已通過。每案使用隔離資料庫；實際商業效果需驗證持久化結果與 adapter 呼叫，不只看 log。

## 會員與白名單

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-FLOW-001 | 新會員最終 ID 命中清單，完成註冊 | 首次可見紀錄 IsTest=true；未命中為 false，不能先以 false 提交再改 true。 |
| TC-TIP-FLOW-002 | 對正式／測試會員簽發 Token | Token.IsTest 與保存的 Member 相同，不由 request 自行指定。 |
| TC-TIP-FLOW-003 | 舊會員／Token 缺少用途，後來 ID 加入清單再登入 | 仍為 false；不重新分類既有資料。 |
| TC-TIP-FLOW-004 | 同時註冊多個會員，或註冊失敗後重試 | ID 唯一；無用途錯配或未完成的部分資料。 |
| TC-TIP-FLOW-005 | Token 與 Member 保存的用途不一致 | 用途 BUG，停止後續效果，不以重新查白名單修復。 |

## Checkout 建立

| ID | Member.IsTest | PaymentId 命中 | Then |
|---|---|---|---|
| TC-TIP-FLOW-101 | false | 否 | Checkout.IsTest=false，保存原 PaymentId。 |
| TC-TIP-FLOW-102 | false | 是 | Checkout.IsTest=true，Member／Token 仍為 false。 |
| TC-TIP-FLOW-103 | true | 否 | Checkout.IsTest=true。 |
| TC-TIP-FLOW-104 | true | 是 | Checkout.IsTest=true。 |

四案均驗證 Cart 不新增用途／會員綁定，create／complete 的 RequestMember 仍由呼叫端明確提供。

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-FLOW-105 | create JSON 缺少 paymentId | 400；不建立 Checkout。 |
| TC-TIP-FLOW-106 | create 明確提供 PaymentId，complete 不提供 | complete result 的 PaymentId 與 create 相同；夾帶其他 paymentId 不改寫保存值。 |
| TC-TIP-FLOW-107 | 建立後更換白名單或使用新的 request scope | Checkout、Order、封存用途及 PaymentId 維持原值。 |
| TC-TIP-FLOW-108 | 已存在舊 Checkout 的 BSON 未保存 PaymentId／值為 null | 回 RecreateRequired；HTTP 409、code=checkout_recreate_required；舊 Checkout、Cart、庫存不變，無 Order／封存。 |
| TC-TIP-FLOW-109 | 新 Checkout 明確提供數值 0 | 以保存的數值處理；不得只因 0 就當成缺少欄位。 |

## 完成與封存

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-FLOW-201 | 正式會員正常結帳，商品 100 元、庫存 10，買 2 件 | Order=false、總額 200、庫存 8；封存原交易並移除作用中紀錄，Order 明確關聯封存。 |
| TC-TIP-FLOW-202 | 相同條件改為測試會員 | 正常計價／驗證，Order=true、庫存仍 10；保留封存，記錄 inventory.deduct 略過。 |
| TC-TIP-FLOW-203 | 正式會員使用測試 PaymentId 完成結帳 | Buyer=false、Order=true；同樣不扣庫存，不重新判定白名單。 |
| TC-TIP-FLOW-204 | test Checkout 庫存 1、買 2 件 | 正常回庫存不足；無 Order／封存，作用中交易保留，無假的扣庫存略過紀錄。 |
| TC-TIP-FLOW-205 | 另一會員（含 test 會員）完成他人 Checkout | 403，交易／Cart 不變；不輸出他人用途或略過紀錄。 |
| TC-TIP-FLOW-206 | 在庫存／Order／封存／移除任一步注入失敗 | rollback 後庫存與作用中紀錄還原，無部分 Order 或封存，不派送事件。 |
| TC-TIP-FLOW-207 | 完成後再 complete，或兩個請求同時完成同一筆 | 最多一筆 Order／封存，一次適用庫存效果；其他請求為 TransactionNotFound／400，不重送事件。 |
| TC-TIP-FLOW-208 | 完成所有作用中交易，再建立新的 Checkout | 不重用封存中的 ID；新 Order 不覆寫既有訂單。 |
| TC-TIP-FLOW-209 | Order 建立成功後 fulfillment 拋錯 | Order／封存保留，FulfillmentStatus=Failed；不恢復作用中交易。 |
| TC-TIP-FLOW-210 | 非 Checkout／歷史訂單沒有關聯 | CheckoutTransactionId=null，仍可依 Order.IsTest 處理事件，不捏造封存。 |

## 效果與執行範圍

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-FLOW-301 | 正式買 3 件與 test 買 2 件並行，庫存起始 10 | 庫存最後 7；兩筆 Order／封存用途各自正確，context 不互相污染。 |
| TC-TIP-FLOW-302 | test 身分新增／刪除 Cart 明細 | Cart 真正保存結果，沒有 transaction，也不虛構 Cart 略過。 |
| TC-TIP-FLOW-303 | test 要求有效商品改價或 SKU 規格變更 | 完成必要檢查後略過；重新讀取原值，記錄目標與原要求內容。未提供維護 API 時以 Core 操作入口驗證，不新增管理網站。 |
| TC-TIP-FLOW-304 | HTTP scope 結束後處理／重送 test Order 事件 | 新 scope 的系統 identity、test Order transaction；所有受保護效果仍受 test 限制。 |
| TC-TIP-FLOW-305 | 同步 dispatch 返回外層，或巢狀／並行操作不同 scope | 內層初始化不改寫外層 context；不同 scope 不共用可變 context。 |
| TC-TIP-FLOW-306 | 事件 Order 不存在或預期交易用途遺失 | 停止受保護效果並記錄 BUG，不當成無交易正常放行。 |
| TC-TIP-FLOW-307 | 若有真實付款／出貨／事件統計 adapter，執行 test 交易 | adapter 真實效果不發生；只對實際到達並略過的效果留下紀錄。不存在的能力標示不適用。 |
| TC-TIP-FLOW-308 | 若有正式查詢型報表，新增 test Order 前後比較 | 正式訂單數／銷量／營收不變；不要求為查詢排除留下略過 log。 |

## 現有自動化證據與限制

- `CheckoutServiceTests.Create_WithExistingCart_CreatesCheckoutTransaction` 驗證 create 保存 PaymentId。
- `CompleteAsync_WithExistingTransaction_CreatesOrderAndDeletesTransaction` 驗證 result 使用保存值；完整封存仍未實作，此舊測試不等於 TC-201 已通過。
- `CompleteAsync_WhenStoredPaymentIsMissing_RequiresRecreationWithoutEffects` 覆蓋 TC-108 的 Core 分支（缺欄位／null 兩案）。
- `CreateAndComplete_WithExplicitZeroPayment_PreservesItAsProvidedValue` 覆蓋 TC-109。
- `CheckoutContractTests` 覆蓋 JSON 必填、shared client 付款輸入時機及 controller 的 409 mapping；不是啟動真實伺服器的 HTTP 測試。
- 其他案例須在 Phase 2 依實際服務與 host 接入補齊自動化／整合驗證，不能用直接寫入模型的測試取代。

## 不可變用途

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-FLOW-401 | 透過既有更新／保存入口嘗試將 Member、Token、Checkout、Order 的 IsTest 由 false 改 true，或反向修改 | 拒絕用途變更，記錄原因；重新讀取原值與關聯用途均不變。若入口不接受該欄位，驗證請求無法改寫；不能只驗證 init 語法。 |
| TC-TIP-FLOW-402 | 同一物件更新其他合法業務欄位 | 依原有規則保存，其既定用途不被重設為預設值。 |

## Create 固定內容補充

2026-10-17 使用者確認金額與明細在 create 時固定。以上交易驗收的價格／數量以保存的 Checkout 為準；complete 只做必要的商品與庫存等檢查，不重讀 Cart 決定交易內容。Create 後修改 Cart／現價／折扣、終態完整搬移與凍結保護的案例見 [生命週期案例](tip-checkout-lifecycle.md)。
