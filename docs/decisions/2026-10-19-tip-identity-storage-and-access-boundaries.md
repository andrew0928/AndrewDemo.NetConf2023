# TiP 身分、儲存與存取邊界

## 狀態

accepted（交易隔離方向與責任分工）

確認日期：2026-10-19。使用者先接受 collection 拆分建議，討論 Stripe 的 API key 邊界後，最終明確決定：「我要嚴格區隔邊界，在於交易，而非更前段的認證來源」。後續已要求同步需求規格並追加 Phase 3；使用者已確認文件並要求提交，尚未實作。

同日補充確認：使用者明確接受「test identity 明確拒絕特定操作（例如變更商品價格）；test identity 可以執行交易，但是 test transaction 不得產生任何實際的業務效果」。此為已確認要求，不再只是評估解讀。

## 背景

承接 [collection 隔離評估範圍](2026-10-19-tip-collection-isolation-assessment-scope.md) 與 [roadmap](../project-roadmap.md)。原建議先拆 Order、作用中 Checkout 與封存 Checkout，保留身分資料與既有 TiP 行為。

## 決策

### 1. 交易用途建立後固定

- 交易資料依 live／test 拆 collection，涵蓋作用中 Checkout、封存 Checkout 與 Order。未來保留實體 database 隔離方向，本次 Phase 3 僅規劃 collection 分離。
- 用途在交易建立時由可信流程判定並保存；同一交易此後不得在 live／test 之間互轉。完成、封存、衍生 Order、後續事件與重試均延續原用途及對應 storage 分區。
- 保留目前建立交易的判定規則：Member.IsTest 或 PaymentId 命中白名單時建立 test 交易；正式會員／Token 可以建立、完成自己的 test 交易。
- 後續取得既有交易時，從保存的交易用途恢復 TransactionContext，不以當下 Token、白名單或 System identity 重新分類。
- 同用途 active → archived 是生命週期轉換，並非 live／test 用途互轉。
- 討論中的 live 對應規格與管理查詢的 normal；公開欄位與參數不變，collection 具體命名已收錄於已確認的 [交易儲存規格](../../spec/tip-transaction-storage.md)。

### 2. 認證與非交易資料共用

- Member／Token 保留身分用途標記，留在共用主 DB；Token.IsTest 目前繼承 Member.IsTest 的規則維持。未來認證機制可朝 OAuth 來源端移轉，本次不實作。
- Cart、Product、SKU、Inventory 等非交易資訊共用；test 交易仍可依權限讀取共用商品與庫存，執行必要的業務驗證。
- 共用指儲存與資訊來源共用，不解除既有驗證、物件歸屬與操作權限。
- 不以 Token.IsTest 決定只能讀哪一種交易 collection，也不以 identity 與 transaction 用途不同作為通用拒絕條件。

### 3. API 統一，交易路由與行為限制分工

- API 介面統一；同一 shop、同類交易物件跨用途保持唯一 ID。單筆存取集中解析 ID 所屬分區、驗證保存用途及呼叫端權限，再固定本次操作的交易路由。
- 寫入必須保證物件用途與 collection 一致；關聯 Checkout／Order 不得落在不同用途分區，缺失或矛盾不得改按另一用途繼續執行。
- TransactionContext 決定該交易的執行模式、storage 分區與商業效果政策；test transaction 不得產生任何實際業務效果，包括真實庫存扣減、付款、出貨及正式商業統計異動。正式 System identity、後續事件及重試亦不得解除限制。test 分區的交易紀錄、封存與診斷 log 仍可正常保存。
- IdentityContext 提供呼叫者身分與用途；特別點名的操作嚴格禁止 test identity 執行，已確認例子為變更商品價格，必須明確拒絕，不可回報成功但略過寫入。身分限制可以拒絕操作，但不得把既有 live 交易改成 test，或將 test 交易改成 live。
- test identity 可以執行交易，仍須通過身分、歸屬、商品與庫存等必要檢查；成功後保存 test Checkout／Order，並阻止所有實際業務效果。test 不保證交易成功，也不免除原有驗證。
- 每項操作明確指定適用政策；不新增跨所有操作的 identity／transaction 通用 OR 規則。
- 集合查詢可依已授權用途條件讀取 live、test 或明確合併；管理查核的預設正式口徑維持，會員歷史保留自己的兩種用途交易。查詢合併不改變任一交易的用途。

以上基於 [標準流程](../../spec/tip-standard-flow.md) 的既有身分、交易用途與效果分工，增加 collection 隔離並調整指定身分限制。商品改價的明確拒絕已由使用者確認；其他禁止操作的完整清單及 HTTP 回應碼留正式規格定義，不擅自擴張至所有共用資料操作。

## 影響

**重大決策；影響 .Core。** 修改範圍集中於交易資料存取、ID 配發、用途路由、結帳原子性、事件用途恢復與查詢合併；不需為此拆分會員／Token 或新增兩套登入流程。

現行 Product／SKU 維護為「必要檢查後略過寫入並記錄」，可回 Succeeded；本決策已確認商品改價須改成明確拒絕，取代先前 test 改價驗證後略過的政策。SKU 等其他操作待完整清單定義。交易模擬中依 test transaction 略過商業效果的政策另行維持，不將兩者混為同一回應語意。2026-10-19 使用者後續要求更新規格並追加 Phase 3；spec／testcases 已同步並獲使用者確認，code 尚未變更，不宣稱現行實作已符合。

後續須 **回頭修正 Phase 1；影響 .Abstract / spec**：更新儲存分區、查詢入口及指定身分限制的正式規格／案例。現有 Abstract 的 identity／transaction 分離可沿用，是否需要修改 Abstract 仍取決於正式介面設計；後續授權已更新本次相關 spec；現有 Abstract 足以承接，保持不變。具體規格與切分見 [Phase 3 計畫](../../spec/tip-phase3-plan.md)。

## 替代方案

- 採用：以交易作為嚴格邊界；認證資訊共用，正式會員／Token 可依權限操作自己的 test 交易，效果由保存的交易用途控制。
- 不採用：仿照 Stripe API key，將所有交易存取直接限制於 Token.IsTest 對應分區。此方案會改變正式會員使用測試 PaymentId 的既有流程，且不符合使用者最終指定的邊界。

先前的 token 分區授權待確認項目已由本次明確決定收斂。

## 後續工作

1. 依上述交易邊界整理 collection 路由、跨用途唯一 ID 與查詢合併的具體設計。
2. 正式規格階段列出禁止 test identity 的操作與拒絕回應，區分交易模擬的略過效果；同步 spec／testcases，只有需要改變公開合約時才修改 Abstract。
3. 驗收正式會員完成 test 交易、System 處理 test Order、指定操作拒絕、用途與分區一致、併發／rollback 及統計口徑。
4. 規格與 Phase 3 計畫已依後續要求整理，已由使用者確認並授權提交文件；尚未開始 code 重構、資料遷移或修改 Abstract。
