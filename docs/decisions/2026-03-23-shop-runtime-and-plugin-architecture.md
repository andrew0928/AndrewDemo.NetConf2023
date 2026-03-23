# 商店啟動組態與插件化架構

## 狀態

- proposed
- 日期：2026-03-23

## 背景

目前系統的核心能力是單一商店、單一商品模型、單一折扣邏輯：

- API 啟動時只切換資料庫檔案路徑，尚未有 `shop-id` 對應的完整商店組態
- 折扣邏輯寫死在 `DiscountEngine`
- 商品模型固定為一般商品，無法表達預約服務、履約 token、時段與服務人員等擴充需求
- 結帳流程直接在 controller 中組裝商品與折扣，缺少可插拔的 domain service 邊界

需求目標為：

1. 同一套 code base 可交付多個商店，單一部署只服務單一商店
2. 折扣規則可依商店動態載入
3. 商品型別與履約流程可依商店動態載入

## 決策

### 1. 採用「單一部署單一商店」模式，不做同 instance 的 request-level multitenancy

canonical 術語建議使用：

- `ShopRuntime`
- `ShopManifest`
- `ShopModule`

啟動時由 `SHOP_ID` 或 `--shop-id` 指定商店，再載入：

- 該商店的資料庫連線
- 啟用模組清單
- 該商店專用的設定檔

這是 deployment-profile 問題，不是傳統 shared-instance multitenancy。先明確定義，避免後續誤長成 request-level tenant routing。

### 2. 折扣引擎改為 instance-based service，接受 `DiscountRule` 插件

將目前靜態 `DiscountEngine.Calculate(...)` 改為注入式服務。

discount domain 的公開 contract 只保留：

- `IDiscountRule`
- `DiscountRecord`

與購物車直接相關的輸入模型，例如：

- `LineItem`
- `CartContext`
- 消費者或會員資訊

應歸在 cart domain 或外層 orchestration service，而不是放在 discount domain contract。

啟動時依 `ShopManifest` 載入可用的 `DiscountRule` 實作；實際「這次試算要啟用哪些規則」的資訊，應在 cart-side context 或外層 orchestration 決定，不讓 `DiscountEngine` 直接依賴 `IShopRuntimeContext`。

### 3. 商品擴充不再直接塞在 `Product`，改為「商品定義」與「購買項目」兩層模型

canonical 術語建議使用：

- `CatalogProduct`: 商店目錄中的商品定義
- `CartItem`: 可加入購物車的購買項目
- `ProductType`
- `Fulfillment`

原因：

- 一般實體商品與預約服務，生命週期不同
- 預約服務會在加入購物車前或加入時產生時段、服務人員、預約保留資訊
- 結帳後還可能產生 token 或兌換資料

因此不建議把「預約成功後的專屬 prod-id」直接混成 catalog product 主鍵。建議內部分成：

- `CatalogProductId`: 商品定義 ID
- `CartItemId` / `ReservationId`: 本次購買或預約實例 ID

若 API 對外需要「專屬 prod-id」語意，可在 contract 層對外暴露 `PurchasableItemId`，但不要與 catalog product 主鍵混用。

### 4. 以模組註冊服務，不以 controller 直接操作資料庫

應把目前 controller 內的組裝邏輯搬到 core/application service：

- `IProductCatalogService`
- `ICartService`
- `ICheckoutService`
- `IOrderFulfillmentHandler`

API 只負責 HTTP contract 與授權邊界，真正的商品解析、折扣套用、履約處理交給 service 與 plugin。

### 5. 插件載入以「啟動時 discovery」為主，不做執行期 hot reload

第一階段建議只支援：

- 啟動時掃描模組 assembly
- 依 `ShopManifest` 決定啟用哪些模組

不做：

- 執行中卸載/重載插件
- 同 instance 服務多 shop

這樣能大幅降低複雜度，且符合目前需求。

## 影響

### 核心高影響

- `Core/Product.cs` 需要重構為可區分商品定義與購買實例的模型
- `Core/Cart.cs` 需要從 `productId + qty` 升級為可帶 payload 的 `CartItem`
- `Core/DiscountEngine.cs` 需要改為可注入規則的 engine，且移除對 runtime 的直接耦合
- `API/Controllers/CheckoutController.cs` 需要把結帳 orchestration 移出 controller
- `ShopDatabaseContext` 需要支援新集合或可擴充文件結構

### API 中高影響

- `Program.cs` 需要引入 `shop-id` 啟動流程與模組 discovery
- `ProductsController` 不應再直接讀 `Products` collection
- `CartsController` 的加入購物車請求模型要能接受 plugin payload
- `CheckoutController` 要支援 fulfillment result，例如 token、reservation confirmation

### 文件與測試高影響

- seed 資料、swagger、README、spec 要同步改用 canonical 術語
- 測試需要從 persistence test 擴充到 module integration test
- 至少要有 physical goods 與 booking service 兩種商品型別的整合測試

## 替代方案

### 替代方案 A：只用 appsettings 切資料庫，不做模組抽象

優點：

- 實作最快

缺點：

- 無法支援不同商店的折扣與商品型別差異
- controller 與資料模型會持續變胖

結論：

- 不足以支撐目標

### 替代方案 B：在 `Product` 上持續加欄位支援預約服務

優點：

- 初期變更量較少

缺點：

- 實體商品與服務商品的生命週期被混在同一模型
- 預約、履約 token、slot lock、staff assignment 會快速污染 core model

結論：

- 不建議

### 替代方案 C：直接做 request-level multitenancy

優點：

- 長期彈性較高

缺點：

- 顯著增加 routing、快取、授權、設定與資源隔離複雜度
- 超出目前需求

結論：

- 現階段不值得

## 後續工作

### Phase 1：建立 shop runtime 與設定載入

- 建立 `ShopManifest`
- 啟動時接受 `SHOP_ID`
- 讓資料庫、模組與設定皆從 `ShopManifest` 解析

### Phase 2：抽出折扣插件邊界

- 將 `DiscountEngine` 改為 service
- 實作 `IDiscountRule`
- 把現有「第二件六折」規則改寫為第一個 plugin
- 將試算輸入改為 cart-side context，而不是 discount 專用 context

### Phase 3：抽出商品型別與結帳服務

- 建立 `CatalogProduct`、`CartItem`、`ICheckoutService`
- 讓 controller 改走 service
- 完成 physical goods 的預設 handler

### Phase 4：實作 booking service 模組

- 建立預約時段與服務人員模型
- 支援 reservation hold / confirm / cancel
- 結帳成功後簽發 fulfillment token

### Phase 5：補文件、seed、spec 與整合測試

- 補商店設定範例
- 補 plugin 開發指引
- 補 booking service end-to-end 測試
