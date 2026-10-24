# 會員查詢差異再次查核

2026-10-18 18:55：使用者以 test 註冊、Compose down 後，Explorer 仍看不到 test。

- AndrewShop 容器已停止並移除；沒有重啟服務。
- 作用中資料庫最後修改時間 18:53:00，沒有旁置 WAL。原檔與隔離副本 SHA-256 同為 45f9832667ed2e49f66fbf0323f61dce552b86efbaa4228381fee3a726fc0900。
- 獨立唯讀程序取得 ID 1 / Name 900001 / IsTest false，以及 ID 2 / Name test / IsTest true；test 有 2 筆 Token，未列出 Token 值。
- lsof 確認 Explorer 的 LiteDbBridge 背景程序仍持有原檔。Compose down 不會關閉 extension 的連線。
- Explorer log 18:53:54～18:54:08 的查詢回覆只有 900001，差異已存在於 bridge 回傳資料，不只是 UI 未刷新。
- 18:56 使用 extension 自帶 bridge 的全新程序查詢隔離副本，成功取得上述兩筆會員。

查核副本：/tmp/tip-member-recheck/shop-database.db。原檔：.local/data/common-site-dev/shop-database.db。

可靠驗證方式：測試前關閉 Explorer 資料庫；完成操作並停止 Compose 後，複製資料庫至新檔名，再讓 Explorer 開啟副本並重新執行查詢。原路徑仍回傳舊資料時，Reload Window 重建 extension 程序；只重跑 SQL 或 refresh collection 不足以確保舊連線狀態已清除。

本次確認 test 已正確保存，未修改會員、未重啟容器、未修改 extension；其內部舊狀態形成機制仍未完全定位。前次證據見 [原調查](2026-10-18-member-query-investigation.md)。
