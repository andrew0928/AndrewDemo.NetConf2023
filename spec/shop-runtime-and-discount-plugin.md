# Shop Runtime 與 Discount Plugin 規格

## 狀態

- phase: 1
- status: draft-for-freeze
- 日期：2026-03-23

## 範圍

本規格只涵蓋以下主題：

1. 啟動時以 `shop-id` 選定商店組態
2. discount plugin contract 與執行邊界
3. `.API` 如何載入商店 runtime
4. `.Core` 如何依 contract 執行 discount rules

本規格暫不涵蓋：

- ProductService plugin
- 預約服務商品
- runtime hot reload
- 同 instance 多商店同時服務

## 目標

- 同一套 code base 可交付多個商店
- 單一部署只服務單一商店
- 啟動時可依 `SHOP_ID` 或 `--shop-id` 載入對應商店設定
- discount rules 改為插件邊界，不再把規則寫死在 `.Core`

## Canonical 術語

- `ShopManifest`: 單一商店部署所需的啟動組態
- `ShopRuntimeContext`: 啟動後已選定的商店 runtime context
- `DiscountRulePlugin`: 一條可獨立啟用或停用的 discount rule
- `DiscountEvaluationContext`: discount engine 執行時使用的輸入資料
- `DiscountApplication`: 單次 discount 計算的輸出結果

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
- 單一 process 啟動後只會有一個 `ShopRuntimeContext`

## Discount Plugin Contract 規格

### IDiscountRulePlugin

- `RuleId`: 規則唯一識別碼
- `Priority`: 規則優先順序，數字越小越先執行
- `Evaluate(DiscountEvaluationContext)`: 回傳本規則產生的 `DiscountApplication`

### IDiscountEngine

- 接收 `DiscountEvaluationContext`
- 只執行 `ShopManifest.EnabledDiscountRuleIds` 中列出的規則
- 依 `Priority`、再依 `RuleId` 排序執行
- 若 manifest 內列出的 rule id 沒有對應實作，直接忽略，不視為啟動失敗

### DiscountEvaluationContext

- `ShopId`
- `Consumer`
- `CartLines`

### DiscountCartLine

- `ProductId`
- `ProductName`
- `UnitPrice`
- `Quantity`

### DiscountApplication

- `RuleId`
- `Name`
- `Description`
- `Amount`

`Amount` 規則：

- 折扣必須以負數表示
- surcharge 若未來需要，才允許正數；本階段不使用

## `.Core` 實作要求

- `.Core` 不得再以靜態寫死規則方式計算折扣
- `.Core` 必須提供可注入 `IDiscountRulePlugin` 的 engine 實作
- `.Core` 需保留既有購物車與訂單流程可運作
- 既有「商品 ID = 1，第二件六折」需被改寫為第一個 built-in plugin

## `.API` 實作要求

- `.API` 啟動時必須建立 `ShopRuntimeContext`
- `.API` 必須以 `ShopRuntimeContext.Manifest.DatabaseFilePath` 初始化資料庫
- `CartsController` 與 `CheckoutController` 的 discount 計算必須改用 `IDiscountEngine`

## 非目標

- 本階段不處理外部 DLL 動態探索
- 本階段不處理 ProductService plugin
- 本階段不做 reservation / booking domain model
