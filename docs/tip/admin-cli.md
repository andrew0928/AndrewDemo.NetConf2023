# AdminCLI 操作說明

AdminCLI 是 .NET 10 唯讀命令列工具，透過既有 `/api/admin/*` 查詢會員、結帳、訂單與統計。API 保持啟動即可使用，不需要接觸 DB 檔案或停止 Docker。DatabaseQuery 與 query-litedb.sh 已移除；ConsoleUI 保留購物／AI demo，用途不同。

## 建置與執行

在 repo root 執行一次：

```sh
dotnet publish src/AndrewDemo.NetConf2023.AdminCLI -c Release -o .local/admin-cli/publish
.local/admin-cli/publish/shop-admin --version
.local/admin-cli/publish/shop-admin --help
```

發佈目錄可整份搬移；此為 framework-dependent 發佈，執行端需要 .NET 10 runtime。Windows 執行 `shop-admin.exe`；也可使用 `dotnet /absolute/path/shop-admin.dll`。查詢與 help 的正常執行不需要 SDK 或 repo；SDK 只用於建置。不要只搬移單一 executable 而漏掉同目錄檔案。

開發時也可直接執行：

```sh
dotnet run --project src/AndrewDemo.NetConf2023.AdminCLI -- --help
```

需要保存純 JSON／CSV 時，請直接使用發佈版，避免把 dotnet build／restore 訊息混入 shell 的 stdout 重導。

## 設定

CLI 預設讀取 **working directory 的 `.env`**，不是 executable 所在目錄。格式參考 [`.env.example`](../../src/AndrewDemo.NetConf2023.AdminCLI/.env.example)：

```dotenv
SHOP_ADMIN_BASE_URL=http://localhost:5128
SHOP_ADMIN_ACCESS_TOKEN=
```

在本機以編輯器填入與 API 相同的 admin token；格式為 `admin_` 加小寫英數字，不含 `-`。實際 token 不貼聊天、不放命令列、不入 Git。範例刻意留空，不是可用憑證。

優先序：程序環境變數 > dotenv > 預設值。環境變數存在但為空會覆蓋檔案並報錯。預設 `.env` 不存在可略過；`--env-file PATH` 明確指定的檔案必須存在且可讀。兩者只選一個檔案，不合併。

```sh
.local/admin-cli/publish/shop-admin stats --source order --env-file .local/admin.env
```

只讀取 `SHOP_ADMIN_BASE_URL` 與 `SHOP_ADMIN_ACCESS_TOKEN`；不展開變數、shell 指令或 `export`。遠端端點需 HTTPS。CLI 不自動取得 Docker 的 token、不替 API 設定認證、不使用會員 Token。

## 日常查核

以下從 repo root 執行，假設環境變數或該目錄 `.env` 已設定：

```sh
# 正式訂單統計；預設為人讀 TXT
.local/admin-cli/publish/shop-admin stats --source order --type normal

# 名稱為 test 的會員；name 是名稱，不是 ID
.local/admin-cli/publish/shop-admin list --source member --type all --name test

# 測試訂單及完成的測試結帳
.local/admin-cli/publish/shop-admin list --source order --type test --all-pages
.local/admin-cli/publish/shop-admin list --source checkout --type test --state completed --all-pages

# 訂單日期：包含起點、不含終點，必須帶時區
.local/admin-cli/publish/shop-admin stats --source order --type normal --from 2026-10-18T00:00:00+08:00 --to 2026-10-19T00:00:00+08:00
```

預設 `--type normal`；`all` 合併正式與測試，不能稱為正式營收。會員不接受日期；結帳可另給 `--time-field closedAt`，訂單固定使用 createdAt。統計為 API 全符合條件結果，訂單金額是保存金額、非實收；未知金額不當成 0。

## 匯出與完整性

```sh
.local/admin-cli/publish/shop-admin list --source order --type all --all-pages --format json --output .local/orders.json
.local/admin-cli/publish/shop-admin list --source order --type all --all-pages --format csv --output .local/orders.csv
```

父目錄必須存在，目標存在時不覆寫；請改用新檔名。`--format` 決定格式，不能搭配矛盾的 .txt／.csv／.json 副檔名。指定 output 後 stdout 留空，exit 0 表示完整發布。

list 預設只查第 1 頁、每頁 50 筆；要完整匯出須明示 `--all-pages`。全頁會檢查可偵測的資料漂移，仍不是原子快照。JSON 保留每次查詢時間、統計、有效條件與分頁證據；CSV 為表格投影，UTF-8 BOM／CRLF，文字公式前置單引號保護；無損原值用 JSON。

## 自我描述與錯誤

```sh
.local/admin-cli/publish/shop-admin --skill
.local/admin-cli/publish/shop-admin --skill tip-verify
.local/admin-cli/publish/shop-admin --skill troubleshoot
```

help／version／skill 全部離線可用，不需要 token，也不讀 dotenv。六個 topic 隨 binary 內嵌，來源與版本一起發佈。

退出碼：0 成功、2 用法、3 設定、4 傳輸／30 秒逾時、5 HTTP、6 回應 schema／跨頁漂移、7 輸出、130 取消。失敗看 stderr，不把非零碼視為空資料。HTTP 401 是獨立 admin 認證失敗；404 只有回傳 admin_disabled 時才能判斷未啟用。

TiP 最後確認請依 [驗收流程](phase1.2-final-verification.md)；三個管理查詢不涵蓋庫存、Token、事件或所有 AC。完整契約與案例見 [Phase 1.2](../../spec/tip-phase1.2.md)，實測結果見 [交付紀錄](phase1.2-delivery.md)。
