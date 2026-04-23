# Product / Order Event Phase 1 設計稿

## 目的

這份文件只回答一件事：

- 在不引入 reservation-specific contract 的前提下，如何把目前系統重構成可支援自訂 `IProductService`、動態商品、以及 checkout 後 order side effect callback 的結構。

本文件是給決策者 review 的設計稿，不是正式公開規格。

## 已確認決策

- 每個 shop 只啟用一個 `IProductService`
- 每個 shop 只啟用一個 `IOrderEventDispatcher`
- 預設使用 `.Core` 內的 `DefaultProductService`
- 預設使用 `.Core` 內的 `DefaultOrderEventDispatcher`
- `ProductId` 改為 `string`
- `/api/products` 只列 published products
- 動態商品可以是 unpublished，但 `GetProductById(productId)` 必須能解析
- order event callback 採 in-process
- callback 失敗不推翻 order complete，但 order 需要區分 fulfillment 是否成功
- product 延伸資料由 shop module 用 side-by-side collection / entity 自行管理
- shared contract 不放 reservation 查詢、建立、確認、取消流程

## 現況問題

目前的實作仍然是「controller + DB collection」直連：

- [ProductsController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/ProductsController.cs#L12) 直接查 `_database.Products`
- [CartContextFactory.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Carts/CartContextFactory.cs#L36) 直接依 `ProductId` 取價格與名稱
- [CheckoutController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs#L158) 結帳時直接讀商品資料並組裝訂單
- [Order.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Order.cs#L31) 的 `OrderLineItem` 只有 `Title` / `Price`，無法描述買了哪個商品、數量多少、哪些 line 是 discount

若不先把這些位置抽成 product boundary，後面只要加入動態商品，結帳、折扣、取消都會一起失控。

## Phase 1 的目標邊界

### Product Domain 要負責的事情

- 列出可公開瀏覽的商品
- 依 `ProductId` 解析單一可購買商品

### Product Domain 不負責的事情

- order complete / cancel 後的 side effect callback
- reservation 查詢
- reservation 建立
- reservation 在結帳前的確認
- reservation-specific API
- product 延伸資料的共用 schema

### Order Event Domain 要負責的事情

- 在 order complete / cancel 後收到 order event
- 對需要後續處理的商品或 reservation 執行 fulfillment / notification

## 這次回頭修正的重點

第一版 Phase 1 把 `HandleOrderCompleted` / `HandleOrderCancelled` 放進 `IProductService`。

這樣做雖然能快速完成 decouple，但長期會讓 `IProductService` 同時負責：

- 商品查詢
- 訂單後續 side effect

這兩個邊界實際上不是同一件事。

因此這次調整改成：

- `IProductService` 只負責 product query
- `IOrderEventDispatcher` 負責 order side effect

## 建議的 shared model

### Product

shared `Product` 只保留主系統需要的固定欄位：

- `Id: string`
- `Name`
- `Description`
- `Price`
- `IsPublished`

這代表：

- published product 可以被 `/api/products` 列出
- unpublished hidden product 不會出現在列表，但仍可被 `GetProductById` 解析

### OrderProductLine

這是 order event 與 order product line 都需要的快照資料：

- `ProductId`
- `ProductName`
- `UnitPrice`
- `Quantity`
- `LineAmount`

它的用途是：

- 讓 callback 不必再回頭猜測當時購買了什麼
- 讓 future cancel 可以指出被取消的 lines

### FulfillmentStatus

建議 order 額外區分 fulfillment 狀態：

- `Pending`
- `Succeeded`
- `Failed`

語意：

- `Order` 建立成功，不代表 fulfillment 一定成功
- order event dispatch 失敗時，只反映在 fulfillment 狀態，不推翻 order complete

## 建議的 service 邊界

### IProductService

Phase 1 只建議放 2 類能力：

- `GetPublishedProducts`
- `GetProductById`

### IOrderEventDispatcher

Phase 1 只建議放 2 類能力：

- `Dispatch(OrderCompletedEvent)`
- `Dispatch(OrderCancelledEvent)`

這裡的 callback 單位是「order-scoped event」。

也就是：

- 一次 callback 對應一張訂單
- payload 帶商品 lines
- 不直接傳 raw `Order`
- discount lines 不進 payload

## 建議的執行流程

### 1. 商品列表

1. `ProductsController` 呼叫 `IProductService.GetPublishedProducts()`
2. 回傳 `IsPublished = true` 的 products
3. hidden product 不出現在列表中

### 2. hidden product 加入購物車

1. shop 自己的流程先完成 reservation 或其他前置整合
2. custom flow 建立或選定一筆 hidden product，取得 `ProductId`
3. 外部流程把這個 `ProductId` 加入 cart
4. `CartsController` 加入購物車前，用 `IProductService.GetProductById(productId)` 驗證商品存在

這裡 shared platform 不需要知道 reservation 的細節。

### 3. 試算

1. `CartContextFactory` 不再直接讀 `_database.Products`
2. 改由 `IProductService.GetProductById(productId)` 補齊商品名稱與價格
3. discount engine 仍只看到 `CartContext`

### 4. 完成結帳

1. checkout 驗證支付成功
2. 建立 `Order`
3. 用商品快照建立 order product lines
4. 用折扣結果建立 order discount lines
5. 將 `Order` 持久化
6. 建立 `OrderCompletedEvent`
7. 呼叫 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)`
8. 若 callback 成功，`FulfillmentStatus = Succeeded`
9. 若 callback 失敗，`FulfillmentStatus = Failed`
10. `checkout/complete` 仍視為成功

這個設計明確表達：

- 訂單完成與 fulfillment 成功是兩件事
- retry / durable callback 是可靠度實作，不是本輪 shared contract 的主題

### 5. 部分取消

未來 cancel 的 shared model 應以 `AffectedLines` 表示被取消的商品 lines。

因此：

- 全單取消 = `AffectedLines` 包含全部商品 lines
- 部分取消 = `AffectedLines` 只包含被取消的 subset

## 為什麼不直接讓 ProductService 吃 raw Order

因為這會把 product domain 綁死在 order persistence model 上，而且 raw order 目前還混有 discount lines。

比較乾淨的做法是：

- order service / checkout orchestration 負責整理成 product event payload
- `IProductService` 只看到 product query 需要的資料
- `IOrderEventDispatcher` 只看到 order side effect 需要的資料

## 為什麼不直接做多 handler event bus

目前這次回頭修正的目標是把責任拆乾淨，不是建立完整事件系統。

若現在就做成多 subscriber：

- handler 排序
- 失敗聚合規則
- `FulfillmentStatus` 對應多 handler 的語意
- retry / outbox 的期待

都會一起進場，對目前目標太重。

Phase 1 保持：

- 每張訂單 callback 一次
- 每個 shop 一個 `IOrderEventDispatcher`

就足夠了。

## 對現有程式碼的重構方向

### Product / Cart

- `Product.Id` 改為 `string`
- `Cart.ProdQtyMap` 需改為 `Dictionary<string, int>`
- request / response model 內的 `ProductId` 一律改為 `string`

### Controllers / Factory

- `ProductsController` 改依賴 `IProductService`
- `CartsController` 加 item 時先做 product lookup
- `CartContextFactory` 改依賴 `IProductService`
- `CheckoutController` 不再直查 `_database.Products`
- `CheckoutService` 改依賴 `IOrderEventDispatcher`

### Order

- product line 與 discount line 不能再共用只有 `Title` / `Price` 的扁平模型
- order 至少要能保留：
  - 商品 lines
  - discount lines
  - fulfillment status

## 本階段不做的事

- reservation domain model
- reservation API
- durable event / outbox / retry
- side-by-side extension schema 標準化
- 多個 order event dispatcher routing
- fulfillment token 的具體實作

## 建議的實作順序

1. 先 freeze `/spec` 與 `.Abstract` 的 Product / Order Event contract
2. 把 `ProductId` 全系統改為 `string`
3. 建立 `IProductService` / `DefaultProductService`
4. 建立 `IOrderEventDispatcher` / `DefaultOrderEventDispatcher`
5. 讓 `ProductsController`、`CartsController`、`CartContextFactory`、`CheckoutController` 全部改走 service
6. 重構 `Order` 與 fulfillment status
7. 最後才補 callback 與整合測試
