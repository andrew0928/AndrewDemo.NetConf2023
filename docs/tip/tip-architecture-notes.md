# TiP 架構設計筆記

更新日期：2026-10-17。

本文件收錄 AC 討論中提出的架構需求、已確認規範與實作差距。14 條核心 AC 與支撐 TiP 體系的架構原則已確認；本輪以歸納 best practice 為範圍，在規範層級收斂。完整應用政策矩陣、欄位、result code、context 型別與傳遞契約仍留待後續階段，也未修改既有 `/spec` 與 `.Abstract`。

## 架構 review 收斂結果

2026-10-17 使用者確認以下邊界，完整紀錄見[研究範圍與架構規範邊界決策](decisions/2026-10-17-tip-contract-and-context-boundaries.md)。編號對應先前八項架構議題。

| 原議題 | 已確認規範／目前狀態 |
|---|---|
| (1)、(2) 物件用途與保存 | 最低層系統服務介面將不可變物件的 `id + type` 視為 immutable，同一身分的用途永遠一致。保證機制交由實作端；create 權限由應用端介面決定。 |
| (3)、(4) Context 與商業效果控制 | 已區分 current identity 與 current transaction；架構確保資訊正確傳遞，應用端各環節依 context 執行政策，在商業效果發生前完成判定。 |
| (5) 系統工作邊界 | 以 event-driven／task queue 等交接機制隔離查詢與系統到期整理兩端，具體基礎設施屬實作議題。 |
| (6) 對外結果 | Context 延續到系統內最後一步，離開系統前判定，再以 header／result code 等明確契約對外傳遞。HTTP API 由 middleware 統一處理出口表達。 |
| (7) Log | 沿用正常 log 機制，由 context 與判定結果決定對應紀錄內容。 |
| (8) 資料使用 | 在 database schema 明確定義各類物件的正式／測試用途及適用規則，查詢與統計依此使用。 |

`type` 在此指正式／測試用途，並非已選定的程式欄位名稱；穩定的 ID 與用途關係不要求 ID 字串編碼用途。既有領域分類及 AC 維持不變。Schema 表達、不可變性防護、queue 與 log 工具等實作事項，保留作實作時檢查，不逐項展開為本輪研究的必要決策。

## 鏈路上的測試模式識別

使用者提出：除了測試 Member，付款工具、API key 或入口也可能提供測試性質的判定依據；需要類似 HTTP context 或追蹤資訊的設計，讓整條鏈路識別是否處於 test mode，用以控制衍生行為與 log。

2026-10-17 補充確認兩類 context 描述：

| Context | 所描述的資訊 |
|---|---|
| current identity | 目前以什麼身分執行，以及該身分／憑證的 test 標記；涵蓋 Member、API access token 或系統排程工作身分。 |
| current transaction | 目前替哪筆交易執行，以及該交易的用途；須涵蓋其後續事件與重試。 |

改價等操作可依 current identity 判定，不必擴大交易定義來涵蓋所有操作。此處只確定兩類語意，尚未指定類別、欄位或傳遞技術。依 AC-TIP-013，正式系統身分處理 test 訂單仍受 test 限制，因此 `currentuser.isTest` 不能作為所有操作唯一的判斷依據。

身分與交易是不同維度，值不同不自動等同用途矛盾；各自不明、遺失或與其已確定用途矛盾仍屬 AC-TIP-014 的 BUG。不涉及交易的操作，須與交易執行途中遺失 context 區分。AC-TIP-012 的系統工作交接依兩端各自責任建立執行範圍，不能以清空資訊代替責任判定。完整用途組合政策尚未逐一定案，列為後續應用規格，不改變已確認的 AC，也不自動採用通用 OR 規則。

已確認的責任分工與設計約束：

- 區分測試判定來源、這次操作的執行模式，以及領域物件已確定的用途。
- Trace／Correlation ID 用於關聯操作；執行模式用於決定允許的效果，兩者語意不同。
- 模式識別需涵蓋同步呼叫、事件、背景工作與重試，不能只依賴原 HTTP request 的存活時間。
- 各商業操作依一致的判定控制效果；測試身分仍須通過必要權限與業務檢查。
- 架構提供標準存取與傳遞管道，確保 context 資訊正確；應用端確保每個相關環節依 context 執行業務政策，在效果發生前判定。

最低層介面的用途不可變契約提供 context 的判定依據。跨責任邊界的具體承接格式留給後續 contract，實際執行時必須能確認用途，用途遺失或判定矛盾屬於 BUG。未來額外來源的組合與判定時機尚未定案；AC-TIP-002 為信用卡支付示意，沒有實作信用卡支付時不適用（N/A），不要求為此引入金流功能。

AC-TIP-013、AC-TIP-014 已接受：測試訂單的後續事件及重試維持測試用途，且用途必須明確，用途不明屬於 BUG；具體 context 型別與錯誤處理機制尚未定案。

## 呼叫端可用程式辨識 test 行為與結果

使用者在接受 [AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005) 時指出：要求改價為 80 元、讀回卻仍是 100 元，可能讓原本預期資料被修改的測試失敗。因此，修改價格的 API／CLI／interface／UI 等入口，需要提供明確且能由系統判讀的方式，讓測試端辨識這是 test behavior。

2026-10-17 已確認：context 延續到系統內最後一個步驟，在離開系統前判定；跨出系統後以明確的 header／result code 等契約傳遞，HTTP API 由 middleware 統一處理出口表達。具體 header、result code 與回傳格式尚未定案；這項需求不改變 AC-TIP-005 中「價格保持原值、略過更新留 log」的預期結果。

商業效果仍在各操作發生前受到控制；出口依 context 與已完成的判定結果產生一致表達。Test Cart 可實際寫入、test Product 更新被略過、test Checkout 可因庫存不足失敗，因此對外結果需承接實際操作結果。具體資訊格式留給後續 contract。

後續正式 contract 須具體化：

- 呼叫端如何識別此次操作的正式／測試性質。
- 如何表達操作實際結果，例如已套用、因 test 略過，或正常業務檢查失敗；僅知道 test 不代表操作一定成功。
- 不同入口如何提供一致的可判讀語意，以及如何對應到操作或交易紀錄。
- 測試端如何依結果驗證預期行為，避免把正常的 test 略過誤判為一般更新失敗。

上述結果分類是待整理的設計方向，不代表已決定正式 enum、HTTP status、header 名稱或回傳格式。

## 系統到期整理與測試來源的邊界

使用者在討論 AC-TIP-012 時選擇：測試會員發現正式預約真正逾期，可以發出不以該會員為執行主體的通知，交由系統正常清理。詳見[系統到期整理決策](decisions/2026-10-16-tip-system-expiration-boundary.md)。[AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012) 已依此修正保留並接受；使用者要求現行 PetShop 不符合的部分記入架構變更，留待實作處理。

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

## 本輪收斂後保留的事項

本輪在 best practice 與規範層級已無阻擋收斂的架構議題。以下尚未定案事項保留到對應階段，不表示已完成規格或實作：

- **應用規格**：完整 identity／transaction 用途組合與各操作政策，須遵循既有不可變契約、大前提及 AC；未接受的組合不自動視為允許。
- **正式 contract 與實作**：Context 型別、傳遞格式、header／result code、schema／log 欄位、queue／worker 與完成觀察機制。
- **未來功能擴充**：信用卡等額外來源的驗證、判定時機及用途相容規則；AC-TIP-002 維持示意。

## 與 AC 的關係

- [AC-TIP-001](tip-acceptance-criteria.md#ac-tip-001)：正常完成測試交易，控制並記錄略過的商業效果。
- [AC-TIP-003](tip-acceptance-criteria.md#ac-tip-003)：正常驗證失敗，沿用一般失敗紀錄，Checkout 自身保留 test 用途。
- [AC-TIP-004](tip-acceptance-criteria.md#ac-tip-004)：已有物件用途不可變，context 設計不能默默改寫用途。
- [AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005)：資料不異動的結果已接受；如何讓呼叫端辨識原因，列入後續架構設計。
- [AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012)：測試查詢允許通知系統執行正常到期整理；PetShop 的事件與系統執行邊界列為待實作變更。
- [AC-TIP-013](tip-acceptance-criteria.md#ac-tip-013)、[AC-TIP-014](tip-acceptance-criteria.md#ac-tip-014)、[AC-TIP-015](tip-acceptance-criteria.md#ac-tip-015)：完整案例已一併接受，分別約束事件與重試、用途明確性及測試預約流程邊界。
