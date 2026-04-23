# Order Event Dispatcher 取代 IProductService 訂單 callback

## 狀態

- accepted
- 日期：2026-04-19
- 重大決策
- 影響 .Core
- 回頭修正 Phase 1
- 影響 .Abstract / spec

## 背景

Phase 1 的早期設計把商品查詢與訂單後續 side effect 綁在同一個 `IProductService` 內：

- `IProductService.GetPublishedProducts`
- `IProductService.GetProductById`
- `IProductService.HandleOrderCompleted`
- `IProductService.HandleOrderCancelled`

這個設計在第一輪把 controller 與資料庫解耦時是有效的，但後續擴充 PetShop 時，問題開始變得明顯：

- `IProductService` 同時承擔「商品查詢邊界」與「訂單完成後的後續處理邊界」
- `HandleOrderCompleted` / `HandleOrderCancelled` 的責任其實不一定屬於 product query domain
- 若未來要處理 reservation finalize、通知、其他 order side effect，概念上都會被迫塞進 `IProductService`

這會讓 `IProductService` 逐漸變成混合型 orchestrator，不利於長期穩定與責任清晰。

同時也評估過是否直接使用 C# 內建 `event` 語意，例如讓 `.Core.CheckoutService` 暴露 `OrderCompleted` / `OrderCancelled` event，再由 host 在啟動時訂閱。這條路線足夠精簡，但會把擴充點綁在 concrete service instance lifetime 與 host subscription code 上，不適合作為 `.Abstract` 的正式架構邊界。

## 決策

### 1. `IProductService` 回歸 product query boundary

`IProductService` 只保留：

- `GetPublishedProducts`
- `GetProductById`

不再承擔訂單完成 / 取消後的 callback。

### 2. 訂單 side effect 邊界改由 `IOrderEventDispatcher` 承接

正式 shared contract 使用：

- `IOrderEventDispatcher`
- `DefaultOrderEventDispatcher`
- `OrderEventDispatcherId`
- `OrderCompletedEvent`
- `OrderCancelledEvent`
- `OrderProductLine`

介面方法使用 overload：

- `Dispatch(OrderCompletedEvent orderEvent)`
- `Dispatch(OrderCancelledEvent orderEvent)`

語意：

- `IOrderEventDispatcher` 是單一 shop runtime 的 order side-effect boundary
- checkout 成功後，由 `.Core` 建立 order event payload 並呼叫 dispatcher
- dispatcher 失敗不推翻已成立的 order，只反映在 `FulfillmentStatus`
- `Dispatcher` 命名代表「單一 runtime 的事件分派入口」，不是單一 event 的 leaf handler
- 若未來需要真正單一 event handler，可另行使用 `IOrderEventHandler<TEvent>` 這類命名，不與目前 dispatcher 角色混淆

### 3. 第一版維持單一 dispatcher，不做 multi-subscriber bus

本次調整的目標是把責任從 `IProductService` 拆開，而不是導入完整事件匯流排。

因此本階段採用：

- 單一 `IOrderEventDispatcher`
- 單一 `OrderEventDispatcherId`
- in-process callback

暫不處理：

- `IEnumerable<IOrderEventDispatcher>` 多重訂閱
- outbox
- durable retry
- message broker

### 4. 不使用 C# `event` 作為正式擴充 contract

C# `event` 可以作為 in-process 語意，但不採用它作為 `.Abstract` 的正式擴充點。

原因：

- event subscription 依賴 `CheckoutService` instance lifetime，若未來從 singleton 改為 scoped / transient，host 啟動時訂閱的語意容易失效
- `EventHandler<T>` 是同步模型，若 PetShop reservation finalize 或通知需要 async，仍會退回自訂 dispatcher / publisher
- 多 subscriber 的錯誤處理、排序、是否繼續執行與 fulfillment status 聚合都不會被 event 語法明確表達
- 顯式 dispatcher 可以由 DI、manifest、測試與啟動流程驗證，避免漏接事件只藏在 host composition code 中

### 5. `ShopManifest` 明確指定 `OrderEventDispatcherId`

`ShopManifest` 新增：

- `OrderEventDispatcherId`

原因：

- 不讓 order event dispatcher selection 隱性依賴 `ProductServiceId`
- 讓 product query 與 order side effect 的選擇邊界都在 manifest 中明確可見

### 6. 不保留雙軌命名

本決策不保留 `Handler` 與 `Dispatcher` 雙軌命名。

canonical 命名為：

- `IOrderEventDispatcher`
- `DefaultOrderEventDispatcher`
- `OrderEventDispatcherId`

## 影響

- `.Abstract.Products` 不再持有 order callback contract
- `.Abstract.Orders` 成為 order event 的正式 contract namespace
- `.Core.CheckoutService` 改由 `IOrderEventDispatcher` 處理 order complete 後的 side effect
- `ShopManifest` / appsettings / startup wiring 必須補上 `OrderEventDispatcherId`
- product-service 與 checkout 相關 spec / testcase 必須同步改寫

## 替代方案

### 替代方案 A：保留 `IProductService` callback

優點：

- 變更最少

缺點：

- product query 與 order side effect 繼續混在同一個 interface
- PetShop 這類 reservation / 通知需求會讓 `IProductService` 變胖

結論：

- 不採用

### 替代方案 B：保留 `IOrderEventHandler` 命名

優點：

- 仍可表達 callback 角色

缺點：

- `EventHandler` 在 C# 與一般業界慣例中通常代表單一 event 的 handler，可以是 interface，也可能是 `EventHandler<TEventArgs>` delegate
- 目前這個邊界實際上是單一 shop runtime 的 event side-effect 入口，不是 leaf handler
- 後續若引入真正 leaf handler，命名會衝突

結論：

- 不採用

### 替代方案 C：直接使用 `CheckoutService.OrderCompleted += ...`

優點：

- 最少 interface
- 符合 C# 內建 event 語意
- 第一版單一 process、單一 subscriber 時可以運作

缺點：

- 擴充點綁定 concrete service lifetime
- async、錯誤處理、啟動驗證與多 subscriber 語意都不明確
- 對 PetShop 這種 correctness-critical reservation finalize 來說，漏接或錯誤處理不應藏在 host subscription 中

結論：

- 不採用

### 替代方案 D：直接做多 handler event bus

優點：

- 未來擴充彈性較高

缺點：

- `FulfillmentStatus` 語意會立刻變複雜
- dispatcher / handler 失敗的聚合規則、排序、重試都會一起被引入
- 超出目前「先把責任拆乾淨」的目標

結論：

- 本階段不採用

## 後續工作

1. PetShop Phase 1 設計沿用 `IOrderEventDispatcher`，不再使用 `IProductService` callback 或 C# event subscription 作為主擴充點。
2. 若未來需要 multi-subscriber、outbox、durable retry 或 message broker，需另立決策重新定義 delivery semantics 與 fulfillment status 聚合規則。
