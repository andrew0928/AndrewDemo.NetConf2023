# TiP 設計原則

更新日期：2026-10-16。

本文件供 AndrewShop 的 TiP 研究與需求討論使用。目前已確認大前提與領域物件分類規則，情境與接受條件（AC）尚未確認。本文件不是已凍結的 `/spec`，也不代表系統已具備所述能力。

## 研究目的與範圍

以 AndrewShop PoC / prototype 作為 Testing in Production（TiP）的研究與實驗對象，探索如何在正式環境中辨識測試行為、執行業務流程，並控制其結果。

此方向對齊 [project roadmap](project-roadmap.md)：共用交易流程、DiscountRule、ProductService 與不同業務模組，都需要遵守一致的測試邊界。

本文件中的 TiP，限定為「服務在正式環境執行，測試行為受到明確控制」這個研究範圍。它不是所有 production testing 技術的總稱；以真實交易驗證金流、canary release 或其他可能產生真實商業效果的驗證方式，需另行定義範圍。

## 已確認的大原則

> **服務部署在正式環境，測試行為不應產生真實商業效果。**

這是後續所有 TiP 規則的大前提，使用者已於 2026-10-16 明確確認。決策紀錄見 [TiP 大前提：不產生真實商業效果](decisions/2026-10-16-tip-no-real-business-effects.md)。

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

本階段先收斂領域物件規則。以下分類已於 2026-10-16 確認，決策紀錄見[領域物件分類決策](decisions/2026-10-16-tip-domain-object-classification.md)；先前提出的情境表尚未經使用者審閱，不視為已確認的 AC。

### 第一類：建立時確定用途，建立後不可變更

`Order`、`Member`、`Cart`、`CheckoutTransactionRecord`、`MemberAccessTokenRecord` 在建立時即確定正式／測試用途。不可從正式改為測試，也不可從測試改為正式；不可變的是用途分類，不是物件的所有業務資料或狀態。

`Member` 在建立時標示用途，衍生的 Cart、Checkout 與 Token 跟隨 Member。測試 Token 引發的行為與影響也屬於測試；測試交易產生的 Order 必須標示為 test。實際欄位名稱、關聯與傳遞機制留待設計。

### 第二類：受保護，測試行為不得異動

`InventoryRecord`、`Product`、`SkuRecord`，以及擴充模組的預約與行事曆／時段占用，均屬受保護範圍。測試行為不得新增、修改或刪除這些受保護資料；因測試而略過的操作，必須留下 log 說明略過原因。正式行為依既有權限與業務規則處理。

必要的身分、權限與業務檢查仍照常執行；不會造成受保護資料異動的必要流程也照常執行。測試用途不代表無條件成功，僅略過被禁止的異動。例如庫存檢查照常，但不扣庫存，且記錄因 test 而未扣減。Log 格式與保存方式尚未定案。

### `.Core` 持久化領域物件完整清單

依 [ShopDatabaseContext](../src/AndrewDemo.NetConf2023.Core/ShopDatabaseContext.cs) 的 collection 註冊及實際持久化型別核對，共 8 種，已全部分類：

| 領域物件 | 實際型別 | Collection | 已確認分類 |
|---|---|---|---|
| Order | `Order` | `orders` | 第一類：用途不可變 |
| Member | `Member` | `members` | 第一類：用途不可變 |
| Cart | `Cart` | `carts` | 第一類：用途不可變 |
| Checkout | `CheckoutTransactionRecord` | `checkout_transactions` | 第一類：用途不可變 |
| Token | `MemberAccessTokenRecord` | `member_tokens` | 第一類：用途不可變 |
| Inventory | `InventoryRecord` | `inventory_records` | 第二類：受保護 |
| Product | `Product` | `products` | 第二類：受保護 |
| SKU | `SkuRecord` | `skus` | 第二類：受保護 |

`Product` 的型別雖定義於 `.Abstract`，持久化由 `.Core` 管理，因此列入盤點。`OrderProductLine`、`OrderDiscountLine`、`OrderShopNotes` 與 Cart 的 `LineItem` 是內嵌資料，跟隨所屬 Order／Cart 的用途；Order 中的 Buyer 是 Member 快照，不是額外的 collection。

本清單延續「會儲存在資料庫的領域物件」範圍。Checkout command／result、`WaitingRoomTicket`、計算用 context、折扣計算結果與事件訊息不是額外的持久化根物件；其流程或傳遞規則仍需在後續設計處理。

### Booking 與 Calendar 的專案對應

| 使用者用語 | 現行專案對應 | 已確認規則 |
|---|---|---|
| Booking／預約 | `.PetShop.Extension` 的 `PetShopReservationRecord`，collection 為 `petshop_reservations`；專案沿用 `Reservation` 命名。 | 受保護。測試觸發的預約不得新增或異動預約，也不得建立其衍生 Product；略過操作留 log。 |
| Calendar／行事曆 | 目前沒有獨立 Calendar 型別或 collection；由 Reservation 推導時段、場地及人員的占用與可用性。 | 受保護。測試不得占用、釋放或改變正式時段資源；略過操作留 log。 |

對應依據見 [PetShopReservationRecord](../src/AndrewDemo.NetConf2023.PetShop.Extension/Records/PetShopReservationRecord.cs)、[PetShopReservationRepository](../src/AndrewDemo.NetConf2023.PetShop.Extension/Services/PetShopReservationRepository.cs) 與 [PetShopAvailabilityService](../src/AndrewDemo.NetConf2023.PetShop.Extension/Services/PetShopAvailabilityService.cs)。兩者已納入規則，但不另計為 `.Core` 的持久化物件。

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

## 待展開的具體規則

使用者已提出的研究情境為：來自 test user、test 信用卡、test API key 或 test endpoint 的行為，產生的 test orders 應不出貨、不扣款、不列入正式商業統計。這些是後續需求展開的輸入，尚未形成完整判定規格。

接下來需依大原則逐項釐清：

1. **來源判定**：誰能標記 test、如何驗證來源，以及多種來源混合或判定失敗時如何處理。先前提出的 OR 規則仍是候選方案。
2. **分類傳遞**：建立時用途不可變及 Member 衍生關係已確認；事件、重試及跨模組如何承接，還有付款階段才辨識出 test 時如何處理，仍待定義。
3. **效果控制**：庫存、商品、SKU、預約及時段資源的保護政策已確認；略過異動後如何回傳結果或延續流程，以及付款、履約、通知的處理規則仍待定義。
4. **資料使用**：因測試略過的操作必須留 log 已確認；log 格式、保存方式，以及正式報表、測試分析、稽核、技術監控與成本統計的資料使用細節仍待定義。

以上待討論項目不影響本版領域分類已確認的狀態，也不代表情境與 AC 已獲接受。

## 文件與實作邊界

本版整理大原則、領域物件分類、定義及參考依據。後續需求收斂後，再將可驗收的規則寫入 `/spec` 與 `/spec/testcases`；若涉及共享 contract，再依 Phase 1 流程處理 `.Abstract`。

本版不決定 API、資料欄位名稱與結構、儲存隔離方式或付款實作，也不以現有 PoC 尚未連接實際金流，作為「測試已成功隔離真實扣款」的驗證證據。盤點完整代表需求分類涵蓋現有物件，不代表現行實作已符合 TiP 規則。
