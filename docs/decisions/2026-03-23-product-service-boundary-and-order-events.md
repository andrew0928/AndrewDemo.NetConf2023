# ProductService 邊界與訂單事件

## 狀態

- accepted
- 日期：2026-03-23

## 背景

目前系統的商品流程仍然是靜態資料表導向：

- `ProductsController` 直接讀取 `Products` collection
- `CartContextFactory` 直接查商品名稱與價格
- `CheckoutController` 直接依賴 `Products` collection 建立訂單商品列
- `OrderLineItem` 只保存 `Title` 與 `Price`，無法識別實際購買的商品

新的需求是支援：

- 每個商店可指定自己的 `IProductService`
- 預設商店使用 `DefaultProductService`
- 商店可販售動態產生的商品，例如預約完成後才建立的可購買 product
- 結帳完成後，ProductService 需要收到商品訂購結果通知，進一步觸發 fulfillment
- 取消允許部分取消，需能指出被取消的商品 lines

同時已確認以下原則：

- reservation 查詢、reservation 建立、結帳前確認等流程不進 `.Abstract`
- product 延伸資料由 shop module 以 side-by-side 方式自行維護
- POC 階段採 in-process callback，不在本輪處理 durable retry 實作

## 決策

### 1. 每個 shop 只啟用一個 `IProductService`

- `ShopManifest` 新增 `ProductServiceId`
- 啟動時依 `ShopManifest.ProductServiceId` 選擇對應的 `IProductService`
- `.Core` 提供 `DefaultProductService`

本階段不設計單一 shop 同時啟用多個 product handler 的 routing 機制。

### 2. Product contract 採固定欄位，`ProductId` 改為 `string`

shared product model 只保留主系統必要欄位，例如：

- `Id`
- `Name`
- `Description`
- `Price`
- `IsPublished`

延伸資訊不放進 `.Abstract` / `.Core` 的通用 payload 欄位。

若商店需要 reservation id、slot、staff assignment、token template 等額外資料：

- 由自訂 `IProductService` 以 side-by-side collection / entity 自行保存
- 主系統只透過 `ProductId` 與 `IProductService` 互動

### 3. `/api/products` 只列 published products

- `GetPublishedProducts` 只回傳可公開瀏覽的商品
- 動態商品可以是 unpublished
- unpublished 動態商品仍可透過 `GetProductById(productId)` 被 cart / checkout 解析

### 4. ProductService 的 callback 單位是「order-scoped product event」

不讓 `IProductService` 直接吃 raw `Order` entity。

`IProductService` 應接收：

- `ProductOrderCompletedEvent`
- `ProductOrderCancelledEvent`

這兩種 event 都是 order-scoped，但 payload 只包含 product domain 需要的資料：

- 訂單識別資訊
- 消費者資訊
- 發生時間
- `ProductOrderLine[]`

其中：

- `ProductOrderCompletedEvent` 帶整張訂單的商品 lines
- `ProductOrderCancelledEvent` 帶被取消的 `AffectedLines`
- discount lines 不進 product event payload

### 5. 訂單完成與 fulfillment 結果分離

只要扣款成功且訂單建立成功，`OrderComplete` 就算成功。

`IProductService` 的 callback 若失敗：

- 不得推翻訂單已完成的事實
- 應反映在 fulfillment 狀態上
- 後續是否有 retry 機制屬於可靠度實作問題，可在 POC 先略過

因此 order model 需要區分：

- 訂單是否完成
- fulfillment 是否成功

### 6. `IProductService` 的 shared scope 只涵蓋查詢與訂單事件

shared contract 只定義：

- published product 查詢
- 依 product id 解析商品
- 訂單完成 / 部分取消後的通知

下列流程不放入 shared contract：

- 預約查詢
- 預約建立
- 結帳前 reservation confirm
- reservation-specific 取消 API
- side-by-side 延伸資料 schema

## 影響

- `Product`、`Cart`、`CartContext`、request / response model 都要接受 `ProductId: string`
- `ProductsController`、`CartsController`、`CartContextFactory`、`CheckoutController` 要改走 `IProductService`
- `Order` 必須補上可識別商品 lines 的資料結構
- `Order` 需要額外表達 fulfillment 狀態
- `ShopManifest` 與 startup wiring 要支援 `ProductServiceId`
- `spec`、`testcases`、整合測試需加入 published / hidden product 與 callback 行為

## 替代方案

### 替代方案 A：讓 `IProductService` 直接接 raw `Order`

優點：

- callback 參數最少

缺點：

- product domain 直接耦合 order persistence model
- discount lines 與 product lines 混在一起
- 後續調整 `Order` 結構時，ProductService 也會被迫一起改

結論：

- 不採用

### 替代方案 B：一完成訂單就拆成多個 `ProductPurchasedEvent`

優點：

- 每個 event 只代表一筆商品購買

缺點：

- 需要額外 routing / dispatcher 設計
- 會過早把系統推向多 handler 模式
- 每個 line 都要重複攜帶 order context

結論：

- 目前不採用

### 替代方案 C：在 shared `Product` 上加入 generic payload / metadata object

優點：

- 看似能支援多種商品型別

缺點：

- 主系統會被迫理解本來不屬於自己的延伸資料
- shared contract 變胖
- payload schema 很快失控

結論：

- 不採用

## 後續工作

1. 先產出 `/docs` 的 Product Phase 1 設計稿。
2. 產出 `/spec` 與 `/spec/testcases` 的 ProductService 與 order event 規格。
3. 待 spec 確認後，再調整 `.Abstract`：
   - `Product`
   - `IProductService`
   - `ProductOrderCompletedEvent`
   - `ProductOrderCancelledEvent`
   - `ProductOrderLine`
4. 再進入 `.Core` 與 `.API` 的重構：
   - `DefaultProductService`
   - `ProductsController`
   - `CartsController`
   - `CartContextFactory`
   - `CheckoutController`
   - `Order`
