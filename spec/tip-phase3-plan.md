# TiP Phase 3：交易 collection 隔離重構

日期：2026-10-19。狀態：**規格與重構計畫已由使用者確認；Phase 3.1～3.6 均未開始實作。** 本次交付只更新 docs／spec／testcases，沒有修改 source、Abstract 或現有 DB。

使用者已確認交易邊界、共用認證與資訊、test identity 改價拒絕，以及 test transaction 無實際業務效果，並要求更新需求規格、追加 Phase 3 後確認。依據：[Phase 3 決策](../docs/decisions/2026-10-19-tip-phase3-spec-and-refactor-plan.md)。本計畫接續已完成的 [Phase 2.1～2.7](tip-phase2-plan.md)，不將舊交付改標為未完成，也不沿用舊改價略過政策。

## 接手順序與定案邊界

1. 讀取 `AGENTS.md`、[project roadmap](../docs/project-roadmap.md)、本文件與 [review 說明](../docs/tip/phase3-spec-review.md)，本次規格已接受，後續實作以此為基準。
2. 以 [交易儲存](tip-transaction-storage.md) 為 collection／路由／遷移的 canonical 規格；同時讀 [標準流程](tip-standard-flow.md)、[HTTP](tip-http-and-operation-results.md)、[Context](tip-context-contracts.md)、[生命週期](tip-checkout-lifecycle.md)、[管理查核](tip-admin-query.md)。
3. 檢查 git status，保留不相關 dirty files，先跑既有 build／test baseline。沿用既有 int IDs、API、normal／test／all 與獨立 context 合約，不修改 Abstract。
4. 後續進入實作時依序實作與自我驗證，完成一階段記錄證據及尚未完成項。若必須改既定業務合約或 Abstract，重新提出具體規格差異。

## 階段切分

案例前綴：STORE＝TC-TIP-STORE；FLOW＝TC-TIP-FLOW；HTTP＝TC-TIP-HTTP。

| Phase | 相依 | 變更範圍與完成條件 | 驗收重點 | 狀態 |
|---|---|---|---|---|
| 3.1 儲存邊界與識別 | 本次規格確認 | Core 集中六區 mapping、locator、受控讀寫、IsTest／collection 一致性、全分區 ID 配發／寫入協調；定義內部 storage 版本及 readiness 檢查。先以隔離 DB fixture 驗證，不切換現有 DB。 | STORE-004～007、014、025；凍結 archive、拒絕 raw 應用繞道。 | 未開始 |
| 3.2 Checkout／Order／事件接入 | 3.1 | Create 依既定用途寫入；complete 同用途 Order／archive 原子提交，接入 CheckoutContextLoader、OrderContextLoader、dispatcher 及 fulfillment 更新。 | STORE-001～003、008～014；既有固定報價、409、買方與 rollback 回歸。 | 未開始 |
| 3.3 指定身分限制與出口 | 3.2 | ProductMaintenanceService.UpdatePrice 對 test identity 明確拒絕；Core 結果、HTTP 403／test_identity_forbidden、log 同步；維持 SKU 略過與交易效果控制的獨立政策。 | STORE-019～021；新版 FLOW-303／309、HTTP-004／018；不新增正式維護 API。 | 未開始 |
| 3.4 查詢與使用端接入 | 3.2、3.3 | AdminQueryService、會員 Order history、ConsoleUI 取消 raw orders 存取；normal／test／all 合併後一致統計分頁，保留 API／AdminCLI／CommonStorefront 輸出契約。 | STORE-015～018；讀寫併發、跨程序一致性、歷史未知值與全區唯一性檢查。 | 未開始 |
| 3.5 遷移、seed 與部署準備 | 3.1～3.4 | 明確的離線非破壞式遷移入口、raw BSON 搬移與版本標記；新 seed／fixture、啟動檢查、備份／切換／回退說明同步。遷移在 temporary DB 副本演練，實際部署資料庫另列明確執行範圍。 | STORE-022～026；失敗 rollback、重跑 no-op、不重分類、不讓舊版混寫。 | 未開始 |
| 3.6 整體驗收與 review | 3.1～3.5 | 跑全套回歸與新增隔離案例；標準 host／CLI／UI 使用隔離環境驗證四組身分付款、兩用途 storage、正式統計與拒絕結果。更新 coverage、操作說明、demo 與交付證據後交使用者確認。 | STORE-001～026、相關 FLOW／HTTP／LIFE／ADMIN；記錄不適用 adapter／擴充領域政策。 | 未開始 |

3.1～3.4 允許必要的 seed／測試 fixture 配合提早落地，但不得先將未完成的分區配置套到現有資料庫。最終一般應用路徑不可保留舊 mixed collection fallback、雙寫或 token 路由兩套邏輯。

## 主要程式影響

| 區域 | 預期修改 | 保持的邊界 |
|---|---|---|
| Core storage | ShopDatabaseContext、Order／Checkout collection guards、新增集中路由／locator／版本與遷移元件 | 一個 shop DB；採具體最小元件，不另造泛用 repository framework。 |
| Core 交易與事件 | CheckoutService、CheckoutContextLoader、OrderContextLoader、DefaultOrderEventDispatcher、訂單履約更新 | 固定報價、買方驗證、單 DB 原子性與 event scope。 |
| Core 政策／HTTP | ProductMaintenanceService、ProtectedWriteExecutor 的操作政策接點、結果及例外映射 | Identity／Transaction 分開；OperationOutcome.Rejected 已足夠，Abstract 不動。 |
| 查詢／hosts | AdminQueryService、API MemberController、ConsoleUI 的訂單存取；相關 client／UI 回歸 | API 不拆，normal／test／all、header、DTO、AdminCLI 命令不改。 |
| 建置資料／操作 | DatabaseInit、必要共用 seed 路徑、測試 fixture、部署說明／demo | 原 destructive seed 不能直接當 migration；不順帶實作擴充模組的 TiP 政策。 |
| 共用資訊 | Member／Token／Cart／Product／SKU／Inventory 的讀取保持共用 | 會員白名單、Token 簽發、商品折扣算法不重寫。 |

## 驗證與交付閘門

使用 .NET 10，solution 的 build／test 串行執行；避免同時跑多個 MSBuild。主方案以外的 ConsoleUI 需另建置。

```sh
dotnet build src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo -m:1
git diff --check
```

- 新行為先以會失敗的對應案例建立驗證，再接入正式入口，測試不得只複製 collection mapping 的實作。
- source／docs／demo／spec 使用同一組 canonical 名稱；搜尋 raw transaction collection 存取並確認只存在受控 infrastructure／migration／fixture，不能只替換 Context 屬性就宣告完成。
- 每階段交付於 `docs/tip/phase3.N-delivery.md`，記錄問題、變更範圍、案例／命令／實際結果、環境及限制；未執行不得標示通過。
- 真實付款／出貨／事件統計 adapter 不存在則明列不適用；AppleBTS／PetShop 只驗證共用改動的回歸，不宣稱已完成其 TiP。
- 本次文件提交僅包含已確認規格與既有 baseline 結果；Phase 3 runtime 行為、DB 搬移及部署驗收全數待實作。
