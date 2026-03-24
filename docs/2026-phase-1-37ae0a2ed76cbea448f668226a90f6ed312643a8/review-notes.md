# Review Notes

這份檔案記錄的是在 reverse-engineering commit `37ae0a2ed76cbea448f668226a90f6ed312643a8` 時看到的主要落差與風險。主體文件已經以 phase 1 的正式 contract 與實作為準。

## Findings

### 1. 任一已登入會員都可以完成別人的 checkout transaction

- `CheckoutController.CompleteAsync` 只驗證目前 bearer token 對應的 `member` 是否存在。
- 讀出 transaction 之後，程式直接用 `transaction.MemberId` 載入 `consumer` 並建立 order，沒有檢查 `member.Id == transaction.MemberId`。
- 因此只要知道別人的 `transactionId`，另一個已登入會員就可能替對方完成結帳。
- 更糟的是 response 的 `ConsumerId` / `ConsumerName` 用的是目前登入者，而 `Order.Buyer` 用的是 transaction 原始 buyer，會造成回傳內容與持久化資料不一致。

### 2. transaction 在所有驗證完成前就被刪除

- `CheckoutController` 一找到 transaction 就先執行 `_database.CheckoutTransactions.Delete(request.TransactionId)`。
- 後面如果 cart 缺失、consumer 缺失、某個 product id 無法解析，都會直接 `BadRequest` 結束。
- 這代表 checkout 會在尚未成功建立 order 前就失去 transaction，讓呼叫端無法安全重試。

### 3. controller 註解仍宣稱完成結帳後會清空購物車，但實作沒有

- phase 0 就存在的語意落差在 phase 1 仍然保留。
- 註解寫的是「完成交易時，會將購物車內容轉換成訂單，並且清空購物車」。
- 但實作只建立 order、更新 fulfillment status，沒有清空 cart，也沒有建立新 cart。

### 4. `AndrewDemo.NetConf2023.API.http` 沒有更新到 phase 1 contract

- `CartsController.AddItemToCartRequest.ProductId` 已改成 `string`。
- 但 `.http` sample 仍送 numeric `productId`。
- `checkout/create` 與 `checkout/complete` 的 sample body 也還保留已被註解移除的 `accessToken` 欄位。
- 這份 sample 會誤導使用者以 phase 0 的 request format 操作 phase 1 API。

## 建議你 review 時優先看什麼

1. 你是否要把「buyer mismatch」視為 phase 1 freeze 前必修的 blocking issue。
2. `transaction` 提前刪除是否應在下一版改成「建立 order 後再刪除」，或至少延後到所有可預期驗證完成之後。
3. `phase0 -> phase1` 的對照圖是否已足夠表達這次 commit 的主要架構演進。
