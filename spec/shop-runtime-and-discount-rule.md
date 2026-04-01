# Shop Runtime 與 Discount Rule 規格

## 狀態

- phase: 1
- status: draft-for-freeze
- 日期：2026-03-23

## 範圍

本規格只涵蓋以下主題：

1. 啟動時以 `shop-id` 選定商店組態
2. discount contract 與 cart-side 試算輸入邊界
3. `.API` 如何依 `ShopManifest` 組裝啟用的 discount rules
4. `.Core` 如何透過 `CartContextFactory` 建立試算輸入

本規格暫不涵蓋：

- ProductService plugin
- 預約服務商品
- runtime hot reload
- 同 instance 多商店同時服務
- tenant share mode / shared physical database

## 目標

- 同一套 code base 可交付多個商店
- 單一部署只服務單一商店
- 啟動時可依 `SHOP_ID` 或 `--shop-id` 載入對應商店設定
- 目前 runtime data topology 維持 tenant isolation mode
- discount rules 改為插件邊界，不再把規則寫死在 `.Core`
- discount domain 只保留必要 contract

補充：

- tenant isolation mode 的正式定義，請以 [shop-runtime-data-isolation-mode.md](/Users/andrew/code-work/andrewshop.apidemo/spec/shop-runtime-data-isolation-mode.md) 為準。

## Canonical 術語

- `ShopManifest`: 單一商店部署所需的啟動組態
- `CartContext`: 當下可立即結帳的購物車試算輸入
- `CartContextFactory`: 由 cart / member / database 投影出 `CartContext` 的 factory
- `LineItem`: 可在 `Cart` 與 `CartContext` 共用的唯讀 line item 型別
- `DiscountRule`: 一條可獨立註冊的 discount rule
- `DiscountRecord`: 單次 discount 計算的輸出結果

## 啟動組態規格

### 啟動輸入

- 環境變數：`SHOP_ID`
- 命令列：`--shop-id=<shop-id>`

優先順序：

1. `SHOP_ID`
2. `--shop-id`
3. `ShopRuntime:DefaultShopId`

### appsettings 結構

```json
{
  "ShopRuntime": {
    "DefaultShopId": "default",
    "Shops": {
      "default": {
        "ShopId": "default",
        "DatabaseFilePath": "shop-database.db",
        "EnabledDiscountRuleIds": [
          "product-1-second-item-40-off"
        ]
      }
    }
  }
}
```

### ShopManifest 規格

- `ShopId`: 商店識別碼，必填
- `DatabaseFilePath`: 該商店使用的資料庫檔路徑，必填
- `EnabledDiscountRuleIds`: 啟用的 discount rule id 清單，可為空

### 行為要求

- 若指定的 `shop-id` 不存在，啟動必須失敗
- 若 `DatabaseFilePath` 為相對路徑，host 以 `AppContext.BaseDirectory` 為基準解析
- 單一 process 啟動後只會有一個已解析的 `ShopManifest`

## Contract 規格

### Shop Domain

- `.Abstract` 保留：
  - `ShopManifest`
  - `IShopManifestResolver`

### Cart Domain

#### CartContext

- `ShopId`
- `ConsumerId`
- `ConsumerName`
- `LineItems`

#### LineItem

- `ProductId`
- `Quantity`
- `ProductName`
- `UnitPrice`

補充：

- raw `Cart.LineItems` 至少保證 `ProductId`、`Quantity`
- `CartContext.LineItems` 會由 `CartContextFactory` 補齊 `ProductName`、`UnitPrice`

#### CartContextFactory

- 屬於 cart-side orchestration
- 負責把 `Cart`、`Member`、`ShopManifest`、`IShopDatabaseContext` 轉成 `CartContext`
- discount domain 不直接碰 `IShopDatabaseContext`

### Discount Domain

#### IDiscountRule

- `RuleId`: 規則唯一識別碼
- `Priority`: 規則優先順序，數字越小越先執行
- `Evaluate(CartContext)`: 回傳本規則產生的 `DiscountRecord`

#### DiscountEngine

- 屬於 `.Core` 的 concrete service，不放在 `.Abstract`
- 接收 `CartContext`
- 只執行啟動階段已組裝進 engine 的規則
- 依 `Priority`、再依 `RuleId` 排序執行
- engine 不直接依賴 `ShopManifest`、`IShopRuntimeContext`、`IShopDatabaseContext`

#### DiscountRecord

- `RuleId`
- `Name`
- `Description`
- `Amount`

`Amount` 規則：

- 折扣必須以負數表示
- surcharge 若未來需要，才允許正數；本階段不使用

## `.Core` 實作要求

- `.Core` 不得再以靜態寫死規則方式計算折扣
- `.Core` 必須提供 concrete `DiscountEngine`
- `.Core` 必須提供 `CartContextFactory`
- `.Core` 不再保留 `DiscountEvaluationContext` / `DiscountConsumerSnapshot` / `IShopRuntimeContext`
- 既有「商品 ID = 1，第二件六折」需被改寫為第一個 built-in rule

## `.API` 實作要求

- `.API` 啟動時必須先解析 `ShopManifest`
- `.API` 必須以 `ShopManifest.DatabaseFilePath` 初始化資料庫
- `.API` 必須依 `ShopManifest.EnabledDiscountRuleIds` 組裝啟用的 `IDiscountRule`
- `CartsController` 與 `CheckoutController` 的 discount 計算必須改用 `CartContextFactory + DiscountEngine`

## 非目標

- 本階段不處理外部 DLL 動態探索
- 本階段不處理 ProductService plugin
- 本階段不做 reservation / booking domain model
