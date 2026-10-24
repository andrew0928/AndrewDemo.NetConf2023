# TiP Phase 1.2：.NET Admin CLI 與最後查核

## 狀態

accepted；使用者已確認完整規格並授權開發，追加預設 working directory `.env`。日期：2026-10-18。

## 背景

Phase 1.1 已提供三個唯讀管理 API；先前的 `admin_query` 僅為 chat 呼叫示例，repo 沒有正式 CLI。Phase 2.1～2.7 已完成標準 TiP 實作與驗證，使用者希望補足 CLI 規格，作為最後人工確認的入口。

本案支援主線模組化與 TiP 可驗證性，讓查核依公開 API 進行，不依賴 DB extension 的開檔與快取狀態。

## 決策

使用者明確指定：

- 用 .NET 開發，取代原 shell CLI 參考方向。
- 組態從環境變數讀取，預設載入 working directory 的 `.env`，`--env-file` 覆寫路徑。
- 自我描述：`--help` 給人，`--skill` 按 topic 提供 agent 情境與使用案例，參考 skill 的漸進揭露原則。
- Query only；統計與明細兩種用途；TXT 供直接閱讀，CSV／JSON 可匯出。
- 資料源 member／checkout／order，類型 all／normal／test，可給起訖時間。
- 經確認，Member 不接受起訖時間；Order／Checkout 支援，沿用既有 API，不新增 Member 時間欄位。

已確認細節：獨立 AdminCLI project、`shop-admin stats|list`、`--source`／`--type`、環境優先於 dotenv（預設 working directory `.env`）、預設 TXT、明示全頁匯出、穩定退出碼。具體契約見 [Phase 1.2](../../spec/tip-phase1.2.md)；使用者已確認按此規格開發。

## 影響

**回頭修正 Phase 1**，**影響 .Abstract / spec**：本次影響為 spec 的 CLI 追加與取代尚未實作的 shell 參考；**不修改 .Abstract**、既有 HTTP schema／認證或 .Core 通用設計。CLI 不直接依賴 Core、LiteDB 或具交易能力的 ConsoleUI。

三個既有 API 可查用途、金額、狀態、付款與關聯，但不能直接證明庫存、Token 或事件隔離。最後驗收採 CLI 資料證據加上既有自動化證據，明示未驗證與不適用項目。若未來要求 CLI 直接查更多證據，另開 API 契約設計。

## 替代方案

- shell＋curl：原參考方案，使用者此次明確改選 .NET。
- 直接讀 LiteDB：需協調 writer 與檔案快取，不符合持續在線查核的目的。
- 擴充現有 ConsoleUI：含交易與模型操作，會混淆 query-only 管理邊界，因此採獨立 client。
- 全部 TiP 結論由三個查詢推出：缺乏庫存／事件等證據，不採用；不為了方便而虛構完整通過。

## 後續工作

完成 .NET CLI、嵌入 topics、專屬測試與發佈說明後，依 [驗收流程](../tip/phase1.2-final-verification.md) 由使用者確認。這輪不設定秘密、不部署、不產生驗收交易、不自動 commit／push。

## 工具取代決策

使用者已明確決定：ConsoleUI 保留；DatabaseQuery 與 `scripts/query-litedb.sh` 移除，未來以 AdminCLI＋Admin API 替代管理查核。

相依查核：兩個既有專案均未被其他專案引用，也不在主要 solution／部署流程中；query-litedb.sh 是 DatabaseQuery 的直接呼叫端，兩者一併移除。ConsoleUI 保留終端機購物／AI demo 用途，與 query-only AdminCLI 分開。

移除後不保留另一套直接查 LiteDB 的產品工具。Admin API 尚未公開庫存／Token 等 collection，因此 AdminCLI 仍只覆蓋已定案的三個資料源；其餘 TiP 證據使用既有自動化測試。若日後需要新增查詢來源，另開 API 契約設計，不擴張本輪規格。歷史交付紀錄保留當時使用的證據，不改寫為 AdminCLI 已驗證。

## dotenv 追加決策

使用者指定 `.env` 預設路徑為 working directory 的 `.env`。`--env-file` 可覆寫路徑；程序環境變數優先。預設檔不存在時略過，明確指定檔不存在時回設定錯誤；已存在但無法讀取的檔案不忽略。help／skill／version 維持離線，不讀任何 dotenv。
