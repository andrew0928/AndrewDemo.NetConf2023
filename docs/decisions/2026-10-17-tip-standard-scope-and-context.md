# TiP 標準實作範圍與 Context 方向

## 狀態

accepted

確認日期：2026-10-17。使用者縮小標準實作範圍，並確認在建立 Checkout 時提供 PaymentId、建立即固定用途。同日使用者確認採用 scoped injection：context 與依賴它的 CheckoutService 採 scoped；後續確認 IdentityContext 與 TransactionContext 獨立建模，DI scope 依 HTTP、事件與 CLI 的實際執行情境決定。具體 accessor 契約、endpoint metadata 與 body 解析仍待定案，見 [Context 實作評估](../tip/tip-architecture-notes.md#standard-context-evaluation)。

**重大決策；影響 .Core。** 後續正式化涉及 **回頭修正 Phase 1；影響 .Abstract / spec**。本次更新討論文件與 AC 範圍，未修改正式 spec、contract 或程式碼。

同日使用者已明確啟動 TiP Phase 1，允許依本次範圍整理正式規格、testcases 與必要的 `.Abstract` contract。目前先列出受影響 interface 供討論；identity／transaction 分離方向已接受，具體介面名稱、簽章與 models 尚未定案，也尚未修改 contract。

後續進度：已建立 IIdentityContext／ITransactionContext 與快照 models，Core 實作已在紅燈檢查確認後補上白名單查詢及 Context 快照保存／清除，詳見 [Context 合約](../../spec/tip-context-contracts.md) 及 [TDD 決策](2026-10-17-tip-allowlist-and-tdd-checkpoint.md)。前述未修改 contract 為初次設計確認時的狀態。

## 背景

標準實作落差分析指出資料用途、Cart 會員承接、context、庫存控制及事件用途等缺口。使用者希望縮小第一版實作，沿用既有 Checkout 主流程與事件處理器；AppleBTS／PetShop 暫不納入本次實作。

本決策局部取代 [領域物件分類](2026-10-16-tip-domain-object-classification.md) 與 [架構規範邊界](2026-10-17-tip-contract-and-context-boundaries.md) 中的 Cart 標記、Token 獨立標記及付款來源延後事項。其他效果控制、必要檢查與用途不可變原則仍適用。

## 決策

1. **標記範圍縮小為 Member、Checkout、Order**：三者明確保存正式／測試用途，建立後不變。Order 承接 Checkout 用途。
2. **Token 跟隨 Member**：依後續 [Core 欄位決策](2026-10-17-tip-core-fields-and-explicit-member.md)，Token 新增 IsTest，建立時從 Member.IsTest 複製保存，保持用途一致。此項取代原先不保存 Token 標記的設計。
3. **Cart 排除用途分類**：Cart 不承接會員、不增加 test 標記；正常新增、修改、刪除明細。到 Checkout 才依交易原則判定用途。此次 TiP 改版不增加 Cart owner／會員綁定需求。
4. **沿用 Checkout 主流程**：檢查與扣庫存可維持同一路徑，在實際異動前依用途判定並留 log；不要求為 TiP 先拆出另一套結帳流程。
5. **以 PaymentId 白名單代表測試卡號**：預先設定特定 PaymentId，命中時視為測試付款來源。這是 PoC 的識別方式，不要求接入實際信用卡服務；後續已確認於 code 或 database 預設 MemberId／PaymentId 白名單；註冊時依 MemberId 設定 Member.IsTest，Checkout create 時查 PaymentId 清單，詳見 [Core 欄位決策](2026-10-17-tip-core-fields-and-explicit-member.md)。
6. **建立 Checkout 時提供 PaymentId，建立即固定用途**：使用者已選擇此時機。建立時依 Member 用途與 PaymentId 白名單決定交易用途；測試 Member 或測試 PaymentId 使交易為 test，正式 Member 搭配一般 PaymentId 為正式。這是本次 Checkout 建立規則，不擴張為所有操作的通用 OR 政策。PaymentId 及判定後用途須可供後續流程使用；既有 Checkout 不因重試或白名單變更重新分類。
7. **事件處理器視為 event／async 邊界**：沿用既有訂單事件處理器作為責任交接點，消費端須能在沒有原 HTTP request 時恢復 Order 用途。標準 PoC 不以新增 queue／worker 基礎設施為必要條件；即使同步呼叫 handler，也按此邊界處理 context。
8. **Context 方向**：HTTP request 可保留 token；明確對應單筆交易或訂單的 request，自動解析 Checkout／Order，將關鍵 ID 放入 HttpContext.Items。具體實作評估與建議另列，不把建議的類別或 key 名稱視為定案。

9. **採用 scoped injection**：identity 與 transaction 各自透過 scoped DI 提供；單次 HTTP 操作可使用同一個 request scope，HttpContext.Items 與 Core 對各種 context 使用相同資料。CheckoutService 由 singleton 改為 scoped，其他直接依賴該 context 的服務一併配合。事件每次處理、CLI 每次操作建立獨立 scope；同步 dispatch 也不共用外層 request 的可變 context。

10. **IdentityContext 與 TransactionContext 獨立建模**：兩者的有效範圍與綁定時機不必相同，不強制合併為單一執行 context。同一身分可處理多筆交易，同一交易也可交由不同身分執行。獨立建模不要求每次建立兩個 DI scope；scope 依實際處理邊界決定，跨邊界明確傳遞或恢復必要資訊。

## 影響

- Member、Checkout、Order 標記用途；Token 新增從 Member 複製的 IsTest，因此共四種物件保存用途。Cart 無用途分類，Inventory／Product／SKU 仍受保護。
- AC-TIP-001／003／004／009／010／011／015 移除 Cart test／owner 要求；Token 的 test 語意建立時由 Member.IsTest 繼承保存。原有 PetShop AC 保留，暫不納入本輪實作。
- AC-TIP-002 從未實作信用卡時 N/A 的示意，改為 PaymentId 白名單的 PoC 驗收方向；不表示真實金流隔離已驗證。
- API 的 create command／request、Checkout 持久化及 CommonStorefront／ConsoleUI 的付款 ID 產生時機須同步調整。已確認 complete 讀取已保存 PaymentId，取消重複提交；Create／Complete command 保留明確的 RequestMember，由 API／CLI 組合，詳見 [Core 欄位決策](2026-10-17-tip-core-fields-and-explicit-member.md)。
- IdentityContext 與 TransactionContext 分開注入，供 HTTP、ConsoleUI 與事件處理器依需求使用；調整相關服務的註冊與解析位置，避免 singleton 捕捉 scoped context。

## 替代方案

- 強制把 identity 與 transaction 合併為單一執行 context：經討論後改採獨立建模，允許各自的有效範圍與綁定時機。

- Cart 用途標記及會員綁定：使用者明確排除；Token 已依後續欄位決策改為保存從 Member 繼承的 IsTest。
- 到 complete 才提供 PaymentId，再改寫既有 Checkout 用途：未採用；使用者選擇 create 時提供，維持建立即不可變。
- 為驗證 event／async context 先部署 queue／worker：本輪沿用事件處理器作為邊界。
- 將庫存檢查與扣減拆成不同服務作為前置條件：不要求；同一流程中仍可正確保留檢查、控制效果。

## 後續工作

- 依 [架構設計筆記](../tip/tip-architecture-notes.md#standard-context-evaluation) 落實已接受的 context 分離與 scoped injection；具體 accessor 契約、body ID 解析與事件初始化細節留待正式設計。
- 依使用者最後確認的順序，先變更 code，再同步建立 Checkout 的 PaymentId 契約、用途欄位、白名單及對外結果至 spec／testcase，並更新標準呼叫端。
- 驗收正式／測試會員與白名單組合、用途不可變、並行隔離，以及原 request 結束後的事件重送；未經過的效果不虛構略過 log。
