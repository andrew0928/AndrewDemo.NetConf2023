# TiP 領域物件分類與不可變用途

## 狀態

accepted

確認日期：2026-10-16。本決策涵蓋領域物件分類與下述行為原則；情境與接受條件（AC）尚未經使用者確認，實作尚未開始。

## 背景

承接 [TiP 大前提](2026-10-16-tip-no-real-business-effects.md)：服務部署在正式環境，測試行為不應產生真實商業效果。本階段先盤點 `.Core` 會儲存在資料庫的領域物件，確認影響邊界，再展開情境與系統設計。

研究對齊 [project roadmap](../project-roadmap.md) 的共用交易流程及業務模組化方向；完整清單與程式碼依據維護於 [TiP 設計原則](../tip-guidelines.md)。

## 決策

1. **建立時確定用途，建立後不可變更**：`Order`、`Member`、`Cart`、`CheckoutTransactionRecord`、`MemberAccessTokenRecord` 必須確定正式／測試用途，兩者不得在建立後互相轉換。Cart、Checkout 與 Token 跟隨 Member，測試 Token 引發的行為屬於測試；測試交易建立的 Order 標示為 test。不可變的是用途，不是所有業務欄位或狀態。
2. **受保護物件不得被測試行為異動**：`InventoryRecord`、`Product`、`SkuRecord` 與預約、行事曆／時段資源均受保護。測試不扣庫存、不異動商品或 SKU，也不建立預約衍生 Product 或異動預約與時段占用。因測試而略過的操作必須留下 log 說明原因。
3. **保留正常檢查與必要流程**：身分、權限及業務檢查照常，測試用途不能無條件繞過驗證。正式行為依既有權限與業務規則處理。

核對 `.Core` 管理的 8 種持久化根物件，第一類 5 種、第二類 3 種，全數完成分類。內嵌的明細與附加資料跟隨所屬物件用途，不另外形成一類。

使用者所稱 Booking，在目前專案對應 `.PetShop.Extension` 的 `PetShopReservationRecord`，沿用 `Reservation` 名稱。Calendar 目前沒有獨立型別或 collection，由 Reservation 推導時段占用。兩者均納入受保護規則，但不另計入 `.Core` 的 8 種物件。

## 影響

- **重大決策；影響 .Core**：用途不可變、Member 衍生關係與受保護資料的異動控制，將影響共用持久化及 checkout 設計。
- **回頭修正 Phase 1；影響 .Abstract / spec**：落實本需求前，需先補齊正式規格與測試案例，調整既有庫存異動等規則；是否變更 `.Abstract` 及其形狀，待正式 contract 設計確認。本次僅記錄需求決策，未修改 frozen spec 或 contract。
- 預約擴充須遵守相同的保護政策；略過 Product／Reservation 寫入後的流程銜接仍待設計。
- 本次更新 docs、決策索引與 roadmap，不修改 source code，也不宣稱隔離能力已完成或已驗證。

## 替代方案

- 建立後切換正式／測試用途：不採用，避免既有關聯與已執行操作的用途不一致。
- 測試時照常異動庫存、商品或預約，再恢復原值：不採用，過程已影響正式資源。
- 一看到 test 就跳過整個流程，或默默略過異動：不採用，必要檢查仍應執行，略過原因必須可追查。

## 後續工作

- 維持先收斂物件規則、再討論情境的順序；先前情境表不視為已接受。
- 後續依此分類展開 AC、來源判定、跨模組傳遞、略過後的回傳與 log 細節。
- 需求收斂後，再進入正式 Phase 1 規格與必要的 contract 調整。
