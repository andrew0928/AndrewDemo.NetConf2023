# Apple Store / Apple BTS 採不同 `ShopId`，但共用同一個實體資料庫

## 狀態

- superseded
- 日期：2026-04-01

補充：

- 本文件是建立在 shared physical database 前提下的替代方向。
- 目前主線已由 [2026-04-01-shop-runtime-tenant-isolation-mode.md](2026-04-01-shop-runtime-tenant-isolation-mode.md) 明確維持 tenant isolation mode。
- 因此本文件暫不作為目前主線實作基準。

## 背景

目前已確認：

- `apple store` 與 `apple bts` 可以接受為兩個獨立 host
- `member`、`product`、`inventory` 不能不同步
- 使用者明確要求兩邊必須共用同一個資料庫
- 可以接受 sidecar tables / extension columns

因此需要先釐清一件事：

- 兩個 host 是否可以共用同一個 `ShopId`，只靠不同 manifest 啟動兩組 API server

## 決策

### 1. 兩個 host 不應共用同一個 `ShopId`

若兩組 host 的行為不同：

- price projection 不同
- eligibility 規則不同
- offer / discount 行為不同
- catalog visibility 不同

那它們就不應共用同一個 `ShopId`。

canonical 原則：

- `ShopId` 代表一組 runtime projection
- `ShopManifest` 代表該 `ShopId` 的唯一啟動設定

因此：

- 不接受「同一個 `ShopId` 對應兩份不同 manifest」這種做法

建議命名：

- `apple-store`
- `apple-bts`

### 2. 兩個 host 應共用同一個實體資料庫

雖然 `ShopId` 不同，但資料來源必須是同一個 physical database。

這代表：

- `members` 共用
- `products` 共用
- `inventory` 共用
- extension tables 也在同一個 database 內

因此不會出現：

- Apple Store 看得到某會員，Apple BTS 看不到
- 一邊更新商品或庫存，另一邊不同步

### 3. 同一資料庫內，資料分成 shared data 與 shop-scoped runtime data

共用同一個 database，不代表所有資料都不分邊界。

建議在同一個 physical database 內區分：

- shared master data
- shop-scoped runtime data

shared master data：

- `members`
- `products`
- `product_inventory_records`
- `member_verification_records`
- `product_price_book_entries`
- `bts_offer_catalog`

shop-scoped runtime data：

- `carts`
- `orders`
- `checkout_transactions`
- 其他流程性資料

補充：

- 這不是「every row 都帶 `ShopId`」的全域 multi-tenant 設計
- 若把所有資料都用 `ShopId` 當 tenant discriminator，shared member / product / inventory 就會被迫跟著 tenant 切開
- 那會直接違反這次需求

因此 canonical 原則是：

- shared master data 不帶 `ShopId`
- shop-scoped runtime data 才帶 `ShopId`

### 4. shop-scoped runtime data 必須可在同一 DB 內辨識 `ShopId`

因為現在要求是同一個 database，所以 runtime data 不應再假設「每個 DB 只服務一個 shop」。

建議方式二選一：

1. 在同一組 runtime tables 內加入 `ShopId`
2. 在同一 DB 內使用按 shop 區分的 table / collection naming

我較推薦：

- shared tables 共用固定名稱
- runtime tables 加 `ShopId` discriminator

原因：

- 查詢邏輯較穩定
- 不需要為每個新 shop 再複製一組 table 名稱規則

### 4.1 若需要表達「同一家業務商店」，應新增 business identity，而不是共用 `ShopId`

你提出的疑問是合理的：

- 如果每筆資料都標 `ShopId`
- 那 Apple Store / Apple BTS 若要共用資料
- 看起來就會逼它們共用同一個 `ShopId`

這正是為什麼我們不應把所有資料都設計成 `ShopId` multi-tenant。

若系統需要表達：

- Apple Store 與 Apple BTS 屬於同一個 business entity

應引入另一個概念，例如：

- `BusinessStoreId`
- `StoreGroupId`
- `MerchantId`

語意：

- `ShopId` = runtime projection / host behavior
- `BusinessStoreId` = 業務上同一家店或同一品牌單位

因此：

- `apple-store` 與 `apple-bts` 可以有不同 `ShopId`
- 但它們可對應到同一個 `BusinessStoreId=apple`

而 shared master data 若未來真的需要歸屬某個 business entity，也應掛在 `BusinessStoreId`，不是 `ShopId`

### 5. host 與 database 的關係

建議最終關係如下：

- Host A:
  - `SHOP_ID=apple-store`
  - manifest 指向 retail projection
- Host B:
  - `SHOP_ID=apple-bts`
  - manifest 指向 education / bts projection

但兩者：

- 都連到同一個 physical database

## 影響

- `ShopId` 的語意會更清楚，不會再跟資料庫實體綁在一起
- manifest 可以專心表達 runtime projection / service wiring
- shared master data 與 shop-specific runtime data 可在同一 DB 內並存
- 系統若需要表達「同一家業務商店」，應在 `ShopId` 之外建立另一個 business identity
- `.Core` 後續需要把「單 DB = 單 shop」的隱性假設移除

## 替代方案

### 替代方案 A：共用同一個 `ShopId`，但用不同 manifest 啟兩個 host

缺點：

- `ShopId` 與 manifest 不再是一對一
- 會讓 routing、trace、資料隔離與設定管理失去 canonical 語意
- 後續 debug 時很難判斷某筆資料到底屬於哪個 runtime projection

結論：

- 不採用

### 替代方案 B：兩個 `ShopId`，但各用不同資料庫

缺點：

- 與「資料必須完全同步」的要求衝突
- shared member / product / inventory 會變成複製與同步問題

結論：

- 不採用

## 後續工作

1. 後續設計與實作都以「不同 `ShopId`、同一 physical database」為基準。
2. `.Core` 重構時，要讓 runtime data 能在同一 DB 內辨識 `ShopId`。
3. `ShopManifest` 後續應偏向描述 projection / service wiring，而不是只代表資料庫路徑。
