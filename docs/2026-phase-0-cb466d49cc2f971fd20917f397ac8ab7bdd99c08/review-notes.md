# Review Notes

這份檔案記錄的是在 reverse-engineering commit `cb466d49cc2f971fd20917f397ac8ab7bdd99c08` 時看到的主要落差與風險。上面的 C4 與 testcase 文件都已經以「實際程式碼」為準，而不是以註解或 README 為準。

## Findings

### 1. Checkout 完成後不會清空購物車

- `CheckoutController` 的註解描述「完成交易時，會將購物車內容轉換成訂單，並且清空購物車」。
- 但實作只做了 `CheckoutTransactions.Delete(transactionId)` 與 `Orders.Upsert(order)`，沒有刪除 cart、沒有清空 `ProdQtyMap`、也沒有建立新的 cart。
- 這會讓「註解中的業務規則」與「實際 API 行為」不一致。

### 2. ConsoleUI 的購物車異動沒有跟上 `Cart.AddProducts` 的持久化責任變更

- `Cart.AddProducts(...)` 在這版已經明確移除自動持久化，改成由呼叫端自行 `Update(cart)`。
- API controller 已有補上 `Context.Carts.Update(cart)`。
- 但 `ConsoleUI` 的 `ShopFunction_AddItemToCart`、`ShopFunction_RemoveItemToCart` 與預算加購流程仍只改了記憶體中的 cart 物件，沒有寫回 LiteDB。
- 若依這版程式碼實際執行，ConsoleUI 的 cart 行為很可能與 API 路徑不一致。

### 3. `compose/README.md` 的測試指令少了 Bearer token

- README 建議直接 `curl http://localhost:5108/api/products`。
- 但 `Program.cs` 的 middleware 會攔住所有 `/api/*`，只有 `/api/login` 例外。
- 實際上未帶 `Authorization` 呼叫 `/api/products` 會收到 `401 Unauthorized`。

### 4. OAuth token response 的 `expires_in` 與實際儲存值不一致

- `LoginController.PostToken` 固定回傳 `expires_in = 3600`。
- 但建立 token 時，`MemberAccessTokenRecord.Expire` 被寫成 `DateTime.MaxValue`。
- 這不會立刻讓 demo 壞掉，但會讓 client 對 token 壽命的理解與真實資料不一致。

## 建議你 review 時優先看什麼

1. 你要不要接受「phase-0 文件以實作為準」這個判讀原則。
2. `ConsoleUI` 的 cart persistence 落差是否要在後續 commit 比對中列為顯著變化點。
3. `CheckoutController` 的 cart 清空語意，是否視為這版的已知 bug，後面版本若修正應特別標出。
