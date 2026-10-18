# TiP Phase 2.4 交付紀錄

日期：2026-10-18。分支：`feature-test-in-production`。範圍為標準 Core 的受保護寫入、實際略過效果與 Cart／並行隔離驗證；事件 scope、完整出口及 UI 接續 2.5～2.7。

## 實作

- `ProtectedWriteExecutor` 集中執行或略過真實寫入。商品／SKU 維護從 current identity 取得用途；Checkout 扣庫存從已綁定且 ID／kind 相符的 transaction 取得用途，不把兩個用途做通用 OR。匿名維護、缺少或錯誤 Checkout context 均在效果前停止並記錄用途錯誤。
- `OperationEffects` 取代原 `CheckoutEffects`；全數 source／測試／DI 使用新名稱，不保留相容別名。每次 Core 操作開始清除前次明細，同一操作內已略過的證據不因後續失敗而抹除。Phase 2.6 再統一形成 OperationResult 與各出口表達。
- Checkout 先在原有資料庫交易內驗證所有商品、SKU 及庫存，再呼叫共用效果執行器。正式扣庫存的 write callback 留在同一 transaction；test 完全不呼叫寫入，保持既有 Order／封存／刪除的原子性。
- Core `ProductMaintenanceService.UpdatePrice`／`UpdateSpecification` 提供規格要求的維護驗證入口，沒有新增 HTTP API 或管理 UI。必要的授權 callback 由受控呼叫端提供，test／normal 均須執行；不因此授予一般會員商品維護權限，也沒有新增角色模型。另檢查目標存在、價格非負及規格非空白。
- 維護先驗證再決定略過或寫入；正式操作只更新要求欄位，保留其他欄位，不以 upsert 重建已移除的目標。test log 記錄 EffectCode、目標、ReasonCode、用途來源及要求的價格／規格；庫存則逐 SKU 記錄要求數量。TraceId／ShopId／Outcome 等完整出口欄位接續 2.6。
- 維護、效果執行器與收集器採 scoped，查詢型 DefaultProductService 保持 singleton；scope 不共用效果集合。沒有把 scoped context 注入 singleton 資料庫服務。
- 舊 `ShopDatabase.Create` 拒絕直接建立 Product／SKU／Inventory，避免無 context 的靜態入口繞過保護。原始資料庫／collection 仍是基礎設施入口，供初始化、測試與本輪排除的擴充模組使用；本次不宣稱能阻止任意 raw DB 寫入。標準維護與扣庫存必須走上述服務。
- Cart 沿用真實保存，新增 HTTP 測試驗證 test 身分下增刪後可重新讀取，Product／SKU／Inventory 不變，沒有 transaction 或虛構 skipped effect。

## 驗收對照

具體測試位於 [ProtectedWriteTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/ProtectedWriteTests.cs)、[CheckoutLifecycleTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/CheckoutLifecycleTests.cs) 與 [ExecutionScopeTests](../../tests/AndrewDemo.NetConf2023.WebContract.Tests/ExecutionScopeTests.cs)。完整 AC 進度見 [自動化覆蓋對照](ac-test-coverage.md)。

| 規格／AC | 自動化證據 |
|---|---|
| FLOW-303／AC-005、006 | `Maintenance_ValidatesThenWritesOrSkips_AndPreservesOtherFields`：改價 100→80、SKU 黑色／128GB→白色／256GB；test 不觸及寫入入口，原值／關聯／庫存不變，log 包含目標與要求內容，後續正式結帳仍用 100 元。另測 normal 真正更新及合法後續結帳。 |
| 必要驗證 | `Maintenance_InvalidOrUnauthorizedRequests_DoNotWriteOrRecordSkips`：normal／test 權限拒絕、負價格、空規格、缺目標，均無寫入及 skipped effects。 |
| 用途來源／BUG | `ExplicitSource_DoesNotOrIdentityAndTransaction` 四種用途組合；`MissingOrMismatchedContext_StopsBeforeWrites` 五種缺失／不匹配；`RealWriteFailure_IsNotReportedAsSkipped`。 |
| FLOW-301／AC-009 | `TwoMembers_ConcurrentFormalAndTestCheckout_OnlyFormalAffectsStockAndTotals`：兩個工作在 barrier 會合後並行執行；100 元商品、庫存 10，正式買 3／test 買 2，訂單 300／200、庫存 7、封存與 context 分離；正式 admin 統計只有 1 筆／300。沒有宣稱付款／出貨／事件統計 adapter 已驗證。 |
| FLOW-302／AC-010 | `TestMember_CartAddAndDeletePersistWithoutProtectedChangesOrSkips`：真實 HTTP、test Token、DB 與 GET 重讀、無用途／會員欄位、受保護資料不變；測試用出口觀測 scoped context／effects。 |
| FLOW-205／AC-011 補強 | `TwoTestMembers_BuyerMismatchPreservesDataAndDoesNotDispatchOrSkip` 與 `TwoTestMembers_CannotCompleteEachOthersCheckout_AndNoEffectsOccur`：雙方皆 test，Core BuyerMismatch／HTTP 403、保存資料不變、無訂單／封存／事件／略過，Core 有拒絕 log。 |
| 逐筆效果／驗證順序 | `TestCheckout_ValidatesEverySkuBeforeSkipping_AndNeverCallsInventoryWrite`：兩個 SKU 的逐筆明細與數量；第二個 SKU 不足時，第一個也不提前產生略過。攔截寫入可抓出先扣再還原。 |
| scope／結果隔離 | `MaintenanceScopes_KeepPurposeAndEffectsSeparateDuringParallelCalls` 與 `NextOperation_ClearsCollectorWithoutMutatingPriorResult`；正式／test 共用商品但不共用用途與明細，下一操作清空集合，先前 OperationResult 快照不變。 |
| FLOW-307 | 真實付款、出貨、事件統計 adapter 尚不存在，標示不適用；不產生假的略過紀錄。 |

## 驗證紀錄

驗證命令：

```sh
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false --verbosity quiet
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

- Solution **285 passed、0 failed、0 skipped**：Core 160、WebContract 85、AppleBTS Extension 19、PetShop Extension 13、PetShop API 8。本次新增 Core 20、HTTP／scope 3 項。
- ConsoleUI build 成功，0 warnings／0 errors；測試重新編譯時只有既有 ShopDatabase obsolete／API XML 文件警告，沒有新增警告。
- 初次測試曾因測試呼叫名稱錯誤而無法編譯，修正後遇到測試執行器的沙箱 socket 限制；取得執行授權後通過上述完整回歸。這些未成功執行不計入通過數。
- `git diff --check` 通過；Abstract 與 frozen 業務規格沒有變更。
- 新增測試使用隔離資料庫與 loopback Kestrel；沒有修改使用者本機 DB 或重建／重啟 Docker。

## 下一階段

- 2.5：事件獨立 scope、System identity、載入 Order 恢復用途、重送及巢狀／並行隔離。共用效果執行器目前只接維護與 Checkout；事件來源須由 2.5 的真實載入邊界接入，不能只靠模型測試宣稱完成。
- 2.6：OperationResult、HTTP headers、CLI／事件結果、完整結構化 log 及 OAuth 明文 Token 清理；沿用 OperationEffects，不建立第二份收集器。
- 2.7：CommonStorefront 明確付款選擇、Docker 與完整 UI 驗收。
- Abstract 與 frozen 業務規格不變；只更新 testcase 證據及階段進度。未自動 commit／push。
