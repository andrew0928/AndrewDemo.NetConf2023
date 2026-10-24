# tip-verify：TiP 最後查核

目標：用唯讀 CLI 確認部署資料成果，並把其他設計保證連到其實際測試證據。先確認操作目標、資料已準備好，且核對期間沒有其他交易寫入。

```text
shop-admin list --source member --type all --format json --output members.json
shop-admin stats --source order --type normal --format json --output normal-stats.json
shop-admin stats --source order --type test --format json --output test-stats.json
shop-admin list --source order --type all --all-pages --format json --output orders.json
shop-admin list --source checkout --type all --all-pages --format json --output checkouts.json
```

會員超過一頁時也加 --all-pages。輸出檔名為範例，避免覆寫既有證據。先查會員取得實際 member ID；日期是任務條件而非任意固定範圍，未知時間資料可能被排除。

依已知的驗收交易核對：

- 一般會員＋一般付款：Order／Checkout 為 normal。
- 一般會員＋測試付款：會員仍 normal，Order／Checkout 為 test。
- 測試會員＋任一付款：Order／Checkout 為 test。
- 成功 Order 透過 checkoutTransactionId 對到 archived／completed 的 Checkout，保存用途／金額一致、付款與時間可讀。
- 庫存拒絕案例：指定 Checkout 仍 created／active、沒有關聯 Order；不能單從這個狀態判斷拒絕原因。
- 在已確認只有本輪交易的前後資料中，正式統計只增加一般訂單，test 訂單有保存但不增加 normal 總額。

本 CLI 不提供 Token、庫存、Cart、商品／SKU、操作 headers／log 或事件派送資料。不能由清單推論庫存未扣減、test Cart 有寫入、Token 繼承、並行／重送／用途 BUG 保護均通過；這些要另引用已執行的 Core／HTTP／事件測試及其時間／版本。不存在的真實付款／出貨 adapter 與範圍外 PetShop AC 標不適用。

不要為了補證據自行產生訂單、改資料、重播事件或停容器。報告分 CLI 已查核、自動化已驗證、未驗證、不適用；若沒有前後基準或有並行寫入，將無法證明的差異列待確認。用 `shop-admin --skill export` 保存完整查詢證據。
