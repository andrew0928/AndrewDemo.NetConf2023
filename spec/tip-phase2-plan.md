# TiP Phase 2 執行與跨 Session 交接

## 狀態與閱讀順序

2026-10-18 使用者確認依序以同一台機器、不同 chat session 完成 Phase 2.1～2.7。本文件只整理實作切分與交接，不重新開啟 Phase 1 或修改既定對外合約。

1. 讀取根目錄 `AGENTS.md` 與 [project roadmap](../docs/project-roadmap.md)。
2. 讀取 [Phase 1 定案基準](tip-phase1.md) 及其中列出的正式規格／測試案例。
3. 讀取本文件當前階段與前階段交付紀錄，檢查 git branch／status。
4. 依序實作、驗證及交付；需要改變 frozen spec 或 Abstract 時停止並詢問使用者。

分支：`feature-test-in-production`。標準 Core／API／CommonStorefront／CLI 為範圍；AppleBTS／PetShop 業務政策不納入。不要把其他里程碑的 Phase 2 與本 TiP 計畫混用。

## 階段與驗收

Phase 2.1～2.3 已於 2026-10-18 完成交付；2.4～2.7 尚未交付。模型、白名單、Context 基礎實作與既有付款前移不等同完整流程完成。

| Phase | 相依 | 主要變更與完成條件 | 規格／案例 |
|---|---|---|---|
| 2.1 | Phase 1 | 會員建立以 Name 查白名單，首次保存即固定 IsTest；Token 繼承 Member；舊資料不重分類、禁止用途改寫、矛盾不得放行；驗證 ID 併發及失敗重試。 | [標準流程](tip-standard-flow.md)、[白名單](tip-test-allowlist.md)、[流程案例](testcases/tip-standard-flow.md) FLOW-001～005、401／402 |
| 2.2 | 2.1 | 獨立 Identity／Transaction scoped 注入；CheckoutService lifetime 配合；HTTP token 初始化 identity、endpoint 明確解析交易 ID、Items 與 DI 同實例；CLI 每次操作獨立 scope；驗證匿名、權限、錯誤 payload 與跨請求隔離。 | [Context](tip-context-contracts.md)、[Context 案例](testcases/tip-context-contracts.md)、[HTTP](tip-http-and-operation-results.md)、[出口案例](testcases/tip-http-and-operation-results.md) |
| 2.3 | 2.1、2.2 | create 固定用途、付款、金額與明細；complete 沿用保存內容及必要驗證；Order 用途／關聯／時間、Checkout 狀態／時間、跨分區 ID 唯一；庫存／Order／完整封存／刪除原子提交，封存防寫；驗證舊格式 409、rollback、重複與並行完成。 | [生命週期](tip-checkout-lifecycle.md)、[生命週期案例](testcases/tip-checkout-lifecycle.md)、[流程案例](testcases/tip-standard-flow.md) FLOW-101～210 |
| 2.4 | 2.2、2.3 | 集中受保護寫入判斷，test 商品／SKU／庫存完成驗證後略過真實寫入；Cart 正常異動；驗證正式與 test 並行、實際略過明細。 | [標準流程](tip-standard-flow.md)、[庫存](core-product-sku-and-inventory.md)、[流程案例](testcases/tip-standard-flow.md) FLOW-301～303、307 |
| 2.5 | 2.2～2.4 | 事件即使同步也開獨立 scope，以 System identity 及載入 Order 的用途建立 transaction；驗證重送用途、巢狀／並行隔離、缺 Order／用途 BUG；履約失敗不得改寫封存。 | [Context](tip-context-contracts.md)、[標準流程](tip-standard-flow.md)、[流程案例](testcases/tip-standard-flow.md) FLOW-209、210、304～307 |
| 2.6 | 2.1～2.5 | 完整串接 OperationResult、HTTP headers／錯誤映射、CLI／事件結果與結構化 log；無交易不給交易 header，實際略過才記錄；清除 API middleware 與 OAuth 全路徑 access token 明文。 | [HTTP 與結果](tip-http-and-operation-results.md)、[出口案例](testcases/tip-http-and-operation-results.md)，含 HTTP-017 |
| 2.7 | 2.1～2.6 | CommonStorefront 明確選擇測試 PaymentId、呈現操作結果；重建本機 images，驗證正式／test 全流程、DB 與 UI；跑 solution 回歸，記錄實際通過與不適用案例。 | [Phase 1 AC 對照](tip-phase1.md)、[流程案例](testcases/tip-standard-flow.md)、[出口案例](testcases/tip-http-and-operation-results.md)、[生命週期案例](testcases/tip-checkout-lifecycle.md) |

## 跨階段銜接規則

- 2.1 完成 Core／Token 的用途規則；HTTP context 與完整錯誤出口分別於 2.2／2.6 接入，不應宣稱已完成 Web TiP。
- 2.2 建立解析與 scope 基礎；create 成功後的交易綁定與 complete 後保留快照由 2.3 接續；完整 header／結果驗收在 2.6。
- 2.3 必須立即完成 test 庫存驗證但不扣減，不能等 2.4 才補安全語意。2.4 整理共用效果控制並擴至商品／SKU；避免兩套長期判斷。
- 各階段實際略過效果時應保存足夠結果資訊，2.6 統一輸出；需要提早建立內部結果收集元件可自行處理，不修改 Abstract 合約。
- 2.5 沒有真實金流／出貨／統計 adapter 時，用測試替身驗證邊界並標示不適用，不為驗收新增業務能力。
- 若後階段必要基礎需提早實作，記錄已完成範圍與仍待驗收部分；不得只因模型存在就勾選整階段完成。

## 已定案限制

2026-10-18 追加範圍：[Phase 1.1 管理查核](tip-phase1.1.md) 獨立定義 admin 查詢，沒有 Web UI；CLI 由使用者後續追加，文件保留呼叫契約參考。原清單「正式報表／查詢 API 不在本輪」指原 Phase 2.1～2.7；新增範圍本輪只交付規格，實作與部署留待後續，不能併入既有階段的完成宣告。

- Identity 與 Transaction 分開；Core command 明確接收 RequestMember，由 API／CLI 組合。
- Member.Name 白名單預設 test、PaymentId 白名單預設 990001；只在建立時分類。Cart 不承接會員或 test 標記。
- Checkout create 固定交易內容；complete 不按當前 Cart／價格／折扣重算；仍驗證可購買性、SKU 關聯與庫存。
- 封存 canonical 名稱 `archived_checkout_transactions`，與作用中使用同一模型；終態完整保存後凍結。Order 關聯 Checkout，沒有 Order.PaymentId。
- 舊 IsTest 缺值視為 false；未知時間／金額／狀態不補造；舊 Checkout 缺必要資料回 409，要求重建。
- 取消的資料語意已定案，取消執行入口後續做；正式報表／查詢 API、退款／沖銷、queue／gateway 不在本輪範圍。
- 開發用 LiteDB SQL 查詢不等同正式報表交付；正式業績排除 test，交易與訂單不可重複加總。

## 交接基準與驗證證據

以下為前一 session 的歷史結果，並非 2026-10-18 重新執行：

- 原始 Phase 1：solution 全套 107 tests 通過，詳見 [基準](tip-phase1.md)。
- 核帳時間追加後：Core 61 tests 通過。
- Checkout 狀態／固定內容 schema 追加後：Core 67 tests 通過，solution build 成功；未重跑整套 solution tests。
- 持久化測試僅證明模型保存，不證明 create 固定、完整封存與凍結服務已實作。
- 本機 DB 已搬至 bind mount，搬移前後 SHA-256 相同；seed 跳過既有 DB，API／網站均 HTTP 200。
- Docker 仍使用較早 source 的 images，只重建容器掛載，尚未 build 最新 source；HTTP 200 不能當作 TiP 通過。

基本命令（repo root）：

```sh
dotnet build src/AndrewDemo.NetConf2023.slnx --nologo
dotnet test tests/AndrewDemo.NetConf2023.Core.Tests/AndrewDemo.NetConf2023.Core.Tests.csproj --nologo
dotnet test src/AndrewDemo.NetConf2023.slnx --nologo
```

每階段依變更選擇相關測試，完整 solution 回歸於整合交付執行；失敗需記錄原因，不把歷史數字當作新結果。

## 同機器新 Session 的環境

- Repo：`/Users/andrew/Workspace/Code/andrew-shop`。
- 已驗證環境：macOS ARM64、.NET SDK 10.0.402／runtime 10.0.13、OrbStack Docker；新 session 仍需確認服務是否執行。
- Compose：`compose/common.site-dev.compose.yaml`；網站 5128、Storefront 5129、API 5108。
- DB：`.local/data/common-site-dev/shop-database.db`，掛載至 `/data/shop-database.db`；Git 與 build context 均排除。同機器保留，不需跨 chat 搬移。
- 原 volume `andrewshop-common-site-dev_common-site-dev-data` 是遷移時備份，之後不再同步，不可當作現行資料。
- 操作與重置見 [Compose 說明](../compose/README.md)。直接開 DB 前先停 API，關閉查詢工具 DB 後再啟動；新 session 不得假定上個 session 已關閉 DB。
- `up --build` 會重建 images；現有 DB 不自動重建。需要空資料驗證時先備份，不覆寫使用者既有驗證資料。

## 各階段交付紀錄

每完成一階段更新本節，供下一個 session 接手。至少記錄變更、測試命令與結果、尚未完成或跨階段工作、環境是否重建；commit ID 若已存在可填入，不以自動 commit 代替使用者授權。

| Phase | 狀態 | 交付證據／剩餘工作 |
|---|---|---|
| 2.1 | 完成，待 review | [交付紀錄](../docs/tip/phase2.1-delivery.md)：MemberService、標準 API／CLI 接入、併發／rollback／用途防寫；solution 131 tests 通過，solution／CLI build 成功。Docker 未重建；HTTP context／完整錯誤出口接續 2.2／2.6。 |
| 2.2 | 完成，待 review | [交付紀錄](../docs/tip/phase2.2-delivery.md)：獨立 scoped contexts、HTTP metadata／MVC 解析、CLI scope、權限與並行隔離；solution 153 tests 通過，solution／CLI build 成功。Docker 未重建；create 綁定與交易生命週期接續 2.3。 |
| 2.3 | 完成，待 review | [交付紀錄](../docs/tip/phase2.3-delivery.md)：create 固定報價／用途與 context、complete 必要驗證、原子庫存／Order／完整封存、防寫與跨分區 ID；solution 191 tests 通過，solution／CLI build 成功。使用者後續授權重建 Common Docker，已備份並保留 DB，網站／API 200；共用效果控制接續 2.4，完整 UI 驗收仍待 2.7。 |
| 2.4 | 尚未開始 | — |
| 2.5 | 尚未開始 | — |
| 2.6 | 尚未開始 | — |
| 2.7 | 尚未開始 | — |

## 新 Session 起始指令範例

```text
在 feature-test-in-production 分支執行 TiP Phase 2.1。
先讀 AGENTS.md、docs/project-roadmap.md、spec/tip-phase1.md 與 spec/tip-phase2-plan.md。
依交接計畫與定案規格實作到該階段完成，補驗證並更新交付紀錄。
不要任意修改 frozen spec 或 Abstract；需要改變契約或未定案決策時停下來問我。
```

後續依序替換階段編號。每個 session 必須以工作樹及前階段交付紀錄為準，不依賴舊 chat context。

2026-10-18：Name 白名單規格、實作、測試與本機 API 部署已完成，見 [交付紀錄](../docs/tip/member-name-allowlist-delivery.md)。

2026-10-18 Phase 1.1 追加 API 已依後續授權實作，262 項回歸通過；Docker 映像重建成功，使用者依 Compose／curl 指令回報正式訂單查詢通過。此交付不改變 2.4～2.7 的狀態，見 [紀錄](../docs/tip/phase1.1-delivery.md)。
