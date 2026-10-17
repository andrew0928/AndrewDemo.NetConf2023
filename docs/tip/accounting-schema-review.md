# 業績統計與核帳的 Schema 檢驗

日期：2026-10-17。範圍為標準 Core；查詢功能尚未實作。

## 最新決策與結論

使用者已確認 Checkout 保存 State，終態完整搬移後凍結；獨立金額與交易明細 **create 時固定**，不保存商品資訊快照。Canonical 封存集合維持 `archived_checkout_transactions`，不另建 checkout_archived。詳見 [決策](../decisions/2026-10-17-checkout-state-and-transaction-details.md) 與 [正式規格](../../spec/tip-checkout-lifecycle.md)。

**Schema 已補齊本輪交易／訂單核對所需的狀態、時間、獨立金額明細及用途欄位。** Phase 2 仍須實作 create 計價、complete 沿用、封存、凍結、關聯與用途繼承，才能實際產生可用資料；欄位存在不等於已可核帳。

## 逐項檢驗

| 需求 | Schema | 實作要求 |
|---|---|---|
| Checkout 起訖時間 | CreatedAt、ClosedAt | 建立與終態時保存 UTC；舊時間未知保留 null。 |
| 區分 complete／cancel | State：Created、Completed、Cancelled；Unknown 保留舊資料未知語意 | 必要檢查失敗不改為 Cancelled；只把 Completed 算成交。 |
| Checkout 金額／明細 | TotalPrice、ProductLines、DiscountLines | create 時獨立保存識別、數量、交易單價、行金額及折扣；complete 不重讀 Cart 或現價計算。 |
| 不保存商品資訊快照 | CheckoutProductLine 無 ProductName／描述／規格 | 商品與 SKU ID 可保存；交易單價屬交易事實。折扣名稱／說明屬已套用折扣紀錄。 |
| Order 時間與金額明細 | CreatedAt、TotalPrice、ProductLines、DiscountLines | 複製 Checkout 成交數值與折扣，另保留 Order 既有顯示資訊；兩者集合獨立。 |
| 交易／訂單互查 | Order.CheckoutTransactionId nullable | 新 Checkout 成交與 Order 明確一對一；非 Checkout／舊資料不捏造關聯。 |
| 排除 test | Checkout.IsTest、Order.IsTest | 預設依各物件自身用途過濾，不依 Buyer.IsTest、Token 或目前白名單。 |
| 取消可查核 | Cancelled 保留同份 create 金額／明細與 ClosedAt | 保存原交易條件但不計成交，不因此建立 Order；取消入口後續再做。 |
| 完整封存與凍結 | 同型別的 archived_checkout_transactions | 原子搬移所有欄位；服務／儲存入口拒絕後續改寫、刪除或重新啟用。 |

## 已收斂的設計

- 狀態與終止原因已定案，ClosedAt 不單獨作為成交判斷依據。
- 交易與訂單各自有金額明細，無須事後從可變的 Cart／Product 重算，也不只共用 Order 的單份數值。
- Create 後變更 Cart、商品價格或折扣規則，不改變既有 Checkout；更改交易內容須重新 create。
- Complete 仍檢查買方、商品可購買性、SKU 關聯與庫存；固定價格不等於保證商品或庫存永遠可用。
- Unknown／缺少固定內容的舊作用中紀錄要求重建，不補造歷史金額。零元交易與未知金額分開表示。

## 後續查詢與資料品質要求

- 正式成交查詢：Checkout.State=Completed 且 IsTest=false，以 ClosedAt 歸期。Order 以自身 IsTest=false 與 CreatedAt 查詢；二者核對而不重複加總。
- 明確要求時才列 test；不可因 Buyer.IsTest=false 就把測試 PaymentId 產生的交易算進正式業績。
- 取消交易可列明細作核對，但金額代表原交易條件，不是成交收入。
- 時間／金額／狀態未知的舊資料個別列出，不以現在時間或數值 0 補造。過去已刪除的 Checkout 無法由新欄位還原。
- 舊 BSON 缺少 IsTest 的政策仍為 false；後續資料庫查詢須驗證缺欄位語意，不只依賴 C# 讀取預設值。
- 查詢時間先換算一致 UTC 區間，建議起點含、終點不含。可依查詢計畫補時間／用途／關聯索引。
- FulfillmentStatus=Failed 仍可能是已成立訂單；不代表 Checkout 取消或付款失敗，不能改寫已凍結封存。
- 本專案未有真實付款成功、退款及沖銷資料；本次支援的是 Checkout／Order 紀錄核對，不宣稱能與外部金流對帳。

## 本次交付與 Phase 2

本次追加 schema、正式規格及持久化案例；服務計價、狀態轉移、原子搬移、防止封存改寫均留 Phase 2。取消入口及查詢 API／UI 後續再做。Phase 2 仍包含先前指定的 access token 明文 log 清理。
