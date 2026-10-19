# TiP Acceptance Criteria 自動化覆蓋對照

2026-10-19 追加：本表的 Phase 2 證據保留；AC-TIP-005 新版明確拒絕尚待 Phase 3，collection 隔離、路由與遷移亦未驗證。新增範圍逐案見 [STORE-001～026](../../spec/testcases/tip-transaction-storage.md)，不可由既有測試總數推定完成。

更新：2026-10-18，Phase 2.7。`accepted` 代表需求已確認；測試總數不代表所有 AC 通過。本表區分 Core／HTTP 行為證據與仍待交付的整體條件，不修改原接受條件。

測試以 `Core.Tests`、`WebContract.Tests` 下同名類別定位；含單元、LiteDB 整合及 Kestrel HTTP 測試。歷史各階段測試數以各自交付紀錄為準。

| AC | 目前具體證據 | 尚未覆蓋／邊界 |
|---|---|---|
| 001 一般測試交易 | MemberServiceTests 註冊／Token；CheckoutLifecycleTests.QuoteAndCompletion_FixPurposeAmountsAndContext；ProtectedWriteTests.TestCheckout_ValidatesEverySkuBeforeSkipping_AndNeverCallsInventoryWrite；AdminQueryTests.Orders_UseOwnPurposeAndSummarizeBeforePagination。 | 2.7 Edge 已完成正常／test 會員與付款全流程；admin／LiteDB 確認正式統計只增加一般交易。付款／出貨及事件統計 adapter 不存在，不算通過。 |
| 002 測試 PaymentId | CheckoutLifecycleTests.QuoteAndCompletion_FixPurposeAmountsAndContext 四組參數；CompletionIgnoresChangedAllowlistAndLegacyMissingPurposeStaysFormal。 | 核心用途與庫存通過；四組 HTTP 結果已由 CheckoutOutputs_UsePersistedPurposeAndKeepEverySkippedTarget 驗證；2.7 Edge 四組付款／會員操作與保存 PaymentId 均通過。 |
| 003 庫存不足 | CheckoutLifecycleTests.NecessaryValidation_RejectsWithoutAnyEffects、MultipleLines_PreserveRelationsAndValidateCombinedSkuQuantity；ProtectedWriteTests.TestCheckout_ValidatesEverySkuBeforeSkipping_AndNeverCallsInventoryWrite。 | 核心拒絕／無副作用通過；HTTP 400／rejected 與無虛構略過已由 InventoryRejection_AndMissingPayment_DoNotInventSkippedEffects 驗證。 |
| 004 用途不可變 | MemberServiceTests.Updates_RejectPurposeChangesAndPreservePurposeOnLegalUpdates；CheckoutLifecycleTests.PersistedPurposes_CannotBeReclassifiedThroughCollectionWrites。 | 已有四種物件雙向用途保護；拒絕與用途矛盾有 Core log，HTTP 失敗出口與 token 診斷清理已於 2.6 驗證。raw DB 不是此應用保證邊界。 |
| 005 商品改價 | 舊 ProtectedWriteTests 維護案例及 2.6 HTTP 探針只驗證當時成功略過政策。 | **新版未覆蓋**：Phase 3 需驗證明確 Rejected、403／test_identity_forbidden、無 product.update 略過及拒絕 log；對應 FLOW-303、HTTP-004、STORE-019／020。 |
| 006 SKU 修改 | ProtectedWriteTests.Maintenance_ValidatesThenWritesOrSkips_AndPreservesOtherFields、Maintenance_InvalidOrUnauthorizedRequests_DoNotWriteOrRecordSkips，包含規格、Product 關聯及庫存不變。 | Core AC 行為已覆蓋；無維護 API。2.6 已驗證結果、逐筆要求 log 與 headers。 |
| 007／008 測試預約 | 無對應 TiP 自動化證據。 | PetShop 排除本輪，既有一般預約測試不等同這兩條通過。 |
| 009 正式／test 並行 | ProtectedWriteTests.TwoMembers_ConcurrentFormalAndTestCheckout_OnlyFormalAffectsStockAndTotals；ExecutionScopeTests.ConcurrentAndSubsequentRequests_HaveIndependentContexts。 | 已對上兩位會員、3／2 件、300／200 元、庫存 7、正式統計 1 筆／300。事件 scope／用途邊界已於 2.5 驗證；2.6 ParallelNormalAndTestOutputs_KeepResultAndLogsSeparate 已驗證完整 headers／log；不存在 adapter 不適用。 |
| 010 test Cart | ExecutionScopeTests.TestMember_CartAddAndDeletePersistWithoutProtectedChangesOrSkips。 | test Token 的真實增刪、DB／HTTP 重讀、無 transaction／略過、受保護資料不變均已覆蓋；2.6 已接入結果，無虛構略過 log。 |
| 011 他人 Checkout | ProtectedWriteTests.TwoTestMembers_BuyerMismatchPreservesDataAndDoesNotDispatchOrSkip；ExecutionScopeTests.TwoTestMembers_CannotCompleteEachOthersCheckout_AndNoEffectsOccur。 | 雙方 test 的 Core／HTTP 行為已覆蓋，含拒絕 log 與無事件／效果。2.6 已驗證 rejected 與不洩漏交易 header。 |
| 012 系統整理到期預約 | 無對應 TiP 自動化證據。 | PetShop 排除本輪；需另實作通知／系統處理邊界。 |
| 013 事件／重送 | OrderEventScopeTests.ReplayAfterCallerScopeDisposed_RestoresPersistedOrderPurpose、NestedDispatch_IncludingFailure_RetainsOuterOrderScope、ParallelDispatch_KeepsSystemOrderAndEffectsIsolated、CheckoutDispatchFailureAndReplay_PreserveCommittedOrderAndFrozenArchive。 | 正式 dispatcher 的獨立 scope、持久化用途、重送／巢狀／並行及替身效果邊界已驗證；真實付款／出貨／統計 adapter 不存在，不適用。2.6 已驗證直接結果、完整 log 與稍後失敗仍保留先前略過。 |
| 014 用途不明 BUG | MemberServiceTests.TokenPurposeContradiction_StopsWithErrorAndDoesNotRepair；ExecutionScopeTests.CorruptStoredPurpose_ReturnsContextBugWithoutEffects；ProtectedWriteTests.MissingOrMismatchedContext_StopsBeforeWrites。 | 會員／Checkout／維護邊界已有證據；事件入口已由 OrderEventScopeTests.InvalidSavedOrder_StopsBeforeHandlerConstructionAndDoesNotRepair／LostEventContext_IsBugBeforeAnyCommercialEffect 與 Core 手工 Bind 防繞過案例覆蓋；2.6 已驗證 500／failed／execution_context_invalid 及不可信用途 header 省略。 |
| 015 測試預約不接 Cart | 無對應 TiP 自動化證據。 | PetShop 排除本輪。 |

Phase 1.1 admin API 是額外查核能力，其專屬認證／篩選／一致性測試見 [Phase 1.1 交付](phase1.1-delivery.md)，不取代上述交易流程驗收。2.4 完整命令、結果及限制見 [交付紀錄](phase2.4-delivery.md)。

Phase 2.5 新增 24 項事件 scope 整合測試與 1 項 Core 載入邊界測試；solution 310 項通過。命令、測試替身範圍與環境狀態見 [2.5 交付紀錄](phase2.5-delivery.md)。

Phase 2.6 新增 17 項並補強既有出口／事件案例；solution 327 項通過。HTTP-001～017、CLI／事件結果、token log 清理的對照與實際限制見 [2.6 交付紀錄](phase2.6-delivery.md)。後續 2.7 已完成標準流程部署驗收，仍不可將不存在的 adapter 或範圍外 AC 視為通過。

Phase 2.7 新增 8 項 Storefront PageModel／client 測試，solution 335 項通過。Edge 四組用途、測試付款、庫存不足拒絕及 test Cart 增刪，搭配 admin 與一致性 LiteDB 快照驗證 FLOW-101～104、201～204、302、308 的部署行為。實際數值、工具限制與既定不適用項目見 [2.7 交付紀錄](phase2.7-delivery.md)；UI 驗證不是 unit test。
