# Phase 3.5：離線遷移、seed 與切換手冊

2026-10-19。工具與 temporary DB 演練已完成；**現有部署資料庫尚未遷移**。

## 工具與啟動閘門

`TransactionStorageMigrator` 在同一 LiteDB transaction 內驗證來源、搬移 true 資料、建立索引、標記版本及核對搬移前後的完整 raw BSON。false／缺 IsTest 留原 normal；ID、PaymentId、時間、關聯、未知欄位完全保留，不補造歷史。null／錯型別、Checkout 重複、關聯矛盾、未標版本卻已有非空 test 目標均停止。完成後再跑會先核對 layout，然後 no-op。

DatabaseInit 的明確命令在原 destructive seed 之前分流；錯參數 exit 2、不存在的路徑 exit 1，不能掉回 seed。命令以 Direct connection 開檔、checkpoint，再建立不覆寫的 `.before-tip-<time>-<guid>.bak`。任何遷移錯誤保留備份與原 transaction rollback 結果。

新空 DB／新 seed 直接配置版本 1。標準 API、AppleBTS.API、PetShop.API 啟動時立即建立 database context，未遷移或未知版本拒絕啟動；不在 request 內偷偷搬移。

## 現有 DB 的正式操作順序

1. 確認目標 shop 的實際 DB 檔案與執行版本，停止**所有** API、CLI writer、事件程序、背景程序與任何舊版連線；避免自動重啟接回舊版本。
2. 保存完整部署設定與一致備份；需要保留復原能力時，另將備份複製到可持久保存的位置。不得只複製尚未 checkpoint 的主檔而遺漏 WAL。
3. 執行指定 DB 的明確遷移命令，記錄回報的備份路徑與搬移筆數：

   ```sh
   dotnet src/AndrewDemo.NetConf2023.DatabaseInit/bin/Debug/net10.0/AndrewDemo.NetConf2023.DatabaseInit.dll --migrate-tip-storage --database /absolute/path/shop-database.db
   ```

4. exit 0 後部署整組新 writer，核對版本 1、兩用途三種集合、ID／金額、會員歷史與正式統計，再開流量。錯誤時停止，不手動改 IsTest／去重／補值以強行通過。
5. 若回退：再次停掉所有新 writers，還原遷移前一致備份，移除本次新 layout 對應的 WAL，配回原版本設定後才啟動舊版。不得把舊 writer 接到版本 1 的 DB，也不得在新寫入後直接還原而忽略新增資料；此時須先處理切換期間的資料保全。

**沒有參數的 DatabaseInit 仍是會刪除目標檔案的初始化程式，不能當 migration。** 本輪 smoke 只把 initializer 複製到全新 temporary 目錄才執行 seed。

## 驗證

- STORE-022：TransactionMigrationTests 核對所有原 BSON 欄位、共享 Member、缺 IsTest／PaymentId 及未知巢狀欄位；舊 quote 不足仍由既有 409 案例拒絕完成。
- STORE-023：六種歧義／不相容版本完整資料比對，沒有部分搬移或版本變動。
- STORE-024：六個 insert／delete 步驟、完成標記 insert 後、commit 前注入故障，全部 rollback；重試成功，再重跑 no-op。
- STORE-025：未遷移與未知版本 context 拒絕；已遷移可用；實際新 seed／API 啟動成功。
- STORE-026：StorageProcessTests 啟動真正 DatabaseInit CLI，驗證備份、重跑、錯參數與缺檔；關閉所有連線後還原備份，raw 資料、舊位置與未標版本均一致。

所有演練使用 temporary DB。最終測試結果與重現入口見 [3.6](phase3.6-delivery.md)。
