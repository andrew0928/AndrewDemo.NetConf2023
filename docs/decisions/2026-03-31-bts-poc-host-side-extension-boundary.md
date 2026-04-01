# BTS POC 採 host-side 擴充，不重開 shared contract

## 狀態

- proposed
- 日期：2026-03-31

## 背景

新的 POC 需求要模擬 Apple Store BTS 活動的三個核心機制：

- 教育資格驗證
- 商品價格差異
- 特定商品搭配贈品組合

但本輪有一個強制限制：

- `.Abstract`
- `.Core`

都不能做任何異動。

同時目前既有 shared contract 有以下限制：

- `Product` 只有單一 `Price`
- `IProductService` 沒有 buyer context
- `Member` 沒有 email 欄位
- `Product` contract 沒有 promotion metadata

因此若要在不改 frozen contract 的前提下做 BTS，必須先明確決定落點邊界。

## 決策

### 1. BTS POC 定位為單一 shop 的 host-side module

不把 BTS 當成 shared contract 演進，而是把它視為特定 shop 的活動模組。

這代表：

- `ShopManifest` 仍作為啟動入口
- `IProductService` 由 BTS shop 指向自訂實作
- discount 邏輯由 host 端新增 `IDiscountRule` 實作後掛入既有 `DiscountEngine`

### 2. 活動資格與贈品組合資料一律 side-by-side 儲存

以下資料不進 shared models：

- school email
- 驗證狀態
- eligible product / promotion product 對應表
- fulfillment entitlement 記錄

這些資料都以 host-side collections 維護。

### 3. `IProductService` 與 discount orchestration 分工明確

`IProductService` 負責：

- 商品查詢
- promotion product 解析
- order complete 後的活動 fulfillment 記錄

discount orchestration 負責：

- 活動折抵判斷
- 折扣明細輸出

也就是說，BTS 的「商品存在與價格」屬於 product domain；BTS 的「是否折抵」屬於 discount domain。

### 4. checkout 防呆放在 API host，不放進 `.Core`

由於 `.Core.CheckoutService` 不能改，因此：

- 未驗證使用者不可完成 BTS 組合結帳
- promotion product 不可單買
- 不相容組合不可完成結帳

這些檢查都應放在 controller / host-side guard。

### 5. `/api/products` 不承擔 promotion metadata

由於 shared `Product` contract 無法表達活動組合，因此新增 host-specific API 表達：

- eligible product
- promotion products
- promotion savings
- requires eligibility

而不是污染既有 `/api/products` 回應。

## 影響

- 這輪可以維持 `.Abstract` / `.Core` 完全不動
- 這輪可以直接驗證 `ProductService` 模組化與 `DiscountRule` 模組化是否足夠
- BTS POC 會以 host-specific API 與 side-by-side collections 為主
- 若未來 BTS 需求變成長期跨 shop 共用能力，才需要重開 Phase 1 調整 spec 與 contract

## 替代方案

### 替代方案 A：直接修改 `.Abstract` 增加 promotion / eligibility contract

優點：

- shared model 看起來更完整

缺點：

- 違反本輪強制限制
- 尚未證明這些欄位值得進入長期共用 contract

結論：

- 不採用

### 替代方案 B：把 BTS 全部塞進 `IProductService`

優點：

- 元件數量較少

缺點：

- 商品解析與折扣判斷混在一起
- 無法重用現有 `DiscountEngine`
- 不利於驗證目前 discount modularization 的價值

結論：

- 不採用

### 替代方案 C：把贈品做成 checkout 後才補送，不經過 cart / discount

優點：

- controller 變更較少

缺點：

- 與 Apple BTS 的交易當下折抵機制差距太大
- estimate / order discount lines 看不到活動效果
- 也無法驗證 discount 邊界是否合理

結論：

- 不採用

## 後續工作

1. 先以 `/docs/apple-store-bts-poc-design.md` 作為 review 基線。
2. 若你接受這個邊界，再進一步決定 BTS side-by-side collections 與 API 命名。
3. 若後續確認要實作，再在 `.API` 或新 host-side project 落實：
   - `BtsEligibilityService`
   - `BtsProductService`
   - `BtsPromotionDiscountRule`
   - `BtsCheckoutGuard`
