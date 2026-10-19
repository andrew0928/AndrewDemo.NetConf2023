# TiP 設計原則

2026-10-19 最新邊界：嚴格隔離交易用途與 storage；認證及非交易資訊共用。test identity 商品改價明確拒絕，test transaction 無任何實際業務效果；SKU 維持既有略過政策。詳見 [交易規格](../../spec/tip-transaction-storage.md) 與 [Phase 3 review](phase3-spec-review.md)，尚未實作。

更新日期：2026-10-19。

本文件供 AndrewShop 的 TiP 研究與需求討論使用。目前已確認大前提、領域物件分類規則及 [AC-TIP-001](tip-acceptance-criteria.md#ac-tip-001)、[AC-TIP-003](tip-acceptance-criteria.md#ac-tip-003)、[AC-TIP-004](tip-acceptance-criteria.md#ac-tip-004)、[AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005)、[AC-TIP-006](tip-acceptance-criteria.md#ac-tip-006)、[AC-TIP-007](tip-acceptance-criteria.md#ac-tip-007)、[AC-TIP-008](tip-acceptance-criteria.md#ac-tip-008)、[AC-TIP-009](tip-acceptance-criteria.md#ac-tip-009)、[AC-TIP-010](tip-acceptance-criteria.md#ac-tip-010)、[AC-TIP-011](tip-acceptance-criteria.md#ac-tip-011)、[AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012)、[AC-TIP-013](tip-acceptance-criteria.md#ac-tip-013)、[AC-TIP-014](tip-acceptance-criteria.md#ac-tip-014)、[AC-TIP-015](tip-acceptance-criteria.md#ac-tip-015)，本輪核心接受條件（AC）已全部確認。本文件不是已凍結的 `/spec`，也不代表系統已具備所述能力。

## 最新標準實作範圍

2026-10-17 [範圍決策](../decisions/2026-10-17-tip-standard-scope-and-context.md)：Member、Checkout、Order 保存用途；Token.IsTest 建立時繼承 Member.IsTest 並保存，Cart 不承接會員且不標記 test。PaymentId 白名單代表測試卡號，建立 Checkout 時即提供 PaymentId 並固定用途；事件處理器視為 event／async 邊界。AppleBTS／PetShop 暫不納入本輪實作。同日已接受 scoped injection：IdentityContext 與 TransactionContext 獨立建模並分開注入，scope 依 HTTP、事件與 CLI 的執行情境決定，兩者可在單次 HTTP 操作共用 request scope；CheckoutService 及直接依賴 context 的服務配合採 scoped。

## 研究目的與範圍

以 AndrewShop PoC / prototype 作為 Testing in Production（TiP）的研究與實驗對象，探索如何在正式環境中辨識測試行為、執行業務流程，並控制其結果。

研究目標為透過 prototype 演練歸納可重用的 TiP 設計 best practice；原則確定後收斂，具體保證機制交由實作端決定。2026-10-17 已確認[研究範圍與架構規範邊界](../decisions/2026-10-17-tip-contract-and-context-boundaries.md)：

- 最低層系統服務介面將用途不可變物件的 `id + type` 視為 immutable，同一物件身分的用途始終一致；建立權限由應用端決定。
- 查詢與系統到期整理以 event-driven／task queue 等機制交接，區分兩端責任；用途承接仍遵守 AC-TIP-012／013。
- Context 延續到出口，再以明確的 header／result code 等對外契約傳遞；HTTP API 由 middleware 統一處理出口表達。
- 沿用正常 log 機制，由 context 與判定結果決定對應紀錄內容。
- 在 database schema 明確定義各類物件的正式／測試用途及適用規則，資料使用依此處理；既有受保護物件分類不變。

Context 已區分為 current identity（目前執行身分及其用途）與 current transaction（目前處理交易及其用途）。架構保證資訊正確傳遞，應用端各環節依 context 執行政策，在商業效果發生前完成判定；改價等操作可依身分判定，交易用途則須在後續處理中保留。使用者已確認支撐 TiP 體系的設計涵蓋完成，本輪原則收斂；標準流程政策與具體 contract 已納入 [Phase 1 基準](../../spec/tip-phase1.md)，標準 runtime 已於 Phase 2 接入，collection 分流與改價拒絕列入 Phase 3，詳見[架構設計筆記](tip-architecture-notes.md)。已先將物件定義、白名單與 Context 合約落入 `/spec`，並建立 `.Abstract` 的獨立 Context 介面；Core 白名單查詢與 Context 基礎行為已在紅燈檢查確認後補上，Phase 2 已有標準流程交付證據，新版 Phase 3 要求仍待實作。

此方向對齊 [project roadmap](../project-roadmap.md)：共用交易流程、DiscountRule、ProductService 與不同業務模組，都需要遵守一致的測試邊界。

本文件中的 TiP，限定為「服務在正式環境執行，測試行為受到明確控制」這個研究範圍。它不是所有 production testing 技術的總稱；以真實交易驗證金流、canary release 或其他可能產生真實商業效果的驗證方式，需另行定義範圍。

## 已確認的大原則

> **服務部署在正式環境，測試行為不應產生真實商業效果。**

這是後續所有 TiP 規則的大前提，使用者已於 2026-10-16 明確確認。決策紀錄見 [TiP 大前提：不產生真實商業效果](../decisions/2026-10-16-tip-no-real-business-effects.md)。

### 正式環境與交易性質是兩個不同維度

「正式環境」說明服務在哪裡執行；「正式或測試」說明一次行為及其交易結果具有什麼性質。服務位於正式環境，不代表它接收的每一次行為都應產生正式交易。

因此，測試可以經過正式環境中的服務、業務邏輯與事件處理。這個前提不預先指定共用資料表、獨立資料庫或專屬 endpoint；隔離方式應在後續規則與設計中決定。

### 商業效果必須在發生前受到控制

測試可以驗證價格計算、折扣、交易狀態與成功或失敗結果。當流程即將影響真實資金、履約義務或正式客戶權益時，就必須依測試性質控制該動作。

僅在最後將 order 標成 test，無法證明之前沒有產生商業效果。先扣款再退款、先占用正式資源再釋放，也已經產生過真實影響，不能直接視為符合這個前提。

### 測試仍會產生技術活動與執行成本

執行測試會使用運算、網路、儲存與監控資源，也可能產生第三方服務費用。這些成本需要被觀察及管理；「不產生真實商業效果」不表示執行成本為零，也不表示對共用系統負載完全沒有影響。

容量、測試頻率與流量限制屬於後續操作規則，不能由本原則推定為無限制。

## 名詞定義

| 名詞 | 本文件中的意義 |
|---|---|
| 正式環境（production environment） | 承載正式服務的執行環境；與單筆行為的正式／測試性質分開描述。 |
| 測試行為（test behavior） | 為驗證系統而執行，且由系統辨識為測試的一次操作或一段流程。目前已確認測試會員及其衍生物件的用途關係，其餘來源判定仍待定義。 |
| 測試來源 | 提供測試性質判定依據的身分、付款工具、憑證或入口。來源本身的標記，不等於後續結果已經受到控制。 |
| 測試交易／測試訂單 | 因測試行為產生的交易結果。可以保存業務資料與執行結果，但須符合不產生真實商業效果的大前提。 |
| 真實商業效果 | 對真實資金、客戶權益、履約義務、正式可用資源或正式商業紀錄造成的影響。 |
| 正式商業統計 | 用來表示真實業務成果的統計，例如營收、銷量與正式訂單數；與測試分析、技術監控及執行成本統計分開看待。 |

## 已確認的領域物件分類

以下分類已於 2026-10-16 確認，決策紀錄見[領域物件分類決策](../decisions/2026-10-16-tip-domain-object-classification.md)。後續案例的逐條確認狀態維護於 [TiP 接受條件](tip-acceptance-criteria.md)；先前未審閱的情境表不自動視為已確認。

### 第一類：建立時確定用途，建立後不可變更

`Order`、`Member`、`CheckoutTransactionRecord` 在建立時即確定正式／測試用途。MemberAccessTokenRecord.IsTest 建立時由 Member.IsTest 複製保存，同樣維持用途不可變；Cart 排除用途分類。不可從正式改為測試，也不可從測試改為正式；不可變的是用途分類，不是物件的所有業務資料或狀態。

[AC-TIP-004](tip-acceptance-criteria.md#ac-tip-004) 已確認兩個方向均須拒絕變更；重新讀取後原用途與關聯物件用途均不變，正常記錄拒絕原因。其他合法業務欄位仍依原有規則更新。

[AC-TIP-010](tip-acceptance-criteria.md#ac-tip-010) 已確認測試會員操作 Cart 可正常新增、刪除商品明細並保存結果，Cart 不保存 test 用途且不承接會員，Product、SKU 與庫存不受影響。已執行的 Cart 異動沿用正常操作紀錄規則，不記成 test 略過。

`Member` 註冊時依預先指定的 Member.Name 白名單自動標示用途，命中為 true、未命中為 false；Token.IsTest 在簽發時由 Member.IsTest 繼承保存。Cart 不承接會員；建立 Checkout 時依會員用途與預先設定的 PaymentId 白名單判定，測試會員或測試卡號使 Checkout 為 test，否則為正式，Order 承接 Checkout 用途。用途一經建立不再由後續白名單變更重算。無交易操作依指定 identity 政策處理；既有交易依保存用途執行，不以當下 Token 分區或重新分類。測試交易產生的 Order 必須標示為 test，正式會員亦可使用測試 PaymentId。由讀取喚起、但依正式資料與時間規則執行的系統到期整理，另依下述責任邊界判定。實際欄位、關聯與傳遞規則見 [Phase 1 基準](../../spec/tip-phase1.md)。

### 第二類：受保護，測試行為不得異動

`InventoryRecord`、`Product`、`SkuRecord`，以及擴充模組的預約與行事曆／時段占用，均屬受保護範圍。測試行為不得新增、修改或刪除這些受保護資料；因測試而略過的操作，必須留下 log 說明略過原因。正式行為依既有權限與業務規則處理。

**系統到期整理的責任邊界**：正式會員 A 的預約已逾期，測試會員 B 的查詢可以喚起系統正常清理；不因 B 為 test 而略過真正到期的狀態更新。此處區分 B 的查詢與系統的生命週期整理，不改變受保護物件分類，也不代表任何不帶會員資訊的事件都可異動正式資料。[AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012) 已依此修正接受；詳見[系統到期整理決策](../decisions/2026-10-16-tip-system-expiration-boundary.md)，PetShop 執行邊界的差距列入[架構變更事項](tip-architecture-notes.md#petshop-expiration-change)，留待實作。

必要的身分、權限與業務檢查仍照常執行；不會造成受保護資料異動的必要流程也照常執行。測試用途不代表無條件成功；明確禁止的身分操作直接拒絕，允許模擬的交易效果才依政策略過。例如庫存檢查照常，但不扣庫存，且記錄因 test 而未扣減。Log 必要欄位與結果代碼見 [HTTP 與結果合約](../../spec/tip-http-and-operation-results.md)，沿用 host logging。

[AC-TIP-011](tip-acceptance-criteria.md#ac-tip-011) 已確認測試會員不能完成其他會員的 Checkout，即使雙方都是 test 也須拒絕；原 Checkout 與 Cart 保持不變，不建立 Order 或產生商業效果，正常記錄買方不符而不留 test 略過紀錄。

[AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005) 已於 2026-10-19 改為 test identity 明確禁止商品改價：回拒絕、保留原價，記錄拒絕原因，不能回成功略過。具體結果見 [HTTP 與結果合約](../../spec/tip-http-and-operation-results.md)。

[AC-TIP-006](tip-acceptance-criteria.md#ac-tip-006) 已確認測試修改 SKU 規格的案例：必要檢查通過後，略過 SKU 更新並留 log；重新讀取仍為原規格，Product 關聯與庫存資料均不受影響。

### `.Core` 持久化領域物件完整清單

依目前 [ShopDatabaseContext](../../src/AndrewDemo.NetConf2023.Core/ShopDatabaseContext.cs) 的 collection 註冊及實際持久化型別核對，共 8 種，已全部分類。新設計已確認 Checkout 的作用中與封存紀錄分區保存，封存延續同一交易的 ID、用途與付款來源，不新增用途分類；Phase 2 已實作同庫封存；Phase 3 將 Order／active Checkout／archive 各拆 normal／test，完整命名見 [交易儲存規格](../../spec/tip-transaction-storage.md)。下表為尚未重構的 Phase 2 實作配置，不是 Phase 3 目標：

| 領域物件 | 實際型別 | Collection | 已確認分類 |
|---|---|---|---|
| Order | `Order` | `orders` | 第一類：用途不可變 |
| Member | `Member` | `members` | 第一類：用途不可變 |
| Cart | `Cart` | `carts` | 排除用途分類，不承接會員、不保存 test 標記 |
| Checkout | `CheckoutTransactionRecord` | `checkout_transactions`、`archived_checkout_transactions` | 第一類：用途不可變 |
| Token | `MemberAccessTokenRecord` | `member_tokens` | 第一類：IsTest 建立時繼承 Member，保存後用途不可變 |
| Inventory | `InventoryRecord` | `inventory_records` | 第二類：受保護 |
| Product | `Product` | `products` | 第二類：受保護 |
| SKU | `SkuRecord` | `skus` | 第二類：受保護 |

`Product` 的型別雖定義於 `.Abstract`，持久化由 `.Core` 管理，因此列入盤點。`OrderProductLine`、`OrderDiscountLine`、`OrderShopNotes` 是內嵌資料，跟隨所屬 Order 的用途；Cart 的 `LineItem` 與 Cart 一樣不標記用途；Order 中的 Buyer 是 Member 快照，不是額外的 collection。

本清單延續「會儲存在資料庫的領域物件」範圍。Checkout command／result、`WaitingRoomTicket`、計算用 context、折扣計算結果與事件訊息不是額外的持久化根物件；其流程或傳遞規則仍需在後續設計處理。

### Booking 與 Calendar 的專案對應

| 使用者用語 | 現行專案對應 | 已確認規則 |
|---|---|---|
| Booking／預約 | `.PetShop.Extension` 的 `PetShopReservationRecord`，collection 為 `petshop_reservations`；專案沿用 `Reservation` 命名。 | 受保護。測試預約操作不得新增或異動預約，也不得建立其衍生 Product；略過操作留 log。真正逾期的系統整理依前述責任邊界正常處理。 |
| Calendar／行事曆 | 目前沒有獨立 Calendar 型別或 collection；由 Reservation 推導時段、場地及人員的占用與可用性。 | 受保護。測試預約操作不得占用、釋放或改變正式時段資源；略過操作留 log。既有 hold 的自然到期仍依正式時間規則判定。 |

對應依據見 [PetShopReservationRecord](../../src/AndrewDemo.NetConf2023.PetShop.Extension/Records/PetShopReservationRecord.cs)、[PetShopReservationRepository](../../src/AndrewDemo.NetConf2023.PetShop.Extension/Services/PetShopReservationRepository.cs) 與 [PetShopAvailabilityService](../../src/AndrewDemo.NetConf2023.PetShop.Extension/Services/PetShopAvailabilityService.cs)。兩者已納入規則，但不另計為 `.Core` 的持久化物件。

[AC-TIP-007](tip-acceptance-criteria.md#ac-tip-007) 已確認：測試 `CreateHold` 通過必要檢查後，略過建立 Reservation、時段占用及衍生 Product 並留 log，正式查詢仍顯示可預約。使用者後續已決定不接續該預約的購物車與 Checkout；完整流程邊界見已接受的 [AC-TIP-015](tip-acceptance-criteria.md#ac-tip-015)。

## 已確認的略過紀錄與商業統計規則

在 AC-TIP-001 確認時，使用者補充以下要求：

- 因 test 略過扣庫存、扣款或出貨，均須留下可對應該次交易的 log，說明略過的操作及原因。
- 若正式商業統計是事後查詢產生的報表，查詢時排除測試訂單，不要求為此另留 TiP log；不影響既有一般查詢紀錄。
- 若由 event 觸發更新正式商業統計，因 test 略過統計更新時，同樣須留下 log。
- 若系統有商業統計功能或報表，AC 必須驗證測試交易不影響其結果，不能只驗證有 log。不存在的統計功能標示不適用，不因此新增報表，也不能宣稱該功能已驗證通過。

AC-TIP-003 補充：若庫存不足等正常業務檢查已使交易終止，依正常流程記錄失敗原因即可；該案例由 Checkout 物件的 test 標記識別用途，不要求失敗 log 額外帶 test 標記。未走到的扣庫存、扣款或出貨操作，不產生「因 test 略過」紀錄。

[AC-TIP-008](tip-acceptance-criteria.md#ac-tip-008) 確認相同原則適用於預約時段不可用：必要檢查失敗就正常終止，既有預約與占用不變，不新增 Reservation 或 Product，也不將正常失敗記為 test 略過。

[AC-TIP-009](tip-acceptance-criteria.md#ac-tip-009) 已確認正式與測試交易並行時，兩者各自維持用途及處理政策；測試交易不影響正式交易的庫存扣減、付款、履約或商業統計，也不能因並行而漏掉自身的效果控制與略過紀錄。

## 附加說明：商業效果的解釋範圍

以下例子協助辨識邊界，並非已凍結的完整政策矩陣。

| 領域 | 需要控制的真實商業效果 |
|---|---|
| 資金 | 對真實付款工具進行授權、圈存、扣款或資金移轉。 |
| 履約 | 建立真實出貨、配送或服務執行義務。 |
| 業務資源 | 扣減正式可售庫存、占用正式預約名額，或消耗真實優惠券與點數。 |
| 商業紀錄 | 將測試結果計入正式營收、銷量或其他商業成果。 |
| 對外通知 | 讓真實客戶或服務人員收到會引發履約、付款或其他業務行動的測試訊息。 |

上述已分類的受保護物件採「保留必要檢查、略過異動並留 log」規則。其他領域是否使用模擬結果、專屬測試資源或略過特定動作，仍需另訂規則。必須能說明測到了哪一段流程，以及哪些真實效果受到隔離。

## 附加說明：Stripe 參考做法

Stripe 是使用者認可的主要設計參考。下列內容依官方公開文件整理，查閱日期為 2026-10-16；「對本研究的啟示」是本專案的歸納，不是 Stripe 發布的 TiP 原則。

| 官方公開行為 | 對本研究的啟示 |
|---|---|
| Sandbox 與 live mode 使用各自的 API keys，兩個模式的物件不能互相存取；測試 product 不能用於正式付款。[API keys](https://docs.stripe.com/keys#sandbox-versus-live-mode) | 測試性質需要連同憑證與資料存取邊界一起處理。 |
| Sandbox 可以模擬付款，但付款不由真實卡組織或支付服務商處理。[Sandboxes](https://docs.stripe.com/sandboxes) | 業務流程可以有測試結果，同時隔離真實資金效果。 |
| 在 Connect webhook 情境，production webhook URL 可能同時收到 live 與 test 事件；Stripe 建議接收端檢查 `livemode` 決定後續動作。Sandbox accounts 另需設定 webhook endpoints。[Connect webhooks](https://docs.stripe.com/connect/webhooks) | 正式服務入口與事件性質必須分開判斷；處理事件的一方也要理解模式。 |
| 部分報表類型只能使用 live-mode 資料，未使用 live-mode API key 時會報錯。[Report Types](https://docs.stripe.com/api/reporting/report_type) | 報表使用哪一類資料，可以成為系統強制執行的邊界。 |

上述文件說明的是對外行為，不能據此推定 Stripe 內部共用同一套部署或資料庫，也不能把 Connect webhook 的特定行為推廣成所有 Stripe endpoint 的規則。

本研究借用模式識別、資料邊界與副作用隔離的設計思路。尚未決定採用 Stripe 金流、複製其 sandbox 架構，或將 `livemode` 作為 AndrewShop 的正式欄位名稱。

## 原則收斂與後續規格範圍

使用者已提出的研究情境為：來自 test user、test 信用卡、test API key 或 test endpoint 的行為，產生的 test orders 應不出貨、不扣款、不列入正式商業統計。目前核心 AC 與支撐體系的架構原則已確認；完整來源組合政策及額外功能仍保留後續規格範圍。

使用者已補充：測試訂單後續事件與重試仍受 test 限制；每個實際執行邊界都必須能明確確認用途，用途不明或判定矛盾視為 BUG。[AC-TIP-013](tip-acceptance-criteria.md#ac-tip-013) 與 [AC-TIP-014](tip-acceptance-criteria.md#ac-tip-014) 的完整案例已與 AC-TIP-015 一併接受。付款來源案例 [AC-TIP-002](tip-acceptance-criteria.md#ac-tip-002) 已改以 PaymentId 白名單作為 PoC 驗收方向，不要求引入實際信用卡服務。

後續事項依責任與階段分類：

1. **應用規格與額外來源**：最低層介面保證物件 `id + type` 不可變；建立權限由應用端決定，保證機制由實作端決定。兩類 context 提供判定依據，完整用途組合與各操作政策由應用規格定義，須符合已確認 AC；PaymentId 白名單已納入本輪 Checkout 建立規則；其他來源仍待功能引入時確認，不推導跨所有操作的通用 OR 規則。
2. **分類傳遞契約**：Member 衍生關係、事件與重試須維持明確用途，以及 AC-TIP-012／013 的責任區分已確認；架構保證 context 正確傳遞，具體承接格式留給後續 contract。
3. **效果控制與結果契約**：保護政策與必要檢查已確認，應用端各環節依 context 完成效果判定；context 延續至出口，HTTP API 由 middleware 統一表達對外結果。具體結果契約留給規格階段，付款、履約、通知的整合機制屬實作事項。
4. **資料與紀錄**：Schema 明確定義用途，正常 log 機制依 context 與判定結果記錄；正式商業統計仍依 AC 驗證結果。實體 schema、log 格式與保存方式交由實作設計，不作為本輪研究必須深入的選型議題。

以上事項不阻擋本輪 best practice 與規範層級收斂，也不代表範圍外的其他情境或 AC 已獲接受。已確認規範與後續事項統一維護於 [TiP 架構設計筆記](tip-architecture-notes.md)；核心 AC 依最新範圍移除 Cart 標記與會員承接要求，Token 跟隨 Member；其餘商業效果控制及正常檢查原則維持。

## 文件與實作邊界

本版已收斂大原則、領域物件分類及架構規範。2026-10-17 使用者最後確認：既有資料 IsTest 一律為 false；Order 新增 nullable CheckoutTransactionId；無交易不給交易 header，物件中的交易為 null。白名單可定義於 code 或 database，第一版採 code 內集中定義 Member.Name／PaymentId 清單。使用者指定先開始變更 code，再同步 `/spec` 與 `/spec/testcases`；本輪共享 contract 隨實作具體化。

API、欄位與儲存隔離的具體規則以正式 spec 為準；本研究不以 PoC 尚未連接實際金流，作為「測試已成功隔離真實扣款」的驗證證據。盤點完整代表需求分類涵蓋現有物件，不代表現行實作已符合 TiP 規則。
