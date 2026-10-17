# TiP Core 欄位與明確會員輸入

## 狀態

accepted

確認日期：2026-10-17。已確認欄位、command 邊界、Checkout 分區封存、nullable Order 關聯、MemberId／PaymentId 白名單、無交易的表示方式與既有資料用途。剩餘命名及機制細節於實作時具體化，不再作為開始改 code 的前置議題。

**重大決策；影響 .Core。回頭修正 Phase 1；影響 .Abstract / spec。** 本次更新設計文件，尚未修改正式 spec 或程式碼。

## 背景

Phase 1 已啟動，使用者確認 Core 欄位提案，並修正 Token 的保存方式及 command 的會員來源。此決策對齊共用交易流程主線，更新 [標準實作範圍與 Context 方向](2026-10-17-tip-standard-scope-and-context.md) 中 Token 不保存獨立欄位的舊敘述。

## 決策

1. `Member.IsTest: bool`：註冊時以實際 MemberId 查詢預先指定的白名單，命中自動設為 true，未命中為 false；建立後用途不可變。
2. `CheckoutTransactionRecord.IsTest: bool`：建立時依會員與測試 PaymentId 判定，建立後用途不可變。
3. `CheckoutTransactionRecord.PaymentId: int`：建立 Checkout 時提供並保存，供完成交易使用；建立後固定。
4. `Order.IsTest: bool`：從 Checkout 承接，不根據當下身分或白名單重新分類。
5. `MemberAccessTokenRecord.IsTest: bool`：建立 Token 時由 `Member.IsTest` 複製並保存，跟隨 Member 用途，不由呼叫端另行指定任意分類。用途保持一致且不可變。
6. `CheckoutCreateCommand` 新增 PaymentId；`CheckoutCompleteCommand` 移除重複的 PaymentId 輸入，使用已保存的 Checkout.PaymentId。`CheckoutCompleteResult.PaymentId` 保留，來源改為保存值。
7. Create／Complete command 都保留明確的 `RequestMember`。API 負責驗證、解析及組合 command，CLI 等呼叫端亦明確提供會員；底層不以注入 IdentityContext 代替這項必要輸入。Scoped context 的設計不改變 command 的輸入責任，不能默默以 ambient 身分覆寫 command。
8. Cart 排除用途標記與會員綁定；Product、SKU、Inventory 欄位維持，寫入受 TiP 原則控制。Order 明細跟隨 Order 用途；Buyer 快照的 IsTest 表示會員用途，可與 Order.IsTest 不同。
9. 既有資料一律視為 `IsTest = false`，此為使用者明確指定的遷移政策；不依目前白名單重新分類既有會員、Token、Checkout 或 Order。
10. 在 code 或 database 預先指定明確的 MemberId 與 PaymentId 白名單。Member 註冊時查 MemberId 清單；PaymentId 只是數值，不新增付款領域物件，建立 Checkout 時查 PaymentId 測試清單，搭配 Member.IsTest 固定交易用途。第一版實作採 code 內集中定義兩份清單，具體 ID 配合標準 demo／測試資料選定，不新增管理 API。
11. `CheckoutTransactionRecord` 應作為交易紀錄保留；作用中的 Checkout 與完成後的封存紀錄分區保存。封存延續原交易 ID、IsTest 與 PaymentId，不重新判定用途。Order 關聯對應的封存交易紀錄，付款來源由該紀錄取得，不新增 `Order.PaymentId`。

12. 新增 `Order.CheckoutTransactionId: int?`，透過 Checkout 建立時填入對應交易 ID；其他來源建立的 Order 為 null。既有 Order 若無可查證的封存紀錄，不因兩者 ID 相同就捏造關聯。
13. 無目前交易時不輸出交易相關 HTTP header；以物件表達時，交易 context 為 null。這不代表 identity 也不存在；預期有交易卻遺失用途的錯誤仍依用途明確原則處理。
14. 使用者指定本輪先開始變更 code，再同步落入 `/spec` 與 `/spec/testcases`。這是本次 TiP 的交付順序調整，不以先凍結 spec 阻擋開工；後續仍須保持 source、contract、spec 與 testcase 一致。本次 commit 僅保存設計文件。

## Order／Checkout 關聯查核與封存方向

使用者希望 Order 綁定對應 CheckoutTransactionRecord，而不直接保存 PaymentId，並要求先確認現況。

- [Order 建構式](../../src/AndrewDemo.NetConf2023.Core/Order.cs) 直接把 transactionId 設為 Order.Id，沒有額外的 Checkout reference 欄位。
- [CheckoutService](../../src/AndrewDemo.NetConf2023.Core/Checkouts/CheckoutService.cs) 建立 Order 後，在同一資料庫 transaction 內刪除 CheckoutTransactionRecord。
- 因此目前僅有 ID 對應，成功後無法循此 ID 查回 Checkout；尚未構成持續可查詢的關聯。
- 使用者確認應另設區域保存封存的 CheckoutTransactionRecord；成功後只從作用中區移除，交易紀錄仍可供 Order 查回。此方向更新既有成功後直接刪除 Checkout 的保存政策；目前尚未實作。

### 實作安排

- 第一版採同一個 shop database 的不同 collection 區分作用中與封存紀錄；不必為領域分區另建資料庫。Collection 名稱與存取 API 於實作時具體化。
- 建立 Order、依用途處理庫存、寫入封存紀錄及移除作用中紀錄，放在同一個資料庫 transaction 中提交或回滾。結帳成立前的檢查失敗保留作用中 Checkout，不產生成功封存紀錄；成立後的 fulfillment 失敗不撤銷 Order 或封存紀錄。
- Complete 僅處理作用中 Checkout；封存紀錄不可再次完成。須在交易內確認其仍可完成，防止重複或併發請求再次產生效果；重複請求的具體回覆於實作時定義並同步 spec／testcase。
- Order 以 nullable CheckoutTransactionId 明確關聯，不依賴 Order.Id 與交易 ID 相等。交易 ID 必須跨作用中區、封存區維持唯一，不能因移出作用中區而被重用。
- 封存區已表達完成後保存的生命週期；時間欄位依實作所需選擇，不先要求新增 Status。

## 影響

- 新增用途欄位的持久化物件為 Member、Token、Checkout、Order，共四種；Cart 排除。
- API／CLI 的 command 組合仍明確提供會員；context 不能成為底層取得必填會員的唯一通道。
- Token 簽發、用途不可變驗收及相關文件須同步納入 Token.IsTest。
- 支付輸入時機、Checkout 分區封存及 nullable Order 關聯已定案；白名單與舊資料用途已有明確規則。

## 替代方案

- Token 僅在使用時透過 Member 解析用途、不保存 IsTest：使用者改為建立時複製保存。
- 從 command 移除 RequestMember，完全改靠注入 IdentityContext：使用者否定，要求呼叫端明確組合輸入。
- 直接在 Order 新增 PaymentId：採用 Order 關聯封存交易紀錄的方向，付款來源保存於 Checkout。
- 完成後永久刪除 Checkout：無法保留可供 Order 查回的交易紀錄，改採分區封存。
- 作用中與已完成 Checkout 混放並用狀態區分：使用者選擇另設區域保存封存紀錄。

## 後續工作

- 依已確認方向開始變更 contract、Core 與標準 host，再同步正式 spec 與 testcase。
- 補齊白名單註冊、Checkout 建立、無交易表示、舊資料 false、nullable 關聯、原子性、重複完成及交易 ID 唯一性的驗證。
