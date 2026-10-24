# export：保存查核證據

目標：依閱讀或資料保存需求選格式，明確記錄當頁或全頁。

```text
shop-admin list --source order --type test --format txt
shop-admin list --source order --type test --all-pages --format csv --output test-orders.csv
shop-admin list --source checkout --type all --all-pages --format json --output checkouts.json
shop-admin stats --source order --type normal --format json --output normal-stats.json
```

預設 TXT 供閱讀；CSV 是固定欄位表格；JSON 是 schemaVersion=1 的 CLI envelope，含 filters、pagination、observations 與 rows，不是原始 API body。完整精度、null／空字串差異與查詢時間證據優先保存 JSON。CSV 文字可能加前置單引號防止試算表公式解讀；null 為空欄，沒有完整跨頁 metadata。

輸出檔案不會自動讀完全部頁，需明示 --all-pages；不能與 --page 合用。全頁檢查可發現部分資料漂移，但不能保證原子快照。驗收時暫停產生新交易，不需停 API。

--output 不覆寫現有檔案；請用新路徑。必須明確給 --format，不根據副檔名猜格式。只有整份成功才發布檔案；退出碼非零時不要把空檔、暫存檔或部分 stdout 當證據。每次輸出名稱、filters、queriedAt 與命令一起記錄，不保存 token。失敗讀 `shop-admin --skill troubleshoot`。
