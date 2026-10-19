# TiP 架構設計筆記

更新日期：2026-10-17。

本文件收錄已確認規範、標準實作範圍及 context 實作評估。2026-10-17 最新決策確認 Member／Checkout／Order 標記，Token.IsTest 建立時繼承 Member.IsTest 並保存，Cart 排除；建立 Checkout 時提供 PaymentId 並固定用途。同日已確認採用 scoped injection；標準流程的具體 contract 已整理為 [Phase 1 定案基準](../../spec/tip-phase1.md)。Context／結果模型、Core 欄位、白名單與付款前移已交付；host、封存與效果控制留待 Phase 2。下列研究筆記以正式規格為準。

<a id="phase1-interface-proposal"></a>

## Phase 1 Interface（已定案）

2026-10-17 使用者啟動 Phase 1，要求先列出 `.Abstract` 受影響的 interface。使用者已確認 IdentityContext 與 TransactionContext 各自獨立建模，其有效範圍可能不同；建議第一版提供兩個獨立讀取介面，並補齊既有事件介面的執行邊界契約。已具體化為 [Context 合約](../../spec/tip-context-contracts.md)：IIdentityContext.Current 回傳 IdentitySnapshot，ITransactionContext.Current 回傳 nullable TransactionSnapshot；Core 初始化、綁定與清除已實作，host 與 DI 接入仍待後續。

| Interface | 變更 | 責任與影響 |
|---|---|---|
| `IIdentityContext` | 新增 | 提供目前執行身分的唯讀資訊，包含主體種類、必要 ID 與用途；可獨立存在於沒有交易的操作。 |
| `ITransactionContext` | 新增 | 提供目前處理的 Checkout／Order 之 ID 與用途；綁定、恢復與有效範圍獨立於 identity，一般操作可沒有目前交易。 |
| `IOrderEventDispatcher` | 補齊行為契約，保留兩個 `Dispatch` 簽章 | 每次處理事件使用獨立 scope，從事件的 OrderId 恢復 Order 用途，原 HTTP request 結束後仍可處理；既有事件已有 OrderId，第一版可由資料庫恢復用途。同步方法亦可表達這個邊界。 |
| `IProductService` | 保留簽章 | 維持商品查詢責任；本次標準商品查詢沒有新增 context 參數的需求。 |
| `IDiscountRule` | 保留簽章 | 維持 CartContext 輸入與折扣計算，TiP 結帳仍執行正常計算。 |
| `IShopManifestResolver` | 保留簽章 | 維持 shop 啟動設定解析，執行用途屬於每次操作的 context。 |

兩個 context 分開注入，服務依所需資訊選擇依賴；初始化及綁定由 Core 提供受控操作。操作結果彙整另依本次操作範圍設計，不把結果當成合併 identity／transaction 的理由。讀取介面與 models 已建立，另新增 Abstract 的 OperationResult／OperationOutcome／SkippedEffect；HTTP 出口與操作結果已依 [正式合約](../../spec/tip-http-and-operation-results.md) 定案，接入留待 Phase 2。

目前 `CheckoutService`、Member、CheckoutTransactionRecord、Order 與 `IShopDatabaseContext` 均在 Core。其用途欄位、PaymentId、資料保護與 DI 規則已納入 Phase 1 規格，但不因本次 TiP 自動搬入 Abstract 或各新增一個公開 interface。商品／SKU 維護與白名單判定第一版可採 Core concrete service，若形成跨模組擴充需求再評估公開 contract。

<a id="core-field-definition"></a>

## Core 欄位定義（已實作欄位與儲存入口）

依據：[Core 欄位與明確會員輸入](../decisions/2026-10-17-tip-core-fields-and-explicit-member.md)。目前四種物件的 IsTest、Checkout.PaymentId、Order.CheckoutTransactionId 與封存 collection 已實作，詳見 [物件規格](../../spec/tip-core-object-definitions.md)。Command 付款前移與舊紀錄 409 已接入服務；用途繼承、自動封存與 scoped host 接入留待 Phase 2。

| 模型／欄位 | 狀態與規則 |
|---|---|
| Member.IsTest | 已確認，bool，註冊時依 Member.Name 白名單自動指定，建立後不可變。 |
| CheckoutTransactionRecord.IsTest | 已確認，bool，建立時固定交易用途。 |
| CheckoutTransactionRecord.PaymentId | 已確認，int，create 時提供並保存供 complete 使用。 |
| Order.IsTest | 已確認，bool，承接 Checkout 用途。 |
| MemberAccessTokenRecord.IsTest | 已確認，bool，建立時從 Member.IsTest 複製並保存，用途不可變且保持一致。 |
| Order.PaymentId | 不新增；透過關聯的封存 Checkout 取得付款來源。 |
| CheckoutTransactionRecord.ClosedAt | 追加 nullable UTC 結束時間，complete／cancel 使用；填值流程留 Phase 2。 |
| Order.CreatedAt | 追加 nullable UTC 建立時間，舊資料未知為 null。 |
| Order.CheckoutTransactionId | 已確認新增 int?；Checkout 建立的 Order 填入交易 ID，其他來源為 null。 |
| CheckoutCreateCommand.PaymentId | 已確認新增。 |
| CheckoutCompleteCommand.PaymentId | 已確認移除，從保存的 Checkout 取得。 |
| Create／Complete command.RequestMember | 保留必填，由 API／CLI 明確組合，不以注入 IdentityContext 取代。 |
| CheckoutCompleteResult.PaymentId | 保留，改以 Checkout 保存值回傳。 |

Member 用途不可變，Order.Buyer 為其快照；正式會員使用測試付款來源時，Buyer.IsTest 可為 false、Order.IsTest 為 true。Cart／明細與 Product／SKU／Inventory 不新增用途欄位。FulfillmentStatus 暫維持現有值，操作略過結果由 OperationResult／SkippedEffect 表達。

已確認作用中的 Checkout 與封存交易紀錄分區保存，Order 可透過關聯查回封存的 CheckoutTransactionRecord；封存保留原交易 ID、IsTest 與 PaymentId。現行程式成功後直接刪除 Checkout，仍須改版達成此方向。

實作建議為同一 shop database 的不同 collection，將 Order 建立、依用途處理庫存、封存及移除作用中紀錄一起提交或回滾。Complete 僅處理作用中紀錄；成立前失敗保留作用中 Checkout，成立後 fulfillment 失敗保留 Order 與封存紀錄。Order 透過 nullable CheckoutTransactionId 關聯；封存入口已定義為 ArchivedCheckoutTransactions／archived_checkout_transactions，保留原 CreatedAt，重複 complete 維持 TransactionNotFound／400；須避免交易 ID 因移出作用中區而被重用。

<a id="standard-context-evaluation"></a>

## 標準 Context 實作評估（Scoped injection 已接受）

依據：[標準實作範圍與 Context 方向](../decisions/2026-10-17-tip-standard-scope-and-context.md)。使用者已接受 scoped injection，以及 identity／transaction 分開建模、依執行情境決定 scope。名稱、讀取契約、endpoint metadata 與 body 解析規則見 [Context](../../spec/tip-context-contracts.md) 與 [HTTP 合約](../../spec/tip-http-and-operation-results.md)。

### 獨立 Context、Scoped injection 與 Middleware 規則已定案

`HttpContext.Items` 是單次 HTTP request 的資料容器。建議 middleware 分別解析 identity 與 transaction，把關鍵 ID 與用途摘要交給各自 context；Items 與 Core 對同一種 context 使用同一份資料，避免重複維護用途。兩個 context 的初始化與綁定時機各自獨立。Token 可沿用既有 request key；驗證 Token 並解析其 Member，確認兩者 IsTest 一致後形成可信 identity。API 以此組合 command 的 RequestMember，底層 command 保留明確會員輸入。

| 做法 | 適用性與取捨 |
|---|---|
| Core 直接讀 IHttpContextAccessor／Items | HTTP 端修改最少，但 ConsoleUI 與事件端無法自然使用；不建議作為共用 Core 契約。 |
| 分開的 scoped context，HTTP Items 各自引用對應資料 | 已接受。兩者獨立建模，依執行情境決定 DI scope；在單次 HTTP 操作中可共用 request scope，事件處理建立新 scope。 |
| AsyncLocal ambient context | 可支援深層同步／await 呼叫，但事件交接仍需明確建立與還原範圍；本輪優先採 scoped，避免引入不必要的隱式傳遞。 |

兩個 context 及操作結果的語意分別為：

- Identity：執行主體種類、MemberId（會員身分時）、由 Member 決定的用途；事件端可為系統身分。
- Transaction：種類（Checkout 或 Order）、ID、從持久化物件取得的用途；一般 request 可沒有 transaction。
- Result：實際操作結果與已發生的略過紀錄摘要，供出口表達；知道 test 不等於所有操作都被略過。

只保存 ID 也能在使用時查詢，但建議保存已解析的用途快照。依已確認方向，成功 Checkout 將由作用中區移至封存區；快照讓本次操作的出口不必重新查詢不同儲存區，仍能取得原交易 ID／用途及結果。事件端則由持久化的 Order 恢復交易用途。

### Context 有效範圍與 DI Scope

兩個 context 沒有固定的一對一或父子關係：

- 商品查詢可只有 identity；建立 Checkout 時先有 identity，建立成功後才綁定 transaction。
- 同一執行身分可處理多筆交易，每筆交易有自己的處理範圍。
- 同一筆交易交給事件處理器後，交易用途延續，執行身分可改為系統。
- 交易跨 request／事件延續時，每次從持久化資料恢復 context；不保持同一個 DI instance 跨 request 存活。

獨立建模不要求每次都建立兩個 DI scope。第一版的單筆 HTTP 操作可在 request scope 中提供兩個獨立 context；遇到批次、巢狀交易處理或事件邊界，再依實際需求建立隔離的操作 scope。具體批次與巢狀 API 不納入本輪新增功能。

.NET 內建 DI scope 沒有自動的父子繼承關係；新交易處理 scope 不會自動取得外層 identity。需要延續身分時明確傳入不可變的身分資料／快照，在新 scope 初始化；事件改由系統執行時則建立系統 identity。新 scope 的服務也須在該 scope 內解析，避免沿用外層已捕捉 context 的服務。處理多筆交易時，不讓並行操作共用可變 transaction 綁定。

### Middleware 依 endpoint 明確解析

建議將用途 middleware 放在 routing 之後，透過 endpoint metadata 指定交易種類、ID 來源及欄位；不用通用規則猜所有名為 `id` 的參數。ID 是輸入，物件用途從資料庫解析，買方與其他業務授權檢查仍須執行。

| 入口 | Context 建立方式 |
|---|---|
| 會員、商品、Cart 等一般操作 | 解析適用的 identity，transaction 不適用；CartId 不當作交易 ID。匿名查詢的 identity 明確為匿名，不憑空建立 Member。 |
| `POST /api/checkout/create` | 入口尚無 Checkout ID；先解析 identity，應用服務依 Member 與 request PaymentId 建立固定用途的 Checkout，成功後補入 transaction context。 |
| `POST /api/checkout/complete` | 依 JSON body 的 TransactionId 載入 Checkout，取得其既定用途與 PaymentId；不重新套用白名單分類。 |
| 明確指向單一 Order 的 request | 若提供此類 endpoint，依 metadata 指定的 OrderId 載入 Order。標準 API 目前沒有單筆訂單 endpoint，不為 context 額外新增。 |
| `GET /api/member/orders` | 是集合查詢，不設單一 current transaction；每筆 Order 保留自身用途。 |

現有 TransactionId 在 body，middleware 比 MVC model binding 更早執行。沿用現有路徑的方案是只對標記端點使用 `EnableBuffering()`，採與 MVC 一致的 JSON 規則解析已知 DTO／欄位，讀完 rewind，再交由 controller 正常 binding；設定合理讀取上限。不要讓 middleware 與 action 對同一 payload 解出不同 ID。這是可行但有重複解析成本的方案。

替代方案是 action filter 在 model binding 後從已解析參數建立 transaction context，仍可寫入相同 Items；這能省去 body 重讀，但 transaction 解析不會全在 middleware。改成 route ID 則便於 middleware 解析，但會擴大公開 API 變更。本輪若維持使用者提出的 middleware 方向，優先沿用 body 與限定端點解析。

錯誤分類須保留：無效輸入、無效 token、找不到交易、買方不符，依正常輸入／授權／業務錯誤處理；已成功載入且應具備用途的物件卻缺少或矛盾，才是用途不明的系統錯誤。Context 解析不代表授權已通過，也不能以解析失敗默認正式。

### PaymentId 與用途固定時機

已確認的 Checkout 建立規則：

| Member 用途 | PaymentId 是否命中白名單 | 新 Checkout／Order 用途 |
|---|---|---|
| 正式 | 否 | 正式 |
| 正式 | 是 | test |
| test | 否 | test |
| test | 是 | test |

已確認 PaymentId 隨 Checkout 保存，complete 移除重複輸入並讀取已保存值；CommonStorefront 的 PaymentId 產生移至 create 前，ConsoleUI 亦同步。Create／Complete command 保留 RequestMember，由呼叫端明確組合；不以注入 IdentityContext 取代必填輸入。Member.Name／PaymentId 白名單可預先定義於 code 或 database，第一版採 code 內集中定義。Member 註冊時依 Name 是否命中自動設定 IsTest；PaymentId 僅為數值，不建立付款物件，Checkout create 當下查清單。白名單用於建立時分類；已建立 Checkout 與後續 Order／事件不因白名單變更而換用途。

### 事件處理器建立自己的範圍

事件每次處理建立獨立 DI scope 已納入 scoped injection 決策。初始化建議為：在 dispatcher／handler 邊界設定明確的系統 identity，以事件 OrderId 從已保存 Order 恢復 transaction context，再執行既有 handler；若事件同時攜帶用途，須驗證一致。現在即使同步 dispatch，也使用獨立 context，返回後不改變外層 HTTP context。

事件只傳遞必要 ID／值與關聯資訊，不攜帶 HttpContext 或 token；HTTP request 結束後仍可重送事件。第一版不必新增 queue、worker 或 outbox；跨 process transport 是後續事項。事件 handler 中仍照常檢查業務條件，在效果發生前判定用途。

### 服務生命週期與出口

目前程式仍將 API 的 CheckoutService 註冊為 singleton；已接受的改版方向為 scoped injection，CheckoutService 與直接依賴 scoped context 的服務須配合改為 scoped。HTTP 在 request scope 內解析服務；事件每次處理與 CLI 每次操作建立並釋放獨立 scope。Singleton dispatcher 若保留為交接入口，應透過 scope factory 在新 scope 內解析 handler，不直接捕捉 scoped context／handler。其餘不依賴 context 的服務不必一律更換 lifetime。此處為已接受的設計，尚未修改程式註冊。

無目前交易時，不輸出交易相關 header；物件中的 transaction context 為 null，identity 仍可存在。HTTP 出口 middleware 須在 response 開始前輸出適用的用途與結果。若採 header，建議事先註冊 `OnStarting`，在送出前讀取最終 context；不要等 `await next()` 結束後才假設 header 仍可修改。事件／CLI 使用相同結果語意，不依賴 HTTP response。

### 技術依據

- [Dependency injection guidelines](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-guidelines)：內建 DI scopes 不具有自動父子繼承關係。
- [ASP.NET Core HttpContext](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/use-http-context?view=aspnetcore-10.0)：Items 的 request 範圍、body buffering 與 HttpContext 跨背景工作限制。
- [ASP.NET Core Routing](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0)：routing 後讀取 endpoint 與 metadata。
- [ASP.NET Core Filters](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-10.0)：action filter 可存取已完成 model binding 的參數。
- [EnableBuffering](https://devblogs.microsoft.com/dotnet/re-reading-asp-net-core-request-bodies-with-enablebuffering/)：middleware 重讀 body 時啟用 buffering 並還原位置。

## 架構 review 收斂結果

2026-10-17 使用者確認以下邊界，完整紀錄見[研究範圍與架構規範邊界決策](../decisions/2026-10-17-tip-contract-and-context-boundaries.md)。編號對應先前八項架構議題。

| 原議題 | 已確認規範／目前狀態 |
|---|---|
| (1)、(2) 物件用途與保存 | 最低層系統服務介面將不可變物件的 `id + type` 視為 immutable，同一身分的用途永遠一致。保證機制交由實作端；create 權限由應用端介面決定。 |
| (3)、(4) Context 與商業效果控制 | 已區分 current identity 與 current transaction；架構確保資訊正確傳遞，應用端各環節依 context 執行政策，在商業效果發生前完成判定。 |
| (5) 系統工作邊界 | 以 event-driven／task queue 等交接機制隔離查詢與系統到期整理兩端，具體基礎設施屬實作議題。 |
| (6) 對外結果 | Context 延續到系統內最後一步，離開系統前判定，再以 header／result code 等明確契約對外傳遞。HTTP API 由 middleware 統一處理出口表達。 |
| (7) Log | 沿用正常 log 機制，由 context 與判定結果決定對應紀錄內容。 |
| (8) 資料使用 | 在 database schema 明確定義各類物件的正式／測試用途及適用規則，查詢與統計依此使用。 |

`type` 在此指正式／測試用途，並非已選定的程式欄位名稱；穩定的 ID 與用途關係不要求 ID 字串編碼用途。領域分類及 AC 已依最新標準範圍調整；用途不可變與效果控制原則維持。Schema 表達、不可變性防護、queue 與 log 工具等實作事項，保留作實作時檢查，不逐項展開為本輪研究的必要決策。

## 鏈路上的測試模式識別

使用者提出：除了測試 Member，付款工具、API key 或入口也可能提供測試性質的判定依據；需要類似 HTTP context 或追蹤資訊的設計，讓整條鏈路識別是否處於 test mode，用以控制衍生行為與 log。

2026-10-17 補充確認兩類 context 描述：

| Context | 所描述的資訊 |
|---|---|
| current identity | 目前以什麼身分執行，以及該身分／憑證的 test 標記；涵蓋 Member、API access token 或系統排程工作身分。 |
| current transaction | 目前替哪筆交易執行，以及該交易的用途；須涵蓋其後續事件與重試。 |

改價等操作可依 current identity 判定，不必擴大交易定義來涵蓋所有操作。具體型別與欄位已納入 Context 合約。依 AC-TIP-013，正式系統身分處理 test 訂單仍受 test 限制，因此 `currentuser.isTest` 不能作為所有操作唯一的判斷依據。

身分與交易是不同維度，值不同不自動等同用途矛盾；各自不明、遺失或與其已確定用途矛盾仍屬 AC-TIP-014 的 BUG。不涉及交易的操作，須與交易執行途中遺失 context 區分。AC-TIP-012 的系統工作交接依兩端各自責任建立執行範圍，不能以清空資訊代替責任判定。本輪已確定 Checkout 建立時的 Member／PaymentId 組合，其他完整用途組合政策仍列為後續應用規格，不自動採用跨所有操作的通用 OR 規則。

已確認的責任分工與設計約束：

- 區分測試判定來源、這次操作的執行模式，以及領域物件已確定的用途。
- Trace／Correlation ID 用於關聯操作；執行模式用於決定允許的效果，兩者語意不同。
- 模式識別需涵蓋同步呼叫、事件、背景工作與重試，不能只依賴原 HTTP request 的存活時間。
- 各商業操作依一致的判定控制效果；測試身分仍須通過必要權限與業務檢查。
- 架構提供標準存取與傳遞管道，確保 context 資訊正確；應用端確保每個相關環節依 context 執行業務政策，在效果發生前判定。

最低層介面的用途不可變契約提供 context 的判定依據。標準訂單事件由 OrderId 恢復持久化用途，實際執行時必須能確認用途，用途遺失或判定矛盾屬於 BUG。本輪已確認 PaymentId 白名單代表測試卡號，建立 Checkout 時即提供並固定用途；AC-TIP-002 改為此 PoC 驗收方向，不要求引入真實金流。其他來源組合仍待後續規格。

AC-TIP-013、AC-TIP-014 已接受：測試訂單的後續事件及重試維持測試用途，且用途必須明確，用途不明屬於 BUG；具體 context 型別與用途 BUG 的 500／execution_context_invalid 已納入正式規格。

## 呼叫端可用程式辨識 test 行為與結果

使用者在接受 [AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005) 時指出：要求改價為 80 元、讀回卻仍是 100 元，可能讓原本預期資料被修改的測試失敗。因此，修改價格的 API／CLI／interface／UI 等入口，需要提供明確且能由系統判讀的方式，讓測試端辨識這是 test behavior。

2026-10-17 已確認：context 延續到系統內最後一個步驟，在離開系統前判定；跨出系統後以明確的 header／result code 等契約傳遞，HTTP API 由 middleware 統一處理出口表達。無交易時不給交易相關 header、物件中的 transaction 為 null；header 名稱、result code 與回傳格式見 [HTTP 與結果合約](../../spec/tip-http-and-operation-results.md)；此為當時結果識別的背景；2026-10-19 已修訂 AC-TIP-005 為「test identity 明確拒絕改價、記錄拒絕原因」，取代成功略過政策，見 [Phase 3 review](phase3-spec-review.md)。

商業效果仍在各操作發生前受到控制；出口依 context 與已完成的判定結果產生一致表達。測試會員操作 Cart 可實際寫入，Cart 本身不標記用途；test identity 對 Product 改價被拒絕、test Checkout 可因庫存不足失敗，因此對外結果需承接實際操作結果。具體資訊格式已納入 OperationResult 與 HTTP 合約。

本次正式 contract 已具體化：

- 呼叫端如何識別此次操作的正式／測試性質。
- 如何表達操作實際結果，例如已套用、因 test 略過，或正常業務檢查失敗；僅知道 test 不代表操作一定成功。
- 不同入口如何提供一致的可判讀語意，以及如何對應到操作或交易紀錄。
- 測試端如何依結果驗證預期行為，避免把正常的 test 略過誤判為一般更新失敗。

正式 enum、HTTP status、header 與結果格式以 [HTTP 與結果合約](../../spec/tip-http-and-operation-results.md) 為準；Outcome 與用途分開，略過明細只記錄實際到達且未執行的效果。

## 系統到期整理與測試來源的邊界

使用者在討論 AC-TIP-012 時選擇：測試會員發現正式預約真正逾期，可以發出不以該會員為執行主體的通知，交由系統正常清理。詳見[系統到期整理決策](../decisions/2026-10-16-tip-system-expiration-boundary.md)。[AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012) 已依此修正保留並接受；使用者要求現行 PetShop 不符合的部分記入架構變更，留待實作處理。

現行程式是查詢同步呼叫 `ExpireHold` 後直接更新資料庫，沒有對應的非同步事件或清理 worker；更新只涉及 `Status` 與 `UpdatedAt`，保留 Reservation 與 Product。

2026-10-17 已確認以 event-driven／task queue 等機制作為兩端交接邊界。責任原則已收斂，以下為後續 contract 與實作須落實的約束：

- 明確區分查詢者、通知來源與系統工作執行主體；系統依正式資料及時間重新判斷到期條件。
- 事件不帶會員資訊不自動代表解除 TiP 限制；須由明確的業務責任決定，避免測試交易效果僅換成事件就被放行。
- 不計名的業務語意與 trace／correlation 資訊如何並存，具體 context 與 log 格式未定。
- 實作事項：非同步通知的傳遞方式與系統處理完成的判定；需處理延遲、重複通知及與確認／取消的狀態競爭，是否另由排程喚起同一套到期整理也由實作設計處理。

<a id="petshop-expiration-change"></a>

### PetShop 架構變更：到期事件與系統處理邊界

狀態：事件／任務交接與責任原則已確認；具體 context 承接契約與機制待規格及實作設計。依據：[AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012)。本次僅記錄，未修改 source code、`/spec` 或 `.Abstract`。

| 範圍 | 現行差距 | 後續變更要求 |
|---|---|---|
| 查詢與寫入 | `ApplyReservationProductPolicy` 直接同步呼叫 `ExpireHold` 寫入資料庫。 | 查詢照常判定商品失效，並發出到期通知；由系統處理到期更新，建立明確的執行邊界。 |
| 到期通知與處理者 | 沒有對應的非同步到期事件或清理 worker；現有 `PetShopOrderEventDispatcher` 處理訂單事件。 | 以事件／任務交接建立到期通知與系統處理能力；具體用途傳遞契約待規格階段，transport、執行載體及是否搭配排程屬實作事項。 |
| TiP 行為歸屬 | 現行查詢未傳入查詢者 Member／Token，亦未建立 test context 與系統工作的區分。 | 保留 B 查詢的 test 性質，同時讓系統依 A 的正式預約資料及真實到期規則完成整理；不靠遺失會員資訊來判定可放行。 |
| 驗收完成點 | 同步方法返回後即可觀察寫入，沒有事件處理完成的觀察方式。 | 能等待並確認對應系統工作完成，再驗證 `Expired` 已持久化；不可只確認通知送出就宣稱清理成功。 |

實作範圍以本案例的正常到期整理為限，沿用 `Holding -> Expired` 並保留預約及商品。既有查詢、取消、訂單確認等呼叫到期整理的路徑，需在架構整理時一併檢查，避免留下不一致的責任判定。

### PetShop 流程邊界：測試預約不接續購物車與結帳

使用者已決定：測試 `CreateHold` 略過 Reservation／Product 建立後，不繼續該筆預約的 Cart／Checkout。[AC-TIP-015](tip-acceptance-criteria.md#ac-tip-015) 完整案例已接受。

後續實作須讓預約入口及呼叫端辨識「已驗證但因 test 未建立預約」，正常停止此流程，不將未建立的預約當成可購買商品。具體 response header、result code 與 UI 呈現沿用已確認的出口表達原則，留給正式 contract 與實作；不引入臨時 Product 或另一套虛擬預約交易來接續結帳。

## 開工前最後確認

2026-10-17 已確認 Member.Name／PaymentId 白名單、Order.CheckoutTransactionId nullable 關聯、無交易時省略交易 header／物件為 null，以及既有資料一律 IsTest = false。舊資料不因白名單重新分類；缺少歷史 Checkout 的 Order 不捏造封存紀錄。

使用者指定先開始變更 code，再落入 `/spec` 與 `/spec/testcases`，本輪不以先凍結 spec 作為前置條件。既有設計方向不變，具體介面成員與格式由實作收斂並同步文件。

## 本輪收斂後保留的事項

標準實作的 Phase 1 已完成。以下區分 Phase 2 實作與範圍外議題：

- **已定案政策**：標準流程的 identity／transaction 用途來源與四種會員／付款組合已落入 spec；範圍外來源仍須另定規格。
- **Phase 2 實作**：依定案 Context、HTTP／結果、schema／log 規則接入 Core 與 host；本輪不要求 queue／worker。PetShop 的到期交接另案處理。
- **未來功能擴充**：真實信用卡等額外來源的驗證及用途相容規則；AC-TIP-002 已以 PaymentId 白名單納入標準 PoC。

## 與 AC 的關係

- [AC-TIP-001](tip-acceptance-criteria.md#ac-tip-001)：正常完成測試交易，控制並記錄略過的商業效果。
- [AC-TIP-003](tip-acceptance-criteria.md#ac-tip-003)：正常驗證失敗，沿用一般失敗紀錄，Checkout 自身保留 test 用途。
- [AC-TIP-004](tip-acceptance-criteria.md#ac-tip-004)：已有物件用途不可變，context 設計不能默默改寫用途。
- [AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005)：資料不異動的結果已接受；呼叫端依 OperationResult 與 HTTP 合約辨識原因。
- [AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012)：測試查詢允許通知系統執行正常到期整理；PetShop 的事件與系統執行邊界列為待實作變更。
- [AC-TIP-013](tip-acceptance-criteria.md#ac-tip-013)、[AC-TIP-014](tip-acceptance-criteria.md#ac-tip-014)、[AC-TIP-015](tip-acceptance-criteria.md#ac-tip-015)：完整案例已一併接受，分別約束事件與重試、用途明確性及測試預約流程邊界。

## TDD 進度

Core 白名單位置已定為 `Testing/TestAllowlist.cs`，預設 Member.Name test、PaymentId 990001，清單建立後唯讀。紅燈階段確認 21 項新測試因 NotImplementedException 失敗；使用者確認後已補上白名單查詢與 Context 保存／綁定／清除，測試沿用原有行為斷言；參見 [白名單規格](../../spec/tip-test-allowlist.md) 與 [Context 合約](../../spec/tip-context-contracts.md)。後續 Phase 1 收尾已修改 Checkout command，付款前移並補舊交易 409；host 與 DI 接入仍留待 Phase 2。

## 核帳 Schema 補充

上述時間欄位由使用者明確追加，為本次有限範圍的 Phase 1 修訂。後續已確認 State、create 時固定的獨立金額／明細，以及終態完整封存後凍結；商品名稱／描述／規格不複製至 Checkout。詳見 [Schema 檢驗](accounting-schema-review.md) 與 [正式規格](../../spec/tip-checkout-lifecycle.md)，查詢功能後續再做。

## 未來訂單後置處理（2026-10-18）

未來擴充 `/api/orders/{id}/xxx` 進行出貨等 checkout 後置處理時，必須附帶 current transaction，從保存的 Order 恢復 Kind／Id／IsTest，並驗證存取權限。轉為事件／任務時也須在獨立 scope 恢復相同用途。目前沒有這些 endpoint／任務，本次僅記錄，不新增實作。詳見[確認決策](../decisions/2026-10-18-tip-order-postprocessing-context.md)。
