# BTS Campaign 與 SalePage Projection 測試案例

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-04-01

## Public Model Mapping

### TC-BTS-001 `Product.Id` 對外等於 `SalePageId`

- Given: 內部存在一筆 `SalePage`
- When: 對外回傳 `Product`
- Then: `Product.Id` 必須等於該 `SalePageId`
- And: 不得直接曝露 `SkuId`

### TC-BTS-002 `SKU` 不對外公開

- Given: 內部有 `SKU` 與庫存資料
- When: 對外回傳 `Product`
- Then: 回應中不需要公開 `SkuId` 與庫存欄位

### TC-BTS-003 同一公開 sale page 可被一般入口與 BTS 入口共用

- Given: 一個公開 `SalePage`
- When: 使用者從一般入口或 BTS 入口瀏覽該商品
- Then: 對外看到的 `Product.Id` 應相同
- And: 是否具備 BTS 資格不得只靠 `ProductId` 判定

## Member Verification

### TC-BTS-101 只有有效 `.edu` 驗證資料才符合資格

- Given: member 存在 `.edu` 驗證資料
- And: 該資料仍在有效期限內
- When: 進入 BTS 入口準備加入購物車
- Then: 視為符合 BTS 資格

### TC-BTS-102 查無驗證資料時不符合資格

- Given: member 沒有 `.edu` 驗證資料
- When: 從 BTS 入口加入商品到購物車
- Then: 系統必須拒絕

### TC-BTS-103 驗證過期時不符合資格

- Given: member 有 `.edu` 驗證資料
- And: 但已過有效期限
- When: 從 BTS 入口加入商品到購物車
- Then: 系統必須拒絕

## BTS Entry

### TC-BTS-201 一般入口不享有 BTS 優惠

- Given: member 已通過 `.edu` 驗證
- And: 使用者從一般入口加入商品
- When: 建立購物車與試算
- Then: 不享有 BTS 折扣

### TC-BTS-202 BTS 入口加入後離開頁面仍保留資格

- Given: member 已通過 `.edu` 驗證
- And: 商品從 BTS 入口加入購物車
- When: 使用者改從一般購物車頁面結帳
- Then: 仍保留 BTS 資格

## Main Product / Gift Group

### TC-BTS-301 主商品可對應贈品群

- Given: 一個 BTS 主商品
- When: 載入其 BTS 活動資訊
- Then: 可得到對應的贈品群

### TC-BTS-302 一個主商品最多只能搭一個贈品

- Given: 一個 BTS 主商品與同一贈品群中的多個贈品
- When: 消費者選擇贈品
- Then: 系統只允許搭配其中一個贈品

### TC-BTS-303 有些主商品只有優惠沒有贈品

- Given: 一個 BTS 主商品沒有可搭配贈品
- When: 進入 BTS 流程
- Then: 仍可享有 BTS 價格或折扣
- And: 不要求選贈品

## Receipt / Pricing

### TC-BTS-401 主商品資料庫需明確存 `bts-price`

- Given: 一個 BTS 主商品
- When: 系統讀取其活動價格
- Then: 可明確取得 `bts-price`
- And: 不需要靠動態回推最終成交價

### TC-BTS-402 對外 product price 維持原價

- Given: 一個可參與 BTS 的主商品
- When: 對外回傳 `Product`
- Then: `Product.Price` 應維持一般售價
- And: 不直接回傳 `bts-price`

### TC-BTS-403 收據以原價與 `BTS 優惠` 顯示

- Given: 主商品有原價、`bts-price` 與選取贈品
- When: 建立收據
- Then: 可條列原價商品行
- And: 可條列 `BTS 優惠` 折扣行
- And: 可條列贈品商品行

### TC-BTS-404 `BTS 優惠` discount record 可關聯主商品與贈品 line

- Given: 一筆 BTS 優惠同時來自主商品價差與贈品折扣
- When: 規則回傳 `DiscountRecord`
- Then: `RelatedLineIds` 可同時包含主商品 line 與贈品 line

## Time Window

### TC-BTS-501 活動期間外不可加入 BTS 商品

- Given: BTS campaign 已過期
- When: 使用者從 BTS 入口加入商品
- Then: 系統必須拒絕

### TC-BTS-502 結帳時活動過期則失去 BTS 折扣並回傳提示

- Given: 商品已從 BTS 入口加入購物車
- And: 結帳時活動已過期
- When: 使用者送出 checkout
- Then: 系統不得套用 BTS 折扣
- And: 系統可回傳 `Hint` 提示優惠已失效
- And: 結帳仍可依原價繼續

## Cart Mutation

### TC-BTS-601 移除主商品後贈品保留但失去 BTS 折扣

- Given: 購物車內已有 BTS 主商品與贈品
- When: 使用者移除主商品
- Then: 贈品可保留在購物車
- And: 但失去 BTS 折扣

## Multiple Groups

### TC-BTS-701 一張訂單可同時存在多組 BTS 組合

- Given: 購物車內有多個主商品
- And: 每個主商品都各自選了合法贈品
- When: 建立試算與結帳
- Then: 系統允許同時成立多組 BTS 主商品 / 贈品組合
