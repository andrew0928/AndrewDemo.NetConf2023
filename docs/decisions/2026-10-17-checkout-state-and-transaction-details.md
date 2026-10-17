# Checkout 狀態、完整封存與獨立交易明細

## 狀態

accepted；使用者已確認 create 時固定金額與明細。Core schema 與正式規格已補齊，執行流程留 Phase 2。

確認日期：2026-10-17。

**重大決策；影響 .Core。回頭修正 Phase 1；影響 .Abstract / spec。** 本次使用者確認前次 schema 檢驗中的狀態及獨立金額明細方向；資料定義與 [正式規格](../../spec/tip-checkout-lifecycle.md)／測試案例已同步。不先修改既定 Abstract。

## 背景

使用者要求從資料庫取得交易與訂單的金額及明細，用於業績統計與核帳，並預設排除 test。只有 Checkout.ClosedAt 無法區分完成與取消，只有 CartId 也無法保留歷史交易內容。

本次補強共用 Checkout 的持久化邊界，對齊 roadmap 的標準交易與模組化主線。

## 決策

1. Checkout 保存 `State`，對應交易狀態機。狀態是持久化交易的生命週期，不使用 CheckoutCompleteStatus（單次呼叫結果）或 Order.FulfillmentStatus 代替。
2. 到達完成／取消等終止狀態後，完整搬移至封存區，成功保存後才從作用中區移除。狀態、ClosedAt、用途、付款來源、金額與明細均須保留；搬移須原子提交。
3. 封存後凍結整筆紀錄，不再改狀態、金額、明細、用途、時間或其他欄位，也不可還原後再次 complete。凍結由後續儲存／服務邊界保證，不能只依靠 C# init。
4. 封存集合沿用既有命名規則：`archived_checkout_transactions`，對應 C# `ArchivedCheckoutTransactions`。這就是使用者所稱 checkout_archived 的 canonical 名稱；目前 repo 已使用此名稱，不新增第二個集合或別名。
5. Checkout 保留獨立金額與交易明細，不保存商品資訊快照。交易資料可包含商品識別、購買數量、該筆交易單價、行金額與折扣金額；不複製 Product 名稱、描述、規格或整份 Product。既有 Order 的商品快照不在本次移除範圍。
6. 交易與訂單各自保存 IsTest；正式業績與核帳預設排除 test，明確要求時才列出。交易及其關聯 Order 為兩個核對視角，不重複加總。

## 金額與明細固定時點

使用者明確回覆：「create 時固定」。

- Create 時計價、計算折扣並固定獨立交易明細，complete 沿用，cancel 保留同份內容但不列成交。
- 之後修改 Cart、商品價格、折扣設定，不改變已建立的交易；要變更內容重新 create。
- Complete 仍檢查身分、商品可購買性及庫存，不重新計價；新 Order 複製交易數值與折扣。
- 此決策明確取代目前 complete 時從 Cart 計價的設計；Phase 2 依新規格調整。

## 影響

- 最小狀態機為 Created → Completed 或 Cancelled；Completed／Cancelled 為終態。必要檢查失敗維持作用中，不自動等同取消。enum 另有 Unknown=0 供舊資料表達未知；不得假裝為 Created。
- 封存現在涵蓋所有已定義的終態，不限成功成交。ClosedAt 表示結束時間，State 決定結束原因。
- 交易的單價與金額是該筆交易的事實，不因不保存商品資訊快照而省略。
- 不新增 cancel API、查詢服務、真實金流或退款能力；執行流程仍留 Phase 2。
- 原 [核帳檢驗](../tip/accounting-schema-review.md) 中「是否保存 State／獨立交易資料」已確認；金額固定時點已確認為 create。

## 替代方案

- 完成後刪除 Checkout，只靠 Order 查金額：無獨立交易內容，不採用。
- 封存時只存部分摘要：不符合完整搬移與凍結要求。
- 複製整份 Product 或使用 OrderProductLine 作為 Checkout 明細：會夾帶商品名稱等快照，應定義 Checkout 自己的交易明細模型。
- 封存同時保留 checkout_archived 別名：造成雙軌命名，沿用 canonical 集合即可。

## 後續工作

- Core 欄位、正式 spec、testcases 與 schema 檢驗結論已同步；依 Phase 2 驗收案例接入實際流程。
- Phase 2 實作狀態轉移、原子搬移、封存寫入保護及交易／訂單內容一致性驗證。
- 保留前次已確認的 Phase 2 工作：時間填值、用途傳遞、移除 access token 明文 log。
