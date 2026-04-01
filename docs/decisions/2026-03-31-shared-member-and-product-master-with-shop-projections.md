# 共享會員與商品主檔，shop 只做 projection 與 extension

## 狀態

- superseded
- 日期：2026-03-31

補充：

- 關於「是否需要維持同一個 DB file 共用」與「`.Core` 是否可調整」的實作邊界，
  已由 [2026-03-31-core-internal-projection-and-extension-strategy.md](2026-03-31-core-internal-projection-and-extension-strategy.md) 補充與取代。
- 關於「目前主線是否採 shared physical database」的結論，已由 [2026-04-01-shop-runtime-tenant-isolation-mode.md](2026-04-01-shop-runtime-tenant-isolation-mode.md) 取代。
- 本文件保留的 canonical 重點是：
  - shared master
  - shop projection
  - typed extension

## 背景

在 Apple Store 與 `bts-education` shop 的討論中，已確認：

- `bts-education` 會是獨立 shop
- 但 `member database` 必須共用
- `product database` 必須共用
- 尤其 inventory / stock control 不能分裂

當時討論背景下，有兩個技術限制：

- `.Abstract` 不可異動
- `.Core` 不可異動

因此必須先決定：如何在不複製主檔的前提下，仍支援 shop-specific 的價格、資格驗證與活動組合。

## 決策

### 1. `member` 與 `product/inventory` 採 shared master

不同 shop 不得複製 member 與 product 主檔。

canonical 原則：

- member identity 是共享主檔
- product identity 與 inventory 是共享主檔
- shop 只決定如何讀取與投影這些共享資料

### 2. 價格差異採 price projection，不採 product duplication

一般商店價格與教育/BTS 價格，視為同一商品在不同 `PriceView` 下的投影。

因此建議採用：

- shared `products`
- shared `product_price_book_entries`
- shop 綁定自己的 `PriceViewId`

而不是：

- 一般商店一份 products
- BTS 商店再複製一份 products

### 3. 會員驗證採 shared member extension

教育資格、UNiDAYS 驗證、email 驗證結果，不進 shared `Member` 本體，而是放進 shared extension collection。

也就是：

- shared `members`
- shared `member_verification_records`

`bts-education` 只讀 shared member verification，不維護第二份 member。

### 4. BTS offer 視為 shared product extension

主商品與 promotion product 的搭配規則，不屬於 shared `Product` 固定欄位，但仍應附著在 shared product master 周邊，而不是複製商品資料。

因此：

- `bts_offer_catalog` 屬於 product-side extension collection
- 由 `applebts-productservice` 與 discount rule 讀取

### 5. code level 一律走 typed repository / service，不用 generic metadata blob

雖然資料庫 schema 必須允許擴充，但不採用無邊界的 generic metadata 設計。

要求：

- 每個 extension collection 都要有自己的 typed model
- 每個 extension collection 都要有對應 repository / service
- `IProductService` 只輸出 shared `Product` projection

補充：

- sidecar extension 可以採 `1:1` 對應方式
- 由程式碼負責 merge 主資料與 sidecar data model

但這個原則要加一條限制：

- 若資料天然屬於多版本、多視圖、多歷史，就不可硬做成 `1:1`

因此本決策接受兩種合法模式：

1. `1:1` sidecar profile
2. `1:1` root + `1:N` children

其中多價格的 canonical 做法是：

- `products` 為 shared master
- pricing 可有 `1:1` profile root
- 真正的 price entries 仍為 `1:N`

### 6. 當 `.Core` 不可調整時，POC 可先接受「同一 DB file 共用」的限制

由於 `.Core` 目前只有單一 `ShopDatabaseContext`：

- 若不修改 `.Core`
- 又要共享 `members` / `products`

則兩個 shop 在 POC 階段最務實的做法，是先指向同一個 DB file。

這代表：

- `carts`
- `orders`
- `member_tokens`
- `checkout_transactions`

也會一起共用。

這不是長期最乾淨的邊界，但在當時 frozen `.Core` 前提下是可接受的 POC 做法。

若接受新版決策：

- [2026-03-31-core-internal-projection-and-extension-strategy.md](2026-03-31-core-internal-projection-and-extension-strategy.md)

則不再需要把「同一個 DB file 共用」視為 canonical 做法。

## 影響

- `applestore-productservice` 與 `applebts-productservice` 可以共用 product master 與 inventory
- 多價格模式可在不改 shared `Product` contract 下先透過 projection 落地
- 教育資格驗證會附著在 shared member master 周邊
- 之後若要把 shared master 與 shop runtime 實體分開，需重開 Phase 1

## 替代方案

### 替代方案 A：每個 shop 複製自己的 member 與 product

缺點：

- inventory 分裂
- identity 分裂
- 價格與上架狀態容易不同步

結論：

- 不採用

### 替代方案 B：把多價格直接塞進 `products` blob 欄位

缺點：

- code level 缺少穩定邊界
- schema 很快失控
- 不同 service 會隱性依賴彼此 metadata 格式

結論：

- 不採用

### 替代方案 C：先為了乾淨邊界而重開 `.Core`

優點：

- 長期架構較乾淨

缺點：

- 違反目前階段限制
- 會把本輪 POC 變成 shared contract 重構專案

結論：

- 目前不採用

## 後續工作

1. 先以 shared master + typed extension + projection 作為後續所有衍生需求的基準。
2. POC 階段在同一 DB file 內新增：
   - `member_verification_records`
   - `product_price_book_entries`
   - `bts_offer_catalog`
   - `product_inventory_records` 或等價集合
3. `applestore-productservice` 與 `applebts-productservice` 只透過不同 `PriceViewId` 讀取價格。
4. 若未來要把 shared master data 與 shop runtime data 物理拆分，再重新開啟 Phase 1。
