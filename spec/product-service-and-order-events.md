# Product Service 與 Order Event 規格

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-03-23

## 範圍

本規格只涵蓋以下主題：

1. 每個 shop 如何指定 `IProductService`
2. 每個 shop 如何指定 `IOrderEventDispatcher`
3. shared `Product` 的固定欄位與 `ProductId: string`
4. published product 查詢與 hidden product 解析
5. order complete / cancel 後的 order event contract
6. fulfillment 狀態與訂單完成狀態的分離

本規格暫不涵蓋：

- reservation query / create / confirm / cancel API
- durable retry / outbox / event store
- product side-by-side 延伸資料 schema
- fulfillment token 的實作細節
- 商品後台編輯 / 上架管理介面

## 目標

- 支援每個商店指定自訂 `IProductService`
- 支援每個商店指定自訂 `IOrderEventDispatcher`
- 預設商品商店可使用 `DefaultProductService`
- 預設 order side effect 可使用 `.Core` 提供的 `DefaultOrderEventDispatcher`
- 支援 unpublished 動態商品進入 cart 與 checkout
- checkout 完成後，order side effect handler 可收到 in-process order event
- 取消支援部分取消的 lines model

## Canonical 術語

- `Product`: 可被 cart / checkout 解析的共享商品模型
- `IProductService`: shop 專用的商品 domain service
- `DefaultProductService`: `.Core` 提供的預設商品服務
- `IOrderEventDispatcher`: shop 專用的 order side-effect dispatcher
- `DefaultOrderEventDispatcher`: `.Core` 提供的預設 order event dispatcher
- `ProductServiceId`: `ShopManifest` 指定的 product service 識別碼
- `OrderEventDispatcherId`: `ShopManifest` 指定的 order event dispatcher 識別碼
- `OrderCompletedEvent`: 訂單完成後的 order event
- `OrderCancelledEvent`: 訂單取消後的 order event
- `OrderProductLine`: event 與 order product line 共用的商品快照資料
- `FulfillmentStatus`: order 完成後，product fulfillment 的狀態

## Shop Runtime 規格

### ShopManifest

`ShopManifest` 新增：

- `ProductServiceId`
- `OrderEventDispatcherId`

規則：

- 必填
- 單一 shop 只對應一個 `ProductServiceId`
- 單一 shop 只對應一個 `OrderEventDispatcherId`
- `.API` 啟動時依 `ProductServiceId` 選出對應的 `IProductService`
- `.API` 啟動時依 `OrderEventDispatcherId` 選出對應的 `IOrderEventDispatcher`

## Product Contract 規格

### Product

shared `Product` 至少包含：

- `Id: string`
- `Name`
- `Description`
- `Price`
- `IsPublished`

規則：

- `Id` 為 opaque string，平台不解析其格式
- `IsPublished = true` 才能出現在商品列表
- `IsPublished = false` 的商品，仍可透過 `GetProductById(id)` 被 cart / checkout 解析

### IProductService

shared `IProductService` 至少提供以下能力：

- `GetPublishedProducts`
- `GetProductById`

規則：

- `GetPublishedProducts` 只回傳 `IsPublished = true` 的 products
- `GetProductById` 可回傳 published 或 hidden product
- 若 `GetProductById` 找不到商品，cart / checkout 必須視為無效 product id

### DefaultProductService

- 由 `.Core` 提供
- 使用主系統的固定 `products` collection
- 不處理 side-by-side extension data

## Order Event 規格

### IOrderEventDispatcher

shared `IOrderEventDispatcher` 至少提供以下能力：

- `Dispatch(OrderCompletedEvent)`
- `Dispatch(OrderCancelledEvent)`

規則：

- `IOrderEventDispatcher` 屬於 order side-effect boundary，不負責 published product 查詢
- 目前每個 shop 只啟用一個 `IOrderEventDispatcher`
- callback 採 in-process，同一個 order complete 流程內執行
- callback 失敗不得推翻已成立的 order

### DefaultOrderEventDispatcher

- 由 `.Core` 提供
- 預設為 no-op
- 可作為沒有 shop-specific side effect 的內建實作

### OrderProductLine

至少包含：

- `ProductId`
- `ProductName`
- `UnitPrice`
- `Quantity`
- `LineAmount`

規則：

- `LineAmount = UnitPrice * Quantity`
- line 代表商品快照，不代表 discount line

### OrderCompletedEvent

至少包含：

- `OrderId`
- `ShopId`
- `BuyerId`
- `BuyerName`
- `CompletedAt`
- `Lines`

規則：

- `Lines` 代表整張訂單中的商品 lines
- discount lines 不得出現在 `Lines`

### OrderCancelledEvent

至少包含：

- `OrderId`
- `ShopId`
- `BuyerId`
- `BuyerName`
- `CancelledAt`
- `AffectedLines`

規則：

- `AffectedLines` 代表這次被取消的商品 lines subset
- 全單取消 = `AffectedLines` 含全部商品 lines
- 部分取消 = `AffectedLines` 只含部分商品 lines

## Order 與 Fulfillment 規格

### Order 完成

只要扣款成功且 order 建立成功，`OrderComplete` 即視為成功。

`IOrderEventDispatcher` callback 失敗：

- 不得推翻 order 已完成
- 不得視為支付回滾
- 只反映在 fulfillment 狀態

### FulfillmentStatus

order 需要額外表達 fulfillment 狀態。

Phase 1 至少支援：

- `Pending`
- `Succeeded`
- `Failed`

語意：

- `Pending`: order 已完成，但 callback 尚未完成或尚未確認結果
- `Succeeded`: `IOrderEventDispatcher` callback 已成功完成
- `Failed`: order event dispatch 失敗，需後續補救

## `.API` / `.Core` 行為要求

### ProductsController

- 不得再直接讀 `_database.Products`
- 必須改走 `IProductService.GetPublishedProducts()` / `GetProductById()`

### CartsController

- 加入購物車前，必須用 `IProductService.GetProductById(productId)` 驗證商品存在
- `ProductId` request model 改為 `string`

### CartContextFactory

- 不得再直接讀 `_database.Products`
- 必須透過 `IProductService.GetProductById(productId)` 補齊商品價格與名稱快照

### CheckoutController

- 不得再直接讀 `_database.Products`
- 必須以 product snapshot 建立 order product lines
- order 持久化後，必須建立 `OrderCompletedEvent`
- 並呼叫 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)`
- callback 若失敗，不可把 checkout 結果改為失敗，但 fulfillment status 需反映失敗

### Cancel 流程

- 當未來加入取消流程時，必須建立 `OrderCancelledEvent`
- event payload 必須用 `AffectedLines` 模型，而不是只支援整張訂單取消

## Database Extension 規格

- 主系統只保證共享 `products` 結構與 `ProductId` 可用
- shop-specific product extension data 由 developer 自行建立 side-by-side collection / entity
- `.Abstract` / `.Core` 不提供 generic metadata payload contract

## 非目標

- 本階段不處理 reservation domain model
- 本階段不處理 retry / durable callback
- 本階段不建立多 order event dispatcher routing
- 本階段不定義 shop-specific extension data schema
