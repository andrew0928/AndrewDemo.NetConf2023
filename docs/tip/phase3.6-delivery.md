# Phase 3.6：整體驗收與 review

2026-10-19。Phase 3.1～3.6 已實作並完成自我驗證，使用者已授權 commit／push。分支 `feature-test-in-production`；未遷移現有部署 DB。`/spec` 與 `src/AndrewDemo.NetConf2023.Abstract` 保持凍結，spec 中「未開始」是上次規格提交時的歷史狀態，本文件是目前實作交付紀錄。

## 交付入口

| 階段 | 內容 |
|---|---|
| [3.1](phase3.1-delivery.md) | 六個 collections、locator、用途一致性、唯一 ID、版本閘門；C4／class 圖 |
| [3.2](phase3.2-delivery.md) | Checkout 原子提交、事件用途延續、交易擁有者；sequence 圖 |
| [3.3](phase3.3-delivery.md) | test identity 改價拒絕、403、SKU 與交易效果政策 |
| [3.4](phase3.4-delivery.md) | Admin／會員歷史／ConsoleUI、跨程序一致性 |
| [3.5](phase3.5-delivery.md) | 非破壞式離線遷移、備份／切換／回退手冊 |

實作採 concrete Storage 元件，沒有新增 repository framework。交易 storage 由保存用途決定，認證及非交易資訊共用；API 路徑與 public contract 不拆分。

## 建置與自動化

macOS arm64、.NET SDK 10.0.401、LiteDB 5.0.21。所有 MSBuild 串行 `-m:1`。測試使用 temporary DB；vstest 需要本機 socket，於 sandbox 外執行。

```sh
dotnet build src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --no-build --nologo -m:1
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo -m:1
python3 scripts/tip/run-phase3-smoke.py --keep-running
git diff --check
```

- 修改前 baseline：448 passed。
- 全方案最終回歸：493 passed，0 failed／skipped。Core 202、WebContract 145、AdminCLI 106、AppleBTS Extension 19、PetShop Extension 13、PetShop API 8。
- 覆蓋 review 再補「綁定兩用途仍拒絕 test identity 改價」及「同一 test Checkout 並行完成」，最後針對性執行 5 個案例全數通過（新增 3 個組合；合計 496 個已執行的不同案例）。未把局部重跑加總成整套數量。
- solution 與獨立 ConsoleUI 最後建置皆為 0 warnings／0 errors。先前完整重編時的既有 obsolete／XML 註解 warning 未當成本輪修正範圍；新增 nullable／analyzer warning 已清理。
- `/private/tmp/tip-phase3-final-all.log`、`tip-phase3-final-target.log`、`tip-phase3-final-boundary.log`、`tip-phase3-build.log`、`tip-phase3-console-build.log` 保存命令結果；未加入 Git。
- source audit：一般應用沒有 raw orders／checkout collection 存取；只使用集中 mapping／store／migration。凍結目錄 diff 為空，無新增依賴套件。281 個本機文件連結均存在；已順手將決策索引既有失效絕對路徑改為相對路徑。

## 標準 host、CLI 與 Edge

[重現腳本](../../scripts/tip/run-phase3-smoke.py) 先複製 initializer 到全新 temporary 目錄、建立全新 DB，再開 `127.0.0.1:5187` API 與 `127.0.0.1:5188` CommonStorefront。獨立產生本次 admin token，不讀取或輸出現有部署秘密。API 真實 HTTP 與 AdminCLI 可自動重現，UI 由 Computer Use 操作 Edge。

商品：可樂 Product 2、SKU-COKE-350、18 元、初始庫存 100。API 先執行四組，再由 Edge 逐組執行登入、加入購物車、填 PaymentID、送單與歷史查詢。

| 會員／付款 | API Checkout／Order | Edge Checkout／Order | 結果 |
|---|---|---|---|
| phase3-normal／0 | 1／1 | 5／5 | normal、succeeded、扣庫存 |
| phase3-normal／990001 | 2／2 | 6／6 | test、succeeded、inventory.deduct 略過 |
| test／0 | 3／3 | 7／7 | test、succeeded、inventory.deduct 略過 |
| test／990001 | 4／4 | 8／8 | test、succeeded、inventory.deduct 略過 |
| test／0，101 件庫存不足 | — | 9／無 | test、rejected、沒有虛構略過；購物車與 active 保留 |

一般會員頁只顯示自己的 #1／#2／#5／#6，含兩用途；test 會員只顯示自己的 #3／#4／#7／#8。AdminCLI normal／test／all 的 rows IDs 與 Admin API 比對一致，API 四筆完成時金額分別為 18／54／72。

所有 hosts 停止後以獨立 raw read-back 核對：

- `orders` 與 `archived_checkout_transactions`：#1、#5，2 筆／36 元。
- `test_orders` 與 `test_archived_checkout_transactions`：#2、#3、#4、#6、#7、#8，6 筆／108 元。
- normal active 為空；test active 只剩拒絕的 #9，原付款 0、固定金額 1818。
- 每筆 Order 關聯相同用途的 archive；另一區沒有相同 ID。庫存只有可樂 100 → 98，其餘兩個 SKU 都仍 100。
- 共用 Member 2 筆、Token 4 筆，storage version 1。沒有 test_members／test_carts 分流。

本輪證據目錄：`/var/folders/xd/dyjwkss97bb5_vfgrsx8q44h0000gr/T/tip-phase3-smoke-trjd9a7p`，內含 API cases、三組 CLI JSON、host logs 與 raw-readback.json。Edge 畫面為 `/private/tmp/tip-phase3-ui-test.jpg`、`/private/tmp/tip-phase3-ui-rejected.jpg`。本次只核對受本重構影響的 UI 行為，沒有重新宣稱全套行動版／無障礙驗收。

## STORE 覆蓋對照

下表全部通過；方法是 executable tests 與上述 host smoke 的組合，不以檔名或 collection 存在代替行為驗收。

| STORE | 證據 |
|---|---|
| 001 | TransactionStorageTests.Lifecycle_UsesOnlyItsPersistedPartition 四組 raw read-back；API／Edge 四組 |
| 002 | CheckoutOutputs_UsePersistedPurposeAndKeepEverySkippedTarget；NormalMemberHistory_IncludesOwnBothPurposesAndExcludesOtherBuyers；Edge 一般會員歷史 |
| 003 | CheckoutLifecycleTests.CompletionIgnoresChangedAllowlistAndLegacyMissingPurposeStaysFormal；QuoteAndCompletion_FixPurposeAmountsAndContext；HTTP 新 scope 完成 |
| 004 | ReinsertCannotChangePurposeOrOverwrite；PersistedPurposes_CannotBeReclassifiedThroughCollectionWrites；ArchiveAndActiveGuards_RejectMutationAndReactivation |
| 005 | Identifiers_AreUniqueAcrossAllFourCheckoutPartitions；ConcurrentCompletionAndCreate_UseUniqueIdsAndDoNotOverwriteUnrelatedOrders；StorageProcessTests 重開後配號 |
| 006 | CorruptData_IsRejectedByLocatorAndAdmin；CorruptStoredPurpose_ReturnsContextBugWithoutEffects；AdminQueryHttpTests 的錯誤映射 |
| 007 | CorruptData_IsRejectedByLocatorAndAdmin；LegacyMissingPurposeStaysFormal；遷移保留缺值 |
| 008 | TwoTestMembers_CannotCompleteEachOthersCheckout_AndNoEffectsOccur；既有 normal／test buyer mismatch 測試 |
| 009 | FailureAtEveryWrite_RollsBackAndAllowsRetry：normal 五步、test 四步；FailureAfterActualSkip_RetainsEffectEvidenceWithoutDatabaseWrites |
| 010 | 同一 Checkout 的 normal／test 並行完成；HTTP 四組重送均 400 |
| 011 | ProtectedWriteTests.TwoMembers_ConcurrentFormalAndTestCheckout_OnlyFormalAffectsStockAndTotals；OrderEventScopeTests 計數替身；raw read-back |
| 012 | NecessaryValidation_RejectsWithoutAnyEffects；InventoryRejection_AndMissingPayment_DoNotInventSkippedEffects；Edge Checkout #9 |
| 013 | OrderEventScopeTests：新 scope、重送、巢狀、並行、履約失敗；TestOrderSystemHandler_CannotEscapeThroughIndependentMaintenance |
| 014 | CorruptData_IsRejectedByLocatorAndAdmin 關聯矛盾；InvalidSavedOrder_StopsBeforeHandlerConstructionAndDoesNotRepair |
| 015 | AdminQueryTests／AdminQueryHttpTests；AdminMergesAllPartitionsBeforePaging_AndDefaultReadsOnlyNormal |
| 016 | CheckoutMove_BlocksBetweenPartitionReads_ThenFreshReadSeesArchive；SeparateWriterProcess_CheckoutMovementAndOrderSummariesRemainCoherent |
| 017 | normal 會員兩用途 HTTP 歷史、AdminCLI／API JSON 比對、Edge 訂單頁；ConsoleUI 使用同 store 並建置 |
| 018 | MemberServiceTests；ExecutionScopeTests 的 Cart／protected BSON read-back；standard host 共用資料 |
| 019 | TestIdentityPriceChange_ReturnsExplicit403_AndLaterCheckoutKeepsOriginalPrice；ProtectedWriteTests 拒絕 log |
| 020 | TestIdentity_PriceChangeIsRejectedInsteadOfSkipped 未綁定／兩用途綁定；normal 改價與一般權限拒絕；維護平行 scope |
| 021 | ProtectedWriteTests 的 SKU validate／skip；MaintenanceOutput_PreservesActualEffectsEvenWhenLaterStepFails |
| 022 | MigrationPreservesRawFieldsAndLegacyMissingValues_AndRepeatsAsNoOp；既有 LegacyQuoteViaHttp 的 409 |
| 023 | MigrationRejectsAmbiguityWithoutPartialWrites 六組 |
| 024 | FailureRollsBackEveryMoveAndVersion_ThenRetrySucceeds 八組 |
| 025 | HostRejectsUnmigratedAndUnknownVersion_AndAcceptsMigratedDatabase；實際 seed／API smoke |
| 026 | MigrationCommand_BackupRestoreRepeatAndInvalidArgumentsPreserveData；3.5 操作手冊 |

## Review 發現與限制

已修正：Shared mode 的巢狀 BeginTrans 洩漏／逾時；locator 提前拒絕後遺漏的事件錯誤 log；舊 mixed-collection fixture 與舊改價成功略過斷言。新增跨程序、raw field preservation、故障 rollback 與交易 begin 次數測試防止回歸。

目前單 LiteDB、共同鎖會序列化交易讀寫，符合 PoC 範圍，沒有宣稱吞吐量 SLO。未來分成實體 DB 時，必須另設計配號、查詢與原子性。現有部署必須依 3.5 停寫遷移，舊 writer 不可接回新 layout。

真實金流、出貨、事件統計 adapter 不存在，標示不適用；以替身驗證禁呼叫與 scope 邊界。AppleBTS／PetShop 僅完成共用改動回歸，其擴充 TiP 政策與預約到期整理仍不在本次範圍。
