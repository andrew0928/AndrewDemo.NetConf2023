# BTS Campaign 與 SalePage Projection 規格

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-04-01

## 範圍

本規格只涵蓋以下主題：

1. Apple BTS 活動在商業上是同一個 shop 內的限期 campaign
2. 公開 `Product` 與內部 `SalePage` / `SKU` 的對應關係
3. BTS 資格、價格、主商品與贈品群的商業規則
4. 購物車與結帳對 BTS 入口、時間窗與驗證狀態的判定

本規格暫不涵蓋：

- 具體資料表 schema
- `.Core` repository / mapper 實作
- 前端頁面與 UI 流程
- UNiDAYS 第三方串接細節
- 退貨、退款與事後追價流程

## 目標

- 讓後續技術規格能直接對齊 `.Abstract` 對外名詞
- 明確定義 `Product` 對外其實是 `SalePage projection`
- 明確定義 BTS 是 campaign，而不是獨立 `ShopId`

## Canonical 術語

- `Member`: 對外公開的買家身分
- `Product`: 對外公開的可販售頁面投影
- `SalePage`: 內部販售投影模型，決定入口、價格與活動規則
- `SKU`: `.Core` 的內部商品主檔，代表型號、規格與庫存
- `BTS Entry`: BTS 活動入口
- `BTS Main Product`: 可享 BTS 優惠的主商品
- `BTS Gift Group`: 某個主商品可搭配的贈品群
- `BTS Gift Product`: 贈品群內可被選擇的一個商品
- `BTS Price`: 主商品在 BTS campaign 下的明確售價
- `BTS Discount`: 收據上顯示的 BTS 優惠折扣項目

## 商業邊界

### 1. BTS 是同一個 shop 內的限期 campaign

canonical 結論：

- BTS 不是獨立 `ShopId`
- BTS 不是獨立 shop database
- BTS 是同一個 shop 內的一個 campaign / 入口 / 販售規則集合

因此：

- 同一個商品可同時存在一般入口與 BTS 入口
- 兩者共用同一個內部商品主檔與庫存

## 公開名詞與內部模型的對應

### 1. `Product` 對外代表 `SalePage projection`

對外 `.Abstract` / API 使用的 `Product`，不是 `SKU master`，而是某個 `SalePage` 對外投影後的販售資訊。

規則：

- `Product.Id = SalePageId`
- `Product` 對外代表可被瀏覽、加入購物車、結帳的販售頁面
- `Product.Price` 對外代表該 `SalePage` 在當下入口下的售價投影

### 2. `SKU` 不對外公開

`SKU` 屬於內部資料模型，負責：

- 型號
- 規格
- 庫存
- `.Core` 標準商品能力
- 實際商品主檔識別

規則：

- `SkuId` 不應直接成為 `.Abstract.Product.Id`
- 庫存資訊不需要透過 `.Abstract` 對外公開
- 若 `Product` 內部保存 `SkuId`，也不應作為產品查詢 API 的預設公開欄位

### 3. `Member` 對外仍維持買家身分語意

對外公開的 `Member` 代表購買者身分。

BTS 相關驗證則是 member-side 狀態：

- member 必須有明確的 `.edu` 驗證結果
- 查得到有效驗證資料才算符合 BTS 資格

## BTS 商業規則

### 1. `.edu` 驗證

- 只有查得到明確驗證資料的 member，才算符合 BTS 資格
- 驗證資料必須有有效期限
- 無資料、資料失效、資料無效都視為不符合

### 2. BTS 價格

- `BTS Price` 只存在主商品
- 贈品商品本身沒有獨立的 `bts-price`
- 收據仍以「原價 - BTS 優惠折扣」方式條列

例如：

- 主商品原價 `35900`
- 主商品 `BTS Price = 31400`
- 若搭配 `AirPods 4 = 5990`
- 收據可表達為：
  - 主商品 `31400`
  - `BTS 優惠 = -5990`
  - 贈品 `5990`

### 3. 主商品與贈品群

- 每個 BTS 主商品對應一個贈品群
- 贈品群內可有多個可選贈品
- 消費者必須自行選擇贈品
- 一個主商品最多只能搭配一個贈品
- 有些主商品可只有 BTS 優惠，沒有贈品

### 4. BTS 入口

- 只有從 `BTS Entry` 加入購物車的商品，才享有 BTS 資格
- 一般入口不需要提醒或要求進行 `.edu` 驗證
- BTS 入口在加入購物車前就必須先完成驗證

### 5. 購物車中的 BTS 資格保留

- 若商品是從 BTS 入口加入購物車，之後離開 BTS 頁面到一般購物車流程結帳，BTS 資格仍保留
- 若商品不是從 BTS 入口加入，即使 member 已通過驗證，也不享有 BTS 折扣

### 6. 活動時間窗

- 活動必須有 `start/end` 時間窗
- 加入 BTS 商品到購物車時，必須仍在活動期間內
- 結帳當下也必須仍在活動期間內
- 若結帳時活動已過期，系統必須禁止結帳
- 消費者需更新購物車後重新結帳

### 7. 主商品移除後的贈品行為

- 若主商品被移除，原先選取的贈品可以保留在購物車
- 但該贈品必須失去 BTS 折扣

### 8. 多組 BTS 組合

- 一張訂單允許同時存在多組 BTS 主商品 + 贈品組合
- 每一組主商品 / 贈品組合各自獨立判定資格與折扣

## 對 `.Abstract` 名詞的直接對應

### Product

- `.Abstract.Product` = `SalePage projection`
- `Product.Id = SalePageId`
- `Product.Price` = 當前販售頁面投影價格

### Member

- `Member` = 買家身分
- BTS 驗證是 member-side qualification

### Cart / Order

- `Cart` 與 `Order` 對外只需要看到 `Product`
- 不需要直接看到 `SKU`
- 收據與訂單行可用 `Product` 與 `BTS 優惠` 來表達交易結果

## 非目標

- 本規格不要求 `.Abstract` 直接曝露 `SalePage`、`SKU` 型別
- 本規格不要求 `.Abstract` 直接曝露庫存欄位
- 本規格不定義第三方驗證 API contract
- 本規格不定義內部資料表名稱
