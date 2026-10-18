# query-stats：統計查詢

目標：使用 API 全符合條件的統計，區分正式與測試資料。

```text
shop-admin stats --source order --type normal
shop-admin stats --source order --type test --from 2026-10-18T00:00:00+08:00 --to 2026-10-19T00:00:00+08:00
shop-admin stats --source checkout --type test --state completed --time-field closedAt
shop-admin stats --source member --type all
```

日期為範例，按任務替換。時間必須帶時區，範圍含起點、不含終點；member 不接受日期。source=checkout 的預設時間是 createdAt，核對完成期間應選 closedAt 與 completed。

member／checkout 提供符合條件筆數；order 另有總金額、已知小計、未知金額筆數。不要讀當頁列資料自己加總。totalAmount=null 代表總額不能確定，knownTotalAmount 不是完整總額；0 與未知不同。

all 合併 normal 與 test，不自動拆分。要比較兩類請分開執行並保留各自 queriedAt；有其他寫入時，不能宣稱兩次結果來自同一快照。正式總額依 Order.IsTest，不依當前會員用途；履約 failed 的成立訂單仍計入，金額不代表實收。要保存證據讀 `shop-admin --skill export`。
