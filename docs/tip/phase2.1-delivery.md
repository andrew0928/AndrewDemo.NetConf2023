# TiP Phase 2.1 交付紀錄

日期：2026-10-18。分支：`feature-test-in-production`。本階段完成會員／Token 用途規則，供 review；下一階段為 2.2。

## 實作與儲存邊界

- Core `MemberService` 集中標準 API／CLI 的會員建立、Token 簽發與驗證、會員更新及 Token 更新。公開 HTTP request 不新增指定 ID／IsTest 欄位。
- 建立會員時先開 LiteDB transaction，寫入 `member_write_lock` 固定文件以取得資料庫寫入鎖，再查既有姓名、配發最終 ID、查白名單並首次 Insert。不同服務實例及 Shared 連線使用相同資料庫鎖；失敗 rollback，重試重新讀取保存狀態。
- 一般 ID 由目前最大會員 ID 加一，空集合從 1 開始；受控初始化可呼叫 `InitializeMember(id, name)`。ID 已存在不覆寫；相同 ID／姓名再次初始化保留原用途。預設白名單維持 MemberId 900001、PaymentId 990001；API 以 factory 明確呼叫無參數建構式，避免 DI 將兩份空 `IEnumerable<int>` 當成覆寫清單。
- Token 僅以已保存 MemberId／IsTest 簽發，使用 Insert 而非 Upsert；驗證存在性、到期與會員存在性。到期時間轉 UTC 後與 TimeProvider 比較，避免 LiteDB 還原為本機時間所造成的誤判。
- Token／Member 用途矛盾時記錄 Error 並拋出 `ExecutionContextInvalidException`，不重新查白名單或降級為匿名。用途改寫或 Token 會員重新綁定記錄 Warning 並拒絕；合法註記／期限更新保留用途。新增 Core log 不包含 Token 明文。
- 原始 collection／`Database` 仍為基礎儲存能力，供 fixture、舊 BSON 與受控資料維護使用；本次保證位於 `MemberService` 應用寫入邊界，不宣稱可防止任意 SQL 或直接 collection 寫入。標準 host 已移除直接會員／Token 寫入，舊 `ShopDatabase.Create` 亦拒絕這兩種物件。AppleBTS seed 等非本階段入口未調整。
- OAuth code 儲存改用 ConcurrentDictionary 與 TryRemove，避免並行登入／交換使用非執行緒安全 Dictionary。

## 驗收證據

既定案例不改寫；新增可執行證據位於 [MemberServiceTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/MemberServiceTests.cs)（13 筆）及 [MemberFlowTests](../../tests/AndrewDemo.NetConf2023.WebContract.Tests/MemberFlowTests.cs)（2 筆）。

| 定案案例 | 本次驗證 |
|---|---|
| FLOW-001 | 最終自動 ID 命中／未命中、預設 900001 受控初始化；提交前檢查首次寫入已帶正確用途。 |
| FLOW-002 | 正式／test Token 繼承保存值；改用不同白名單服務簽發仍一致；OAuth controller 實際保存 test Token。 |
| FLOW-003 | 缺 IsTest 的原始 BSON 會員／Token 登入後維持 false，未回填原 BSON；重複初始化不重新分類。 |
| FLOW-004 | 32 筆不同名與 16 次同名並行註冊；兩條 Shared 連線並行建立 20 筆；ID 衝突、Insert 後 Commit 前注入故障、rollback 後重試。 |
| FLOW-005 | Token／Member 兩方向矛盾均拋用途錯誤並留 Error log，不修補；controller 在進入 Checkout 前終止。 |
| FLOW-401／402（Member／Token 部分） | 兩方向用途變更拒絕、資料不變、Warning log 不帶 Token；合法註記與期限更新保持用途，Token 不可重新綁定會員。Checkout／Order 部分留 2.3。 |
| Token 驗證 | 缺少、未知、到期邊界與會員不存在；不存在會員不能簽發。 |

2026-10-18 實測：

```sh
dotnet build src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo --verbosity minimal -m:1 -nr:false
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --nologo -m:1 -nr:false
```

- solution build 成功，0 errors、5 個既有 warnings（ShopDatabase obsolete 2 個、ShopRuntimeOptions XML comment 3 個）。
- solution tests：131 passed、0 failed、0 skipped；Core 80、WebContract 11、AppleBTS.Extension 19、PetShop.Extension 13、PetShop.API 8。
- ConsoleUI build 成功，0 errors、0 warnings。
- 起初 NuGet 網路與 MSBuild named pipe 受沙箱限制，取得執行授權後完成還原／測試；不將受阻的執行計為通過。
- 未重建 Docker images、未修改使用者本機 DB；測試全部使用隔離暫存 DB。未執行瀏覽器或完整 HTTP server 驗收。

## 下一階段交接

- 2.2：接入獨立 scoped Identity／Transaction、HTTP middleware 與 CLI 操作 scope；會員驗證沿用 `MemberService.ValidateToken`。目前 controller 只在需要會員的操作呼叫驗證，middleware 全 request 身分驗證仍待接入。
- 2.3：Checkout／Order 分類、固定內容、用途不可變、封存與原子性尚未交付。
- 2.6：將 `ExecutionContextInvalidException` 映射為定案的 500／`execution_context_invalid`、OperationResult 與 headers；既有 middleware／OAuth 明文 Token log 仍須依原計畫完整清除。
- 本次只有交接計畫的進度欄與 docs 更新，Phase 1 frozen spec／testcases 與 Abstract 未異動。沒有重新開啟契約或新增待決業務政策。

> 2026-10-18 後續修訂：會員白名單已改為 Name（預設 test），本文原 ID 白名單敘述保留為當時交付證據；現行規則見 [決策](../decisions/2026-10-18-tip-member-name-allowlist.md)。
