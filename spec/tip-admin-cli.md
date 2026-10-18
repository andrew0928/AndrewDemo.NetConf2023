# TiP Admin CLI 合約

日期：2026-10-18。歸屬：[Phase 1.2](tip-phase1.2.md)。狀態：使用者已確認定案並授權實作。HTTP 基準：[管理查核 API](tip-admin-query.md)。

## 執行模型

.NET 10 獨立 console app，命令 `shop-admin`；公開入口不是 shell script，也不依賴 curl、jq、Python、Docker 或 DB 檔案。程式內部分為參數／組態、HttpClient 查詢、DTO 驗證、輸出 renderer、內嵌 help／skill 文件，採薄層組裝，不建立另一套統計服務。

僅執行 GET `/api/admin/orders`、`/api/admin/checkouts`、`/api/admin/members`。不登入會員、不產生交易、不寫資料、不控制容器或設定 token；使用者已啟動並設定 API 後再呼叫。

## 命令文法

```text
shop-admin stats --source <member|checkout|order> [查詢條件] [輸出選項] [--env-file PATH]
shop-admin list  --source <member|checkout|order> [查詢條件] [分頁選項] [輸出選項] [--env-file PATH]
shop-admin --help
shop-admin <stats|list> --help
shop-admin --skill [topic-id]
shop-admin --version
```

選項可放在子命令後任意順序；值採獨立 argument，不提供縮寫或 `key=value`／任意 route 文法。未知、重複或不適用選項必須拒絕，不能忽略後擴大查詢。`--source` 必填，不跨多資料源自動合併。

| 選項 | 規則 |
|---|---|
| --type | all／normal／test；預設 normal，映射 API purpose。all 是合併集合，不分組，也不把合併總額稱作正式營收。 |
| --from、--to | 訂單／結帳可用；ISO 8601，秒必填，可含小數秒，須有 Z 或 offset。UTC 後為 `[from,to)`，可單側，雙側須 from < to。member 給任一日期即本地拒絕，不送 HTTP。 |
| --member-id | 三個來源可用，正整數；用於縮小核對範圍。 |
| --name | 僅 member，沿用 API 精確名稱／collation；與 member-id 取交集，數字名稱不轉 ID。 |
| --state | 僅 checkout，all／unknown／created／completed／cancelled；預設 all。 |
| --time-field | 僅 checkout，createdAt／closedAt；預設 createdAt。order 固定 createdAt，不接受此選項。 |
| --page、--page-size | 僅 list，預設 1／50，page 正整數、page-size 為 1～100；映射 API page／pageSize。 |
| --all-pages | 僅 list，從第 1 頁循序讀完符合條件的資料；可搭配 page-size，不能搭配 page。預設不啟用，即使輸出檔案也只查當頁。 |
| --format | txt／csv／json，預設 txt；不依 TTY 或副檔名自動切換。 |
| --output | 選填，輸出至檔案；未給則 stdout。不存在的父目錄回錯誤；已存在目標不覆寫。 |
| --env-file | 選填，覆寫 dotenv 路徑；預設為 working directory 的 `.env`。 |

CLI 本地驗證文法、枚舉、整數範圍與帶時區的時間；API 仍為業務驗證權威。所有值只當資料並正確 URL encode，不做 shell 展開、eval 或命令執行。省略 type 時仍在輸出摘要明示有效 normal；`--name test` 不會隱含 test，應搭配 `--type all` 或 test。

```text
shop-admin stats --source order --type normal
shop-admin stats --source checkout --type test --state completed --time-field closedAt
shop-admin list --source member --type all --name test
shop-admin list --source order --type all --member-id 4 --page-size 20
shop-admin list --source checkout --type test --from 2026-10-18T00:00:00+08:00 --to 2026-10-19T00:00:00+08:00
shop-admin list --source order --type test --all-pages --format csv --output test-orders.csv
shop-admin stats --source order --type normal --format json --output normal-stats.json --env-file .local/admin.env
```

## 組態與 dotenv

組態沿用 `SHOP_ADMIN_BASE_URL`（預設 `http://localhost:5128`）與 `SHOP_ADMIN_ACCESS_TOKEN`（必填）。優先序為 **程序環境變數 > dotenv > 預設值**。環境變數已存在但為空，視為明確覆蓋；空 token 報錯，空 base URL 報錯，不偷偷回退。

dotenv 最小可攜格式：UTF-8，可含 BOM；逐行 `KEY=VALUE`，空行與以 `#` 開頭的註解可用；key 與未加引號 value 的外側空白移除。value 可用成對單／雙引號包覆，內容原樣保留；不執行 escape、變數替換、shell command、export 或多行值，不解讀行尾註解。重複 key／破損引號／非法行為設定錯誤，訊息只含行號，不回顯原行或值。其他合法 key 忽略，僅讀取上述兩個設定，不修改父程序環境。

預設 working directory 的 `.env` 不存在時略過，繼續使用環境變數與預設值；若存在但無法讀取則失敗。`--env-file` 明確指定的檔案不存在或無法讀取即失敗，即使環境變數已齊全也不忽略。實作時提供 `.env.example`，實際 `.env` 不入 Git、不複製到 build／publish 輸出。只出現路徑的 `--env-file` 不是 token argument；不另提供 `--token`、`--base-url` 或自動讀 Docker／會員 Token 的選項。

base URL 只接受 scheme／host／選填 port 與根路徑，尾端 `/` 正規化；不接受 userinfo、query、fragment 或額外 path。除 host 精確為 localhost／127.0.0.1／[::1] 外必須 HTTPS；HTTPS 使用預設憑證驗證。token 必須符合 `^admin_[a-z0-9]+$`，不 trim、不轉大小寫，真正授權仍由 API 精確比對。

HttpClient 使用 Authorization Bearer，不跟隨 redirect、不自動重試；每個請求逾時 30 秒，Ctrl+C 取消剩餘工作。token 不在 URL、argv、stdout、stderr、匯出檔或暫存檔。CLI 不自動修正網路路由或服務端停用設定。

## 統計與明細

`stats` 對指定來源送一次 page=1、pageSize=1 查詢；使用 API 全符合條件統計，忽略該頁列資料，不遍歷清單自行加總。

| source | stats 指標 | list 明細 |
|---|---|---|
| member | totalCount、excludedUnknownTimeCount | id、name、isTest |
| checkout | totalCount、excludedUnknownTimeCount | transactionId、memberId、cartId、paymentId、isTest、state、createdAt、closedAt、totalPrice、storage |
| order | totalCount、excludedUnknownTimeCount、totalAmount、knownTotalAmount、unknownAmountCount | id、memberId、memberName、isTest、createdAt、totalPrice、fulfillmentStatus、checkoutTransactionId |

checkout 統計是指定 state／時間口徑的筆數；不把全部 Checkout 金額當成交，亦不將其與 Order 加總。order 金額是保存的訂單金額，不表示實收；Failed 履約訂單仍按既有口徑計入。缺金額時 totalAmount=null，knownTotalAmount 與 unknownAmountCount 必須一併呈現，null 不替換成 0。

## 分頁與一致性

單頁 list 只送一次 HTTP，超出最後頁時 rows 空，但 API totalCount／總額保留。`--all-pages` 將各頁依 API ID 降冪串接，記錄每頁 queriedAt／統計；沒有跨頁或跨 endpoint 原子快照。

全頁模式依首頁 totalCount 決定預期筆數／頁數。後續 totalCount、有效條件、orders 金額統計改變，ID 重複／順序異常、提前空頁或筆數不符時停止，回「查詢期間資料改變」，不輸出部分成功；不自動重試。這些檢查只能發現部分漂移，即使通過，也不能宣稱快照一致。最後驗收應在沒有其他寫入的期間執行，記錄各次 queriedAt。

## 輸出合約

三種格式都由同一查詢結果投影；先完成所有讀取、schema／分頁驗證與格式化，再發布結果，避免半份成功輸出。數值使用 .NET decimal 與 invariant culture，不轉 double、不強制四捨五入至兩位、不自行宣告貨幣種類。時間維持 API UTC；控制字元在 TXT 中可見轉義，不執行 ANSI 序列。

成功 schema 必須符合 Phase 1.1：JSON object、所需欄位／型別／nullable／枚舉合法；允許額外欄位，但缺失／非法核心欄位、重複 JSON key 或不合法回應視為失敗，不假造 normal、空清單或總額。

### TXT

UTF-8、繁體中文標籤，適合直接閱讀。先顯示命令、來源、有效 filters、查詢時間；stats 顯示完整指標；list 顯示頁次／當頁或已讀筆數／符合條件總數，再逐筆顯示所有列欄位。長名稱不截斷。

用途標示 normal／test 並保留 IsTest=true／false；state／storage／fulfillmentStatus 保留 API 原文。null 顯示 `未知 (null)`，真實 0 顯示 0；本頁空但 totalCount>0 時明示「本頁無資料」。list 的 orders 摘要可顯示 API 總額，但明示涵蓋全部符合資料，不能標成當頁合計。全頁模式明示「跨請求讀取，非原子快照」。

### JSON

為 CLI 的 **schemaVersion=1 envelope**，與 API 原始 JSON 區別如下：

```json
{
  "schemaVersion": 1,
  "command": "list",
  "source": "order",
  "filters": { "purpose": "normal", "memberId": null, "from": null, "to": null },
  "pagination": { "mode": "page", "page": 1, "pageSize": 50 },
  "observations": [{ "page": 1, "queriedAt": "2026-10-18T14:00:00Z", "totalCount": 0, "excludedUnknownTimeCount": 0, "totalAmount": 0, "knownTotalAmount": 0, "unknownAmountCount": 0 }],
  "rows": []
}
```

filters 使用 API 有效值並去除 page／pageSize；pagination mode 為 stats／page／all，stats 的 page／pageSize 為 null，all 的 page 為 null、pageSize 為實際值。observations 保留每次 API queriedAt 與所有適用統計，僅 order 有三個金額欄位，不捏造其他來源金額。stats rows=[]；list rows 保留上表的 API camelCase 列 schema，不加會員推導用途、虛構 PaymentId 或空值預設。JSON number 保留 decimal 精度，日期字串與 null 不變。stdout 不混入說明／進度。

### CSV

UTF-8 BOM、逗號分隔、CRLF，固定一列 header；欄位含逗號／引號／換行時加雙引號，內部引號重複。不依地區改分隔符。CSV 是查詢投影，完整的分頁／查詢時間證據另用 JSON 保存。

- stats：固定一筆資料，欄序為 source、type、memberId、name、state、timeField、from、to、queriedAt、totalCount、excludedUnknownTimeCount、totalAmount、knownTotalAmount、unknownAmountCount。不適用欄位空白，最後三欄只適用 order。
- list：欄序依上方各 source 明細欄位順序，無額外摘要／總計列；空集合仍輸出 header。
- nullable 欄位以空欄表示 null，數值 0 與 false 明確輸出；CSV 空字串與 null 不提供無損區別，要精確保存用 JSON。
- 僅文字欄位以 `= / + / - / @` 或控制字元起始且可能被試算表解讀時，前置單引號；不改負數金額、ID、bool 或日期。CSV 說明明示此顯示保護，無損原值由 JSON 提供。

`--output` 格式由 --format 決定，不猜副檔名；若副檔名為 .txt／.csv／.json 且與 format 不符，視為用法錯誤。既有檔案不覆寫，第一版不提供 force。必要暫存檔放目標同目錄、權限限使用者，完成後原子發布；失敗／取消清除暫存，不留下成功命名的半份檔案。stdout 模式同樣完整準備後才輸出，stderr 僅診斷；檔案成功時 stdout 空白，stderr 可說明輸出路徑。CLI 不替使用者的 shell redirect 提供檔案原子性。

## 自我描述與 Agent topics

`--help`／子命令 help：繁體中文人用說明，包含用途、文法、預設值、source 適用限制、環境變數／dotenv 優先序、短例、格式、退出碼與 `--skill` 入口。`--version` 顯示實際 CLI 版本。

`--skill` 不給 topic 時輸出 [SKILL.md](skills/admin-cli/SKILL.md) 的索引；`--skill <topic-id>` 輸出對應完整 Markdown 指引，不一次載入所有 topics。索引與 topic 必須隨組件內嵌，從任何 cwd／離線執行都可用，輸出版本需與 binary 一致。沒有 topic 的近似比對或默默 fallback；未知 topic exit 2 並列可用 ID。

| topic-id | 使用情境 |
|---|---|
| configure | 環境變數、dotenv、認證與端點設定。 |
| query-stats | 正式／test／all 統計口徑、日期與未知金額。 |
| query-details | 會員名稱、訂單／結帳用途與關聯查核。 |
| export | TXT／CSV／JSON、分頁、完整性與檔案輸出。 |
| tip-verify | TiP 最後確認、證據分類與不可推論的事項。 |
| troubleshoot | 參數、認證、停用、傳輸、資料與分頁漂移。 |

help／skill／version 不讀 `.env`、不要求 token、不發 HTTP、不寫檔；與查詢命令、--output 或 --env-file 混用回用法錯誤。各 topic 採可獨立使用的目標、前提、具體命令、判讀、失敗停止條件與下一個 topic；不包含秘密，不把查詢指引擴張為交易／部署授權。canonical 文件在 `spec/skills/admin-cli`，實作以嵌入方式引用，避免複製成第二份說明。

## 退出碼

| code | 意義 |
|---|---|
| 0 | 查詢／輸出成功，或 help／skill／version 成功；不表示 TiP AC 全部通過。 |
| 2 | 命令、選項、條件或 topic 用法不合法。 |
| 3 | 組態／dotenv/token/base URL 不合法或無法讀取。 |
| 4 | 網路、TLS、逾時等傳輸失敗。 |
| 5 | HTTP 非 200，包含 redirect、400、401、404、500。 |
| 6 | 200 body 不符合 schema，或可偵測的跨頁資料漂移。 |
| 7 | 檔案已存在、權限／磁碟／stdout 寫入失敗。 |
| 130 | 使用者取消。 |

失敗 stderr 提供繁中原因、可判讀的 HTTP status／已知 API code，不 dump 未知 body、headers、例外堆疊或秘密。401 不向會員認證 fallback；404/admin_disabled 說明服務端未啟用，普通 route 404 不捏造相同原因。失敗前不發布結果；底層 stdout 裝置中途失敗可能已有部分 bytes，仍以非零碼表明，不能保證外部管線回滾。

詳細驗收見 [CLI 案例](testcases/tip-admin-cli.md) 與 [使用者流程](../docs/tip/phase1.2-final-verification.md)。
