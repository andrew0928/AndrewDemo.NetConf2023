# CheckoutService Phase 2 搬移策略

## 狀態

- accepted
- 日期：2026-03-24

## 後續補充

- checkout correctness 修正另見 [2026-03-24-checkout-consistency-and-buyer-validation.md](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-03-24-checkout-consistency-and-buyer-validation.md)

## 背景

目前 checkout 流程的主要商業邏輯仍然集中在 API controller：

- `POST /api/checkout/create`
- `POST /api/checkout/complete`

具體來說，[CheckoutController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs#L50) 與 [CheckoutController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs#L108) 直接處理：

- checkout transaction 建立
- waiting room
- cart / buyer 載入
- order 建立
- discount 試算
- product callback
- fulfillment status 更新

反而 `.Core` 的 [Checkout.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Checkout.cs#L7) 目前只有 `WaitingRoomTicket`，沒有真正的 checkout service。

這代表系統目前仍然是：

- API controller 在主導交易流程
- `.Core` 只提供零散的 helper 與 domain object

不符合目前已經逐步建立的 architecture 方向。

## 決策

### 1. Phase 2 的主題是「搬移 checkout orchestration 到 .Core」

本階段新增 `.Core` 的 concrete `CheckoutService`，讓 checkout transaction 與 order 建立流程回到 `.Core`。

API controller 只保留：

- access token / member 驗證
- request mapping
- response mapping
- HTTP status mapping

### 2. 本階段只做搬移，不修正既有行為

即使目前已發現部分流程有設計缺失，例如：

- transaction 先刪除再建立 order
- waiting room 與 transaction state 表達不足
- callback / persistence 的可靠度不足

本階段都先不修正。

原則是：

- 先把責任邊界搬對
- 再在後續階段修正行為缺失

也就是說，Phase 2 的成功條件是：

- 現有 checkout 行為被搬進 `.Core`
- 對外 observable behavior 維持一致

### 3. `CheckoutService` 先保留 concrete service，不建立 shared abstract contract

目前看不到第二種 checkout engine 的需求，因此：

- `.Core` 先提供 concrete `CheckoutService`
- 不在 `.Abstract` 建立 `ICheckoutService`

### 4. API request / response model 不直接進 `.Core`

`.Core` 應建立自己的 command / result model，例如：

- `CheckoutCreateCommand`
- `CheckoutCreateResult`
- `CheckoutCompleteCommand`
- `CheckoutCompleteResult`

controller 再負責與 HTTP request / response 互相轉換。

### 5. waiting room 與其他 orchestration helper 一併搬回 `.Core`

`WaitingRoomTicket` 現在已經在 `.Core`，但使用位置仍由 controller 決定。

本階段應讓其使用時機也由 `CheckoutService` 控制，避免 controller 主導流程。

### 6. checkout transaction 的既有資料模型先保持不變

`CheckoutTransactionRecord` 本階段不新增 status / payment / completed-at 等欄位。

原因不是這些欄位不重要，而是本階段先避免 mixing concerns：

- Phase 2 處理責任搬移
- 後續 phase 再處理 checkout transaction 行為修正

## 影響

- `.Core` 需要新增 `CheckoutService`
- `.Core` 需要新增 checkout command / result model
- `CheckoutController` 需要大幅瘦身
- waiting room 的使用位置會從 controller 移到 `.Core`
- 測試需新增 checkout service unit / integration cases

## 替代方案

### 替代方案 A：維持 controller orchestration，只補 helper method

優點：

- 初期變更量較小

缺點：

- controller 仍是交易流程主體
- 無法真正建立 `.Core` 的 checkout boundary

結論：

- 不採用

### 替代方案 B：搬移 checkout，同時一起修掉 transaction / reliability 問題

優點：

- 一次到位

缺點：

- 變更面太大
- 容易混淆「責任搬移」與「行為修正」
- review 難度升高

結論：

- 本階段不採用

## 後續工作

1. 產出 `/docs` 的 Checkout Phase 2 設計稿。
2. 產出 `/spec` 與 `/spec/testcases` 的 CheckoutService 搬移規格。
3. 實作 `.Core/Checkouts/CheckoutService`。
4. 讓 `CheckoutController` 改走 `CheckoutService`。
5. 補齊 `.Core.Tests` 的 checkout service 測試。
