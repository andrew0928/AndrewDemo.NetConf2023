# Checkout Phase 2 設計稿

## 目的

這份文件只處理一件事：

- 把目前位於 API controller 的 checkout orchestration 搬進 `.Core`

後續 correctness 修正另見 [checkout-correctness-fixes.md](/Users/andrew/code-work/andrewshop.apidemo/docs/checkout-correctness-fixes.md)。

本文件刻意不處理：

- checkout 行為修正
- transaction state 補強
- retry / reliability

因為這一輪的重點是責任搬移，不是行為改善。

## 現況問題

目前 checkout 的主流程寫在 [CheckoutController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs#L17)：

- [Create](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs#L50) 直接建立 checkout transaction
- [CompleteAsync](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs#L108) 直接負責：
  - waiting room
  - transaction 載入與刪除
  - cart / buyer 載入
  - order 建立
  - discount 試算
  - order event callback
  - fulfillment status 更新

`.Core` 中反而只有 [WaitingRoomTicket](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Checkout.cs#L7)，沒有真正的 `CheckoutService`。

這讓目前的 checkout boundary 仍然是：

- API controller = transaction workflow owner
- `.Core` = data model + helper

這不是我們要的方向。

## Phase 2 的目標

### 要做到的事

- 在 `.Core` 新增 concrete `CheckoutService`
- 讓 create / complete 的 orchestration 都移到 `CheckoutService`
- 讓 `CheckoutController` 只保留 HTTP 層責任

### 不做的事

- 不修正目前 transaction 先刪除再建 order 的行為
- 不新增 transaction status
- 不引入 outbox / retry
- 不改 payment model

## 建議結構

### Core 層

建議新增：

- `src/AndrewDemo.NetConf2023.Core/Checkouts/CheckoutService.cs`
- `src/AndrewDemo.NetConf2023.Core/Checkouts/CheckoutModels.cs`

其中 `CheckoutModels.cs` 可先放：

- `CheckoutCreateCommand`
- `CheckoutCreateResult`
- `CheckoutCompleteCommand`
- `CheckoutCompleteResult`

### Controller 層

`CheckoutController` 保留：

- access token 解析
- member 驗證
- command mapping
- result -> HTTP response mapping

但不再直接操作：

- `CheckoutTransactions`
- `Orders`
- `DiscountEngine`
- `OrderEventFactory`

## CheckoutService 建議責任

### Create

輸入：

- `Member`
- `CartId`

處理：

- 載入 cart
- 建立 `CheckoutTransactionRecord`
- 寫入資料庫
- 回傳 transaction result

### Complete

輸入：

- `Member`
- `TransactionId`
- `PaymentId`
- `Satisfaction`
- `ShopComments`

處理：

- 執行 waiting room
- 載入 checkout transaction
- 載入 cart / buyer
- 建立 order
- 套用 discount
- 建立 order event
- 執行 order event callback
- 更新 fulfillment status
- 回傳 completed result

### 注意

這一輪要刻意保留既有行為，包含：

- transaction 在既有流程中的刪除時機
- callback 失敗後仍視為 order complete

也就是把現在 controller 做的事搬進 service，而不是順手重寫流程。

## 建議 API / Core 邊界

### API 不應再知道的事情

- `WaitingRoomTicket`
- `Order` 如何組裝
- 何時建立 `OrderCompletedEvent`
- 何時更新 `FulfillmentStatus`

### Core 不應依賴的事情

- `HttpContext`
- API request / response class
- `ActionResult`

## 測試方向

本階段應補 checkout service tests，至少覆蓋：

- create 可建立 transaction
- complete 可建立 order
- complete 會套用 discount
- complete 會觸發 order event dispatch
- callback 失敗時 fulfillment status 仍更新為 `Failed`

但這些測試應以「維持既有行為」為主，不在本輪擴大成 checkout redesign。

## 後續 Phase

等搬移完成後，下一輪才處理：

- transaction 刪除時機
- transaction status
- payment / callback reliability
- cancel flow
