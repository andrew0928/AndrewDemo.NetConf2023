# `.Core` 採 internal projection pipeline 與 extension merge，`.Abstract` 保持 frozen

## 狀態

- superseded
- 日期：2026-03-31

補充：

- 本文件描述的是為了支援 shared master / shared physical database 所提出的 `.Core` 重構方向。
- 目前主線已由 [2026-04-01-shop-runtime-tenant-isolation-mode.md](2026-04-01-shop-runtime-tenant-isolation-mode.md) 明確維持 tenant isolation mode。
- 因此本文件暫不作為目前主線實作基準。

## 背景

前面的討論已經確認：

- `member` 與 `product/inventory` 要走 shared master
- `bts-education` 是獨立 shop
- `.Abstract` 的公開 contract 不應為了這輪需求而變更

使用者進一步放寬的條件是：

- `.Core` 若有必要可以調整
- 可以改 `.Core` 封裝的內部行為
- `IProductService` 如何載入資料並轉換成 model，可以設計成可擴充
- `member` / `product` 可以接受固定 `.Extension` 欄位
- 或可接受 schema-free 的 custom extension model

這代表現在真正要決定的是：在不改 `.Abstract` 的前提下，`.Core` 內部應如何承接 shared master、sidecar extension 與 projection。

## 決策

### 1. `.Abstract` 保持 frozen，`.Core` 可重構 internal persistence 與 mapping

公開 contract 維持不變：

- `Product`
- `IProductService`
- `CartContext`
- `IDiscountRule`

但 `.Core` 不再要求「資料庫 document 必須等同於公開 model」。

### 2. `.Core` 應導入 internal persistence records

建議方向：

- `ProductMasterRecord`
- `ProductProfileRecord`
- `ProductPriceEntryRecord`
- `ProductInventoryRecord`
- `MemberMasterRecord`
- `MemberProfileRecord`
- `MemberVerificationRecord`

這些 record 屬於 `.Core` 內部 persistence / repository 邏輯，不暴露為 `.Abstract` contract。

### 3. `IProductService` 的資料載入流程正式模組化

`IProductService` 的 public surface 不變，但其內部流程應可拆成：

1. 載入 product master
2. 載入 product profile / extension
3. 載入 price book entries
4. 載入 inventory / availability
5. 套用 shop-specific projection policy
6. 投影成 shared `Product`

這樣：

- `AppleStoreProductService` 可讀 `retail`
- `AppleBtsProductService` 可讀 `education` / `bts-2026`

但兩者都共用同一份 product master。

### 4. `Extension` 可以存在，但只能是 `.Core` internal extension，不是 public contract

接受下列兩種模式：

1. fixed `.Extension` 欄位
2. schema-free custom extension model

但規定如下：

- `Extension` 不得放進 `.Abstract.Product` / `.Abstract` 其他公開 model
- `Extension` 只能存在於 `.Core` internal record 或 sidecar profile record
- 業務正確性關鍵資料不得只放在 schema-free `Extension`

### 5. correctness-critical 資料一律 typed

以下資料必須維持 typed schema：

- price book
- inventory
- member verification
- BTS offer rule

理由：

- 需要查詢與索引
- 需要 deterministic 行為
- 不應依賴 blob payload 隱性約定

### 6. 推薦採用 `1:1 root + 1:N children` 作為通用 extension pattern

因為使用者已說明過往設計常用：

- 主資料不變
- 擴充表 `1:1`
- 程式碼 merge

本決策接受這個方向，但做一個必要修正：

- 若資料天然是多版本、多視圖、多歷史，就以 `1:1 root + 1:N children` 實作

例如：

- `products`
- `product_pricing_profiles`
- `product_price_book_entries`

或：

- `members`
- `member_education_profiles`
- `member_verification_records`

## 影響

- 先前「兩個 shop 指向同一個 DB file 才能共享 member/product」不再是唯一可行方案
- `.Core` 可以進一步拆出 shared master data context 與 shop runtime data context
- `DefaultProductService` 將不應繼續直接讀 `ILiteCollection<Product>`
- 後續 product/member 擴充會以 internal records + repository + mapper 方式承接

## 替代方案

### 替代方案 A：維持目前 `.Core` 直接把 collection 當公開 model

優點：

- 變更量最小

缺點：

- 無法乾淨承接 sidecar extension merge
- shared master 與 runtime data 仍會糾纏
- product/member 的 projection 難以模組化

結論：

- 不採用

### 替代方案 B：把 `.Extension` 直接放進 `.Abstract.Product`

優點：

- 使用端最直接

缺點：

- 汙染公開 contract
- 與本輪 frozen `.Abstract` 原則衝突

結論：

- 不採用

### 替代方案 C：所有擴充都改成 schema-free blob

優點：

- 初期看起來很彈性

缺點：

- 正確性關鍵資料無法穩定查詢與索引
- service 間容易形成隱性格式耦合

結論：

- 不採用

## 後續工作

1. 在 `.Core` 內定義 product/member 的 internal persistence records。
2. 抽出 repository 與 mapper，取代目前直接讀 collection 的 `DefaultProductService`。
3. 定義哪些欄位屬於 typed extension，哪些欄位允許進 `Extension`。
4. 再依此決定：
   - shared master data context
   - shop runtime data context
   - BTS / retail product service 的 projection policy
