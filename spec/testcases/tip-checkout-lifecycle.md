# Checkout 狀態與固定交易內容測試案例

2026-10-19 修訂：與 [交易隔離案例](tip-transaction-storage.md) 共同驗收 Phase 3。下方 Phase 1／2 證據是當時版本；本次新增／變更要求尚未實作或驗證。

依據：[正式規格](../tip-checkout-lifecycle.md)。區分 schema 證據與尚未實作的流程驗收。

Phase 3 將下列 normal／test fixture 寫入各自 active／archive，Order 亦同用途分區；CHECKOUT-105～109 必須檢查所有相關 collections，而非只驗證 IsTest 欄位。ID 唯一性擴及四區，錯區與 rollback／事件驗收對照 STORE-004～014。固定報價案例中的商品改價由具權限的 normal identity 執行，不使用被禁止的 test identity 改價。

## 已有持久化測試

| ID | 案例 | 證據 |
|---|---|---|
| TC-TIP-CHECKOUT-001 | Created 紀錄轉成 Completed／Cancelled，正式／test 各一案；完整寫入封存後所有 BSON 欄位一致，保留價格、數量、折扣、狀態、用途及時間 | CheckoutRecordPersistenceTests.TerminalRecord_PreservesAllFieldsWhenStoredInArchive，4 筆。測試直接操作 collection，不代表服務已封存／凍結。 |
| TC-TIP-CHECKOUT-002 | 新資料明確 0 與舊 BSON 缺金額不同；舊資料 State=Unknown、TotalPrice=null、時間 null、明細空 | ExplicitZeroAmount_IsDifferentFromLegacyMissingAmount。 |
| TC-TIP-CHECKOUT-003 | 保存後修改呼叫端集合，另存 Order 的顯示明細 | StoredTransactionDetails_AreIndependentOfCallerAndOrderCollections；Checkout 自己的行關係與數值仍可讀取。 |

TC-001 亦檢查實際 BSON 的 ProductLines 只有交易欄位，沒有商品名稱、描述、規格或完整 Product。

## Phase 2 流程驗收

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-CHECKOUT-101 | create：商品 100、2 件、折扣 -20 | 保存 Created／TotalPrice=180、2 件明細與折扣，ClosedAt=null，庫存未扣減。 |
| TC-TIP-CHECKOUT-102 | create 後改 Cart 數量／刪除 Cart、商品改價或折扣停用，再 complete | 仍以原交易 2 件、180 成交，不從 Cart／現價重新計算；商品及庫存檢查照常。 |
| TC-TIP-CHECKOUT-103 | create 後商品不可購買、被刪除、SkuId 改變或庫存不足 | complete 正常拒絕，State 仍 Created、ClosedAt=null，原明細及金額不變，無 Order／封存。 |
| TC-TIP-CHECKOUT-104 | create 缺必要商品或計價失敗 | 不保存部分 Checkout；API 400（業務拒絕）或既有內部錯誤結果，不偽造 Created。 |
| TC-TIP-CHECKOUT-105 | 正式／test 完成同份固定交易 | Order 交易數值與 Checkout 一致，IsTest 正確，ClosedAt／Order.CreatedAt 相同；僅正式扣庫存。 |
| TC-TIP-CHECKOUT-106 | 終態、Order、封存、移除作用中紀錄任一步注入失敗 | 全部 rollback；不留下半套狀態／時間／金額，commit 前不派送事件。 |
| TC-TIP-CHECKOUT-107 | 成功後重送或並行 complete | 至多一筆 Order／封存，無重複效果；封存原 ID 不被新交易重用。 |
| TC-TIP-CHECKOUT-108 | 嘗試修改封存的狀態、時間、用途、付款來源、金額或任一明細，或刪除／重啟 | Core 受控寫入入口拒絕，重新讀取整筆仍不變；不能只驗證 init 語法。 |
| TC-TIP-CHECKOUT-109 | 履約失敗或事件重送 | Checkout 仍 Completed，封存內容完全不變；只更新適用的 Order 履約結果。 |
| TC-TIP-CHECKOUT-110 | 舊作用中交易缺 State／固定交易內容 | 409／checkout_recreate_required，要求重建，不依現價補資料；原紀錄與庫存不變。 |
| TC-TIP-CHECKOUT-111 | 已保存金額與明細合計矛盾 | 停止完成並記錄資料完整性錯誤，不重新計價掩蓋問題。 |

## 後續取消與查詢驗收

- 取消入口提供後：Created → Cancelled，保存 ClosedAt、完整搬移並凍結；金額與明細仍為 create 時內容，不建立 Order、不列成交額。取消 API 不在本輪新增範圍。
- 查詢功能提供後：預設只計 Completed 且非 test 的交易；與 Order 核對金額、明細及關聯；未知歷史資料個別列出，不以另一筆 Order 或當前 Cart 補造。明確列 test 時仍不混入正式合計。
