# 決策文件規範

本目錄用來記錄已確認或準備採納的重要架構與設計決策，避免討論只留在 chat 中。

## 目的

- 記錄為什麼要做這個決策
- 記錄有哪些替代方案被評估過
- 記錄決策會影響哪些模組、文件與測試
- 作為後續實作、spec 與 demo 同步更新的依據

## 檔名格式

- `YYYY-MM-DD-主題.md`

範例：

- `2026-03-23-shop-runtime-and-plugin-architecture.md`

## 建議結構

每份文件至少包含以下段落：

1. `狀態`
2. `背景`
3. `決策`
4. `影響`
5. `替代方案`
6. `後續工作`

## 狀態定義

- `proposed`: 已提出，待確認或待實作
- `accepted`: 已確認採用，後續實作應以此為準
- `superseded`: 已被更新決策取代，需註明取代者

## 維護原則

- 若 canonical 路徑、欄位名、術語改變，應同步更新 source code、docs、demo、spec
- 若決策明確不追求相容，文件內要直接寫清楚，不保留長期雙軌命名
- 若新需求與主線架構關聯較弱，先在文件中說明為何值得插隊

## 決策索引

| 日期 | 決策 |
|---|---|
| [2026-03-23](2026-03-23-discount-domain-boundary.md) | **Discount Domain 與 Cart Domain 邊界劃分**。原本折扣 contract 同時包含購物車輸入格式、消費者投影與 shop runtime 感知，職責過重。決策後 discount domain 只保留 `IDiscountRule` 與 `DiscountRecord`，購物車與消費者相關模型改歸 cart domain，discount engine 不再直接依賴 runtime 與 database。 |
| [2026-03-23](2026-03-23-product-service-boundary-and-order-events.md) | **ProductService 邊界與訂單事件設計**。原本商品流程是靜態資料表導向，controller 直接讀取 Products collection，OrderLineItem 也無法識別實際商品。決策後每個 shop 啟用單一 `IProductService`，product contract 採固定欄位且 `ProductId` 改為 string，結帳完成後透過 `OrderEventNotification` 通知 ProductService 觸發 fulfillment。 |
| [2026-03-23](2026-03-23-shop-runtime-and-plugin-architecture.md) | **商店啟動組態與插件化架構**（proposed）。原本系統只有單一商店、寫死的折扣邏輯與固定商品模型。決策採用「單一部署單一商店」模式，啟動時依 `SHOP_ID` 載入 `ShopManifest` 與對應模組；折扣引擎改為 instance-based 注入式服務，商品擴充拆為「商品定義」與「購買項目」兩層模型。 |
| [2026-03-24](2026-03-24-checkout-consistency-and-buyer-validation.md) | **Checkout 交易一致性與 Buyer 驗證修正**。原本 checkout transaction 在建立 order 前就被刪除，且未驗證 access token 對應的 member 是否為 transaction buyer。決策後 transaction 必須在 order 建立成功後才刪除，buyer 不符時回傳 403，fulfillment 失敗不推翻已成立的 order。 |
| [2026-03-24](2026-03-24-checkout-service-phase2-migration.md) | **CheckoutService Phase 2 搬移策略**。原本 checkout 的交易編排邏輯全部寫在 API controller，`.Core` 只有零散 helper。決策將 checkout orchestration 搬進 `.Core` 的 `CheckoutService`，controller 只保留 request/response mapping 與 HTTP status 對應，本階段只做搬移不修正既有缺失。 |
| [2026-03-25](2026-03-25-spec-first-phase-workflow-skill.md) | **Spec-First Phase Workflow skill 化**。原本合作流程的規則分散在 decisions、AGENTS.md 與各 phase 文件中，每次需口述重建。決策將這套 spec-first、phase-gated 開發流程整理為可重複使用的 skill，明確區分 Phase 1（規格確認）、Phase 2（依 frozen spec 重構）、version analysis 三種工作模式。 |
| [2026-04-01](2026-04-01-shop-runtime-tenant-isolation-mode.md) | **Shop Runtime 維持 Tenant Isolation Mode**。目前正式基準為：每個 `ShopId` 對應一份 `ShopManifest` 與一個專屬 `DatabaseFilePath`；host 啟動後連向單一 shop-local database，現行結構不是 row-level `ShopId` 的 tenant share mode。 |
| [2026-04-01](2026-04-01-line-based-cart-is-core-refactor.md) | **Line-based Cart 屬於 `.Core` 主線重構**。`Cart` 由 `ProdQtyMap` 改為 line-based aggregate，不再被視為 Apple BTS 專案專用修正；`.Abstract.Carts` 需補齊 `EvaluatedAt`、`LineId`、`ParentLineId`、`AddedAt`。 |
| [2026-04-01](2026-04-01-bts-product-id-maps-to-salepage-id.md) | **BTS 商業模型中，對外 `Product.Id` 對應 `SalePageId`**。BTS 被重新定義為同一個 shop 內的限期 campaign；對外 `Product` 代表 `SalePage projection`，而 `SKU` 與庫存屬於內部資料模型，不透過 `.Abstract` 直接公開。 |
| [2026-04-01](2026-04-01-sku-and-inventory-are-core-standard-capabilities.md) | **SKU 與 Inventory 屬於 `.Core` 的標準能力**。`Product` 在內部可關聯 nullable `SkuId`，checkout 標準流程必須驗證庫存，並以同一個資料庫 transaction 完成 inventory 扣減、order 建立與 checkout transaction 刪除。 |
| [2026-04-01](2026-04-01-bts-campaign-technical-boundary.md) | **Apple BTS Campaign 技術邊界與 Cart/Projection 重構方向**（superseded）。原提案已被後續的單一 SalePage 與 `DiscountRecord` 擴充決策取代。 |
| [2026-04-01](2026-04-01-discount-record-kind-and-related-lines.md) | **DiscountRecord 擴充為 discount/hint 單一型別**。保留 `IDiscountRule` 與 `DiscountEngine` 的回傳型別，改由 `DiscountRecord.Kind` 與 `RelatedLineIds` 同時表達有效折扣、提示訊息與其對應的 cart lines。 |
| [2026-04-01](2026-04-01-bts-single-salepage-and-price-delta-discount.md) | **Apple BTS 採單一 SalePage 與價差型折扣**。`Product.Price` 維持一般售價，`bts-price` 由 sidecar 定義，折扣與活動失效提示改由 `BtsDiscountRule` 輸出，不先引入 checkout blocking hook。 |
