# Project Roadmap

## 目的

本文件是後續討論與執行的長期依據。重要架構結論應優先寫入本文件，再展開 spec、decision 或實作。

後續若 conversation context 過長，應以本文件作為重新載入脈絡的第一來源；chat 中已收斂的結論不應只停留在對話。

## TiP Phase 1.1：管理查核追加規格

2026-10-18 使用者指定追加唯讀 admin API，用來驗證總訂單金額、訂單、結帳與會員清單；admin 使用獨立、預先設定的 `admin_` token，採 Bearer 與 SHOP_ADMIN_ACCESS_TOKEN，未設定即停用。沒有 Web UI，CLI 由使用者後續追加。本輪規格已定案，實作留待後續，正式入口為 [Phase 1.1](../spec/tip-phase1.1.md)，依據見 [決策](decisions/2026-10-18-tip-admin-query.md)。不取代既有 Phase 2.1～2.7，亦不將未交付階段視為完成。

2026-10-18 使用者後續授權執行 Phase 1.1，管理查核 API 已完成實作、262 項 solution 回歸及真實 host 隔離驗證。CLI 由使用者後續追加，無 Web UI；Docker daemon 恢復後已成功重建 Common API 映像；使用者依 Compose 啟動與 curl 指令親自驗證正式訂單查詢通過，完整容器案例未重跑。見 [交付紀錄](tip/phase1.1-delivery.md)。

## TiP：Phase 1 規格與合約已定案

最後記錄：2026-10-17。

2026-10-19 使用者確認本專案為 PoC，拆 collection 已足以代表本次研究所需的隔離性；本輪僅評估同一 shop-local LiteDB 內拆 collection 的變更範圍，不展開 database 分離，不修改 code、spec 或 Abstract。此為評估範圍確認，尚未定案拆分對象、命名或實作方案，也未重新開啟 Phase 1。見 [評估範圍決策](decisions/2026-10-19-tip-collection-isolation-assessment-scope.md)。

同日最終決定：使用者接受以「交易」作為 live／test 的嚴格邊界；用途在交易建立時判定，此後執行、storage、封存、衍生訂單與事件均維持原用途。Member／Token 與非交易資料共用，不以 Token.IsTest 限定可存取的交易分區；既有身分驗證、物件歸屬與權限檢查仍適用，特別點名的操作嚴格禁止 test identity 執行。正式會員使用測試 PaymentId 的流程保留，未來保留實體分庫方向。先前 token 嚴格分區方案不採用；本次只更新設計決策，未修改 code、spec 或 Abstract。見 [身分、儲存與存取邊界](decisions/2026-10-19-tip-identity-storage-and-access-boundaries.md)。

同日補充確認：test identity 變更商品價格必須明確拒絕；test identity 可執行交易，但 test transaction 不得產生任何實際業務效果，test 交易紀錄與 log 仍可保存。此新決策取代 AC-TIP-005 的「驗證後略過改價」政策。使用者後續要求確認需求規格並追加 Phase 3：spec／testcases 已同步，具體命名、拒絕回應及重構切分已由使用者確認並要求提交文件，code 與 Abstract 未改。SKU 維持既有政策，不擴張禁止清單。見 [Phase 3 review](tip/phase3-spec-review.md)、[執行計畫](../spec/tip-phase3-plan.md) 與 [追加決策](decisions/2026-10-19-tip-phase3-spec-and-refactor-plan.md)。

以 AndrewShop PoC / prototype 研究 Testing in Production（TiP），驗證共用交易流程與各業務模組能否一致區分測試行為及其商業效果。此方向延續模組化、DiscountRule 與 ProductService 的主線，標準實作已完成 [Phase 1 定案基準](../spec/tip-phase1.md)，Phase 2 接入 Core 與 host。

已確認的大前提：**服務部署在正式環境，測試行為不應產生真實商業效果。**

已確認 `.Core` 管理的 8 種持久化領域物件範圍，2026-10-17 縮小直接標記對象為 `Order`、`Member`、`CheckoutTransactionRecord`，建立時確定正式／測試用途且不可變更；Token.IsTest 建立時繼承 Member.IsTest 並保存，Cart 不承接會員且不標記用途；`InventoryRecord`、`Product`、`SkuRecord` 受保護，測試行為不得異動，因測試而略過的操作必須留下 log。PetShop 的預約及行事曆／時段占用比照受保護規則。

原則、分類與名詞解釋見 [TiP 設計原則](tip/tip-guidelines.md)，確認紀錄見 [TiP 大前提決策](decisions/2026-10-16-tip-no-real-business-effects.md)及[領域物件分類決策](decisions/2026-10-16-tip-domain-object-classification.md)。[AC-TIP-001](tip/tip-acceptance-criteria.md#ac-tip-001) 已確認：測試會員可完成一般商品結帳；略過扣庫存、扣款、出貨必須留 log。查詢型報表排除測試資料不須另留 TiP log，事件觸發的統計更新若因 test 略過則須留 log；若有商業統計功能或報表，AC 必須驗證其結果不受測試交易影響。

[AC-TIP-003](tip/tip-acceptance-criteria.md#ac-tip-003) 已確認：庫存不足時正常終止結帳、保留 test Checkout，不建立訂單；按照正常流程記錄庫存不足及交易終止，未走到的操作不留 test 略過紀錄。

[AC-TIP-004](tip/tip-acceptance-criteria.md#ac-tip-004) 已確認：Order、Member、Checkout、Token 建立後，正式與 test 用途不得互換；Token.IsTest 由 Member.IsTest 繼承，Cart 排除標記；拒絕變更後原用途及關聯物件用途不變，正常記錄拒絕原因。

[AC-TIP-005](tip/tip-acceptance-criteria.md#ac-tip-005) 已於 2026-10-19 修訂確認：test identity 商品改價明確拒絕，Product 維持原價，記錄拒絕原因，不回成功略過；Core／HTTP 結果須可由程式辨識。新版實作與驗證列入 Phase 3。

[AC-TIP-006](tip/tip-acceptance-criteria.md#ac-tip-006) 已確認：測試修改 SKU 規格仍執行必要檢查，但略過更新並留 log；重新讀取為原規格，Product 關聯與庫存資料均不受影響。

[AC-TIP-007](tip/tip-acceptance-criteria.md#ac-tip-007) 已確認：測試預約通過必要檢查後，不新增 Reservation 或衍生 Product、不占用時段並留 log；正式查詢仍顯示可預約。此案例驗收至 `CreateHold`；使用者後續已決定不接續該預約的購物車與 Checkout，完整邊界見已接受的 AC-TIP-015。

[AC-TIP-008](tip/tip-acceptance-criteria.md#ac-tip-008) 已確認：測試預約遇到已占用時段時，照常回報不可用並終止，不新增預約或商品、不影響既有正式預約及占用；正常記錄失敗，不留 test 略過紀錄。

[AC-TIP-009](tip/tip-acceptance-criteria.md#ac-tip-009) 已確認：正式與測試交易同時結帳，各自建立正確用途的 Order；只有正式交易改變庫存與商業統計，正式流程正常執行，測試流程略過商業效果並留 log。

[AC-TIP-010](tip/tip-acceptance-criteria.md#ac-tip-010) 已確認：測試會員操作 Cart 可正常新增、刪除商品明細並保存結果，Cart 不承接會員且不保存用途；Product、SKU 與庫存不受影響，已執行的 Cart 異動不記成 test 略過。

[AC-TIP-011](tip/tip-acceptance-criteria.md#ac-tip-011) 已確認：測試會員不能完成其他測試會員的 Checkout，買方不符時正常拒絕；原 Checkout、Cart 不變，不建立 Order 或產生商業效果，正常記錄失敗，不留 test 略過紀錄。

[AC-TIP-012](tip/tip-acceptance-criteria.md#ac-tip-012) 已依使用者修正保留並接受：測試會員查詢發現正式預約真正逾期，正常通知系統清理；處理完成後，預約須保存為 `Expired`，預約與 Product 紀錄保留。現行 PetShop 查詢同步寫入，尚缺對應非同步通知與系統處理邊界，已記入[架構變更事項](tip/tip-architecture-notes.md#petshop-expiration-change)，留待實作。決策與查核結果見[系統到期整理決策](decisions/2026-10-16-tip-system-expiration-boundary.md)。

使用者已一次接受最後三條核心案例：[AC-TIP-013](tip/tip-acceptance-criteria.md#ac-tip-013) 的測試訂單後續事件與重試維持 test；[AC-TIP-014](tip/tip-acceptance-criteria.md#ac-tip-014) 要求用途明確，否則為 BUG；[AC-TIP-015](tip/tip-acceptance-criteria.md#ac-tip-015) 不允許測試預約接續 Cart／Checkout。本輪 14 條核心 AC 已全部確認。[AC-TIP-002](tip/tip-acceptance-criteria.md#ac-tip-002) 已依最新範圍改為 PaymentId 白名單的 PoC 驗收方向，建立 Checkout 時提供 PaymentId 並固定用途。

研究目標進一步確認為：透過 prototype 演練歸納可重用的 TiP best practice，原則確定後，具體保證機制交由實作端決定。已確認最低層系統服務介面將用途不可變物件的 `id + type` 視為 immutable、以事件／任務交接隔離查詢與系統工作、context 延續至出口並由 HTTP middleware 統一轉成對外結果、沿用正常 log 機制，以及在 database schema 明確定義用途。

Context 已進一步區分為 **current identity**（目前執行身分及其用途）與 **current transaction**（目前處理交易及其用途）。架構負責資訊正確傳遞，應用端依 context 執行業務政策：改價等操作可依身分判定，交易衍生操作另須保留交易用途；跨事件／async 邊界仍須遵守 AC-TIP-012／013 的責任區分。

使用者已確認支撐 TiP 體系的架構原則涵蓋完成，本輪在 best practice 與規範層級收斂。完整用途組合政策留給應用規格，具體欄位、結果代碼及傳遞機制留給正式 contract／實作，PaymentId 白名單已納入標準 PoC，其他額外付款來源留待功能擴充；標準流程部分已於後續 Phase 1 定案；其他來源維持擴充範圍，不採用跨所有操作的通用 OR 規則。

決策見 [TiP 研究範圍與架構規範邊界](decisions/2026-10-17-tip-contract-and-context-boundaries.md)；[TiP 架構設計筆記](tip/tip-architecture-notes.md) 區分已確認規範與後續事項。原研究階段未改程式；後續 TiP Phase 1 已同步 `/spec`、`.Abstract` 與 Core 基礎實作。

2026-10-17 標準實作範圍補充：Cart 排除、Token 跟隨 Member；沿用 Checkout 同一路徑控制效果，事件處理器視為 event／async 邊界，暫不處理 AppleBTS／PetShop。使用者已選擇建立 Checkout 時提供 PaymentId 並固定用途。已接受 scoped injection：IdentityContext 與 TransactionContext 獨立建模並分開注入，CheckoutService 採 scoped；兩者可在單次 HTTP 操作中共用 request scope，其他執行範圍依事件／CLI 等處理邊界決定。HTTP Items 與 Core 對各種 context 使用相同資料；middleware 以 endpoint metadata 明確指定交易／訂單 ID 來源，解析規則已納入 [HTTP 合約](../spec/tip-http-and-operation-results.md)。決策見 [TiP 標準實作範圍與 Context 方向](decisions/2026-10-17-tip-standard-scope-and-context.md)。

2026-10-17 Core 欄位已確認：新增 Member／Checkout／Order.IsTest、Checkout.PaymentId 及建立時從 Member 繼承的 Token.IsTest。Create／Complete command 保留明確 RequestMember，API／CLI 自行組合；PaymentId 移至 create，complete 使用保存值。Checkout 採作用中與封存分區保存，Order 關聯封存交易且不新增 Order.PaymentId；新增 nullable CheckoutTransactionId；Member.Name／PaymentId 白名單預設於 code 或 database，舊資料 IsTest = false；無交易時不給交易 header，物件中的交易為 null。詳見 [Core 欄位決策](decisions/2026-10-17-tip-core-fields-and-explicit-member.md)。

TiP Phase 1 已於 2026-10-17 由使用者啟動並完成。使用者最後指定先開始變更 code，再同步正式 spec 與 testcases；本輪不以先凍結 spec 作為開工前置條件。最小共用 contract 已具體化並定案；規格、案例與交付證據見 [Phase 1 基準](../spec/tip-phase1.md)。Phase 2 的用途傳遞與效果控制尚未交付。

2026-10-17 已開始 TiP Core 物件定義：新增四種物件的 IsTest、Checkout.PaymentId、Order.CheckoutTransactionId（nullable），以及 ArchivedCheckoutTransactions 封存入口；同步 [物件規格](../spec/tip-core-object-definitions.md) 與 [測試案例](../spec/testcases/tip-core-object-definitions.md)。初批僅交付欄位；後續已完成 command 付款前移、白名單查詢與 Context 基礎行為，封存編排留待 Phase 2。

2026-10-17 TiP 進入 TDD 紅燈檢查點：新增 Core TestAllowlist 與 Abstract 的獨立 Context 介面，紅燈階段先保留 Core 骨架，確認 21 項新增案例預期失敗、75 項既有測試通過。使用者確認後已補上白名單查詢及 Context 初始化／綁定／清除，host 與 DI 整合留待後續。詳見 [決策](decisions/2026-10-17-tip-allowlist-and-tdd-checkpoint.md)。

2026-10-17 Phase 1 收尾：使用者確認舊 Checkout 缺少 PaymentId 明確回 409 並要求重建，不做向前相容。已實作該分支並同步所有共用呼叫端；補齊標準流程、操作結果、HTTP／log、scope 與 AC 對應案例。全套 107 項測試通過。詳見 [定案決策](decisions/2026-10-17-tip-phase1-contract-baseline.md)。

2026-10-17 核帳需求追加：新增 Checkout.ClosedAt 與 Order.CreatedAt，歷史未知時間為 null；業績／核帳查詢預設依交易／訂單的 IsTest 排除測試資料。時間填值與移除 access token 明文 log 留 Phase 2，查詢功能後續再做。Checkout 終態與獨立金額明細後續已確認並補上 schema，create 時固定；見 [決策](decisions/2026-10-17-tip-accounting-timestamps.md) 與 [檢驗](tip/accounting-schema-review.md)。

2026-10-17 Checkout 追加定案：保存 State 與獨立交易金額／明細，create 時固定，complete 沿用且不重算 Cart／現價；Completed／Cancelled 完整搬移至 archived_checkout_transactions 後凍結。Checkout 不保存商品名稱、描述或規格快照。正式 [生命週期規格](../spec/tip-checkout-lifecycle.md) 與案例已同步，執行流程留 Phase 2。

2026-10-18 使用者確認會員白名單改以 Name（預設 `test`）判定，名稱比對沿用作用中資料庫 collation；PaymentId 保留 `990001`，既有會員用途不重新分類。規格與實作同步修訂，Abstract 不變。見 [決策](decisions/2026-10-18-tip-member-name-allowlist.md)。

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

## TiP Phase 1.2 Admin CLI（2026-10-18）

使用者追加 .NET query-only CLI 作為最後查核入口：統計／明細、環境變數與預設 working directory `.env`、人用 --help、agent --skill 情境指引、TXT／CSV／JSON，以及 member／checkout／order 與 all／normal／test 篩選。會員不接受日期經使用者確認；訂單／結帳沿用既有起訖時間語意。這是 TiP 驗證主線的工具補齊，不改動 .Core、Abstract 或既有 API 契約。

[Phase 1.2](../spec/tip-phase1.2.md) 已定案並授權實作；取代先前 shell CLI 參考。最後確認以 CLI 資料證據加上既有 Core／HTTP／事件測試，不把三個唯讀查詢當成所有 AC 的證明。見 [決策](decisions/2026-10-18-tip-admin-cli.md)、[實作交付](tip/phase1.2-delivery.md) 與 [使用者驗收流程](tip/phase1.2-final-verification.md)。

## TiP Phase 2 跨 Session 交接（2026-10-18）

使用者確認以同一台機器、不同 chat session 依序執行 2.1～2.7；canonical 執行與進度入口為 [Phase 2 計畫](../spec/tip-phase2-plan.md)。每階段更新交付證據與剩餘工作；保持 Phase 1 與 Abstract 定案，不將資料模型或舊 Docker image 的可用性當作完整 TiP 驗收。

2026-10-18 TiP Phase 2.1 完成：Core 會員／Token 共用服務接入標準 API 與 CLI，驗證最終 ID 分類、Token 繼承、舊資料不重分類、用途拒絕改寫與併發／失敗重試。solution 131 tests 通過；HTTP context 與完整出口仍待 2.2／2.6，Docker 未重建。詳見 [交付紀錄](tip/phase2.1-delivery.md)。

2026-10-18 TiP Phase 2.2 完成：獨立 scoped contexts、HTTP 身分與 metadata 交易解析、CLI 每次操作 scope；solution 153 tests 通過。未來 `/api/orders/{id}/xxx` 後置處理須攜帶 Order transaction，現階段只留[確認註記](decisions/2026-10-18-tip-order-postprocessing-context.md)，不新增任務。詳見 [2.2 交付紀錄](tip/phase2.2-delivery.md)。

2026-10-18 TiP Phase 2.3 完成：create 固定用途、付款與報價，complete 沿用保存內容並驗證商品／SKU／庫存；正式扣減、test 略過並留證據。Order、完整終態封存與作用中移除原子提交，補上用途／封存防寫、跨分區 ID、rollback 與併發驗證；solution 191 tests 通過，solution／CLI build 成功。Docker 未重建；後續接續 2.4 共用效果控制。詳見 [2.3 交付紀錄](tip/phase2.3-delivery.md)。

2026-10-18 TiP Phase 2.4 完成：受保護寫入共用執行器、商品／SKU Core 維護、OperationEffects，以及 test Cart／並行交易／雙 test 買方拒絕的精確案例。solution 285 項通過；完整事件／出口／UI 留待 2.5～2.7，未重建 Docker。見 [交付紀錄](tip/phase2.4-delivery.md) 與 [AC 自動化覆蓋對照](tip/ac-test-coverage.md)。

2026-10-18 TiP Phase 2.5 完成：標準 dispatcher 每次派送建立獨立 scope，以 System identity 載入保存 Order 用途，事件效果驗證原快照未遭替換。補齊重送、巢狀／並行、缺 Order／用途 BUG 及履約失敗封存不變；solution 310 項通過。真實商業 adapter 不適用；完整出口與 UI 留待 2.6／2.7。見 [交付紀錄](tip/phase2.5-delivery.md)。

2026-10-18 TiP Phase 2.6 完成：統一 OperationResult、HTTP headers／錯誤映射、CLI／事件結果及完整略過 log，移除 OAuth 明文 token／授權碼診斷輸出。solution 327 項通過，CLI build 成功；2.5 已提交為 `64ced22`。CommonStorefront／Docker／UI 驗收留待 2.7，本輪未重建部署。見 [交付紀錄](tip/phase2.6-delivery.md)。

2026-10-18 TiP Phase 2.7 完成標準流程交付：CommonStorefront 明確一般／測試付款、操作結果與用途呈現；solution 335 項通過，Docker 已重建。Edge 四種會員／付款組合、庫存拒絕流程與 400px 手機主要任務通過，admin／一致性 LiteDB 快照確認正式統計排除 test、只有一般交易扣庫存。2.6／2.7 依使用者授權一併提交；實際驗證與不適用範圍見 [2.7 交付紀錄](tip/phase2.7-delivery.md)。

## CommonStorefront PaymentID 輸入調整（2026-10-18）

依使用者要求，付款改為直接輸入 PaymentID（模擬卡號），用途由既有系統規則判定，次要文字提示預設測試 ID 990001 與測試會員規則。不改動 Core／Abstract／API，與已提交的 AdminCLI 分開 commit；見 [決策](decisions/2026-10-18-storefront-payment-id-input.md) 與 [交付](tip/payment-id-ui-delivery.md)。
