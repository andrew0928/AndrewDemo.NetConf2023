# Product Service 與 Order Event 測試案例

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-03-23

## Shop Runtime

### TC-PR-001 ShopManifest 可指定 ProductServiceId

- Given: manifest 內設定 `ProductServiceId=default-product-service`
- When: API 啟動
- Then: 解析出對應的 `IProductService`

### TC-PR-002 ShopManifest 可指定 OrderEventDispatcherId

- Given: manifest 內設定 `OrderEventDispatcherId=default-order-event-dispatcher`
- When: API 啟動
- Then: 解析出對應的 `IOrderEventDispatcher`

### TC-PR-003 未知 ProductServiceId 啟動失敗

- Given: manifest 內設定未知的 `ProductServiceId`
- When: API 啟動
- Then: 啟動階段直接失敗，不可默默 fallback

### TC-PR-004 未知 OrderEventDispatcherId 啟動失敗

- Given: manifest 內設定未知的 `OrderEventDispatcherId`
- When: API 啟動
- Then: 啟動階段直接失敗，不可默默 fallback

## Published / Hidden Product

### TC-PR-101 published products 可列出

- Given: store 內有多筆 `IsPublished = true` 的商品
- When: 呼叫 `GetPublishedProducts`
- Then: 回傳所有 published products

### TC-PR-102 hidden product 不出現在列表

- Given: store 內有一筆 `IsPublished = false` 的 hidden product
- When: 呼叫 `GetPublishedProducts`
- Then: 該商品不得出現在列表中

### TC-PR-103 hidden product 可由 id 解析

- Given: store 內有一筆 `IsPublished = false` 的 hidden product
- When: 呼叫 `GetProductById(productId)`
- Then: 可成功取得該商品

## Cart / Estimate

### TC-PR-201 cart 加入商品前會先驗證 product id

- Given: 使用者傳入 `ProductId`
- When: 加入購物車
- Then: `CartsController` 先呼叫 `IProductService.GetProductById(productId)`
- And: 找不到商品時回傳錯誤，不可直接寫入 cart

### TC-PR-202 CartContextFactory 改走 IProductService

- Given: cart 內有多筆商品
- When: 建立 `CartContext`
- Then: 每個 line 的名稱與價格都來自 `IProductService.GetProductById(productId)`

## Checkout / Order

### TC-PR-301 checkout 以商品快照建立 order product lines

- Given: cart 內有多筆商品
- When: 完成 checkout
- Then: order product lines 需保留 `ProductId`、`ProductName`、`UnitPrice`、`Quantity`、`LineAmount`

### TC-PR-302 discount lines 不得進入 order event

- Given: checkout 同時有商品 lines 與 discount lines
- When: 建立 `OrderCompletedEvent`
- Then: event payload 只包含商品 lines

### TC-PR-303 order complete 成功後觸發 order completed event

- Given: 支付成功且 order 建立成功
- When: 完成 checkout
- Then: 建立 `OrderCompletedEvent`
- And: 呼叫 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)`

### TC-PR-304 order event callback 失敗不推翻 order complete

- Given: 支付成功且 order 建立成功
- And: `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)` 拋出例外
- When: 完成 checkout
- Then: order 仍視為完成
- And: fulfillment status 為 `Failed`

### TC-PR-305 order event callback 成功時 fulfillment status 為 Succeeded

- Given: 支付成功且 order 建立成功
- And: `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)` 成功
- When: 完成 checkout
- Then: fulfillment status 為 `Succeeded`

## Cancel

### TC-PR-401 cancel event 使用 affected lines 模型

- Given: 一張 order 內有多筆商品
- When: 取消其中部分商品
- Then: 建立 `OrderCancelledEvent`
- And: `AffectedLines` 只包含被取消的商品 lines

### TC-PR-402 全單取消是 affected lines 全部命中

- Given: 一張 order 內有多筆商品
- When: 全單取消
- Then: `AffectedLines` 包含全部商品 lines

## Database Extension

### TC-PR-501 side-by-side extension data 不進 shared product contract

- Given: custom product service 需要額外 reservation 資料
- When: 設計 shop-specific data model
- Then: 可自建 side-by-side collection / entity
- And: `.Abstract` / `.Core` 不需新增 generic metadata payload 欄位
