# Phase 3.4：查詢與使用端接入

2026-10-19。已實作及驗證，待 review。

AdminQueryService、API 會員歷史與 ConsoleUI 改由 TransactionStore 讀取。外部 API／DTO、int ID、normal／test／all、active／archived 用語維持；不以 access token 的用途決定讀哪一區。

Admin 預設 normal；all 先將選定 collections materialize 成同一份結果，才過濾、計數、排序及分頁。舊資料未知時間／金額保留既有語意。讀取之前取得共用 `checkout_write_lock`，避免 LiteDB 各 collection 分別取得 read version，導致移轉時漏列或重列。管理查詢遇到用途／唯一性損壞轉為 admin_data_invalid，不去重或任選一筆。

會員歷史合併自己的兩用途 Order，保留買方篩選，不設單筆 transaction header。CommonStorefront／AdminCLI 無需改合約；ConsoleUI 已接入相同 store 並獨立建置。

## 證據

| 驗收 | 測試／實測 |
|---|---|
| STORE-015：normal／test／all、合併分頁、未知值 | AdminQueryTests、AdminQueryHttpTests、TransactionStorageTests.AdminMergesAllPartitionsBeforePaging_AndDefaultReadsOnlyNormal |
| STORE-016：跨 collection 一致性 | AdminQueryTests 的 Direct／Shared 讀取邊界協調；StorageProcessTests 啟動獨立 dotnet writer，六筆兩用途 Checkout 持續完成，同時反覆檢查完整 ID 集合與 Order summary／items 一致 |
| STORE-017：會員兩用途／他人排除、CLI／UI | NormalMemberHistory_IncludesOwnBothPurposesAndExcludesOtherBuyers；標準 API＋AdminCLI normal／test／all JSON 比對；Edge 實際登入與訂單頁 |
| STORE-018：共用資訊 | ExecutionScopeTests 的 Cart 增刪與 protected BSON 比對；MemberServiceTests；temporary seed／API／UI read-back 共用 Member 2 筆、Token 4 筆 |

AdminCLI 多頁仍是多個查詢，不宣稱跨 HTTP requests 的原子快照。完整實測數字與環境見 [3.6](phase3.6-delivery.md)。
