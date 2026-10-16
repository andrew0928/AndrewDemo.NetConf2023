# TiP 架構待議筆記

更新日期：2026-10-16。

本文件收錄 AC 討論中提出的架構需求、候選方向與實作差距。本輪核心 AC 已確認，後續依使用者要求統一整理架構；目前尚未定案 API、欄位、result code、context 型別或傳遞方式，也未修改既有 `/spec` 與 `.Abstract`。

## 鏈路上的測試模式識別

使用者提出：除了測試 Member，付款工具、API key 或入口也可能提供測試性質的判定依據；需要類似 HTTP context 或追蹤資訊的設計，讓整條鏈路識別是否處於 test mode，用以控制衍生行為與 log。

先保留的設計方向：

- 區分測試判定來源、這次操作的執行模式，以及領域物件已確定的用途。
- Trace／Correlation ID 用於關聯操作；執行模式用於決定允許的效果，兩者語意不同。
- 模式識別需涵蓋同步呼叫、事件、背景工作與重試，不能只依賴原 HTTP request 的存活時間。
- 各商業操作依一致的判定控制效果；測試身分仍須通過必要權限與業務檢查。

來源組合、判定時機及與既有不可變物件的相容規則，留待架構階段設計；使用者已明確要求實際執行時必須能確認用途，用途遺失或判定矛盾屬於 BUG。AC-TIP-002 為信用卡支付示意；沒有實作信用卡支付時不適用（N/A），不要求為此引入金流功能。

AC-TIP-013、AC-TIP-014 已接受：測試訂單的後續事件及重試維持測試用途，且用途必須明確，用途不明屬於 BUG；context 與錯誤處理機制尚未定案。

## 呼叫端可用程式辨識 test 行為與結果

使用者在接受 [AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005) 時指出：要求改價為 80 元、讀回卻仍是 100 元，可能讓原本預期資料被修改的測試失敗。因此，修改價格的 API／CLI／interface／UI 等入口，需要提供明確且能由系統判讀的方式，讓測試端辨識這是 test behavior。

使用者提出的候選方式包括 HTTP response header、result ID 或 result code。具體形式尚未定案；這項需求不改變 AC-TIP-005 中「價格保持原值、略過更新留 log」的預期結果。

後續整理時需處理：

- 呼叫端如何識別此次操作的正式／測試性質。
- 如何表達操作實際結果，例如已套用、因 test 略過，或正常業務檢查失敗；僅知道 test 不代表操作一定成功。
- 不同入口如何提供一致的可判讀語意，以及如何對應到操作或交易紀錄。
- 測試端如何依結果驗證預期行為，避免把正常的 test 略過誤判為一般更新失敗。

上述結果分類是待整理的設計方向，不代表已決定正式 enum、HTTP status、header 名稱或回傳格式。

## 系統到期整理與測試來源的邊界

使用者在討論 AC-TIP-012 時選擇：測試會員發現正式預約真正逾期，可以發出不以該會員為執行主體的通知，交由系統正常清理。詳見[系統到期整理決策](decisions/2026-10-16-tip-system-expiration-boundary.md)。[AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012) 已依此修正保留並接受；使用者要求現行 PetShop 不符合的部分記入架構變更，留待實作處理。

現行程式是查詢同步呼叫 `ExpireHold` 後直接更新資料庫，沒有對應的非同步事件或清理 worker；更新只涉及 `Status` 與 `UpdatedAt`，保留 Reservation 與 Product。

後續待設計：

- 查詢者、通知來源與系統工作執行主體如何區分；系統重新依正式資料及時間判斷到期條件的邊界。
- 事件不帶會員資訊不自動代表解除 TiP 限制；須由明確的業務責任決定，避免測試交易效果僅換成事件就被放行。
- 不計名的業務語意與 trace／correlation 資訊如何並存，具體 context 與 log 格式未定。
- 非同步通知的傳遞方式與系統處理完成的判定；需處理延遲、重複通知及與確認／取消的狀態競爭，並評估是否另由排程喚起同一套到期整理。

<a id="petshop-expiration-change"></a>

### PetShop 架構變更：到期事件與系統處理邊界

狀態：待設計、待實作。依據：[AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012)。本次僅記錄，未修改 source code、`/spec` 或 `.Abstract`。

| 範圍 | 現行差距 | 後續變更要求 |
|---|---|---|
| 查詢與寫入 | `ApplyReservationProductPolicy` 直接同步呼叫 `ExpireHold` 寫入資料庫。 | 查詢照常判定商品失效，並發出到期通知；由系統處理到期更新，建立明確的執行邊界。 |
| 到期通知與處理者 | 沒有對應的非同步到期事件或清理 worker；現有 `PetShopOrderEventDispatcher` 處理訂單事件。 | 補上到期通知與系統處理能力；事件 payload、transport、執行載體及是否搭配排程留待設計。 |
| TiP 行為歸屬 | 現行查詢未傳入查詢者 Member／Token，亦未建立 test context 與系統工作的區分。 | 保留 B 查詢的 test 性質，同時讓系統依 A 的正式預約資料及真實到期規則完成整理；不靠遺失會員資訊來判定可放行。 |
| 驗收完成點 | 同步方法返回後即可觀察寫入，沒有事件處理完成的觀察方式。 | 能等待並確認對應系統工作完成，再驗證 `Expired` 已持久化；不可只確認通知送出就宣稱清理成功。 |

實作範圍以本案例的正常到期整理為限，沿用 `Holding -> Expired` 並保留預約及商品。既有查詢、取消、訂單確認等呼叫到期整理的路徑，需在架構整理時一併檢查，避免留下不一致的責任判定。

### PetShop 流程邊界：測試預約不接續購物車與結帳

使用者已決定：測試 `CreateHold` 略過 Reservation／Product 建立後，不繼續該筆預約的 Cart／Checkout。[AC-TIP-015](tip-acceptance-criteria.md#ac-tip-015) 完整案例已接受。

後續實作須讓預約入口及呼叫端辨識「已驗證但因 test 未建立預約」，正常停止此流程，不將未建立的預約當成可購買商品。具體 response header、result code 與 UI 呈現仍沿用呼叫端結果識別議題，留待架構設計；不引入臨時 Product 或另一套虛擬預約交易來接續結帳。

## 與 AC 的關係

- [AC-TIP-001](tip-acceptance-criteria.md#ac-tip-001)：正常完成測試交易，控制並記錄略過的商業效果。
- [AC-TIP-003](tip-acceptance-criteria.md#ac-tip-003)：正常驗證失敗，沿用一般失敗紀錄，Checkout 自身保留 test 用途。
- [AC-TIP-004](tip-acceptance-criteria.md#ac-tip-004)：已有物件用途不可變，context 設計不能默默改寫用途。
- [AC-TIP-005](tip-acceptance-criteria.md#ac-tip-005)：資料不異動的結果已接受；如何讓呼叫端辨識原因，列入後續架構設計。
- [AC-TIP-012](tip-acceptance-criteria.md#ac-tip-012)：測試查詢允許通知系統執行正常到期整理；PetShop 的事件與系統執行邊界列為待實作變更。
- [AC-TIP-013](tip-acceptance-criteria.md#ac-tip-013)、[AC-TIP-014](tip-acceptance-criteria.md#ac-tip-014)、[AC-TIP-015](tip-acceptance-criteria.md#ac-tip-015)：完整案例已一併接受，分別約束事件與重試、用途明確性及測試預約流程邊界。
