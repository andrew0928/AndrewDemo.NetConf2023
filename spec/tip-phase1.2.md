# TiP Phase 1.2：Admin CLI 規格

日期：2026-10-18。分支：`feature-test-in-production`。狀態：使用者已確認規格並授權實作；實際結果見 [交付紀錄](../docs/tip/phase1.2-delivery.md)。

## 目標與交付

使用者要求補足 admin CLI 規格，後續以 CLI 做 TiP 成果的最後確認。本案為 Phase 1 的追加設計與後續已授權實作，承接 Phase 1.1 管理查核 API 與已交付的 Phase 2.1～2.7；不新增交易行為，不將規格文件當成可執行 CLI。

使用者已確認：

1. 使用 .NET 開發獨立 CLI。
2. 組態取自環境變數，預設讀 working directory 的 `.env`，另提供 `--env-file` 覆寫路徑。
3. `--help` 給人讀；`--skill` 提供 agent 指引，按 topic 說明不同情境，採漸進揭露與具體案例。
4. Query only，分統計與明細，支援 TXT／CSV／JSON。
5. 可選資料源 member／checkout／order、類型 all／normal／test、起訖時間；**會員不接受起訖時間，訂單／結帳支援**。

| 文件 | 用途 |
|---|---|
| [CLI 合約](tip-admin-cli.md) | 命令、組態、輸出、錯誤與一致性。 |
| [CLI 案例](testcases/tip-admin-cli.md) | 後續實作與 agent 指引驗收。 |
| [Agent 指引入口](skills/admin-cli/SKILL.md) | `--skill` 索引及各 topic 的 canonical 內容。 |
| [最終驗收流程](../docs/tip/phase1.2-final-verification.md) | 使用者後續執行的命令及 TiP 證據邊界。 |
| [追加決策](../docs/decisions/2026-10-18-tip-admin-cli.md) | 已確認需求、設計選擇與影響。 |

## 專案與契約

- 專案：`src/AndrewDemo.NetConf2023.AdminCLI`，.NET 10 console app；命令名稱 `shop-admin`。不擴充既有具交易能力的 ConsoleUI。
- 使用 HttpClient、CLI 自有 HTTP DTO／輸出投影，不參考 Core／LiteDB，不直接讀 DB；無需新增 Abstract 業務介面或模型。
- Phase 1.1 的 HTTP endpoints、認證、schema、篩選與統計口徑保持定案。CLI 命令是 adapter 層；source=order 對應 `/orders`、type 對應 API purpose，不改名既有領域欄位。
- 本次 .NET 設計取代 Phase 1.1 尚未實作的 shell CLI 參考建議。早先 chat 中的 `admin_query 'orders?purpose=normal'` 只是示例，不保留第二套 shell 函式或 query-string 文法。
- 第一版 stats：三個資料源均提供 totalCount；order 另有 totalAmount／knownTotalAmount／unknownAmountCount。checkout 不額外加總營收；不新增按日分組、圖表、庫存、Token、事件或自動對帳 API。

## 階段完成條件

使用者已 review 定案並授權開發；實作交付條件：

1. 建立 .NET CLI 與測試專案，封裝與版本一致的 help／skill 內容，提供 build／publish／執行說明。
2. 驗證參數、dotenv 優先序、HTTP 認證、分頁、TXT／CSV／JSON、失敗退出碼及 agent 指引案例。
3. 對隔離 fixture 與已部署 API 分別驗證；記錄實際結果，不將既有 335 項測試算成新增 CLI 測試。
4. 使用者依驗收流程保存查詢證據，搭配既有 Core／HTTP／事件測試，完成標準 TiP review。

最終報告分別標示 CLI 已查核、自動化已驗證、未驗證、不適用。三個 admin 查詢的成功不能單獨證明庫存未扣減、Token 繼承或事件隔離；若未來要求純 CLI 直接查核這些內容，須另開 HTTP 合約設計。

## 規格階段文件檢查（實作前）

已檢查 19 份新增／更新 Markdown 的相對連結、程式碼區塊及 JSON 範例，六個 topic 索引與檔案對應正確，36 個案例 ID 無重複，`git diff --check` 通過。這些是規格檢查，不是 CLI 功能測試。

Agent 指引依 skill-creator 的短索引、按情境揭露、具體例子與授權邊界原則整理。其 quick_validate 工具因本機 Python 缺少 PyYAML 無法執行，改以 Ruby YAML parser 驗證 frontmatter／名稱／描述，並另查 topic 與連結；不宣稱該工具已通過。未變更 source／Abstract，未部署、commit 或 push。
