# Checkout Service Phase 2 搬移規格

## 狀態

- phase: 2
- status: draft-for-review
- 日期：2026-03-24

## 範圍

本規格只涵蓋以下主題：

1. 將 checkout orchestration 從 API controller 搬移至 `.Core`
2. 建立 concrete `CheckoutService`
3. 建立 `.Core` 專用的 checkout command / result model
4. 讓 `CheckoutController` 縮減為 HTTP boundary

本規格暫不涵蓋：

- checkout transaction 行為修正
- transaction status 補強
- retry / outbox / event store
- cancel flow 實作
- payment gateway 整合

## 目標

- `.Core` 擁有 checkout transaction 與 order 建立的主流程
- `.API` 只保留 HTTP 與 authentication boundary
- 不改變目前對外 observable checkout 行為

## Canonical 術語

- `CheckoutService`: `.Core` 內的 concrete checkout orchestration service
- `CheckoutCreateCommand`
- `CheckoutCreateResult`
- `CheckoutCompleteCommand`
- `CheckoutCompleteResult`

## Core 規格

### CheckoutService

`.Core` 必須提供 concrete `CheckoutService`。

`CheckoutService` 至少負責：

- create checkout transaction
- complete checkout transaction
- 建立 order
- 套用 discount
- 觸發 product callback
- 更新 fulfillment status

### Checkout command / result

`.Core` 必須提供 checkout command / result model，不可直接重用 API request / response class。

至少包含：

- `CheckoutCreateCommand`
- `CheckoutCreateResult`
- `CheckoutCompleteCommand`
- `CheckoutCompleteResult`

### Waiting room

waiting room 的執行位置必須從 controller 移入 `CheckoutService`。

本階段不要求改變 waiting room 的現有行為。

## API 規格

### CheckoutController

`CheckoutController` 必須改成：

- 解析 access token
- 解析 member
- 建立 `.Core` command
- 呼叫 `CheckoutService`
- 將 result 轉成 HTTP response

`CheckoutController` 不應再直接：

- 操作 `CheckoutTransactions`
- 操作 `Orders`
- 執行 `WaitingRoomTicket`
- 組裝 order lines
- 觸發 product callback

## 行為要求

### 搬移原則

本階段採「先搬移、後修正」原則。

因此：

- 若 controller 既有流程存在已知缺失，本階段先維持既有行為
- 不可在此階段順手修正 transaction delete timing、state model、retry 等問題

### 既有行為保留

以下行為在本階段視為既有行為，先保留：

- create 與 complete 的既有對外 API contract
- complete 後建立 order 與 fulfillment status 的既有結果
- product callback 失敗不推翻 order complete

## 非目標

- 本階段不修正 transaction 先刪除再建立 order 的問題
- 本階段不新增 transaction status / payment fields
- 本階段不建立 checkout 的 abstract contract
- 本階段不處理 cancel flow
