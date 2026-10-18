# TiP Phase 1.1 管理查核 API 實作交付

日期：2026-10-18。分支：feature-test-in-production。

## 狀態與範圍

使用者在規格定案後指示「NEXT: 執行 phase 1.1」，本次依 [管理查核規格](../../spec/tip-admin-query.md) 完成 API 實作。沒有 Web UI，沒有新增 admin shell CLI；CLI 由使用者後續追加。Abstract 與定案業務契約未變更。

實作及本機真實 host 驗證完成。Docker image 首次建置因 daemon 連線 EOF 中斷；使用者要求重試後，確認 OrbStack 已停止，重新啟動並成功建置 `andrewdemo-shop-common-api:local`（linux/arm64）。映像建置完成後，提供 Common Compose 啟動與 curl 查詢命令；使用者親自執行並回報 `orders?purpose=normal` 查詢測試通過。此為使用者手動驗證證據，不宣稱完整容器案例已重跑；agent 建置過程沒有改寫原站資料。

## 已交付

- GET `/api/admin/orders`：訂單分頁、全符合條件的 totalCount／totalAmount／knownTotalAmount／unknownAmountCount。
- GET `/api/admin/checkouts`：合併作用中與封存後篩選、排序與分頁。
- GET `/api/admin/members`：只回 id／name／isTest，名稱比對沿用資料庫 collation。
- Core AdminQueryService 共用資料判讀與查詢；缺少 IsTest 依既定舊資料規則為 false，其餘無法判讀值回 admin_data_invalid，不以偽造金額或用途繼續統計。
- API 嚴格驗證參數：重複／未知欄位、非法用途／時間／分頁等回 400。總額與清單使用同一份 materialized 結果；日期起點含、終點不含。
- AdminQueryMiddleware 與會員 middleware 分流。AdminAccessToken 啟動時驗證設定，使用 SHA-256 固定長度摘要與固定時間比對；不存入會員資料、不初始化會員或系統 identity。
- 未設定或空值回 404 / admin_disabled；錯誤認證回 401 / admin_unauthorized；格式非法的設定使 API 啟動失敗且不回顯秘密。標準會員入口拒絕 admin_ token。
- 每次管理回應 no-store；資料錯誤與基礎設施錯誤分別回 admin_data_invalid／admin_query_failed，不洩漏例外內容。
- Common api-dev／site-dev／site-prod Compose 與 API `.env.example` 加入 SHOP_ADMIN_ACCESS_TOKEN，沒有預設秘密。

## 一致性機制與實際限制

訂單與會員一次讀取單一 collection，再由同份結果計算、分頁。Checkout 則在同步資料庫交易內，以 ForUpdate 讀取既有 checkout_write_lock collection 名稱鎖，與 CheckoutService.BeginWrite 協調；取得鎖後讀取 active 與 archived，最後 rollback 釋放讀取範圍。沒有 Upsert、補資料或建立鎖文件。

已核對 LiteDB 5.0.17 原始碼：[QueryExecutor](https://github.com/litedb-org/LiteDB/blob/v5.0.17/LiteDB/Engine/Query/QueryExecutor.cs) 的 ForUpdate 使用 Write lock 且 addIfNotExists=false；[SnapShot](https://github.com/litedb-org/LiteDB/blob/v5.0.17/LiteDB/Engine/Services/SnapShot.cs) 先取得 collection 名稱鎖再取 read version，即使 collection 尚未建立也會協調。每個 collection 各自取得 read version，所以不僅依賴 BeginTrans。

測試以可控同步點停在兩分區讀取之間，交錯執行與正式 Checkout 相同的 lock／搬移交易；Direct 與 Shared、已有／尚無 lock collection 均通過。標準寫入端須繼續使用 checkout_write_lock；繞過服務的外部 raw DB 寫入不在此保證內。

第一版為 PoC 規模，全量讀取及記憶體篩選，成本隨集合大小增加。pageSize 限制回應大小，不代表只讀當頁；Checkout 查詢持鎖期間會延後相同協調鎖的寫入。不同 HTTP 請求或頁次不共用快照，亦未新增自動對帳功能。

## 驗證證據

```bash
dotnet build src/AndrewDemo.NetConf2023.API --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

- Solution **262 項通過，0 failed、0 skipped**：Core 140、WebContract 82、AppleBTS Extension 19、PetShop Extension 13、PetShop API 8。
- 本次新增 Core 22、HTTP 44 項。涵蓋分頁總額、用途與 Buyer 分離、缺值、文化／大小寫比對、錯誤欄位、無資料改寫、讀取中提交、rollback 與跨分區協調。
- 新增 HTTP 案例覆蓋三個入口、認證雙向隔離、URL／cookie／body 不代傳、多值 header、停用、非法設定、換 token、錯誤參數與穩定錯誤 envelope。
- 另以真正 API Program 的新程序、資料副本及隨機測試 token 驗證：停用 admin 404 且 products 200；啟用與換新後三入口皆 200；缺 token 與 admin 呼叫會員入口回 401；舊 token 換新後失效；非法設定啟動失敗；主程式 log 未出現測試秘密。
- 隔離程序與臨時資料驗證後已移除；原資料庫 SHA-256 保持不變。
- HTTP parser 會處理 header 欄位尾端 OWS；應用 token 比對本身不 trim／改大小寫，另有直接比對測試。沒有把傳輸層的 OWS 當成可保留的秘密字元。
- build 只有既有 2 項 obsolete、3 項 XML 文件警告；git diff --check 通過。
- Docker 重試：`docker compose -f compose/common.site-dev.compose.yaml build common-api` 成功，映像 digest 為 `sha256:3551ffa7e35993e330b8d612f3c7792acdc6996b3b497ad01a3d7911c679481b`；建置僅有上述既有警告。
- 使用者手動驗證：依提供的啟動命令及 shell `admin_query` 函式，執行 `orders?purpose=normal`，搭配 `python3 -m json.tool --no-ensure-ascii` 格式化後回報測試通過。
- ADMIN-CLI 完整案例未執行，因專用 shell CLI 不屬本次範圍；上述函式只是 curl 呼叫示例。

## 啟用與讀取

Docker daemon 已恢復且映像建置完成。可從 repo root 設定自行保管的 token，再建立／啟動 API 容器。例如產生一次新的設定值（變更並重新建立 API 會使舊 token 失效）：

```bash
export SHOP_ADMIN_ACCESS_TOKEN="admin_$(openssl rand -hex 32)"
docker compose -f compose/common.site-dev.compose.yaml up -d --build common-api
```

環境值需保留供後續呼叫；沒有設定就停用。此範例不把 token 印出或寫入版控。下面使用 stdin 傳 Authorization，避免將秘密放進 curl argv：

```bash
printf 'Authorization: Bearer %s\n' "$SHOP_ADMIN_ACCESS_TOKEN" |
  curl --fail-with-body --silent --show-error --header @- \
  'http://localhost:5108/api/admin/orders?purpose=normal&pageSize=20'
```

其他路徑：`/api/admin/orders?purpose=test`、`/api/admin/checkouts?state=completed&timeField=closedAt`、`/api/admin/members?purpose=all&name=test`。若需查指定時間，使用含 Z 或 offset 的時間值並正確 URL encode。

site-prod 的 API 仍遵循既有內網邊界，未修改 nginx 對外開放規則；不因本次新增 admin API 就開放所有 `/api/*`。後續 CLI 依部署網路選擇可存取的 API base URL。
