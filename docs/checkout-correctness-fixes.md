# Checkout Correctness 修正設計稿

## 目的

這份文件只處理兩個已確認的 correctness 問題：

- transaction 先刪除再建立 order
- access token 未驗證是否對應 transaction buyer

## 修正原則

### 1. order persistence 先於 transaction delete

`CheckoutService.CompleteAsync(...)` 的最小修正是：

1. 驗證 transaction 與 buyer
2. 建立 order 並寫入資料庫
3. 刪除 transaction
4. 執行 product callback
5. 更新 fulfillment status

這樣可確保：

- order 尚未成功前，transaction 不會提早消失
- 若 product callback 失敗，不影響 checkout 已完成的事實

### 2. buyer mismatch 視為 authorization failure

buyer mismatch 不是資料格式錯誤，因此不應回 `400 Bad Request`。

此情境定義為：

- 呼叫者已登入
- 但欲完成的 transaction 不屬於該 member

因此 API 應回：

- `403 Forbidden`

## 非目標

- 不新增 outbox / retry
- 不補 transaction status
- 不修改 fulfillmenet callback 的同步模型
