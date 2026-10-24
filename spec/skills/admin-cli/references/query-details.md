# query-details：明細與關聯

目標：找到會員、訂單及結帳保存資料，核對用途／付款／狀態／關聯。

```text
shop-admin list --source member --type all --name test
shop-admin list --source member --type all --name 900001
shop-admin list --source order --type all --member-id 4
shop-admin list --source checkout --type all --member-id 4 --state completed
```

member-id=4 是示意，先由會員查詢取得實際 ID。名稱 900001 仍是名稱，不轉成 ID。名稱與 member-id 同給時取交集；不依名稱或白名單重判現有 IsTest。

Order 的 checkoutTransactionId 對到 Checkout 的 transactionId，不能用 Order.id 代替。一般會員使用測試付款時，可以 member=false、order=true、checkout=true；這是合法組合。封存 completed 的付款與用途用保存值核對，不重新查白名單。

每次預設一頁 50 筆；找不到資料先看 type、filters、totalCount 與 page，不直接斷言不存在。指定 --all-pages 可讀所有頁，但不是原子快照。沒有 transaction-id／order-id 篩選參數，不能捏造選項；必要時輸出 JSON 再讀列資料。

空關聯／時間／金額要標未知，不補 0 或現在時間。關聯跨兩次 HTTP 讀取只作核對觀察，不宣告服務端原子對帳。資料不足時讀 `shop-admin --skill tip-verify` 的證據邊界。
