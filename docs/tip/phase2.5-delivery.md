# TiP Phase 2.5 交付紀錄

日期：2026-10-18。分支：`feature-test-in-production`。前階段 2.4 已依使用者指示提交為 `a5c94f6`。本階段處理標準訂單事件的 scope、保存用途及效果邊界；沒有修改 Abstract 或擴充模組的業務政策。

## 實作

- `DefaultOrderEventDispatcher` 維持既有兩個 Dispatch 簽章與 manifest ID；singleton 只注入 IServiceScopeFactory，每次 Completed／Cancelled 派送皆建立新 scope，同步呼叫、重送及巢狀呼叫也不例外。
- 事件 scope 初始化 System identity，以 `OrderContextLoader` 從作用中 shop DB 的 Order 載入 IsTest，綁定 Order transaction。忽略買方目前用途、白名單及原 HTTP／CLI context；沒有 CheckoutTransactionId 的歷史訂單也能處理，不補造封存。
- 缺 Order、非法 ID、已保存 IsTest 為 null／錯誤型別均記錄 Error 並拋 `ExecutionContextInvalidException`，在解析處理器前終止。依既定舊資料政策，完全缺少 IsTest 欄位仍為 false；這與必要執行 context 遺失不同。
- 新增 Core 的 `IOrderEventHandler`，作為 dispatcher scope 內的處理端。應註冊為 scoped，依注入的獨立 identity／transaction 執行；此為 Core 內部組裝方式，未新增或修改 Abstract 合約。
- `ProtectedWriteExecutor.ForOrder` 共用 2.4 的效果控制。使用 loader 實際載入的快照，要求目前仍為 System identity 及原 Order snapshot；即使有人以相同 ID 重新綁定 false，也會因快照遭替換而停止。單獨手工 Bind 不能取代持久化載入。
- 真實效果前仍執行必要驗證；test Order 即使 System.IsTest=false，也不呼叫 write callback，只記錄實際略過的目標與原因。每個事件有獨立 OperationEffects；巢狀事件、失敗及 scope dispose 不改寫外層 context 或明細。
- API 既有 manifest factory 會以 DI 建立更新後的標準 dispatcher；ConsoleUI 改用 DI 註冊，移除沒有 scope factory 的靜態實例。與事件無關的既有 focused Checkout／controller 測試改用明確替身；正式 dispatcher 由新增整合測試直接驗證。
- 所有實際 production handler 目前仍為空集合；不新增付款、出貨、事件統計 adapter，不為空集合製造略過紀錄。PetShop dispatcher 不在本輪 TiP 政策範圍。

## 已有語意與限制

Checkout 仍在 commit 後派送事件。事件拋錯時 Order／Completed 封存保留，只有 Order.FulfillmentStatus 依既有流程設為 Failed；complete response 與外層 context 保持 Checkout。重送透過 dispatcher 重新載入 Order，不能覆寫封存、重建作用中 Checkout 或再扣庫存。

本階段沒有新增重送 API、queue／worker、去重或 exactly-once 機制。正常事件的真實效果是否可重複仍由將來 adapter 的業務契約決定；直接重送也不自行修改 FulfillmentStatus。取消事件只接入既有 Dispatch 合約的 scope，沒有新增取消入口或取消業務。

處理器是受信任的 Core 執行程式，須使用 ForOrder 執行訂單效果；任意 raw DB 或刻意繞過共用 executor 的程式不在保證範圍。完整 OperationResult、HTTP／CLI／事件出口與 TraceId／ShopId／Outcome log 接續 2.6。

## 驗收證據

[OrderEventScopeTests](../../tests/AndrewDemo.NetConf2023.WebContract.Tests/OrderEventScopeTests.cs) 使用正式 dispatcher、DI scoped services 與隔離 LiteDB；出貨／統計只有可計數測試替身，沒有外部商業效果。新增的 Core 防繞過案例在 [ProtectedWriteTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/ProtectedWriteTests.cs)。

| 規格／AC | 具體測試 |
|---|---|
| FLOW-304、210／AC-013 | `ReplayAfterCallerScopeDisposed_RestoresPersistedOrderPurpose`：正式／test、Completed／Cancelled 四組，原 scope 已 dispose 後重送兩次，每次 System identity、獨立 Order context／effects，買方用途相反也不影響；不捏造 Checkout。 |
| FLOW-305 | `SyncDispatch_DoesNotOverwriteCallerContextsOrEffects`；`NestedDispatch_IncludingFailure_RetainsOuterOrderScope`；`ParallelDispatch_KeepsSystemOrderAndEffectsIsolated`。並行在兩個 handler 進入後以 barrier 交錯；所有成功／失敗 handler 均 dispose。 |
| FLOW-306／AC-014 | `InvalidSavedOrder_StopsBeforeHandlerConstructionAndDoesNotRepair` 五種錯誤；`LostEventContext_IsBugBeforeAnyCommercialEffect` 五種 context 損壞／替換；Core `OrderEffect_RejectsManuallyBoundContextWithoutPersistedLoad`。沒有真實效果、沒有假的略過，記錄 Error。 |
| 舊資料政策 | `LegacyMissingPurpose_RemainsNormal_WithoutInventingCheckout`：缺欄位為 false，讀取不改寫原 BSON。 |
| 必要驗證 | `HandlerValidationFailure_IsNotASkippedEffect`：正式／test 都執行驗證，拒絕不算略過。 |
| FLOW-209／封存凍結 | `CheckoutDispatchFailureAndReplay_PreserveCommittedOrderAndFrozenArchive`：正式與測試付款交易都用真正 CheckoutService／dispatcher；事件失敗後 complete 仍成立、履約 Failed；原 caller scope 已結束後重送，Order／封存完全不變、庫存不再扣。 |
| FLOW-307／不存在 adapter | `NoAdapters_DoesNotInventEffects_ButStillRequiresStoredOrder`；其他測試只以替身驗證正常 callback 執行、test callback 不執行及逐筆 log。真實付款／出貨／事件統計整合標示不適用。 |

## 執行紀錄

```sh
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false --verbosity quiet
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

- Solution **310 passed、0 failed、0 skipped**：Core 161、WebContract 109、AppleBTS Extension 19、PetShop Extension 13、PetShop API 8。本階段新增事件 scope 整合 24 項與 Core 1 項。
- ConsoleUI build 成功，0 warnings／0 errors；完整回歸重新編譯僅有既有 2 項 ShopDatabase obsolete 與 3 項 API XML 文件警告。
- `git diff --check` 通過；Abstract 與 frozen 業務規格沒有變更。
- 首輪新增 23 項事件整合測試通過，後續同 ID 用途替換與手工 Bind 防繞過案例已納入最終回歸。測試 DI options 初次因目標型別歧義無法編譯，明確指定型別後修正；未成功執行不計入通過。

本次測試使用隔離資料庫，不改寫使用者本機 DB，也未重建／重啟 Docker。2.4 已提交；2.5 變更留待 review，未自動 commit／push。

## 後續

- 2.6：統一結果、headers、CLI／事件出口、完整結構化 log 與明文 Token 清理；沿用既有 OperationEffects 及本次事件 scope。
- 2.7：CommonStorefront 付款選擇與完整 Docker／UI 驗收。
- [AC 覆蓋對照](ac-test-coverage.md) 分開記錄已驗證的執行邊界、尚未整合的出口，以及不存在的真實 adapter，不以模型或測試總數宣稱全部 AC 完成。
