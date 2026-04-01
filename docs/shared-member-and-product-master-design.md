# 共享會員與共享商品主檔的擴充設計

## 狀態

- status: draft-for-review
- 日期：2026-03-31

## 決策問題

目前要優先決定的不是 BTS 細節，而是更上層的資料架構原則：

- 不同 shop 是否共用同一份 member database
- 不同 shop 是否共用同一份 product database
- 若要共用，是否能同時保留 shop-specific price / eligibility / promotion 擴充能力

這個決策會直接影響後續：

- `IProductService` 的實作方式
- BTS 驗證資料的放置位置
- 庫存控管是否能維持單一來源
- 後續 schema 擴充是否會失控

## 核心結論

結論是：**可行，而且應該這樣做**。

但 canonical 做法不是把所有差異都塞進 shared `products` / `members` 文件本體，而是採：

- shared master data
- typed extension collections
- service-specific projection

也就是：

1. `member` 與 `product/inventory` 作為共享主檔
2. 驗證、價格、活動組合等差異資料放 side-by-side extension collections
3. `applestore-productservice` 與 `applebts-productservice` 讀取同一份主檔，但套用不同 projection 規則

## 邊界修正：`.Abstract` freeze，`.Core` 可重構內部載入與投影

2026-03-31 的新前提是：

- `.Abstract` 的公開 contract 不變
- `.Core` 的內部封裝與資料載入流程可以調整

這會直接改變建議實作方式。

前一版設計為了不動 `.Core`，只能接受：

- 多個 shop 指向同一個 DB file

但在新前提下，較好的 canonical 方向是：

- `.Core` 不再直接把資料庫文件等同於 `.Abstract.Product` / `.Core.Member`
- `.Core` 改由 internal persistence records 載入資料
- `.Core` 內部負責 merge master + extension + price/inventory/verification
- 最後才投影成 `.Abstract` 公開 model 或 `.Core` runtime model

也就是說，現在可以把「主資料 + sidecar + merge」這個模式正式內建到 `.Core`，而不是只在 application layer 硬組。

## 補充原則：主資料不變，擴充資料 sidecar 化，由程式碼合併

你補充的做法是合理的，而且應該成為這條主線的實作原則之一。

canonical 做法可描述為：

- 主資料表保持穩定
- 擴充資料表以 sidecar collection / table 附掛
- 兩者以主鍵做對應
- 由 application service / product service 合併成程式內的 view model

這個模式的優點是：

- 不會把 shared master schema 越改越胖
- 擴充邏輯可被明確封裝在程式碼裡
- 可依 shop / campaign 選擇是否套用某些 extension

### 哪些情境適合 `1:1` sidecar

若擴充資料與主體之間是「每個主體最多一份擴充」，就適合 `1:1`：

- member education profile
- member compliance profile
- product operational extension
- product publishing extension

這類情境很適合用：

- 主表保存 canonical identity
- sidecar 表保存額外欄位
- service 讀取後 merge 成 domain model / view model

### 哪些情境不適合硬做成 `1:1`

若擴充資料本質上是一個主體對多個版本、視圖或規則，就不應硬塞進 `1:1`：

- 多價格
- 多活動組合
- 多驗證紀錄歷史
- 多庫存異動紀錄

例如：

- 同一商品可同時有 `retail`、`education`、`bts-2026`
- 同一商品可能參與多個 promotion offer

這種情況較合理的是：

- `1:N` extension tables
- 或 `1:1` root extension + `1:N` subordinate entries

也就是說，sidecar 化是對的，但不等於所有 sidecar 都必須是 `1:1`。

## `.Extension` 欄位與 schema-free model 的定位

你現在接受兩種做法：

- 固定的 `.Extension` 欄位
- 自訂 product-extension model，甚至 schema-free

在新的 `.Core` 邊界下，這兩者都可行，但建議不要無差別混用。

### 建議採用的 hybrid 規則

1. correctness-critical 資料維持 typed 結構
2. shop-private / app-private 的非關鍵資料可放 `Extension`

### 哪些資料不適合放 schema-free `Extension`

下列資料若要參與查詢、索引、計價或驗證，應維持 typed schema：

- price book
- inventory
- member verification status
- BTS offer eligibility rule

原因：

- 這些資料直接影響商務正確性
- 需要明確索引與查詢條件
- 不適合藏在無型別 blob 裡

### 哪些資料可以接受放 `Extension`

下列資料可視為 module-private / app-private payload：

- UI 顯示用附加欄位
- 內部備註
- 非共用的 product rendering hints
- app-specific metadata

因此 canonical 建議是：

- `Product` / `Member` 的正式共享欄位維持精簡
- `.Core` 內部可有 `ProductProfile.Extension` / `MemberProfile.Extension`
- `IProductService` 或其他 service 需要時自行解讀

### `.Extension` 應放在哪一層

因為 `.Abstract` 不可變，所以不建議把 `.Extension` 放在公開 `Product` contract 上。

較合理的放置位置是：

- `.Core` internal persistence record
- 或 `.Core` sidecar profile record

例如：

- `ProductProfileRecord.Extension`
- `MemberProfileRecord.Extension`

這樣既保留彈性，也不會污染公開 contract。

## 為什麼這是正確方向

### 1. 會員身分本來就不應因 shop 而重複

若 `applestore` 與 `bts-education` 各自維護 member：

- 同一個人會有兩份 identity
- access token 與資格驗證容易分裂
- 後續訂單、審計、客服查詢都會變複雜

因此 member 應該是 shared identity，而不是 shop-local entity。

### 2. 商品與庫存主檔也不應因 shop 而分裂

如果一般商店與 BTS 商店各有自己的 product 資料：

- 庫存會變雙軌
- 同一商品 id 可能失去單一來源
- 價格調整與上架狀態容易不同步

尤其你已明確指出「庫存等等管控機制」必須共用，這代表：

- `ProductId`
- inventory
- publication lifecycle

至少應有單一 canonical source。

### 3. 價格差異是 projection，不是 product identity 差異

一般價與教育價通常不是兩個不同商品，而是同一商品在不同銷售情境下的不同價格視圖。

因此更合理的建模是：

- shared product master 定義「這是什麼商品」
- price projection 定義「在這個商店 / 情境下賣多少錢」

而不是：

- 建兩筆不同 product records

## 對你提出例子的直接回答

你問的這種設計：

- 資料庫可儲存多個價格
- `applestore-productservice` 讀一般價格
- `applebts-productservice` 讀優惠價格

**可行，而且是建議方向。**

但我不建議把這個概念直接綁成「哪個 service 就讀哪個欄位」。

更穩定的 canonical 應該是：

- price data 依 `PriceViewId` / `PriceBookId` 建模
- product service 只是選用哪個 `PriceViewId`

例如：

- `retail`
- `education`
- `bts-2026`

這樣 price 維度是業務語意，不是 code module 名稱。

## 建議的資料分層

### A. shared member master

建議保留單一 member 主檔，承載最穩定的識別資料。

最少包含：

- `MemberId`
- `DisplayName`

現況限制：

- `.Core.Member` 目前只有 `Id`、`Name`
- 若不改 `.Core`，其他 member 資料必須走 extension collections

### B. shared product master

單一商品主檔負責：

- `ProductId`
- 名稱與描述
- 商品生命週期
- 是否可銷售

現況可沿用既有 `products` collection 作為 shared product master 的起點。

### C. shared inventory data

庫存與可售量必須與 product master 同層視為 shared data。

建議不要把 inventory 綁在某個 shop-specific service 內部。

### D. extension collections

把會因 shop / audience / campaign 改變的資料拆出去：

- member verification
- price book
- BTS offer catalog
- shop publication / sorting

## 建議的 collection 設計

### 1. `members`

角色：

- shared member master

備註：

- 目前可沿用既有 collection

### 2. `member_verification_records`

角色：

- 存放 education / UNiDAYS / 其他資格驗證

建議欄位：

- `MemberId`
- `VerificationKind`
- `VerificationStatus`
- `VerifiedEmail`
- `VerifiedAt`
- `ExpiresAt`
- `Provider`

規則：

- 可同時支援多種驗證類型
- BTS 只吃 `education-student` 或類似 kind

補充：

- 若只需要「目前有效的教育資格狀態」，可再加一張 `member_education_profiles` 做 `1:1` 快照投影
- 若需要保留驗證歷史，則 `member_verification_records` 仍維持 `1:N`

### 3. `products`

角色：

- shared product master

備註：

- 目前可沿用既有 collection
- `Product.Price` 在現況仍會存在，但它只適合當 default/base price
- 若要正式支援多價格，長期不應只依賴這個欄位

### 4. `product_price_book_entries`

角色：

- 存同一商品在不同 price view 下的價格

建議欄位：

- `ProductId`
- `PriceViewId`
- `Amount`
- `Currency`
- `EffectiveFrom`
- `EffectiveTo`
- `IsActive`

規則：

- `(ProductId, PriceViewId, EffectiveFrom)` 應可唯一識別一筆價格版本
- 一個 product 可同時有多個價格視圖

補充：

- 若你偏好「主表 + 1:1 擴充表」的習慣，可再增加 `product_pricing_profiles`
- `product_pricing_profiles` 可作為 `1:1` pricing root
- `product_price_book_entries` 則作為該 product 的多價格明細

這樣可以同時兼顧：

- schema 上的 sidecar 擴充習慣
- 多價格天然是 `1:N` 的事實

### 5. `product_inventory_records` 或 `inventory_ledger`

角色：

- 存共享庫存

建議欄位：

- `ProductId`
- `AvailableQuantity`
- `ReservedQuantity`
- `UpdatedAt`

若後續要更嚴謹，可升級為 ledger 模式。

### 6. `bts_offer_catalog`

角色：

- 定義 eligible product 與 promotion product 的搭配規則

建議欄位：

- `OfferId`
- `EligibleProductId`
- `PromotionProductId`
- `RequiredVerificationKind`
- `PromotionSavings`
- `PriceViewId`
- `IsActive`

這裡的 `PriceViewId` 可表達：

- 這個 offer 適用哪個價格視圖

### 7. `shop_catalog_views`

角色：

- 將 `ShopId` 對應到哪個商品視圖

建議欄位：

- `ShopId`
- `PriceViewId`
- `ProductServiceId`
- `VisibilityPolicyId`

這樣：

- 一般商店可對應 `retail`
- `bts-education` 可對應 `education` 或 `bts-2026`

## 建議的 sidecar pattern 分類

### Pattern A：`1:1` sidecar profile

適用於：

- 單一主體對單一擴充狀態

例子：

- `members` -> `member_education_profiles`
- `products` -> `product_catalog_extensions`

程式碼模式：

- repository 先讀 master
- 再讀 sidecar
- service 合併為同一個 domain projection

### Pattern B：`1:N` sidecar entries

適用於：

- 同一主體有多個價格、規則、歷史、版本

例子：

- `products` -> `product_price_book_entries`
- `products` -> `bts_offer_catalog`
- `members` -> `member_verification_records`

程式碼模式：

- service 依 `PriceViewId`、`VerificationKind`、`OfferId` 等條件選擇有效資料
- 再投影成 runtime model

### Pattern C：`1:1` root + `1:N` children

這是我最推薦的折衷做法。

適用於：

- 你希望 schema 上看得出這是一個正式 extension module
- 但該 module 內部又需要多筆子資料

例子：

- `products`
- `product_pricing_profiles`
- `product_price_book_entries`

或：

- `members`
- `member_education_profiles`
- `member_verification_records`

優點：

- 符合你熟悉的 `1:1` extension 主軸
- 也不會扭曲多價格 / 多驗證這種天然 `1:N` 的資料型態

## code level 如何保持符合規範

關鍵不是「資料庫能擴充」而已，而是「程式碼不能讓擴充失控」。

建議原則：

### 1. shared collections 與 extension collections 要分層

不要把所有資料都塞進 `Product.Metadata` 或 `Member.Metadata` 類型的 blob。

因為那會造成：

- schema 無邊界
- service 間隱性耦合
- 測試與 migration 很難管理

### 2. 每個 extension collection 都要有對應的 typed repository / service

例如：

- `IMemberVerificationRepository`
- `IMemberEducationProfileRepository`
- `IProductPriceBookRepository`
- `IProductPricingProfileRepository`
- `IBtsOfferRepository`

即使先不進 `.Abstract`，也應在 host-side project 保持 typed contract。

### 3. `IProductService` 只輸出 shared `Product` view，不直接暴露底層 schema

也就是：

- `AppleStoreProductService` 讀 shared product master + `retail` price view
- `AppleBtsProductService` 讀 shared product master + `education` / `bts` price view

最後都投影成 shared `Product`：

- `Id`
- `Name`
- `Description`
- `Price`
- `IsPublished`

這裡的合併流程可以明確分成：

1. 讀 shared `products`
2. 讀對應的 pricing profile / price book entries
3. 依目前 shop 的 `PriceViewId` 選出有效價格
4. merge 成 shared `Product.Price`

### 3.1 `IProductService` 的載入流程應正式可擴充

既然 `.Core` 現在允許調整，建議把目前 `DefaultProductService` 這種直接讀 collection 的方式，升級為明確的載入管線：

1. 載入 shared product master
2. 載入 product profile / extension root
3. 載入 price book entries
4. 載入 inventory / availability
5. 依 shop 或 service 的 projection policy 合併
6. 投影成 shared `Product`

這樣：

- `AppleStoreProductService` 可選 `retail`
- `AppleBtsProductService` 可選 `education` / `bts-2026`
- 兩者共用 master，但 projection 不同

### 3.2 若有需要，可在 `.Core` 引入 internal persistence models

這是目前我最推薦的方向。

例如：

- `ProductMasterRecord`
- `ProductProfileRecord`
- `ProductPriceEntryRecord`
- `MemberMasterRecord`
- `MemberProfileRecord`
- `MemberVerificationRecord`

然後由 `.Core` mapper / repository 將這些資料投影成：

- `.Abstract.Product`
- `.Core.Member`
- 或其他 runtime model

### 4. 驗證規則與資料分離

`BtsEligibilityService` 不應直接耦合 login controller 的細節，而應：

- 從 shared member identity 取 `MemberId`
- 從 `member_verification_records` 判斷是否符合資格

若之後要簡化讀取成本，也可以：

1. `member_education_profiles` 保存目前有效資格快照
2. `member_verification_records` 保存完整驗證歷史

這也是 `1:1 root + 1:N history` 的同一套模式。

### 5. 庫存只允許 shared source 更新

若 product service 只是在不同 shop 投影價格，則庫存扣減邏輯仍必須指向同一份 inventory source。

## 目前 repo 下的可行性判斷

### 在不改 `.Abstract`、但允許調整 `.Core` 的前提下

**可行，而且已經不需要被迫接受「所有資料共用同一個 DB file」這個臨時解法。**

可行之處：

- 目前 `IShopDatabaseContext.Database.GetCollection<T>()` 可存取 side-by-side collections
- `products` 與 `members` 已經是穩定 shared collection 名稱
- `IProductService` 已能用 projection 方式回傳不同 `Price`
- `.Core` 現在可改，因此可以把直接讀取 collection 的實作改成 repository + projection pipeline

若維持舊實作不重構，限制仍然是：

- `.Core` 目前只有單一 `ShopDatabaseContext` 連到單一 DB file
- 若兩個 shop 要共享 `members` / `products`，最直接做法就是兩個 shop 指向同一個 DB file
- 但這樣 `carts`、`orders`、`member_tokens`、`checkout_transactions` 也會一起共用

但在新的可接受邊界下，建議改走第三條路：

### 路線 C：重構 `.Core` 的 data access，拆出 master data 與 runtime data

建議方向：

- shared master data context
- shop runtime data context
- product service projection pipeline

這條路的好處是：

- `members` / `products` / `inventory` 可真正共享
- `carts` / `orders` / `checkout_transactions` 可維持 shop-runtime scope
- 不需要為了 POC 而把所有 collection 綁在同一個 DB file

因此現況下可視為有三條路：

### 路線 A：POC 先共用同一個 DB file

優點：

- 不用動 `.Core`
- 最快驗證共享 member / product / inventory + shop-specific price projection

缺點：

- cart / order / token 也一起共享
- 邊界不夠乾淨

### 路線 B：若只想共享 member/product master，需重開 Phase 1

原因：

- 目前 shared DB / shop DB 無法分開注入
- 若要做成乾淨架構，需要重新定義：
  - master data context
  - shop runtime context
  - inventory context

### 路線 C：在不動 `.Abstract` 下直接重構 `.Core`

這是現在可接受、而且更合理的方案。

做法：

- 保持 `IProductService` public contract 不變
- 在 `.Core` 內引入 internal records / repositories / mappers
- 讓 product/member 的 master 與 extension 合併邏輯內建在 `.Core`

若你要把這條主線走長期，我建議直接採路線 C，不再把路線 A 當 canonical。

## 對 BTS 的直接影響

一旦採用這個決策，BTS POC 的設計方式就會變成：

- `bts-education` 是獨立 shop
- 但它不複製 member master
- 也不複製 product master / inventory
- 它只是用自己的 `ProductService` 與 `PriceViewId` 去讀 shared data
- 教育資格走 shared member extension
- 活動組合走 shared product extension

## 建議的 canonical 術語

- `Shared Member Master`
- `Shared Product Master`
- `Price View`
- `Price Book Entry`
- `Member Verification Record`
- `Shop Catalog View`

避免使用：

- 「BTS 商品資料庫」
- 「教育版會員資料庫」

因為這些說法會讓團隊誤以為主檔要複製。

## 建議的下一步

1. 先確認你是否接受這個 canonical 結論：
   - shared master
   - typed extensions
   - service-specific projection
2. 若接受，POC 實作階段先走路線 A：
   - 同一 DB file
   - 先驗證共享 member/product/inventory + 多價格投影可行
3. 若你接受 `.Core` 可調整這個前提，則建議直接改採路線 C：
   - 在 `.Core` 建立 internal master/profile/entry models
   - 建立 product/member projection pipeline
   - 將 shared master data context 與 shop runtime data context 分開
