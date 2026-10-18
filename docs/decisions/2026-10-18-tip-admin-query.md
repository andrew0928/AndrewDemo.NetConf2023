# TiP Phase 1.1 管理查核與獨立認證

## 狀態

accepted（查核方向、獨立 admin token、shell CLI、規格先行）；認證傳遞與設定方式待使用者確認。

2026-10-18 使用者要求查總訂單金額、訂單／結帳／會員清單，接受 `/api/admin/*` 設計，指定預先設定、小寫且無連字號、以 `admin_` 為前綴的 token，並要求 shell script 讀取。隨後指定為 Phase 1.1，明確確認「先完成 Phase 1.1 規格，實作留待後續」。

**重大決策、影響 .Core、回頭修正 Phase 1、影響 .Abstract / spec**：追加查詢規格及後續 Core 查詢服務；目前不需要變更 Abstract，亦不重開既有交易用途、生命週期或會員 token 契約。

## 背景

直接以宿主機 extension 檢查 LiteDB 曾反覆讀到舊連線狀態。離線唯讀 CLI 可診斷檔案，但使用者需要在服務運行中依一致口徑快速驗證 TiP 資料。管理查詢承接已定案核帳方向，支援驗證正式／測試隔離，與 roadmap 主線相關。

## 決策

- 運行中的 API 提供唯讀管理查詢，查詢服務集中處理用途、時間、總額與分頁。
- 訂單總額與清單共用篩選，不依會員或 PaymentId 當前白名單重分類；Checkout 合併作用中與封存但不與訂單重複加總。
- admin token 獨立認證，與會員 token 雙向隔離；無需讓 admin 冒充 Member 或 System。
- 第一版以 shell CLI 操作；後台 UI 留後續，沒有任意 SQL 或管理寫入能力。
- 本輪完成規格與案例，實作與部署另行授權。使用者允許必要時拆成兩個 commit；目前僅規格交付，不替尚未存在的實作建立 commit。

## 待確認

建議採 Authorization Bearer 與 SHOP_ADMIN_ACCESS_TOKEN 環境變數，未設定停用入口。問題已送出，未回覆前不視為定案。

## 替代方案

- 持續直接開 DB：適合離線診斷，無法提供一致的正式統計入口。
- 沿用會員 token：不符合使用者指定的獨立機制。
- shell 自行讀 DB 與計算：會形成第二份查詢口徑，並帶回跨程序檔案問題。
- 先建報表資料庫或完整後台：超出目前四項查核需求。

## 影響與交接

正式入口見 [Phase 1.1](../../spec/tip-phase1.1.md)、[API／CLI 規格](../../spec/tip-admin-query.md)、[案例](../../spec/testcases/tip-admin-query.md)。原 Phase 2.1～2.7 清單保留；追加功能不代表其餘階段完成。現有工作區其他改動不自動納入本次規格 commit。
