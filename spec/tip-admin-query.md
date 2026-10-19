# TiP 管理查核 API 規格

2026-10-19 修訂：交易 collection 隔離與指定身分限制已同步至本規格並獲使用者確認，見 [Phase 3 review](../docs/tip/phase3-spec-review.md)。業務原則已確認；新 storage 與改價拒絕尚未實作。既有 Phase 1／2 的交付數字及紀錄僅代表當時版本。

日期：2026-10-18。歸屬：[Phase 1.1](tip-phase1.1.md)。狀態：Phase 1.1 規格定案；API 已實作及驗證，Docker 映像已重建，使用者手動查詢通過，見 [交付紀錄](../docs/tip/phase1.1-delivery.md)。

## 目的與邊界

查詢總訂單金額、訂單清單、結帳清單、會員清單。由作用中 API 的資料庫服務讀取已提交資料，不要求停止 container、不透過宿主機 DB extension，不提供任意 SQL 或資料修改能力。

適用標準 Core／API。需求僅有 API，沒有 Web UI；使用者已於 Phase 1.2 追加 .NET CLI 設計，其命令合約由新規格獨立追蹤。CSV 匯出、外部金流對帳、退款／沖銷與獨立報表資料庫不納入。此處「總訂單金額」是成立訂單的保存金額，不表示實收收入。

## 獨立 admin 認證

已確認規則：

- `/api/admin/*` 使用預先指定的 admin access token，與 `member_tokens` 完全分開。
- token 格式為 `admin_` 加上一個以上的小寫 ASCII 英數字元，即 `^admin_[a-z0-9]+$`；不接受大寫、空白、連字號或額外底線，不做大小寫轉換或 trim 後比對。
- 格式正確不代表有權限，必須與預先設定的完整 token 精確相等。產生設定值時建議使用 32 bytes 隨機值的小寫 hex，不使用固定 demo token。
- 會員 token 不可呼叫 admin API；admin token 也不等同會員 token，不可用於 checkout 或會員入口。不得以名稱 `admin`、Member.IsTest、白名單或 token 前綴單獨授權。
- 不將 admin token 寫入會員／Token collection、URL、回應、log 或版控。比對使用固定時間的秘密比對方式。
- admin 唯讀查詢不冒充會員或系統交易執行，不新增 Abstract IdentityKind；集合查詢無 current transaction，不輸出單筆交易 headers。

已定案設定方式：

- 使用 `Authorization: Bearer <admin token>`；不接受 query string、cookie 或 body 代傳。
- API 啟動時讀取 `SHOP_ADMIN_ACCESS_TOKEN`。未設定或空字串時停用 admin 入口，回 404；有值但格式不合法則啟動失敗，訊息不得帶出秘密。
- 已啟用時，缺少、錯誤、多值或格式不合法的 Authorization 回 401，且不執行查詢；不向會員認證 fallback。
- token 變更於 API 重新啟動後生效；沒有註冊、簽發、refresh 或 token 管理 API。

## Endpoints

| Method／path | 功能 |
|---|---|
| GET /api/admin/orders | 訂單分頁、符合條件的總筆數及總金額。 |
| GET /api/admin/checkouts | 合併作用中與封存的 Checkout 分頁。 |
| GET /api/admin/members | 會員分頁。 |

不新增單筆 Order／Checkout 操作端點；未來訂單出貨等後置處理必須承接交易 context 的既有決策不變。所有回應（含錯誤）使用 `Cache-Control: no-store`，每次請求重新查詢，不使用上次結果作為成功回應。

## 篩選與分頁

共用參數：

| 參數 | 規則 |
|---|---|
| purpose | normal（預設）、test、all；依各筆資料自身 IsTest 判定。 |
| page | 正整數，預設 1。 |
| pageSize | 1～100，預設 50。 |

訂單另接受 `memberId`（正整數）、`from`、`to`。結帳另接受 `memberId`、`state`（all／unknown／created／completed／cancelled，預設 all）、`timeField`（createdAt／closedAt，預設 createdAt）、`from`、`to`。會員另接受 `memberId` 與 `name`（依現有資料庫 collation 精確名稱比對，兩者同時提供時取交集）。訂單、結帳不以當前會員名稱重建歷史買方資訊。

- 時間使用 ISO 8601 且必須帶 Z 或 offset；換成 UTC 後採 `[from, to)`，可只給單側。訂單時間欄位固定為 CreatedAt。
- 提供雙側時 from 必須小於 to。非法枚舉、時間、分頁、memberId、重複或不支援的參數回 400，避免拼錯條件卻查全部。
- ID 由大至小排序；Checkout 單用途合併 active／archived 兩區，all 合併四區；Order all 合併兩用途。依 [交易儲存規格](tip-transaction-storage.md) 選區、取得一致集合後才排序／分頁。超過最後一頁回空 items，總筆數與統計保留。
- 不跨頁保留快照；有新資料時重查頁面可改變。需要保存某次結果時由 CLI 保存 JSON。

## 統計口徑與舊資料

- 正式訂單總額只加總 `Order.IsTest=false` 的保存 TotalPrice，日期使用 Order.CreatedAt。Buyer.IsTest、目前 token 或白名單均不得取代訂單用途。
- 總筆數、總金額與 items 使用同一篩選集合；總額涵蓋所有符合條件的訂單，不受分頁影響。FulfillmentStatus=Failed 仍是已成立訂單，不直接從總額扣除。
- 舊 BSON 缺少 IsTest 視為 false；其他無法判讀的用途值是資料異常，回 500，不靜默改為正式。
- 時間／金額／狀態缺失不得補現在時間、0 或 Completed。缺少付款與 Checkout 關聯保留 null。Member 本身沒有建立時間，不新增會員日期查詢。
- 無日期篩選時，未知時間資料仍可列出。存在日期界線時，該時間未知者排除，並回傳 excludedUnknownTimeCount；此計數先套用用途、會員與狀態等非時間條件。
- 符合最終條件的訂單若有未知金額，totalAmount 回 null；knownTotalAmount 仍回傳已知金額之和，unknownAmountCount 表示未知筆數。無符合資料時兩個金額皆為 0。
- 未知日期或關聯的舊訂單可以參與無日期條件的「訂單金額」，但不能宣稱已與 Checkout 核對完成。
- 結帳集合中的 Created、Cancelled、Unknown 不列為成交；核對成交時使用 state=completed、timeField=closedAt。API 不把全部 Checkout 的金額加成營收。
- 訂單與 Checkout 是同一成交的兩份紀錄，用 CheckoutTransactionId 核對，不能將兩份總額相加。

## 回應 schema

成功回應為 JSON，欄位 camelCase，日期為 UTC ISO 8601，金額為 JSON number（decimal），未知值明確為 null。枚舉採下列字串，不回 C# 整數值。

共用 envelope：

| 欄位 | 型別／語意 |
|---|---|
| queriedAt | UTC 查詢時間；供追蹤，不是可重用的資料庫快照版本。 |
| filters | 有效篩選值，包含預設值及已換成 UTC 的時間；不含 token。 |
| page、pageSize | 本次分頁值。 |
| totalCount | 所有符合條件的筆數，分頁前計算。 |
| excludedUnknownTimeCount | 因指定日期區間而排除的未知時間筆數；無日期條件或會員查詢為 0。 |
| items | 當頁資料。 |

orders 額外回傳 `totalAmount: decimal?`、`knownTotalAmount: decimal`、`unknownAmountCount: int`。列欄位：`id`、`memberId`、`memberName`（保存的 Buyer 名稱）、`isTest`、`createdAt`、`totalPrice`、`fulfillmentStatus`（pending／succeeded／failed）、`checkoutTransactionId`。舊資料缺買方欄位以 null 表示。

checkouts 列欄位：`transactionId`、`memberId`、`cartId`、`paymentId`、`isTest`、`state`（unknown／created／completed／cancelled）、`createdAt`、`closedAt`、`totalPrice`、`storage`（active／archived）。舊資料缺狀態為 unknown，缺時間／金額／PaymentId 為 null。

members 列欄位：`id`、`name`、`isTest`。不回傳 token、ShopNotes 或其他無關資料。

第一版列資料不包含商品／折扣明細，也不提供自動對帳結論；可用清單核對關聯、用途與總額。若未來加入逐項對帳，須在服務端一致讀取中比對，不能將兩個不同時間的 HTTP 回應拼成一致快照。

錯誤 body：`{ "code": "...", "message": "..." }`。400 為 admin_query_invalid，401 為 admin_unauthorized，停用 404 為 admin_disabled；不可判讀資料為 500 / admin_data_invalid，基礎設施故障為 500 / admin_query_failed。錯誤訊息不得含秘密、連線字串或堆疊。已啟用入口的未知 route 仍由正常 routing 回 404，不要求套此 envelope。

## 讀取一致性與副作用

Phase 3 的讀取須與 normal／test 的全部交易 writers 協調；不能將各 collection 的獨立 read version 視為跨 collection 共同快照。用途與 collection 不符或已讀資料 ID 重複須回 admin_data_invalid，不默默去重。每個回應的 items、totalCount、金額均從同一次一致讀取的集合計算，不各自執行可觀察到不同提交狀態的查詢。Checkout 完成期間查清單，只能看到移轉前的 active 或移轉後的 archived，不能漏掉、重複或看到未提交資料。

由 Core 查詢服務負責此保證。實作時須驗證 LiteDB 跨集合讀取的隔離語意；可採適當讀取協調或穩定快照策略，不能以「有呼叫 BeginTrans」取代並行驗證。不得為查詢重新分類會員、補值、更新業務資料、派送事件或產生 test skipped-effect log。索引等初始化工作屬部署／啟動，不是每次查詢的業務副作用。

不同 endpoints 與分頁請求各自反映當時已提交狀態，不承諾跨請求一致。第一版針對 PoC 規模；實作交付須記錄全量掃描／記憶體成本，不把分頁誤稱為已完成資料庫端效能最佳化。

## 後續 Admin CLI

2026-10-18 使用者開啟 [Phase 1.2](tip-phase1.2.md)，明確指定 .NET、環境變數與預設 working directory `.env`、人用 help／agent skill topics、統計／明細及 TXT／CSV／JSON。CLI canonical 合約改為 [tip-admin-cli.md](tip-admin-cli.md)，取代本節原先尚未實作的 shell 建議；HTTP API 契約不變。

原 chat 的 `admin_query` 是呼叫示例，不是獨立交付工具。新的 .NET CLI 已依 Phase 1.2 實作；其獨立測試與驗證見 [交付紀錄](../docs/tip/phase1.2-delivery.md)，不能將 Phase 1.1 API 驗收視為 CLI 測試。會員不接受日期篩選的既有規則經使用者再次確認，維持不變。

## 驗收

依 [案例](testcases/tip-admin-query.md) 驗證。規格階段不提供測試已通過的宣告；後續 API 實作記錄 HTTP、並行及部署證據；CLI 案例於使用者追加該工具時驗證。
