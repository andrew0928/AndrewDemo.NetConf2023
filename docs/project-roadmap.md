# Project Roadmap

## 目的

本文件是後續討論與執行的長期依據。重要架構結論應優先寫入本文件，再展開 spec、decision 或實作。

後續若 conversation context 過長，應以本文件作為重新載入脈絡的第一來源；chat 中已收斂的結論不應只停留在對話。

## Milestone 1 - 基礎建設

目標：建立可支援模組化商店的共用基礎。

範圍：

- Shop runtime 與 `ShopManifest`
- 單一部署單一 shop 的 runtime 模式
- Discount rule 模組化
- Product service 模組化
- Checkout orchestration 移入 `.Core`
- `TimeProvider` 與可測試時間基礎
- line-based cart 與基本 checkout correctness

完成基準：

- `.Abstract` 有可共用 contract
- `.Core` 不再由 API controller 主導主要交易流程
- API host 可依 manifest 組裝 product service、discount rule、order event dispatcher

狀態：完成。
最後記錄：2026-04-23。

### M1-P0 原始系統盤點

狀態：完成。
最後記錄：2026-03-24。

目標：以 reverse engineering 固定 phase 0 的系統基準，作為後續 Phase 1 / Phase 2 對照依據。

範圍：

- OAuth2-like login / token flow
- member profile 與 shop notes
- product list / cart add / discount estimate
- checkout / order history
- C4、testcase、review notes 反推文件

完成基準：

- `docs/2026-phase-0-*` 文件集完成
- phase 0 主要 testcase 與已知落差已記錄
- 文件不回填後續 Phase 1 / Phase 2 術語

### M1-P1 Shop Runtime / DiscountRule Phase 1

狀態：完成。
最後記錄：2026-03-23。

目標：建立 shop runtime 與 discount rule 的正式 contract，讓系統能依 shop manifest 組裝折扣能力。

範圍：

- `AndrewDemo.NetConf2023.Abstract`
- `ShopManifest`
- 單一部署單一 shop runtime
- `IDiscountRule`
- `DiscountRecord`
- `CartContext`
- `DiscountEngine`
- `/spec/shop-runtime-and-discount-rule.md`
- `/spec/testcases/shop-runtime-and-discount-rule.md`

完成基準：

- `.Abstract` 有 shop / cart / discount contract
- `.Core` discount engine 改由注入的 discount rules 執行
- API host 可依設定解析 shop manifest 與啟用折扣規則
- docs / decisions / spec / testcases 與 source 命名一致

### M1-P2 ProductService / Order Event Phase 1

狀態：完成。
最後記錄：2026-03-24。

目標：建立 product service 邊界，讓 published products 與 hidden products 都能透過同一個查詢 contract 接入 cart / checkout。

範圍：

- `IProductService`
- `Product`
- string `ProductId`
- `ProductServiceId`
- published / hidden product lookup
- order product lines / discount lines
- fulfillment status
- checkout 後 product side effect 的第一版 contract
- `/spec/product-service-and-order-events.md`
- `/spec/testcases/product-service-and-order-events.md`

完成基準：

- `.Abstract` 有 product service contract
- `ProductId` 已從 `int` 收斂為 `string`
- `.Core` 有 `DefaultProductService`
- cart estimate / checkout 不再直接依賴 `products` collection
- order 可分辨商品列、折扣列與 fulfillment 結果

### M1-P3 CheckoutService Phase 2

狀態：完成。
最後記錄：2026-03-24。

目標：將 checkout orchestration 從 API controller 搬進 `.Core`，並修正已知交易一致性與 buyer authorization 缺口。

範圍：

- `CheckoutService`
- checkout create / complete command 與 result model
- waiting room、transaction、order、discount、side effect orchestration
- transaction 刪除時序
- transaction buyer 與 access token member 驗證
- API controller HTTP mapping
- `/spec/checkout-service-phase2-migration.md`
- `/spec/checkout-correctness-fixes.md`

完成基準：

- `.Core` 成為 checkout orchestration owner
- API controller 只負責 auth、request / response mapping 與 HTTP status
- order 建立成功後才刪除 checkout transaction
- buyer mismatch 回傳 403
- `CheckoutServiceTests` 覆蓋主要 create / complete / failure 情境

### M1-P4 Cart / SKU / Inventory Core 回補

狀態：完成。
最後記錄：2026-04-01。

目標：補齊 AppleBTS 暴露出的通用 `.Core` 能力，讓 cart 能保存 line identity，並讓 checkout 成為 inventory correctness owner。

範圍：

- line-based cart aggregate
- `LineId`
- `ParentLineId`
- `AddedAt`
- `EvaluatedAt`
- nullable `Product.SkuId`
- `SkuRecord`
- `InventoryRecord`
- transactional inventory validation / deduct
- `/spec/core-cart-line-based-aggregate.md`
- `/spec/core-product-sku-and-inventory.md`

完成基準：

- cart 不再以 product quantity map 作為唯一資料型態
- discount 與 checkout 可辨識 cart line relation
- checkout 在同一個 transaction 內完成 inventory 檢查、扣減、order 建立與 checkout transaction 清理
- SKU / inventory 被定義為 `.Core` 標準能力，不屬於 AppleBTS extension

### M1-P5 可測試時間與通用語意回補

狀態：完成。
最後記錄：2026-04-05。

目標：補齊 storefront / AppleBTS 驗證期間暴露出的 `.Core` 基礎缺口，讓時間、checkout 語意與 cart line 操作可穩定驗證。

範圍：

- `TimeProvider`
- `ShiftedTimeProvider`
- time-shift host 設定
- production code 目前時間來源替換
- `BuyerSatisfaction` nullable semantics
- cart line removal
- child line cascade
- CommonStorefront / AppleBTS Storefront 對應 spec 回補

完成基準：

- `.Core` / API / AppleBTS API 使用注入的 `TimeProvider`
- 本機驗證環境可透過設定模擬 BTS 活動時間
- checkout 不再以 `0` 代表未提供的 buyer satisfaction
- cart 可用 `LineId` 刪除項目，刪除主 line 時會移除子 line

### M1-P6 OrderEventDispatcher 邊界修正

狀態：完成。
最後記錄：2026-04-23。

目標：將 checkout 後 side effect 從 product service callback 拆出，讓 `IProductService` 回歸商品查詢邊界。

範圍：

- `IOrderEventDispatcher`
- `OrderCompletedEvent`
- `OrderCancelledEvent`
- `DefaultOrderEventDispatcher`
- `OrderEventDispatcherId`
- 移除 `IProductService.HandleOrderCompleted`
- 移除 `IProductService.HandleOrderCancelled`
- 同步更新 product / checkout spec 與 decisions

完成基準：

- checkout side effect 由 `IOrderEventDispatcher` 承接
- `IProductService` 只保留 `GetPublishedProducts` / `GetProductById`
- `ShopManifest` 可指定 order event dispatcher
- 後續 PetShop reservation confirmed transition 可接入 order event dispatcher

## Milestone 2 - 標準系統建立

目標：建立標準商店 baseline，讓後續 vertical extension 可以重用。

範圍：

- Default product service
- 標準 product list / cart / checkout / member order flow
- Common storefront baseline
- Storefront shared BFF / auth / session / UI grammar
- Docker / local validation topology

完成基準：

- Common storefront 可作為 AppleBTS / PetShop 的骨架
- 標準 API 與 storefront flow 可獨立運作
- 後續 vertical extension 只需補自己的 domain、API 與 UI orchestration

狀態：完成。
最後記錄：2026-04-05。

### M2-P1 Storefront Family / CommonStorefront Phase 1

狀態：完成。
最後記錄：2026-04-04。

目標：建立 storefront family 的共同 BFF / UI / auth baseline，讓 CommonStorefront、AppleBTS Storefront 與 PetShop Storefront 可沿用同一套 grammar。

範圍：

- server-side BFF 架構
- auth / session flow
- `Storefront.Shared`
- `CommonStorefront` page routes
- GOV.UK 類型 UI grammar
- agent-browser 驗收原則
- `/spec/storefront-family-ui-and-bff.md`
- `/spec/common-storefront-baseline.md`
- `/spec/testcases/common-storefront-baseline.md`

完成基準：

- CommonStorefront 的 route、auth boundary、typed client 與 page model 邊界已定義
- storefront 完成條件不只看 build / unit test，而是依 testcase 做 browser 驗收
- vertical storefront 明確以 CommonStorefront 作為骨架，不自行發明另一套 UI / BFF 架構

### M2-P2 CommonStorefront 實作與本機驗證拓樸

狀態：完成。
最後記錄：2026-04-05。

目標：實作標準商店 storefront baseline，並建立可驗證 product / cart / checkout / member order flow 的本機拓樸。

範圍：

- `AndrewDemo.NetConf2023.Storefront.Shared`
- `AndrewDemo.NetConf2023.CommonStorefront`
- product list / product detail
- cart / checkout
- member profile / member orders
- server-side token exchange
- anonymous products API
- nginx 同源 reverse proxy
- CommonStorefront compose / deployment docs

完成基準：

- CommonStorefront 可獨立完成標準 storefront flow
- browser 不直接持 token 呼叫 backend API
- `/api/products*` 支援匿名瀏覽需求
- 本機 compose topology 可作為後續 AppleBTS / PetShop storefront 驗證參考

## Milestone 3 - AppleBTS 擴充設計

目標：以 Apple BTS 驗證 extension、campaign、sidecar data 與 storefront vertical 的擴充能力。

範圍：

- AppleBTS extension project
- BTS campaign / offer / qualification sidecar records
- BTS discount rule
- AppleBTS API 與 Storefront
- AppleBTS seed / database init / local deployment

完成基準：

- AppleBTS 不需要取代 `.Core` checkout 主流程
- AppleBTS 以 sidecar data 與 discount rule 擴充標準系統
- AppleBTS storefront 沿用 storefront family 架構

狀態：完成。
最後記錄：2026-04-09。

### M3-P1 Campaign 技術邊界與 `.Core` 回補方向

狀態：完成。
最後記錄：2026-04-01。

目標：確認 AppleBTS 的 business / technical boundary，並分辨哪些需求屬於 AppleBTS extension，哪些其實是 `.Core` 應補齊的標準能力。

範圍：

- BTS 是同一個 shop 內的限期 campaign
- 對外 `Product.Id` 對應 `SalePageId`
- `Product.Price` 維持一般售價
- BTS price 以 discount rule 表達價差
- qualification / offer / campaign sidecar records
- line-based cart 回補方向
- SKU / inventory 回補方向
- `DiscountRecord.Kind` 與 `RelatedLineIds`

完成基準：

- AppleBTS 不被建模為另一個 `ShopId`
- AppleBTS extension 只擁有 campaign / offer / qualification sidecar 與 BTS discount rule
- line-based cart、SKU / inventory、discount hint 等通用缺口回歸 M1 `.Core` 主線處理

### M3-P2 AppleBTS Phase 1 Spec / Skeleton

狀態：完成。
最後記錄：2026-04-02。

目標：凍結 AppleBTS campaign 的 Phase 1 規格、testcase 與最小 extension skeleton。

範圍：

- `AndrewDemo.NetConf2023.AppleBTS.Extension`
- `BtsCampaignRecord`
- `BtsMainOfferRecord`
- `BtsGiftOptionRecord`
- `MemberEducationVerificationRecord`
- `BtsDiscountRule`
- catalog / qualification service skeleton
- gift subsidy semantics
- M/G/P/C scenario decision table
- `/spec/bts-campaign-salepage-projection.md`
- `/spec/testcases/bts-campaign-salepage-projection.md`

完成基準：

- AppleBTS spec / testcases 已可作為後續實作基準
- `MaxGiftSubsidyAmount` 語意定案為 gift 補貼上限，不可轉移到主商品
- 不再追蹤 BTS cart provenance，gift relation 改由 `ParentLineId` 表達
- extension skeleton 與測試骨架可 build / test

### M3-P3 AppleBTS API / Seed / Local Topology

狀態：完成。
最後記錄：2026-04-04。

目標：讓 AppleBTS extension 可以透過專屬 API、seed 與本機 compose topology 被實際啟動與驗證。

範圍：

- `AndrewDemo.NetConf2023.AppleBTS.API`
- `AndrewDemo.NetConf2023.AppleBTS.DatabaseInit`
- `/bts-api/catalog`
- `/bts-api/qualification/*`
- AppleBTS seed data
- AppleBTS appsettings / module registration
- applebts compose
- local `.http`
- decision table 驗證腳本
- time mock / TimeProvider 遷移規劃

完成基準：

- 標準 API 可載入 AppleBTS 模組
- AppleBTS API 可查詢 catalog 與處理教育資格驗證
- DatabaseInit 可重建 AppleBTS 專屬 shop database
- 本機 compose 可啟動標準 API、BTS API 與 seed
- parent line gift flow 可透過標準 cart API 承接

### M3-P4 AppleBTS Storefront Phase 1

狀態：完成。
最後記錄：2026-04-05。

目標：以 CommonStorefront 為骨架建立 AppleBTS Storefront，驗證 storefront vertical 可以只補自己的頁面與 orchestration。

範圍：

- `AndrewDemo.NetConf2023.AppleBTS.Storefront`
- `/bts`
- `/bts/qualification`
- `/bts/products/{id}`
- BTS catalog display
- education qualification UI
- main product + gift server-rendered confirm flow
- cart line removal
- member / orders BTS qualification 與 discount 顯示
- AppleBTS storefront compose / nginx topology
- storefront smoke script

完成基準：

- AppleBTS Storefront 沿用 CommonStorefront 的 auth / session / BFF / UI grammar
- browser 不直接呼叫 `/api` 或 `/bts-api`
- gift 加入流程可先加入主商品，再以 `ParentLineId` 加入 gift
- CommonStorefront 與 AppleBTS Storefront 共用 cart delete / order discount display 能力

### M3-P5 折扣拆分與部署文件收斂

狀態：完成。
最後記錄：2026-04-09。

目標：修正 AppleBTS discount output 的業務表達，並補齊部署拓樸說明。

範圍：

- `BtsDiscountRule`
- 主商品價差 discount line
- gift subsidy discount line
- AppleBTS extension scenario tests
- integration tests
- storefront smoke script
- AppleBTS storefront spec / testcase
- AppleBTS docker compose / production deployment C4 文件

完成基準：

- 主商品價差輸出為 `BTS 主商品優惠`
- 贈品補貼輸出為 `BTS 贈品優惠`
- hint 維持 BTS 優惠語意但不混同折扣金額
- extension tests 與 solution build 通過
- AppleBTS compose / production environment 說明可與 CommonStorefront topology 對照

## Milestone 4 - PetShop 擴充設計

目標：支援寵物美容預約服務，並讓預約結果透過 hidden standard `Product` 接入既有 cart / checkout / order event 流程。

### 擴充專案命名規範

後續 vertical extension 專案命名比照 AppleBTS，使用 `{NAME}` 作為 business vertical 名稱。

- 擴充：`AndrewDemo.NetConf2023.{NAME}.Extension`，project type 為 class library。
- 服務：`AndrewDemo.NetConf2023.{NAME}.API`，project type 為 ASP.NET Core Web API。
- 網站：`AndrewDemo.NetConf2023.{NAME}.Storefront`，project type 為 ASP.NET Core website。
- 資料：`AndrewDemo.NetConf2023.{NAME}.DatabaseInit`，project type 為 console app。

其餘能力優先沿用共用服務或套件，例如 `.Abstract`、`.Core`、`Storefront.Shared`、標準 `.API` 與既有 database/runtime infrastructure。

### M4-P1A Reservation / Hidden Product Projection 核心模型

狀態：完成。
最後記錄：2026-04-23。

本階段只檢視兩個核心模型：

- `reservation`
- hidden standard `Product` projection

本階段明確不處理：

- 預約服務折扣
- Storefront UI
- durable notification channel / outbox
- durable retry / outbox
- scheduler / background expiration worker
- checkout 後取消交易
- 多 handler event bus

核心結論：

- `reservation` 是 PetShop 預約子系統的主要 aggregate，保存預約時間、場地、服務人員、預約人與狀態。
- 原本設想的 `dynamic-product` 收斂成標準 `Product(IsPublished=false)` record；它是 checkout bridge，不是 PetShop 自有 entity。
- `reservation.ProductId` 指向 hidden `Product`，讓預約結果可以加入 cart 並與一般商品一起 checkout。
- `.Core` 不理解 PetShop reservation schema，只透過 `IProductService.GetProductById(productId)` 取得商品快照。
- hidden reservation product 不應公開瀏覽或搜尋，只應由 PetShop reservation flow 回傳給預約本人。
- hidden product 不保存獨立 lifecycle status；可不可 checkout 完全由 reservation status 與 `HoldExpiresAt` 推導。
- checkout 成功後，PetShop 透過 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)` 將 reservation 從 checkout 前狀態推進到已預約狀態。

#### `reservation` 模型檢視

`reservation` 是預約子系統的事實來源，不是 product 的附屬資料。

建議 canonical state：

- `Holding`: 顯示為「預約確認中」。建立預約成功後進入此狀態，會暫時阻擋相同時間、場地、服務人員被其他人預約。
- `Confirmed`: 顯示為「已預約」。checkout 成功且 order event dispatcher 成功處理後進入此狀態。
- `Expired`: `Holding` 超過保留時間且未完成 checkout。
- `Cancelled`: checkout 前 hold 已取消。checkout 後取消交易與 confirmed reservation 取消 / 改期 API 屬於後續延伸。

必要欄位方向：

- `ReservationId`: reservation 自身識別碼，不使用 order id 或 product id 當主鍵。
- `BuyerMemberId`: 預約人。
- `ServiceId`: 美容服務。
- `StartAt` / `EndAt`: 預約時間，內部以 UTC 儲存。
- `VenueId`: 場地。
- `StaffId`: 服務人員。
- `Status`: reservation lifecycle state。
- `HoldExpiresAt`: `Holding` 狀態的保留期限，預設建立後 30 分鐘。
- `ProductId`: 對應的 hidden standard `Product`。
- `ConfirmedOrderId`: checkout 成功後綁定的 order id。
- `CreatedAt` / `UpdatedAt`: audit 與除錯用。

一致性規則：

- 建立 `reservation` 與 hidden `Product` 必須在 PetShop extension 的同一個 transaction 中完成。
- 只有 `Holding` 與 `Confirmed` 會佔用預約資源；`Expired` / `Cancelled` 只保留歷史紀錄，不阻擋其他客戶 reserve 同樣資源。
- `Expired` / `Cancelled` 不回到 `Holding`；重新預約必須建立新的 reservation 與新的 hidden `Product`。
- `Holding` 中且尚未過期的 reservation 必須阻擋相同 `StartAt + EndAt + VenueId + StaffId` 被其他 reservation hold 或 confirm。
- `CreateHold` 是第一版唯一需要檢查跨 reservation slot 衝突的一致性操作，必須用 database transaction 包住「檢查 slot 未被佔用」與「建立 reservation / hidden Product」。
- `ConfirmFromOrder`、`ExpireHold`、`CancelHold` 不重新檢查 slot 衝突，但仍需以 transaction 保持 reservation 狀態一致。
- P1A 不把 `slot-lock` 定為第三個核心模型；slot lock 可先由 reservation 狀態與唯一索引/查詢規則表達。若後續發現 concurrency 或查詢成本不足，再把 slot lock 拆成 implementation detail。
- `Holding` 過期第一版採 lazy expiration：建立新 reservation、查詢 availability、解析 reservation product、處理 checkout event 時都要視 `HoldExpiresAt` 判斷是否已失效。
- `Confirmed` transition 必須 idempotent：同一 `OrderId` 重複 dispatch 不應造成重複通知或重複狀態變更。

已確認決策：

- `Holding` / `Confirmed` 佔位，其他狀態不佔位。
- `Cancelled` / `Expired` 不回 `Holding`。
- `CreateHold` 負責 slot conflict check，並以 database transaction 保護。
- 原 `dynamic-product` 收斂成標準 hidden `Product`，不再有獨立 status；soft delete 語意由 reservation terminal state 表達。
- checkout 成功後透過 `IOrderEventDispatcher` 回推 reservation confirmed。
- PetShop extension 內部採 concrete-first，不為 ID 產生與 repository 預先建立 interface；真正穩定的跨邊界擴充點仍是 `.Abstract` 的 `IProductService` 與 `IOrderEventDispatcher`。
- `CreateHold` 可保留 request DTO；`CancelHold` / `ConfirmFromOrder` 使用 method parameters，不採用 command pattern。

#### hidden `Product` projection 模型檢視

hidden `Product` 是 checkout bridge，不是 reservation 的主資料來源，也不是 PetShop 自有 aggregate。

必要欄位方向：

- `ProductId`: opaque、高 entropy、不可猜測的商品識別碼。
- `Name`: 結帳商品名稱快照。
- `Description`: 結帳商品描述快照。
- `Price`: 結帳價格快照。
- `IsPublished`: 固定為 `false`。

一致性規則：

- hidden `Product` 必須與 reservation 一對一，由 `reservation.ProductId` 維持 mapping。
- hidden `Product` 在 reservation `Holding` 成功後建立；reservation `Expired` / `Cancelled` / `Confirmed` 後，product record 保留但不可解析。
- `PetShopProductService.GetPublishedProducts()` 不回傳 hidden reservation product。
- `PetShopProductService.GetProductById(productId)` 只在 reservation `Holding` 且未過期時回傳 shared `Product` snapshot。
- 若 reservation 已過期、取消或已 checkout confirmed，`GetProductById(productId)` 應回傳 `null`，讓既有 cart / checkout product validation 擋下後續結帳。
- checkout 成功後，`PetShopOrderEventDispatcher` 依 `OrderCompletedEvent.Lines.ProductId` 反查 reservation，再標記 confirmed。
- 若 `PetShopOrderEventDispatcher` 失敗，既有 checkout 語意是不 rollback order；reservation 可能停留在 `Holding` 或後續過期，需由後續 retry / 補償設計處理。
- direct API caller 若已取得 product id，`.Core` 目前不會驗證 buyer 或 quantity；P1A 接受此限制，因為前置決策已把 buyer-aware product access 放在 PetShop API / storefront 控制。若後續要把這件事提升為核心安全保證，需另開 `.Core` cart line validation 決策。

#### P1A 程式邊界收斂

- `PetShopReservationService` 是 lifecycle application service。
- `PetShopReservationRepository` 是 concrete persistence boundary，負責 slot conflict check 與 reservation / hidden `Product` 成對寫入。
- 不建立 `IPetShopIdGenerator`；ID 產生先作為 PetShop service 內部實作細節。
- 不建立 `IPetShopReservationStore`；目前沒有第二種 storage implementation，不先為 repository 建立 interface。
- 不建立 `PetShopReservationCommands`；只有 `CreateHold` 因參數較多使用 request DTO。

#### P1A 暫不處理

- 預約搭配一般商品滿額折扣。
- 通知 template、通知 channel 與發送可靠度。
- checkout 後取消交易與 confirmed reservation 的取消 / 改期 API。
- Pet profile / pet medical notes 的完整模型。
- 服務人員排班與可用性規則的完整後台。
- reservation product quantity 的 Core-level enforcement。

完成基準：

- reservation 狀態機已固定為 `Holding` / `Confirmed` / `Expired` / `Cancelled`。
- checkout bridge 已收斂為 hidden standard `Product(IsPublished=false)`，不建立獨立 `dynamic-product` entity / status。
- `CreateHold`、`CancelHold`、lazy expiration、`OrderCompletedEvent` confirmation 與 duplicate order event 已有 executable tests。
- `AndrewDemo.NetConf2023.PetShop.Extension` skeleton 已加入 solution，並可透過 `DefaultProductService` 解析 hidden product 後套用 reservation policy。
- checkout confirmation 新成立時會輸出 PoC console log，代表通知服務人員與消費者；正式通知 channel / outbox 留到後續。

### M4-P1B PetShop Lifecycle / API Spec

目標：在 P1A 核心模型確認後，定義 reservation lifecycle、API contract 與 testcase。

狀態：完成。
最後記錄：2026-04-24。

預期產出：

- `docs/petshop-reservation-phase1-design.md`
- `spec/petshop-reservation.md`
- `spec/testcases/petshop-reservation.md`
- 必要 decision

API spec draft：

- `spec/petshop-reservation-api.md`
- `spec/testcases/petshop-reservation-api.md`

已接受的 API 結論：

- PetShop vertical API route prefix 使用 `/petshop-api`。
- 第一版 API 只處理 service catalog、availability、reservation hold、reservation list、reservation status、checkout 前取消 hold。
- Core cart / checkout API 不搬進 PetShop API；reservation hold 成功後只回傳 `checkoutProductId`，client 再呼叫 Core cart API 加入購物車。
- hidden reservation product id 只回傳給 reservation owner，且只在有效 `Holding` 狀態回傳。
- service name、price、duration 與 hold duration 由 server-side catalog / policy 決定，不由 client 傳入。
- availability 第一版只回傳可用 slot。
- cancel hold API 維持 `POST /petshop-api/reservations/{reservationId}/cancel-hold` 的 command semantics。
- confirmed reservation 取消 / 改期、checkout 後取消交易、staff/admin 排班維護與 durable notification 仍不納入第一版 API。

本階段不再處理 discount 與 UI；discount 已在 M4-P4 補齊，UI 留到 M4-P3。

### M4-P2A PetShop Extension Implementation

目標：實作 PetShop domain、repositories、product service decorator 與 order event dispatcher。

預期產出：

- `AndrewDemo.NetConf2023.PetShop.Extension`
- `PetShopProductService`
- `PetShopOrderEventDispatcher`
- reservation records 與 hidden standard product projection
- extension unit tests

### M4-P2B PetShop API

目標：提供 `/petshop-api/*`，讓 storefront 或測試流程可以建立 reservation 並取得 hidden product id。

狀態：完成。
最後記錄：2026-04-24。

預期範圍：

- 查詢可預約服務
- 建立 reservation hold
- 查詢可用 slot
- 查詢目前會員 reservations
- 取得 reservation 對應 hidden product id
- 查詢 reservation 狀態
- checkout 前取消 hold
- 必要的 buyer authorization

完成基準：

- `AndrewDemo.NetConf2023.PetShop.API` 已建立。
- `/petshop-api/services`、`/petshop-api/availability`、`/petshop-api/reservations/holds`、`/petshop-api/reservations`、`/petshop-api/reservations/{id}`、`/petshop-api/reservations/{id}/cancel-hold` 已實作。
- service catalog 與 availability 第一版採 configuration-backed concrete service。
- hidden product id 只在 owner 查詢有效 `holding` reservation 時回傳。
- `tests/AndrewDemo.NetConf2023.PetShop.API.Tests` 已覆蓋匿名/登入邊界、owner isolation、availability filtering、create hold 與 cancel-hold 核心情境。

### M4-P2C PetShop Host / Seed / Config

目標：讓 PetShop 可以用獨立 shop runtime 啟動並完成 API-level E2E。

狀態：完成。
最後記錄：2026-04-24。

預期範圍：

- PetShop appsettings
- database init / seed
- module registration
- local validation topology

完成基準：

- 標準 `.API` 已可透過 `appsettings.PetShop.json` 啟用 `petshop` module。
- 標準 `.API` 已可依 `ShopManifest` 解析 `PetShopProductService` 與 `PetShopOrderEventDispatcher`。
- `AndrewDemo.NetConf2023.PetShop.DatabaseInit` 已建立，負責 seed PetShop 一般商品、SKU 與 inventory；reservation / hidden product 仍由 runtime flow 動態建立。
- `compose/petshop.api-dev.compose.yaml` 已提供 PetShop API-level E2E 本機環境。
- `compose/petshop-local.http` 已提供 OAuth、reservation hold、cart、discount estimate、checkout complete 與 reservation confirmed 的手動驗證流程。
- `compose/petshop.site-dev.compose.yaml` 已提供 frontend + nginx reverse proxy 整合環境；第一版 storefront 先沿用 `CommonStorefront` baseline，PetShop reservation UI 留到 M4-P3。

### M4-P3 PetShop Storefront

目標：在已確認 API 與 domain flow 後，再實作 PetShop storefront。

狀態：完成。
最後記錄：2026-04-24。

預期範圍：

- `AndrewDemo.NetConf2023.PetShop.Storefront`
- reservation flow pages
- cart / checkout integration
- member reservation/order status

拆解原則：

- PetShop Storefront 需要同時驗證 reservation BFF、hidden product 加入 cart、checkout completed 後 reservation confirmed，以及 member reservation 狀態顯示；一次完成容易讓規格、UI 與 topology 混在同一個 review package。
- 因此 M4-P3 拆成 P3A / P3B / P3C；P3A 先固定 route / BFF contract / testcase 與最小 skeleton，P3B 實作核心預約購物流程，P3C 再做 member/order 整合與 browser smoke。

#### M4-P3A PetShop Storefront Spec / Skeleton

狀態：完成。
最後記錄：2026-04-24。

目標：固定 PetShop Storefront 第一版頁面、BFF client 邊界與驗收案例，建立可 build 的最小網站骨架。

預期範圍：

- `spec/petshop-storefront-baseline.md`
- `spec/testcases/petshop-storefront-baseline.md`
- `AndrewDemo.NetConf2023.PetShop.Storefront` skeleton
- `PetShopApiClient` typed client contract
- `PetShopApiOptions`
- PetShop Storefront appsettings / Dockerfile / solution registration

完成基準：

- PetShop Storefront 明確沿用 storefront family 的 server-side BFF 與 UI grammar。
- 第一版 route、auth boundary、reservation flow 與 non-goals 已固定。
- testcase 能覆蓋 service catalog、availability、create hold、add reservation product to cart、cancel hold、checkout confirmation 與 member reservation status。
- 專案可 build，且不改動 `.Abstract` / `.Core` contract。

完成內容：

- `spec/petshop-storefront-baseline.md` 已建立。
- `spec/testcases/petshop-storefront-baseline.md` 已建立。
- `AndrewDemo.NetConf2023.PetShop.Storefront` skeleton 已加入 solution。
- `PetShopApiClient`、PetShop API DTO 與 `PetShopApiOptions` 已建立。
- `/` 與 `/petshop` 最小頁面已建立，`/petshop` 可透過 BFF client 讀取 service catalog。

#### M4-P3B Reservation Flow Pages

狀態：完成。
最後記錄：2026-04-24。

目標：實作使用者建立美容預約、加入購物車、取消 checkout 前 hold 的主要頁面。

預期範圍：

- `/petshop`
- `/petshop/reservations/new`
- `/petshop/reservations`
- `/petshop/reservations/{id}`
- availability 查詢與 slot 選擇
- create hold 成功後顯示「預約確認中」
- holding reservation 加入購物車
- checkout 前 cancel hold

完成基準：

- 使用者可從服務目錄完成 reservation hold。
- holding reservation 可透過 server-side BFF 加入標準 cart。
- checkout 前可取消 hold，取消後不可再加入 cart。
- cart / checkout 仍使用標準 storefront 頁面與標準 `.API`。

完成內容：

- `/petshop` 顯示 PetShop 美容服務目錄，並提供建立預約與我的預約入口。
- `/petshop/reservations/new` 支援服務/日期查詢 availability、slot 選擇與登入後 create hold。
- `/petshop/reservations` 與 `/petshop/reservations/{id}` 顯示目前會員 reservation list / detail。
- holding reservation detail 可透過 server-side action 取得 owner-visible `checkoutProductId`，並加入標準 cart。
- holding reservation detail 可在 checkout 前執行 `cancel-hold`，取消後不再顯示加入購物車 action。
- PetShop Storefront 已補齊 `/products`、`/cart`、`/checkout`、`/member`、`/member/orders` 與 `/auth/*`，其中 cart / checkout / member 沿用 CommonStorefront 的標準頁面模式。
- `/member` 第一版提供 PetShop reservation 入口；更完整的 reservation/order browser smoke 與 compose 驗證留到 M4-P3C。

#### M4-P3C Member / Order Integration 與 Browser Smoke

狀態：完成。
最後記錄：2026-04-24。

目標：補齊會員 reservation 狀態、訂單折扣顯示與 PetShop storefront compose/browser 驗證。

預期範圍：

- `/member` reservation 摘要或入口
- `/member/orders` PetShop reservation order / discount 顯示確認
- `petshop.site-dev.compose.yaml` 切換為 `PetShop.Storefront`
- nginx edge 驗證
- browser smoke 驗收記錄

完成基準：

- checkout completed 後，reservation detail/list 可看到 `confirmed`。
- 含 reservation + 一般商品滿額的訂單可在 storefront 顯示折扣明細。
- PetShop storefront 可透過 `http://localhost:5238` 完成主要 browser flow。

完成內容：

- `compose/petshop.site-dev.compose.yaml` 已切換為啟動 `AndrewDemo.NetConf2023.PetShop.Storefront`。
- nginx edge 仍維持 `http://localhost:5238` 作為整合入口，`/api/*` 指向標準 `.API`，`/petshop-api/*` 指向 PetShop API，其他 route 指向 PetShop Storefront。
- 使用者已以 browser 驗證 PetShop storefront flow 可運作。
- M4-P3 第一版完成範圍維持在 consumer-facing reservation / cart / checkout / member flow；confirmed 後取消/改期、staff/admin UI 與 durable notification 留待未來需求。

### M4-P4 PetShop Discount / Promotion

目標：補上「同次結帳含預約，且一般商品滿額」折扣。

狀態：完成。
最後記錄：2026-04-23。

規則：

- 同次結帳至少有一筆有效 PetShop reservation line，且一般商品金額大於 1000 時折 100
- 有效 reservation line 只作為折扣資格條件
- 規則類別命名為 `PetShopReservationPurchaseThresholdDiscountRule`
- 有效 reservation line 必須對應狀態為 `Holding` 且未過期的 reservation
- 門檻只計算非 reservation 的一般商品 line
- 一般商品金額以 `CartContext.LineItems` 的 `UnitPrice * Quantity` 小計判定，不扣除其他 discount rule
- 第一版每次 cart evaluation 只輸出一筆 `-100` discount，不因 reservation 筆數增加，也不做每滿 1000 累加

完成基準：

- `PetShopReservationPurchaseThresholdDiscountRule` 已實作。
- `spec/testcases/petshop-reservation-purchase-threshold-discount.md` 已固定 testcase。
- extension tests 已覆蓋無預約、單筆預約、多筆預約、門檻邊界、只有預約與過期預約。
