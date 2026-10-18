# TiP Acceptance Criteria 自動化覆蓋對照

更新：2026-10-18，Phase 2.5。`accepted` 代表需求已確認；測試總數不代表所有 AC 通過。本表區分 Core／HTTP 行為證據與仍待交付的整體條件，不修改原接受條件。

測試以 `Core.Tests`、`WebContract.Tests` 下同名類別定位；含單元、LiteDB 整合及 Kestrel HTTP 測試。歷史各階段測試數以各自交付紀錄為準。

| AC | 目前具體證據 | 尚未覆蓋／邊界 |
|---|---|---|
| 001 一般測試交易 | MemberServiceTests 註冊／Token；CheckoutLifecycleTests.QuoteAndCompletion_FixPurposeAmountsAndContext；ProtectedWriteTests.TestCheckout_ValidatesEverySkuBeforeSkipping_AndNeverCallsInventoryWrite；AdminQueryTests.Orders_UseOwnPurposeAndSummarizeBeforePagination。 | 整條 UI 流程及交易前後正式統計對照尚未完成；完整出口 2.6／UI 2.7。付款／出貨及事件統計 adapter 不存在，不算通過。 |
| 002 測試 PaymentId | CheckoutLifecycleTests.QuoteAndCompletion_FixPurposeAmountsAndContext 四組參數；CompletionIgnoresChangedAllowlistAndLegacyMissingPurposeStaysFormal。 | 核心用途與庫存通過；完整出口及 UI 付款選擇待 2.6／2.7。 |
| 003 庫存不足 | CheckoutLifecycleTests.NecessaryValidation_RejectsWithoutAnyEffects、MultipleLines_PreserveRelationsAndValidateCombinedSkuQuantity；ProtectedWriteTests.TestCheckout_ValidatesEverySkuBeforeSkipping_AndNeverCallsInventoryWrite。 | 核心拒絕／無副作用通過；HTTP Outcome 待 2.6。 |
| 004 用途不可變 | MemberServiceTests.Updates_RejectPurposeChangesAndPreservePurposeOnLegalUpdates；CheckoutLifecycleTests.PersistedPurposes_CannotBeReclassifiedThroughCollectionWrites。 | 已有四種物件雙向用途保護；所有入口 log 的完整斷言仍待 2.6。raw DB 不是此應用保證邊界。 |
| 005 商品改價 | ProtectedWriteTests.Maintenance_ValidatesThenWritesOrSkips_AndPreservesOtherFields、Maintenance_InvalidOrUnauthorizedRequests_DoNotWriteOrRecordSkips。 | Core AC 行為已覆蓋：授權／驗證、完全不寫、重讀原價、正式交易原價、目標與要求 log；無維護 API。完整 HTTP／結果另屬 2.6。 |
| 006 SKU 修改 | 同上，包含規格、Product 關聯及庫存不變。 | Core AC 行為已覆蓋；無維護 API。完整出口另屬 2.6。 |
| 007／008 測試預約 | 無對應 TiP 自動化證據。 | PetShop 排除本輪，既有一般預約測試不等同這兩條通過。 |
| 009 正式／test 並行 | ProtectedWriteTests.TwoMembers_ConcurrentFormalAndTestCheckout_OnlyFormalAffectsStockAndTotals；ExecutionScopeTests.ConcurrentAndSubsequentRequests_HaveIndependentContexts。 | 已對上兩位會員、3／2 件、300／200 元、庫存 7、正式統計 1 筆／300。事件 scope／用途邊界已於 2.5 驗證；完整 headers／log 2.6；不存在 adapter 不適用。 |
| 010 test Cart | ExecutionScopeTests.TestMember_CartAddAndDeletePersistWithoutProtectedChangesOrSkips。 | test Token 的真實增刪、DB／HTTP 重讀、無 transaction／略過、受保護資料不變均已覆蓋；完整出口 log 串接仍在 2.6。 |
| 011 他人 Checkout | ProtectedWriteTests.TwoTestMembers_BuyerMismatchPreservesDataAndDoesNotDispatchOrSkip；ExecutionScopeTests.TwoTestMembers_CannotCompleteEachOthersCheckout_AndNoEffectsOccur。 | 雙方 test 的 Core／HTTP 行為已覆蓋，含拒絕 log 與無事件／效果。完整出口格式仍在 2.6。 |
| 012 系統整理到期預約 | 無對應 TiP 自動化證據。 | PetShop 排除本輪；需另實作通知／系統處理邊界。 |
| 013 事件／重送 | OrderEventScopeTests.ReplayAfterCallerScopeDisposed_RestoresPersistedOrderPurpose、NestedDispatch_IncludingFailure_RetainsOuterOrderScope、ParallelDispatch_KeepsSystemOrderAndEffectsIsolated、CheckoutDispatchFailureAndReplay_PreserveCommittedOrderAndFrozenArchive。 | 正式 dispatcher 的獨立 scope、持久化用途、重送／巢狀／並行及替身效果邊界已驗證；真實付款／出貨／統計 adapter 不存在，不適用。完整事件 OperationResult／log 格式待 2.6。 |
| 014 用途不明 BUG | MemberServiceTests.TokenPurposeContradiction_StopsWithErrorAndDoesNotRepair；ExecutionScopeTests.CorruptStoredPurpose_ReturnsContextBugWithoutEffects；ProtectedWriteTests.MissingOrMismatchedContext_StopsBeforeWrites。 | 會員／Checkout／維護邊界已有證據；事件入口已由 OrderEventScopeTests.InvalidSavedOrder_StopsBeforeHandlerConstructionAndDoesNotRepair／LostEventContext_IsBugBeforeAnyCommercialEffect 與 Core 手工 Bind 防繞過案例覆蓋；完整出口待 2.6。 |
| 015 測試預約不接 Cart | 無對應 TiP 自動化證據。 | PetShop 排除本輪。 |

Phase 1.1 admin API 是額外查核能力，其專屬認證／篩選／一致性測試見 [Phase 1.1 交付](phase1.1-delivery.md)，不取代上述交易流程驗收。2.4 完整命令、結果及限制見 [交付紀錄](phase2.4-delivery.md)。

Phase 2.5 新增 24 項事件 scope 整合測試與 1 項 Core 載入邊界測試；solution 310 項通過。命令、測試替身範圍與環境狀態見 [2.5 交付紀錄](phase2.5-delivery.md)。
