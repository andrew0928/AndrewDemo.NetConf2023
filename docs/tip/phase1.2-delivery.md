# TiP Phase 1.2：AdminCLI 實作交付

日期：2026-10-18。分支：feature-test-in-production。使用者已確認規格並授權實作，追加預設 working directory `.env`、保留 ConsoleUI、移除 DatabaseQuery 與 query-litedb.sh。CLI 已完成開發與驗證，最後人工接受確認由使用者依 [驗收流程](phase1.2-final-verification.md) 執行。

## 交付內容

- 新增 .NET 10 `AndrewDemo.NetConf2023.AdminCLI`，命令 `shop-admin`，版本 1.2.0；加入主要 solution。無 Core／LiteDB reference、無新增第三方套件。
- 參數、組態、HTTP／回應 schema、輸出、自我描述分開；stats／list、三個資料源與三種用途、日期及來源限制、分頁／全頁、TXT／CSV／JSON 依定案規格實作。
- dotenv 預設 working directory `.env`，環境優先、明確路徑可覆寫；預設檔缺失可略過，其餘讀取／格式錯誤不忽略。提供 `.env.example`，不打包實際 `.env`。
- HTTP 僅 GET；獨立 Bearer、不跟轉址、不重試、每請求 30 秒、Ctrl+C 取消。驗證 schema／nullable／枚舉／重複 JSON key／分頁漂移，沒有半份成功輸出。
- JSON 保留各次 observations；decimal 不轉 double，超出 decimal 可精確表示的回應拒絕而不四捨五入；TXT 使用繁中標籤、顯示 null 與控制字元轉義；CSV BOM／CRLF／固定欄序及文字公式保護。輸出不覆寫，暫存於目標同目錄，完整後才發布。
- help／version／六個 skill topics 離線可用；直接嵌入 canonical Markdown，無維護第二份指引。
- 移除 DatabaseQuery 專案、其建置產物及 query-litedb.sh；原 [LiteDB 文件](litedb-query.md) 改為遷移入口。ConsoleUI 保留，未修改 Abstract、Core 或 Admin API 契約。

## 自動化與案例覆蓋

新增 [AdminCLI.Tests](../../tests/AndrewDemo.NetConf2023.AdminCLI.Tests/CliTests.cs) 與 [程序／HTTP 測試](../../tests/AndrewDemo.NetConf2023.AdminCLI.Tests/ProcessTests.cs)，最終 **106 項通過**。測試使用隔離 HTTP fixture／暫存目錄；fixture 只有固定測試 token。

| 案例 | 主要驗證 |
|---|---|
| CLI-001～005 | 三來源、stats/list、type、來源限制、非法參數拒絕、名稱 URL encode、offset 轉 UTC／單側範圍；含起點不含終點的業務語意沿用既有 API 測試。 |
| CLI-006～010 | 子程序不同 cwd 預設 dotenv、缺檔略過、明確路徑覆寫；BOM／引號／其他 key、環境與空值優先、破損／不可讀／缺失設定、URL 限制。 |
| CLI-011、207 | 請求只含 GET admin endpoint、Bearer 不放 argv、秘密不出輸出／錯誤、成功回應反射 token 亦拒絕；CLI 無 DB／Docker 依賴。 |
| CLI-101～105 | normal/test/all fixture 統計 300/900/1200、stats 單次 pageSize=1 不加總列、完整統計與部分頁面、未知／0／null／自身 IsTest。 |
| CLI-106～108 | 三來源 × stats/list × 三格式；TXT 長名稱／控制字元／本頁空、JSON metadata、CSV quoting／BOM／CRLF／公式保護與負數 decimal。 |
| CLI-109～110 | 不覆寫／副檔名矛盾／父目錄缺失／stdout I/O 失敗；中途 HTTP／JSON／取消均不發布檔案或留下暫存；Unix 輸出 mode 0600。 |
| CLI-111、301～303 | 子程序異 cwd、無組態離線 help/version/topics；發佈版逐 topic 與 canonical 原文一致、沒有秘密檔入 publish。 |
| CLI-201～203 | 0／1／多頁、各次 observations；count／金額／條件／重複 ID／順序／空頁漂移拒絕；相同 count 更新仍明示非原子。 |
| CLI-204～206 | 非 200、停用與未知路由、安全錯誤、不重試；真實 loopback HTTP 驗證不跟 redirect；傳輸／取消分類、30 秒設定；JSON／型別／缺欄位／枚舉／重複 key／額外欄位。 |
| CLI-304～307 | 六個 topic 人工內容 review：查 test 用 all/test、訂單金額非實收／未知／全頁、TiP 證據邊界、401 不擴張交易／部署授權。這是指引 review，未當成 agent 行為自動化。 |

既有 solution 的 **335 項回歸全部通過**：Core 161、WebContract 134、AppleBTS 19、PetShop Extension 13、PetShop API 8。回歸當時 CLI 是 90 項，其後補齊 CLI 至 106 項並獨立重跑通過；合計目前 441 項的通過證據，不宣稱最後一次單一命令跑了 441 項。

```sh
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet test tests/AndrewDemo.NetConf2023.AdminCLI.Tests --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet publish src/AndrewDemo.NetConf2023.AdminCLI -c Release --no-restore -o .local/admin-cli/publish --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

建置／發佈成功。受 sandbox 網路限制，NuGet vulnerability feed 曾產生 NU1900 警告；既有 API 有 3 項 CS1591 文件警告。沒有以停用 audit 隱藏警告。部分測試執行曾受 sandbox socket 限制中止，取得工具核准後重跑通過，未將中止算成功。

## 已部署 API 驗證

使用發佈版 `shop-admin.dll` 對既有 `http://localhost:5128` 執行，只讀既有資料。驗證 harness 將既有部署憑證透過子程序環境傳入，未將秘密存入證據或 argv；CLI 本身仍不讀 Docker。沒有重建／重啟容器或新增交易。

| 來源 | normal | test | all |
|---|---:|---:|---:|
| member 筆數 | 3 | 1 | 4 |
| checkout 筆數 | 3 | 6 | 9 |
| order 筆數 | 3 | 5 | 8 |
| order 保存總額 | 226 | 272 | 498 |

三來源每頁 2 筆全頁查詢分別完成 2／5／4 次 GET，共取得 4／9／8 筆。三類型統計、test 名稱查詢、帶 +08:00 日期、completed／closedAt 結帳查詢通過；TXT／CSV／JSON 匯出成功，檔案不包含管理憑證。保存結果另核對 test 會員用途、全部有關聯訂單的用途／金額／完成封存狀態，以及 active／created Checkout 8 無對應訂單，均通過。

本機最終發佈版證據保存在 `.local/admin-cli/deployed-uq1kenlm/`（首輪為 `.local/admin-cli/deployed-hlb_337e/`）（忽略版控），包含 9 份統計 JSON、三來源全頁 JSON 與訂單 TXT／CSV；日期與 queriedAt 以各檔內容為準，跨請求不是原子快照。發佈輸出位於 `.local/admin-cli/publish/`，使用方式見 [操作說明](admin-cli.md)。

## 限制與後續

測試／發佈驗證在本機 macOS／.NET 10 執行；未宣稱其他 OS 已驗證。傳輸錯誤與取消使用可控制 fixture，驗證實際 timeout 設定為 30 秒，未逐一製造外部 DNS／TLS 故障或磁碟容量耗盡；寫入錯誤以 I/O fixture／缺目錄／既有檔案驗證。

三個 API 未提供庫存、Token、事件查詢，這些依既有 Core／HTTP 自動化與歷史交付證據；未擴張 API、不用 CLI 查詢成功宣稱所有 TiP AC 通過。最後人工接受確認尚待使用者執行。22 份新增／更新 Markdown 的相對連結、程式碼區塊與 JSON 範例，以及 `git diff --check` 均通過。未自動 commit／push。
