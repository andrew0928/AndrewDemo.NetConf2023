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

## 重大決策標記規則

- 若決策會影響 `.Core` 的通用設計，必須在文件內明確寫出：
  - `重大決策`
  - `影響 .Core`
- 若決策代表需要回頭修正 Phase 1、frozen spec、或 `.Abstract` contract，必須在文件內明確寫出：
  - `回頭修正 Phase 1`
  - `影響 .Abstract / spec`
- 若決策同時符合上述兩者，兩種標記都要出現
- AppleBTS 過程中發現的這類通用修正，除了 decision 本文外，還必須同步登錄到：
  - [AppleBTS 過程中的 Phase 1 / `.Core` 回頭修正追蹤](/Users/andrew/code-work/andrewshop.apidemo/docs/apple-bts-phase1-backtracking-tracker.md)
- AppleBTS 擴充結案時，應以追蹤文件中的累計數量與清單，摘要回報「過程中回頭修正了哪些通用設計」

## 決策索引

| 日期 | 決策 |
|---|---|
| [2026-03-23](2026-03-23-discount-domain-boundary.md) | **Discount Domain 與 Cart Domain 邊界劃分**。原本折扣 contract 同時包含購物車輸入格式、消費者投影與 shop runtime 感知，職責過重。決策後 discount domain 只保留 `IDiscountRule` 與 `DiscountRecord`，購物車與消費者相關模型改歸 cart domain，discount engine 不再直接依賴 runtime 與 database。 |
| [2026-03-23](2026-03-23-product-service-boundary-and-order-events.md) | **ProductService 邊界與訂單事件設計**。原本商品流程是靜態資料表導向，controller 直接讀取 Products collection，OrderLineItem 也無法識別實際商品。決策後每個 shop 啟用單一 `IProductService`，product contract 採固定欄位且 `ProductId` 改為 string；訂單 side effect 後續收斂為 `IOrderEventDispatcher`。 |
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
| [2026-04-02](2026-04-02-apple-bts-extension-project-boundary.md) | **AppleBTS Extension 採最小骨架與 sidecar-only 設計**。保留 `BtsDiscountRule`、catalog query、admin façade 與 qualification service；不再追蹤 BTS cart provenance，也不建立專屬 product service。 |
| [2026-04-02](2026-04-02-apple-bts-subsidy-semantics-and-decision-table.md) | **Apple BTS gift subsidy 語意與完整情境決策表**。`MaxGiftSubsidyAmount` 是 gift 補貼上限，只作用在 gift line，未使用額度不得轉移到主商品；並正式凍結 M/G/P/C 四類情境表。 |
| [2026-04-02](2026-04-02-apple-bts-local-environment-and-host-topology.md) | **AppleBTS 本機測試環境與 Host 拓樸**。以獨立 compose 啟動 `applebts-seed`、`applebts-api`、`applebts-btsapi` 三個容器，共用單一 AppleBTS 專屬資料庫；標準 cart / checkout 走既有 `.API`，BTS 專屬入口走 `/bts-api/*`。 |
| [2026-04-02](2026-04-02-timeprovider-based-time-shift-and-mock.md) | **TimeProvider 化的時間平移與 Time Mock 遷移方向**。後續時間抽象採 `TimeProvider`，runtime 以自訂 `ShiftedTimeProvider` 承接 `appsettings` 設定的期待啟動時間與固定 offset；此案已明確標記為 AppleBTS 過程中回頭修正的 Phase 1 / `.Core` 基礎決策。 |
| [2026-04-04](2026-04-04-storefront-family-and-bff-architecture.md) | **Storefront Family 與 BFF 架構**。Common、AppleBTS、PetShop 三套 storefront 採獨立網站 + ASP.NET Core server-side BFF；第一版沿用 `/api/login` 作為 OAuth authority，browser 不直接持有 token 呼叫 backend API。 |
| [2026-04-04](2026-04-04-storefront-ui-guidelines-govuk-style.md) | **Storefront UI 指南採 GOV.UK 類型風格**。storefront UI 以任務導向極簡風格為準，目標是簡潔、可讀、無障礙與 mobile-first；第一版不採品牌敘事型設計，也不引入 SPA framework 作為必要基礎。 |
| [2026-04-04](2026-04-04-common-storefront-phase1-boundary.md) | **CommonStorefront Phase 1 邊界**。以 `CommonStorefront` 作為 storefront family 的 baseline implementation，固定 page routes、auth/session、`CoreApiClient` 與 `Storefront.Shared` 的最小邊界，並作為後續 storefront vertical 的骨架基準。 |
| [2026-04-04](2026-04-04-common-storefront-anonymous-products-and-nginx-validation-topology.md) | **CommonStorefront 匿名商品 API 與 nginx 驗證拓樸**。為了符合 storefront spec，`.API` 必須允許匿名 `GET/HEAD /api/products*`；本機驗證則採 nginx 同源 reverse proxy，整合 `/*` 與 `/api/*`，並保留原始 `Host:port` 以支援 OAuth callback。 |
| [2026-04-05](2026-04-05-buyer-satisfaction-nullable-semantics.md) | **BuyerSatisfaction 改為 Nullable 語意**。`null` 代表沒有判讀或沒有提供，`0` 僅代表明確的最低分；此案屬於 `.Core` 的通用語意修正，而不是 storefront 專屬行為。 |
| [2026-04-05](2026-04-05-cart-line-removal-and-child-cascade.md) | **Cart Line 刪除與子項目 Cascade**。line-based cart 必須補齊以 `LineId` 為單位的刪除能力；刪除主商品 line 時，所有以 `ParentLineId` 綁定的子 line 必須一併移除。此案屬於 AppleBTS 過程中重新暴露的 `.Core / Phase 1` 通用缺口。 |
| [2026-04-05](2026-04-05-apple-bts-storefront-phase1-boundary.md) | **AppleBTS Storefront Phase 1 邊界**。沿用 `CommonStorefront` 的 grammar 與 BFF 基礎，追加 `/bts`、`/bts/qualification`、`/bts/products/{id}`，並以 server-rendered 確認步驟處理主商品 + gift 的加入流程。 |
| [2026-04-09](2026-04-09-bts-split-main-and-gift-discount-lines.md) | **AppleBTS 折扣需拆成主商品與贈品兩筆折扣行**。`BtsDiscountRule` 若同時存在主商品價差與 gift subsidy，必須分別輸出 `BTS 主商品優惠` 與 `BTS 贈品優惠`；此案屬於 AppleBTS extension 表達修正，不影響 `.Core` contract。 |
| [2026-04-19](2026-04-19-order-event-dispatcher-replaces-product-callback.md) | **Order Event Dispatcher 取代 IProductService 訂單 callback**。`IProductService` 回歸商品查詢邊界，只保留 `GetPublishedProducts` / `GetProductById`；checkout 後的 side effect 改由 `IOrderEventDispatcher` 與 `OrderCompletedEvent` / `OrderCancelledEvent` 承接，`ShopManifest` 新增 `OrderEventDispatcherId`；不採用 C# event subscription 作為正式 `.Abstract` 擴充 contract。 |
| [2026-04-23](2026-04-23-petshop-reservation-p1a-scope.md) | **PetShop Reservation P1A 範圍界定**。M4-P1A 只處理 reservation / hidden standard `Product` projection 的 `CreateHold`、checkout 前 `CancelHold`、`ExpireHold` 與 `OrderCompletedEvent` confirmation；checkout 後取消交易與 confirmed reservation 取消 / 改期列為未來延伸，現階段 `OrderCancelledEvent` 不改 PetShop 狀態。 |
| [2026-04-23](2026-04-23-petshop-p1a-concrete-first-boundary.md) | **PetShop P1A 採 Concrete-First 內部邊界**。移除 `IPetShopIdGenerator`、`IPetShopReservationStore` 與 reservation command pattern；保留 concrete `PetShopReservationRepository` 作為 transaction / persistence boundary，`CreateHold` 使用 request DTO，`CancelHold` / `ConfirmFromOrder` 使用 method parameters。 |
| [2026-04-23](2026-04-23-petshop-reservation-uses-hidden-standard-product.md) | **PetShop Reservation 採 Hidden Standard Product Projection**。不建立獨立 `dynamic-product` entity / status；reservation hold 成功時建立標準 `Product(IsPublished=false)`，`PetShopProductService` 以 decorator 套用 reservation lifecycle policy，reservation 維持唯一 lifecycle source of truth。 |
