# TiP Phase 1 定案與舊 Checkout 重建規則

## 狀態

accepted，2026-10-17。

**重大決策；影響 .Core。回頭修正 Phase 1；影響 .Abstract / spec。** 本輪由使用者啟動 TiP 規格階段，並授權處理至 Phase 1 完成；以下完成本輪定案，不授權後續任意變更 frozen contract。

## 背景

既有 Checkout 的付款資料原本於 complete 才提供。PaymentId 前移至 create 後，舊作用中紀錄沒有可信付款來源，必須確定如何處理。此議題已停下來詢問使用者。

使用者明確回覆：「明確傳回 409；重新建立（不需要處理向前相容，最小化的處理就好）」。

本次延續 roadmap 的共用交易／模組邊界主線；AppleBTS／PetShop 領域政策排除。

## 決策

1. 舊 Checkout 缺少或 null PaymentId 時，complete 回 HTTP 409，code 為 `checkout_recreate_required`，訊息為「Checkout 未保存 PaymentId，請重新建立 Checkout。」。
2. 驗證身分與買方後再回重建結果；保留舊紀錄，不補 PaymentId、不重算 IsTest、不建立 Order、不產生商業效果。新交易明確保存的數值 0 不代表缺少欄位。
3. Create command／request 必填 PaymentId；Complete 移除該輸入，使用保存值；API／CLI 仍明確提供 RequestMember。共用 client、storefront 與示範請求同步，不保留雙軌命名。
4. 依「完成 Phase 1」授權，將已確認原則具體化為獨立 Context、OperationResult／SkippedEffect、HTTP header／結果代碼、scoped 邊界、封存原子性與正式測試案例；canonical 入口為 [TiP Phase 1 基準](../../spec/tip-phase1.md)。具體名稱是此次合約交付內容，並非使用者另行逐項指定。
5. Phase 1 完成後，規格與 Abstract 合約定案；Phase 2 依此實作 Core／host，若需變更對外語意先請使用者重新開啟規格階段。

## 影響

- 缺少 PaymentId 的檢查在反序列化前完成，避免 null 對 int 的映射先拋錯。
- 新增 Core 缺欄位／null／明確 0 案例，WebContract 驗證必填 JSON、client 傳送時機與 409 mapping。
- 操作結果用途、Outcome、SkippedEffects 分別表達；不合併 IdentityContext 與 TransactionContext。
- 完整用途傳遞、效果保護、自動封存、scoped DI、middleware 與事件接入仍屬 Phase 2；編譯與模型測試通過不代表已能在 production 安全執行 TiP。
- ConsoleUI 未列於 solution，額外獨立編譯；新工作環境沒有 `.env` 時不複製該檔案，執行所需設定仍由環境提供。

## 替代方案

- 在 complete 接受 PaymentId 補舊資料：違反建立時固定付款來源，且使用者要求不做相容。
- 自動以 0 或任意卡號補值：無法還原原付款來源。
- 默默刪除舊交易或自動重建：使用者選擇明確 409，由呼叫端重新 create。
- 以 init 欄位與編譯成功當作完整 TiP 已實作：不足以證明效果隔離，另列 Phase 2 整合案例。

## 後續工作

依 [Phase 1 基準](../../spec/tip-phase1.md) 的 Phase 2 順序實作；驗收涵蓋原子封存、用途不可變、事件 scope、HTTP 結果及正式／test 並行，不擴大至 PetShop 預約政策。
