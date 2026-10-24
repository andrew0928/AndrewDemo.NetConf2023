# TiP 交易 collection 隔離驗收案例

依據：[交易儲存規格](../tip-transaction-storage.md)、[Phase 3 計畫](../tip-phase3-plan.md)。狀態：2026-10-19 規格已補齊並獲使用者確認；以下全部為 Phase 3 待實作／待執行案例，既有 Phase 2 綠燈不代表新規格通過。

每案使用隔離的 temporary DB。透過正式 Core／HTTP 入口執行，再以獨立 raw BSON read-back 檢查六個 collections；raw 寫入只用於 migration fixture 或故意損壞資料。無真實 adapter 的效果以可計數替身驗證呼叫邊界，實際整合標示不適用。

## 儲存與用途

| ID | Given | When | Then |
|---|---|---|---|
| TC-TIP-STORE-001 | 會員 true／false × PaymentId 命中／未命中，共四組 | 建立並完成交易 | 僅 false＋未命中落 normal，其餘落 test；Checkout、Order、封存三者同用途，另一用途 collections 完全沒有該 ID。 |
| TC-TIP-STORE-002 | 正式會員以測試 PaymentId 建立 Checkout | 使用自己的正式 Token 完成、查 history | 成功保存 test Order／封存，不因 Token=false 而拒絕；history 含自己的兩用途 Order，其他會員資料不可見。 |
| TC-TIP-STORE-003 | 已保存 test／normal Checkout | 改白名單、換新 scope／Token 後完成 | 原 PaymentId、IsTest、storage 不變；RequestMember 仍須通過認證及買方驗證。 |
| TC-TIP-STORE-004 | 三種交易根物件，分別 normal／test | 從應用存取入口變更用途、指定錯 collection、覆寫封存 | 雙向拒絕；原資料、關聯及兩區筆數不變，沒有跨區複製。 |
| TC-TIP-STORE-005 | 六區均有資料，四個 Checkout 區含最大 ID，Order 有非 Checkout 舊訂單 | normal／test 並行建立、完成，關閉後再建立 | Checkout ID 跨四區唯一，Order ID 跨兩區唯一；不重用封存 ID、不覆寫舊訂單。 |
| TC-TIP-STORE-006 | ID 在同類不同用途或 active／archive 重複；或 raw 用途與區域相反 | 定位單筆或查含該資料的集合 | 失敗並記錄錯誤，不選第一筆、不去重、不執行效果；用途矛盾交易出口 500 execution_context_invalid，admin 為 admin_data_invalid。 |
| TC-TIP-STORE-007 | normal 舊資料缺 IsTest；test 缺欄位；任一區 null／錯型別 | 讀取／操作 | 僅 normal 完全缺欄位視為 false；其餘停止，不能默默歸正式。 |
| TC-TIP-STORE-008 | 正式會員的 test Checkout 與他人 normal／test Checkout | 不同會員嘗試完成 | 自己的 test 可完成；他人兩用途皆 403、無 transaction headers、無 Order／庫存／封存異動。 |

## 原子性與效果

| ID | Given | When | Then |
|---|---|---|---|
| TC-TIP-STORE-009 | normal／test 各一組可完成交易 | 在庫存、Order insert、封存 insert、active delete 各步注入失敗 | 該次資料全 rollback，兩用途各區均無部分結果，庫存還原、不派事件；test 本來就無庫存寫入。 |
| TC-TIP-STORE-010 | 同一 normal 或 test active Checkout | 並行 complete，完成後再重送 | 最多一筆同用途 Order／封存及一次適用效果；其餘維持 400，不從封存重做。 |
| TC-TIP-STORE-011 | 庫存 10、單價 100，正式買 3、test 買 2 | 並行完整結帳與正式統計 | 庫存 7、normal 金額 300、test 金額 200；正式統計只增 1 筆／300；test 無真實付款／出貨／事件統計呼叫。 |
| TC-TIP-STORE-012 | test 交易庫存不足、SKU 關聯錯誤或商品不可購買 | complete | 正常業務拒絕、active 保留；無 Order／封存／虛構 skipped effect。 |
| TC-TIP-STORE-013 | 已提交的 test Order、原 request scope 已結束 | System 新 scope 派送、重送、巢狀／並行事件及 fulfillment 失敗更新 | 依持久化 Order 定位 test 區；所有真實效果仍禁止，包含 handler 嘗試改走一般維護入口的繞道；normal 區不變，封存凍結，其他 scope 不受污染。 |
| TC-TIP-STORE-014 | 已存在的 Order 關聯 Checkout 用途相反，或必要 Order 遺失 | 載入／執行後置效果 | 停止並記錄用途／完整性錯誤；不以 System 或新白名單補救，不向另一區寫入。 |

## 查詢與共用資訊

| ID | Given | When | Then |
|---|---|---|---|
| TC-TIP-STORE-015 | normal／test Order，兩用途 active／archived，含未知時間／金額舊資料 | admin 未指定 purpose、normal、test、all；各條件分頁／日期篩選 | 未指定等同 normal；各區選擇正確；all 合併後再計數、排序及分頁，不以 Buyer 分類；未知值規則與 storage=active/archived 維持。 |
| TC-TIP-STORE-016 | 兩用途交易在持續完成，writer 與 reader 可為不同程序 | 重複 admin 查詢含 active／archive 或兩用途 Order | 每次回應只見完整已提交狀態；無移轉漏列／重列；items 與 summary 同一集合，不混用 collection 的不同版本。 |
| TC-TIP-STORE-017 | 正式會員同時有兩用途 Order，另有他人 Order | 既有會員 API、CommonStorefront、ConsoleUI 查自己的歷史 | 顯示自己兩用途，不洩漏他人；API 路徑、ID 型別不變，不出現單筆 transaction header。AdminCLI normal／test／all 與 API 一致。 |
| TC-TIP-STORE-018 | test Member／Token 與共用商品、SKU、庫存、Cart | 登入、商品查詢、Cart 增刪、test Checkout | 認證資料仍在共用 collections；Cart 變更實際保存，交易不改商品／SKU／庫存；不存在 test_members 或 test_carts 分流。 |

## Identity 指定限制

| ID | Given | When | Then |
|---|---|---|---|
| TC-TIP-STORE-019 | 有效且具一般維護權限的 test identity，商品 100 元 | 由 Core 改價為合法 80 元；HTTP 測試探針映射結果 | 明確 Rejected，HTTP 403／test_identity_forbidden，Behavior=test；商品重讀及後續交易仍 100，沒有 product.update 略過，正常拒絕 log 記目標／原因。 |
| TC-TIP-STORE-020 | normal identity 有維護權限，或未授權／無效認證 | 改價；另驗證 test identity 不可藉綁定 transaction 繞過 | normal 依原規則更新；一般認證／權限拒絕維持；test identity 仍拒絕。交易用途不反向決定獨立維護操作的身分政策，也不變更交易用途。 |
| TC-TIP-STORE-021 | test identity 合法修改 SKU 規格 | 執行既有維護入口 | 維持驗證後略過 sku.update，商品關聯／SKU／庫存不變；不誤套改價的禁止清單。 |

## 離線遷移與啟動

| ID | Given | When | Then |
|---|---|---|---|
| TC-TIP-STORE-022 | 舊三區混合 true／false／缺 IsTest，含未知 BSON 欄位及缺 PaymentId／時間／關聯資料 | 停寫、備份、執行指定 DB 的遷移 | true 移至對應 test 區；false／缺欄位留 normal，ID／所有原欄位完全保留，共用資料不變；不補造歷史。缺必要交易資料後續仍 409。 |
| TC-TIP-STORE-023 | 舊 DB 含 null／錯型別用途、重複 ID、已存在關聯用途矛盾或不明非空目標 | 遷移 | 明確失敗，原 DB 內容／版本不變，不自動修復或覆寫。 |
| TC-TIP-STORE-024 | 合法待遷移 DB | 各搬移步驟與完成標記提交前注入失敗，再重試 | 失敗全 rollback；重試可完成，完成後重跑 no-op；不能遺失／重複資料。 |
| TC-TIP-STORE-025 | 未遷移有資料 DB、新空 DB、已遷移 DB、未知版本 DB | 啟動新 host／執行 seed | 舊資料庫與未知版本拒絕服務並明確提示；新空 DB／seed 直接新配置；已遷移可用，不在 request 內自動搬移或清空資料。 |
| TC-TIP-STORE-026 | 遷移前一致備份與已遷移測試副本 | 演練新版本驗證及停止 writers 後還原 | 原始內容可還原；操作手冊明確禁止舊 writer 接入新 layout，seed 不可冒充 migration。僅在 temporary 副本驗證，不動現有業務 DB。 |

## 證據要求

Phase 3 交付需記錄每個 ID 的測試方法、執行結果與未覆蓋原因；raw collection 筆數、業務資料 read-back、效果呼叫次數與 HTTP 結果共同構成證據。不能以 collection 名稱存在、編譯成功或 Phase 2 測試總數代替隔離驗收。
