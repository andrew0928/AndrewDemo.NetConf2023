# 快速查核 LiteDB

使用 repo 的 DatabaseQuery 小工具，每次啟動新的 .NET 程序，以與 Core 相同版本的 LiteDB 5.0.17 唯讀查詢，結束即關閉。無 extension 背景程序、不建立索引、不自動建立缺少的資料庫。

## 操作

前提：.NET 10 SDK；第一次執行需要還原 LiteDB 套件。請先關閉 extension 的資料庫連線，停止資料庫寫入端。標準本機站只需停 API，不必 down 整個 Compose：

```bash
docker compose -f compose/common.site-dev.compose.yaml stop common-api
bash scripts/query-litedb.sh
bash scripts/query-litedb.sh "SELECT \$ FROM members WHERE Name = 'test';"
bash scripts/query-litedb.sh 'SELECT $ FROM orders;'
bash scripts/query-litedb.sh 'SELECT $ FROM archived_checkout_transactions;'
docker compose -f compose/common.site-dev.compose.yaml start common-api
```

若已執行 compose down，可直接查詢。工具不會自行停止或啟動容器。

預設資料庫為 repo 的 `.local/data/common-site-dev/shop-database.db`；可用第二個參數指定其他檔案：

```bash
bash scripts/query-litedb.sh 'SELECT $ FROM members;' /absolute/path/shop-database.db
```

每行輸出一筆 LiteDB JSON；stderr 顯示完整路徑、檔案修改時間與結果筆數，避免開錯檔。只接受單一 SELECT 查詢，連線設 ReadOnly，寫入語句不適用；分號只能出現在結尾，這版不支援字串內含分號。錯誤回傳非零 exit code；可將 stdout 重導至檔案保存查核結果。

## 保證邊界

這是停止寫入後的磁碟資料查核。容器仍寫入時，尤其跨 Docker bind mount 與宿主機的連線，不宣稱取得一致快照；不要只複製正在寫入的主檔而遺漏 WAL。若無法正常停止、檔案仍被其他程式持有或讀取出錯，先處理該狀態，不以空結果當成成功。

此工具只讀目前檔案，無法證明使用者剛才操作的是同一部署；需要時另核對 Compose 的 API 掛載。它不修改 frozen TiP spec 或 Abstract，僅支援階段驗證。

## 交付驗證（2026-10-18）

建置成功，0 warnings／errors。預設命令與 Name 篩選均讀到 test / IsTest=true；原檔 SHA-256 不變。在隔離副本驗證 UPDATE、多句查詢、語法錯誤與缺少檔案均回傳非零值；資料庫內容未變，缺少的檔案未被建立。未重啟部署服務。
