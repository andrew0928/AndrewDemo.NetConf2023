# TiP 研究文件

本目錄集中收錄 AndrewShop 的 Testing in Production（TiP）說明、接受條件與架構設計筆記；相關決策統一收錄於 `docs/decisions`，由下方索引連結。文件供需求討論與確認使用。標準實作的 [Phase 1 規格與合約](../../spec/tip-phase1.md) 已定案；標準 TiP 流程已完成 Phase 2.1～2.7 交付，實際證據與不適用範圍見各階段紀錄。

## 文件索引

| 文件 | 內容 |
|---|---|
| [Phase 1.1 API 實作交付](phase1.1-delivery.md) | 三個 admin 唯讀 API、獨立認證與 262 項回歸；真實 host 驗證及 Docker 映像建置完成，使用者手動查詢通過。 |
| [Phase 1.1 管理查核](../../spec/tip-phase1.1.md) | 獨立 admin API、訂單總額與三種清單；未設定 token 即停用。規格定案且 API 已交付；沒有 Web UI，CLI 由使用者後續追加。 |
| [Phase 2 執行與交接](../../spec/tip-phase2-plan.md) | **新 chat 接手入口**：2.1～2.7 範圍、相依、驗收、最新基準及同機器本地環境。 |
| [Phase 2.2 交付紀錄](phase2.2-delivery.md) | HTTP／CLI scoped context、22 筆新增驗證、153 項回歸及後置處理註記。 |
| [Phase 2.7 交付紀錄](phase2.7-delivery.md) | Storefront 付款／結果、Docker 重建、Edge 全流程與 admin／LiteDB 核對；335 項回歸。 |
| [Phase 2.6 交付紀錄](phase2.6-delivery.md) | OperationResult、HTTP／CLI／事件出口、結構化 log 與明文 token 清理；327 項回歸。 |
| [Phase 2.5 交付紀錄](phase2.5-delivery.md) | 事件獨立 scope、System／保存 Order 用途、重送與巢狀／並行隔離；310 項回歸。 |
| [Phase 2.4 交付紀錄](phase2.4-delivery.md) | 共用受保護寫入、Core 商品／SKU 維護、test Cart／並行隔離；285 項回歸。 |
| [AC 自動化覆蓋對照](ac-test-coverage.md) | 每條 AC 對應具體測試與尚未驗收範圍，區分部分覆蓋、後續階段與不適用。 |
| [Phase 2.3 交付紀錄](phase2.3-delivery.md) | 固定報價、原子 Order／庫存／完整封存、防寫與跨分區 ID；38 筆新增驗證、191 項回歸。 |
| [Phase 2.1 交付紀錄](phase2.1-delivery.md) | 會員／Token 用途規則、API／CLI 接入、131 項回歸證據與後續階段邊界。 |
| [業績與核帳 Schema 檢驗](accounting-schema-review.md) | 時間、State 與獨立交易金額明細已補齊；create 時固定、不含商品資訊快照，執行流程留 Phase 2。 |
| [Phase 1 定案基準](../../spec/tip-phase1.md) | 正式規格、Abstract 合約、AC 對照、107 項測試證據與 Phase 2 邊界。 |
| [舊 Checkout 409 與定案決策](../decisions/2026-10-17-tip-phase1-contract-baseline.md) | 缺少 PaymentId 明確回 409、要求重建，不處理相容；付款前移與合約定案。 |
| [Core 欄位與明確會員輸入](../decisions/2026-10-17-tip-core-fields-and-explicit-member.md) | 確認 IsTest 與 Checkout.PaymentId 欄位；command 保留明確會員輸入。Checkout 採作用中與封存分區保存，Order 關聯封存交易；新增 nullable CheckoutTransactionId；Member.Name／PaymentId 白名單預設於 code 或 database，舊資料用途為 false，無交易時省略交易 header／物件為 null。 |
| [標準實作範圍與 Context 方向](../decisions/2026-10-17-tip-standard-scope-and-context.md) | 最新範圍：Member／Checkout／Order 標記，Token.IsTest 建立時繼承 Member.IsTest 並保存，Cart 排除；PaymentId 白名單在建立 Checkout 時判定用途，事件處理器為 event／async 邊界；已接受 scoped injection，IdentityContext／TransactionContext 各自獨立，scope 依執行情境決定。 |
| [TiP 設計原則](tip-guidelines.md) | 大前提、領域物件分類、略過紀錄與商業統計規則。 |
| [TiP 接受條件](tip-acceptance-criteria.md) | 逐條確認的 AC 與驗收結果要求。目前已接受 AC-TIP-001、AC-TIP-003、AC-TIP-004、AC-TIP-005、AC-TIP-006、AC-TIP-007、AC-TIP-008、AC-TIP-009、AC-TIP-010、AC-TIP-011、AC-TIP-012、AC-TIP-013、AC-TIP-014、AC-TIP-015。 |
| [TiP 架構設計筆記](tip-architecture-notes.md) | 已確認的 context、效果控制與責任分工，以及後續應用規格、contract／實作及擴充事項。 |
| [不產生真實商業效果](../decisions/2026-10-16-tip-no-real-business-effects.md) | TiP 大前提的決策紀錄。 |
| [領域物件分類與不可變用途](../decisions/2026-10-16-tip-domain-object-classification.md) | 物件分類、已接受案例及使用者補充決策。 |
| [系統到期整理的責任邊界](../decisions/2026-10-16-tip-system-expiration-boundary.md) | AC-TIP-012 已修正接受：測試查詢可以喚起正常到期整理；PetShop 架構差距留待實作。 |
| [研究範圍與架構規範邊界](../decisions/2026-10-17-tip-contract-and-context-boundaries.md) | 以 prototype 歸納 best practice；確認不可變契約、事件／任務交接、出口表達、正常 log 與 schema 用途定義，具體保證機制交由實作端。同日補充 current identity／current transaction 兩類 context 定義。 |

## 維護方式

原研究的 14 條核心 AC 與架構原則已確認。2026-10-17 縮小標準實作範圍，相關 AC 已移除 Cart 標記與會員承接要求；AC-TIP-002 改以 PaymentId 白名單作為 PoC 驗收方向。AppleBTS／PetShop 暫不納入本輪實作，既有案例保留。正式規格已整理於 Phase 1 基準；接受條件的確認不表示完整流程已實作。

- TiP 說明、接受條件與架構設計筆記以本目錄為 canonical 位置，不保留舊路徑副本。
- TiP 決策統一收錄於 `docs/decisions`，沿用[共用決策文件規範](../decisions/README.md)，並同步維護該處的決策索引。
- 跨主題的 [project roadmap](../project-roadmap.md) 留在原位置，以連結指向本目錄。
- 標準流程的用途政策與具體 contract 已納入 Phase 1；其他來源與擴充模組政策留待各自規格階段。

## Core 物件定義交付

2026-10-17 已新增用途與付款欄位、nullable Order 關聯及封存 collection。正式 [物件規格](../../spec/tip-core-object-definitions.md) 與 [測試案例](../../spec/testcases/tip-core-object-definitions.md) 已同步；白名單查詢、Context 基礎行為與 command 付款前移亦已完成；用途繼承、自動封存及 host 流程仍待 Phase 2。

## TDD 紅燈檢查點

已建立 [TestAllowlist 規格](../../spec/tip-test-allowlist.md)、[獨立 Context 合約](../../spec/tip-context-contracts.md) 與對應行為測試。Core 查詢、context 初始化／綁定曾保留未實作骨架；紅燈檢查已確認，使用者後續授權補上白名單查詢與 Context 基礎實作。

紅燈階段實測：完整 solution 編譯成功（0 errors、5 個既有 warnings）；全套 96 項測試中，75 項既有測試通過，21 項新增案例因預期的 NotImplementedException 失敗，0 skipped。此為補上實作前的紅燈結果。

Green 階段實測：白名單與 Context 的 21 項案例全部轉綠；全套 96 項測試通過，0 failed、0 skipped。完整 solution 編譯成功（0 errors、5 個既有 warnings）。會員註冊、Checkout 與 host 的接入仍為後續工作。

## Phase 1 完成

已補齊標準流程、HTTP／結果、不可變用途與整合測試案例；Abstract 操作結果模型與 Checkout command 已同步。全套 107 項測試通過。規格與合約依 [Phase 1 基準](../../spec/tip-phase1.md) 定案，下一階段接入 Core／host，尚未完成 Web UI 的 TiP 驗收。

## 核帳時間追加

使用者追加 Checkout.ClosedAt（complete／cancel）與 Order.CreatedAt，均保存 UTC，舊資料未知為 null；本次只交付欄位與持久化驗證。用途繼承、時間填值、自動封存與明文 token log 移除留在 Phase 2，查詢功能後續再做。詳見 [追加決策](../decisions/2026-10-17-tip-accounting-timestamps.md) 與 [Schema 檢驗](accounting-schema-review.md)。

## Checkout 固定內容追加

已確認 create 時固定獨立金額與交易明細，終態完整移至 archived_checkout_transactions 並凍結；Schema 已補上 State、TotalPrice、ProductLines／DiscountLines，商品名稱／描述／規格不複製至 Checkout。正式 [生命週期規格](../../spec/tip-checkout-lifecycle.md) 與 [案例](../../spec/testcases/tip-checkout-lifecycle.md) 已同步，服務執行留 Phase 2。

2026-10-18：Name 白名單規格、實作、測試與本機 API 部署已完成，見 [交付紀錄](member-name-allowlist-delivery.md)。
