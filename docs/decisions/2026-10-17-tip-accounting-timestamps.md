# TiP 核帳時間欄位與 Schema 檢驗

## 狀態

accepted，2026-10-17；接受範圍為兩個時間欄位、預設排除 test 的查詢原則，以及 Phase 2 移除 access token 明文 log。額外 Checkout 終態與金額快照目前僅為評估建議。

**重大決策；影響 .Core。回頭修正 Phase 1；影響 .Abstract / spec。** 本次由使用者明確追加規格，限於 Core 與 spec；不需變更 Abstract 簽章。

## 背景

使用者希望事後能從 DB 取得交易與訂單的金額及明細，用於業績統計與核帳，並明確排除 test，必要時才列出。既有 Order 缺少建立時間，Checkout 只有起始時間；執行 log 亦不構成完整交易紀錄。

此需求延續共用交易流程與 TiP 效果隔離的 roadmap 主線。

## 決策

1. CheckoutTransactionRecord 新增 `DateTime? ClosedAt`：完成或取消時的 UTC 結束時間。作用中為 null；舊資料未知亦保持 null，不偽造時間。ClosedAt 不單獨表達結束原因。
2. Order 新增 `DateTime? CreatedAt { get; init; }`：訂單成立時的 UTC 時間。新訂單由建立流程明確填入，舊資料未知為 null，後續履約更新不改寫。
3. 本次交付 schema、規格及持久化測試。時間填值、封存、用途繼承與其他執行流程留 Phase 2；查詢功能後續再做，不新增 cancel 入口。
4. 業績／核帳查詢預設排除 test；依 Checkout／Order 自己的 IsTest 過濾。只有明確要求時才列 test，正式統計不得默默包含 test。
5. 後續 Phase 2 移除 access token 明文 log，包含 API middleware 與 OAuth 路徑；本次僅記錄後續工作。

## 影響

- Order 已有金額、商品與折扣快照，新增時間後具備基本期間查詢欄位；實際資料仍依賴 Phase 2 填值。
- Checkout 還缺結束原因與獨立金額明細。透過 Order 關聯只足以取得已成交訂單資料，無法獨立核對交易快照，也不涵蓋取消交易。
- 詳細落差與待確認方案見 [Schema 檢驗](../tip/accounting-schema-review.md)，不把尚未確認方案寫成既定合約。
- 舊資料無時間時不能歸入任意期間；不自動遷移或猜測。

## 替代方案

- 將取消時間也命名為 CompletedAt：容易與成功完成混淆，採中性的 ClosedAt。
- 時間使用非 nullable 或預設現在時間：會替歷史資料創造假的時間，故不採用。
- 事後從 Cart／Product 重算歷史金額：資料可變，不能還原成交當時的金額。
- 本次直接新增完整核帳服務、終態與交易快照：超出兩個欄位的追加範圍，待確認後另行更新規格。

## 後續工作

- Phase 2 實作新資料時間填值、原子封存、IsTest 繼承及明文 token log 移除。
- 確認 Checkout 終態與是否保存獨立金額明細，再決定取消時的快照語意。
- 後續查詢驗收涵蓋時間邊界、未知歷史時間、預設排除 test、顯式列出 test 與交易／訂單不重複加總。

## 後續定案

前述待確認的 Checkout State 與獨立交易內容已由使用者確認，並指定 create 時固定；見 [後續決策](2026-10-17-checkout-state-and-transaction-details.md)。本文件保留時間欄位初批確認的歷史範圍。
