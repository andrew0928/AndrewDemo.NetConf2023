# 業績統計與核帳的 Schema 檢驗

日期：2026-10-17。範圍為標準 Core；查詢功能尚未實作。本文件供設計評估，未確認的額外欄位不自動成為 frozen contract。

## 結論

新增 Checkout.ClosedAt 與 Order.CreatedAt 後，Order 的時間、金額、明細與 IsTest 已具備基本資料欄位；但 Checkout 尚無完成／取消結果與獨立金額明細，無法完整支撐「逐筆交易及訂單核帳」。Phase 2 仍須實際填值、封存、建立關聯並正確繼承 IsTest，才能產生可用資料。

此需求對齊共用交易流程主線：須能從持久化結果驗證 TiP 不影響正式業績，而不是只從 log 推測執行結果。

## 逐項檢驗

| 需求 | 現有／本次新增欄位 | 判斷 |
|---|---|---|
| Checkout 起訖時間 | CreatedAt、此次新增 ClosedAt | 可保存開始及結束時間；填值流程留 Phase 2。 |
| 區分 complete／cancel | 尚無持久化終態 | 不足；ClosedAt 只能表示已結束，不能判定成交。不可用 FulfillmentStatus 替代，它屬訂單履約結果。 |
| Checkout 金額／明細 | 只有 CartId、PaymentId，沒有金額快照 | 不足；Cart 可異動，Product 價格亦可異動，不能事後重新計價當歷史成交資料。PaymentId 不是金額。 |
| 已成交 Checkout 查對應金額 | Order.CheckoutTransactionId + Order 的金額／明細 | Phase 2 接入關聯及封存後，可由 Order 反查取得；但交易與訂單共用同一份金額來源，不能獨立比對兩份紀錄，且無法涵蓋未產生 Order 的取消交易。 |
| Order 建立時間 | 此次新增 CreatedAt | 新訂單由建立流程明確提供；舊資料未知保留 null，無法回推精確日期。 |
| Order 金額／明細 | TotalPrice、ProductLines、DiscountLines | 已保存單價、數量、行金額、商品名稱與折扣快照，可不依賴目前 Cart／Product 取得。後續合法更新不得意外覆寫原成交內容。 |
| 排除 test | Checkout.IsTest、Order.IsTest | 欄位足夠；查詢時必須用所查物件自己的 IsTest，不可用 Buyer.IsTest 或目前白名單代替。 |
| 預設排除 test | 尚無業績查詢服務 | Schema 不會自動替查詢加條件。未來查詢預設只含正式資料，明確要求時才列 test，且分別呈現用途，避免併入正式合計。 |
| 交易／訂單互查 | Order.CheckoutTransactionId nullable | 支援正式來源關聯；無關聯訂單需個別列示，不能猜測 Order.Id = TransactionId。新 Checkout 與 Order 應維持明確一對一關係。 |
| 歷史完整性 | 封存 collection 已有，服務尚未寫入 | 尚未具備；成功 Checkout 現行仍被刪除。過去已刪除紀錄不能補造，新增時間欄位也不能還原舊資料。 |

## 最小補強建議（待確認，這次不新增）

1. **Checkout 終態**：保存 Completed／Cancelled，搭配 ClosedAt；作用中與歷史未知的表示須明確。只有完成的正式交易才可計入成交統計，取消應可獨立查核。
2. **Checkout 金額與明細快照**：若要交易與訂單各自具有可核對資料，建議 Checkout 另存總額、商品與折扣明細；完成時與 Order 同次提交，保留相同成交基準。僅透過 Order 取得金額是較小方案，但只適合成交查詢，無法提供獨立交易核帳依據。
3. **取消時的快照語意**：尚未成交不應偽裝成有成交額。若要保存取消前的預計金額／明細，須確認快照時點及其「報價、非成交」語意；本次不自行決定，也不新增 cancel API。

上述額外合約須經確認後再更新規格，Phase 2 不直接擴充 frozen schema。付款成功、退款及沖銷資料目前不存在；本次檢驗只涵蓋專案訂單／Checkout 的記錄對照，不宣稱能與外部金流對帳。

## 後續查詢與資料品質要求

- 訂單期間依 Order.CreatedAt，交易結束期間依 Checkout.ClosedAt；使用 UTC 時間，查詢端將指定時區換算為一致的起訖區間，建議採起點含、終點不含。
- 時間為 null 的舊資料應列為時間未知，不補現在時間、不塞入任意日期業績。
- 舊 BSON 缺少 IsTest 的政策仍為 false；資料庫條件須明確處理缺欄位／null 語意，不能只因 C# 讀取預設 false 就假設原始查詢能得到相同結果。查詢功能交付時須驗證。
- 正式會員使用測試 PaymentId 可產生 test Order；只篩 Buyer.IsTest 會把這些資料算進正式業績。
- 交易與訂單是兩個核對視角，關聯後不得重複加總為兩筆業績。
- FulfillmentStatus=Failed 仍可代表已成立訂單；不能用它推斷交易取消或付款失敗。
- 後續可按查詢計畫補時間、用途、Order.CheckoutTransactionId 索引；缺索引是效能議題，不是缺少歷史快照的替代解法。

## 本次交付與 Phase 2

此次只新增兩個 nullable UTC 時間欄位、持久化測試及正式規格補充。既有 Checkout complete 填入 ClosedAt、Order 建立填入 CreatedAt、封存與用途傳遞均留 Phase 2。額外終態／金額快照仍待確認；查詢 API／UI 後續再做。

使用者另指定後續移除 access token 明文 log：Phase 2 清查 API middleware、OAuth 登入與 token exchange 的所有輸出路徑；本次不修改執行 log 程式。
