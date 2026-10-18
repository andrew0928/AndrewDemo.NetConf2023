# TiP 會員白名單改以 Name 判定

## 狀態

accepted。使用者已確認預設名稱 test，名稱比對沿用現有會員查找規則。

2026-10-18 使用者明確要求：「我要的白名單，是 user 可判別的 name, not id」，並要求先釐清登入後查不到會員的異常，再修改規格。

**重大決策、影響 .Core、回頭修正 Phase 1、影響 .Abstract / spec**：實際改動位置為 Core TestAllowlist 與正式 spec；目前沒有需要修改 Abstract 的欄位或介面。

## 背景

原規格以首次建立後的 MemberId 命中白名單，但 UI 登入輸入的是 Name，使用者無法透過登入名稱選擇測試帳號。登入名稱 `900001` 不等同 ID `900001`，造成操作上的誤解。

## 決策

- Member 首次建立時以 Name 比對會員白名單，不再以 MemberId 分類。
- Member.Id 繼續作為資料識別與 Token／交易關聯，不作為會員白名單判定值。
- Payment 白名單仍以 PaymentId 判定；既有用途不可變、Token 繼承與 Checkout 建立時固定用途的原則保留。
- 不保留長期 Name／ID 雙軌會員判定。
- 預設測試名稱為 `test`，依作用中 LiteDB 資料庫的 collation 比對；不額外 trim 或轉數字。PaymentId 預設仍為 `990001`。

## 影響

需同步 tip-test-allowlist、標準流程、Core 物件規格、Phase 1／Phase 2 交接、相關 testcase 與架構文件；實作時同步 Core TestAllowlist、MemberService 及測試。歷史交付證據保留其當時語意，追加本決策連結。

## 替代方案

保留數值 ID 並預先建立測試會員，仍不符合使用者要求的可辨識 Name 白名單，不採用。

## 後續工作

登入查核見 [會員查詢差異紀錄](../tip/2026-10-18-member-query-investigation.md)。已確認預設名稱與比對規則，本次僅修訂 frozen spec 的會員白名單相關範圍；這次授權不等同重新開啟其他已定案契約或重新分類資料。

2026-10-18：Name 白名單規格、實作、測試與本機 API 部署已完成，見 [交付紀錄](../tip/member-name-allowlist-delivery.md)。
