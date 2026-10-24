# TiP 領域物件分類與不可變用途

## 狀態

accepted

部分內容已由 [2026-10-17-tip-standard-scope-and-context](2026-10-17-tip-standard-scope-and-context.md) 更新：Cart 排除用途分類，Token 跟隨 Member，PaymentId 白名單納入標準 PoC，並在建立 Checkout 時提供以固定用途。以下保留原確認脈絡，現行範圍以新決策為準。

確認日期：2026-10-16。本決策涵蓋領域物件分類與下述行為原則。同日後續已確認 [AC-TIP-001](../tip/tip-acceptance-criteria.md#ac-tip-001)、[AC-TIP-003](../tip/tip-acceptance-criteria.md#ac-tip-003)、[AC-TIP-004](../tip/tip-acceptance-criteria.md#ac-tip-004)、[AC-TIP-005](../tip/tip-acceptance-criteria.md#ac-tip-005)、[AC-TIP-006](../tip/tip-acceptance-criteria.md#ac-tip-006)、[AC-TIP-007](../tip/tip-acceptance-criteria.md#ac-tip-007)、[AC-TIP-008](../tip/tip-acceptance-criteria.md#ac-tip-008)、[AC-TIP-009](../tip/tip-acceptance-criteria.md#ac-tip-009)、[AC-TIP-010](../tip/tip-acceptance-criteria.md#ac-tip-010)、[AC-TIP-011](../tip/tip-acceptance-criteria.md#ac-tip-011)、[AC-TIP-012](../tip/tip-acceptance-criteria.md#ac-tip-012)、[AC-TIP-013](../tip/tip-acceptance-criteria.md#ac-tip-013)、[AC-TIP-014](../tip/tip-acceptance-criteria.md#ac-tip-014) 及 [AC-TIP-015](../tip/tip-acceptance-criteria.md#ac-tip-015)，包含略過紀錄、商業統計、正常失敗紀錄、用途不可變、商品改價、SKU 規格、預約建立保護、時段不可用、並行交易隔離、測試購物車正常異動、買方權限檢查、系統到期整理、事件與重試、用途明確性及測試預約流程終點的驗收要求。本輪 14 條核心 AC 已確認，信用卡 AC-TIP-002 僅作示意；實作尚未開始。

## 背景

承接 [TiP 大前提](2026-10-16-tip-no-real-business-effects.md)：服務部署在正式環境，測試行為不應產生真實商業效果。本階段先盤點 `.Core` 會儲存在資料庫的領域物件，確認影響邊界，再展開情境與系統設計。

研究對齊 [project roadmap](../project-roadmap.md) 的共用交易流程及業務模組化方向；完整清單與程式碼依據維護於 [TiP 設計原則](../tip/tip-guidelines.md)。

## 決策

1. **建立時確定用途，建立後不可變更**：`Order`、`Member`、`Cart`、`CheckoutTransactionRecord`、`MemberAccessTokenRecord` 必須確定正式／測試用途，兩者不得在建立後互相轉換。Cart、Checkout 與 Token 跟隨 Member，以測試 Token 代表會員執行的業務行為屬於測試；測試交易建立的 Order 標示為 test。不可變的是用途，不是所有業務欄位或狀態。系統到期整理另依第 15 點判定責任。
2. **受保護物件不得被測試行為異動**：`InventoryRecord`、`Product`、`SkuRecord` 與預約、行事曆／時段資源均受保護。測試不扣庫存、不異動商品或 SKU，也不建立預約衍生 Product 或異動預約與時段占用。因測試而略過的操作必須留下 log 說明原因。
3. **保留正常檢查與必要流程**：身分、權限及業務檢查照常，測試用途不能無條件繞過驗證。正式行為依既有權限與業務規則處理。
4. **AC-TIP-001 的略過紀錄與統計驗收補充**：測試會員完成一般商品結帳的案例已接受。略過扣庫存、扣款、出貨均須留 log；查詢型報表排除測試訂單不須另留 TiP log，event 觸發的統計更新因 test 略過則須留 log。若存在商業統計功能或報表，AC 必須驗證其結果未被測試交易影響。
5. **AC-TIP-003 的正常失敗紀錄**：測試會員遇到庫存不足時，照常終止結帳、不建立訂單並保留 test Checkout。正常記錄庫存不足及交易終止即可，測試用途由 Checkout 物件的 test 標記識別；未到達的操作不產生「因 test 略過」紀錄。
6. **AC-TIP-004 的用途不可變驗收**：Order、Member、Cart、Checkout、Token 均須驗證正式轉 test、test 轉正式會被拒絕；重新讀取後原用途及關聯物件用途不變，正常記錄拒絕原因，其他合法業務欄位仍依原規則處理。
7. **AC-TIP-005 的商品改價保護**：測試改價的必要檢查照常執行，通過後略過 Product 更新並留 log；後續讀取及正式交易仍使用原售價。
8. **架構議題延後統一整理**：使用者要求測試端能以程式判讀 API／CLI／interface／UI 操作是否為 test behavior，避免將預期的略過誤判為測試失敗。此方向與先前鏈路 context 討論先收錄於[架構待議筆記](../tip/tip-architecture-notes.md)，不改動 AC-TIP-005；具體介面與結果表示方式待 AC 完成後決定。
9. **AC-TIP-006 的 SKU 規格保護**：測試修改 SKU 的必要檢查照常執行，通過後略過更新並留 log；重新讀取仍為原規格，Product 關聯與庫存資料均不受影響。
10. **AC-TIP-007 的預約建立保護**：測試 `CreateHold` 的必要檢查照常執行，通過後不新增 Reservation 或衍生 Product、不占用時段並留 log；正式查詢仍顯示可預約。後續使用者已決定不接續該預約的購物車與 Checkout，見第 18 點與已接受的 AC-TIP-015。
11. **AC-TIP-008 的預約正常失敗**：已被占用的時段不可因 test 而放行；照常回報不可用並終止，不新增 Reservation 或 Product，不改變既有正式預約與占用，正常記錄失敗而不留 test 略過紀錄。
12. **AC-TIP-009 的並行交易隔離**：正式與測試交易同時結帳，均建立正確用途的 Order；只有正式交易影響庫存及商業統計。正式流程不得被測試模式影響，測試流程仍須控制商業效果並留略過紀錄。
13. **AC-TIP-010 的測試購物車正常異動**：測試 Cart 可正常新增、刪除商品明細並保存結果，用途全程維持 test，Product、SKU 與庫存不受影響。沿用正常操作紀錄規則，不將已執行的 Cart 異動記成 test 略過。
14. **AC-TIP-011 的買方權限檢查**：測試會員不能完成另一位測試會員的 Checkout，沿用 `BuyerMismatch` 與 HTTP `403 Forbidden`；原 Checkout、Cart 保持不變，不建立 Order 或產生商業效果。正常記錄買方不符、交易遭拒，未走到的操作不記成 test 略過。
15. **AC-TIP-012 的系統到期整理**：使用者要求修正保留本案例並接受：測試查詢可以通知系統，正常處理真正逾期的正式預約；清理完成後驗證 `Expired` 已持久化，保留預約與商品。此項補充第 1、2 點的行為歸屬，不改變物件分類；詳見[系統到期整理決策](2026-10-16-tip-system-expiration-boundary.md)。PetShop 現行執行邊界的差距已記入[架構變更事項](../tip/tip-architecture-notes.md#petshop-expiration-change)，留待實作。

核對 `.Core` 管理的 8 種持久化根物件，第一類 5 種、第二類 3 種，全數完成分類。內嵌的明細與附加資料跟隨所屬物件用途，不另外形成一類。

使用者於 2026-10-16 補充以下決策，並已一次接受 AC-TIP-013～015 的完整案例：

16. **AC-TIP-013 的事件與重試用途**：測試訂單後續事件、背景處理及重試維持測試用途，不因執行者改為系統而放行其商業效果。
17. **AC-TIP-014 的明確用途要求**：系統必須能明確確認用途；用途遺失、無法判定或判定矛盾視為 BUG，不作正常業務分支接受。
18. **AC-TIP-015 的預約流程終點**：測試預約略過建立 Reservation／Product 後，不接續該筆預約的購物車與 Checkout。
19. **AC-TIP-002 的示意範圍**：保留測試信用卡案例作為示意；沒有實作信用卡支付時為 N/A，不納入當前必要驗收，也不要求為此增加支付功能。

使用者所稱 Booking，在目前專案對應 `.PetShop.Extension` 的 `PetShopReservationRecord`，沿用 `Reservation` 名稱。Calendar 目前沒有獨立型別或 collection，由 Reservation 推導時段占用。兩者均納入受保護規則，但不另計入 `.Core` 的 8 種物件。

## 影響

- **重大決策；影響 .Core**：用途不可變、Member 衍生關係與受保護資料的異動控制，將影響共用持久化及 checkout 設計。
- **回頭修正 Phase 1；影響 .Abstract / spec**：落實本需求前，需先補齊正式規格與測試案例，調整既有庫存異動等規則；是否變更 `.Abstract` 及其形狀，待正式 contract 設計確認。本次僅記錄需求決策，未修改 frozen spec 或 contract。
- 預約擴充須遵守相同的保護政策；略過 Product／Reservation 寫入後停止該筆預約流程，具體結果表示及 UI 呈現留待設計。
- 本次更新 docs、決策索引與 roadmap，不修改 source code，也不宣稱隔離能力已完成或已驗證。

## 替代方案

- 建立後切換正式／測試用途：不採用，避免既有關聯與已執行操作的用途不一致。
- 測試時照常異動庫存、商品或預約，再恢復原值：不採用，過程已影響正式資源。
- 一看到 test 就跳過整個流程，或默默略過異動：不採用，必要檢查仍應執行，略過原因必須可追查。
- 只檢查略過 log 就認定商業統計未受影響：不採用，既有統計功能或報表仍須驗證實際結果。

## 後續工作

- 已收斂物件規則並接受 AC-TIP-001、AC-TIP-003、AC-TIP-004、AC-TIP-005、AC-TIP-006、AC-TIP-007、AC-TIP-008、AC-TIP-009、AC-TIP-010、AC-TIP-011、AC-TIP-012、AC-TIP-013、AC-TIP-014、AC-TIP-015；本輪核心 AC 已全部確認，AC-TIP-002 為示意；範圍外情境不自動視為已接受。
- 後續依已接受的分類與 AC，統一設計來源判定、跨模組傳遞、略過後的回傳與 log 細節。
- 核心需求已收斂；架構整理後，再進入正式 Phase 1 規格與必要的 contract 調整。
