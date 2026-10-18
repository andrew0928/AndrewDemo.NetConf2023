# TiP Phase 2.2 交付紀錄

日期：2026-10-18。分支：`feature-test-in-production`。Phase 2.2 完成，供 review；下一階段為 2.3。前階段未提交的變更保留於同一工作樹。

## 確認的 endpoint 範圍

| Endpoint | Transaction context |
|---|---|
| `POST /api/checkout/create` | 入口為 null。成功建立後綁定新 Checkout 的工作接續於 2.3。 |
| `POST /api/checkout/complete` | MVC 解析 body 的 transactionId；確認 Member 身分與買方權限後載入保存的 Checkout 用途。 |
| Cart 全部操作、Products、Member 與會員訂單集合、OAuth | 不綁定單筆 transaction；未宣告 metadata 的一般 id 不視為交易 ID。 |

未來 `/api/orders/{id}/xxx` 出貨等後置處理必須附帶 Order transaction context，事件／任務另開 scope 恢復保存用途。目前不存在這些 endpoint／工作，只有[決策註記](../decisions/2026-10-18-tip-order-postprocessing-context.md)，本階段沒有新增其實作。

## 實作

- Core `AddShopExecutionContexts` 分別註冊 IdentityContext／TransactionContext 為 scoped，讀取介面解析到各自同一實例；CheckoutService 改為 scoped。
- API 明確 `UseRouting` 後執行 `ShopExecutionMiddleware`。匹配的受保護 API endpoint 驗證 Bearer Token，從保存會員初始化身分；匿名商品 endpoint 無 Token 時維持匿名，有 Token 時仍驗證。OAuth 使用匿名 request scope。
- `HttpContext.Items[typeof(IdentityContext)]`／`[typeof(TransactionContext)]` 引用 DI 中同一實例；已驗證的 Token 保留於既有 `access-token` key。
- `CheckoutTransactionAttribute` 明確宣告 ID 來自 MVC 綁定的 CheckoutCompleteRequest.TransactionId。全域 action filter 僅處理具此 metadata 的 endpoint，在 MVC 模型驗證成功後呼叫 Core `CheckoutContextLoader`。
- JSON 只由 MVC 解析一次，filter 不讀取或消耗 body，因此 casing、數字字串、重複欄位與 UTF-16 等行為直接沿用同一套 MVC 設定。Complete request 標記 TransactionId 必須存在，並設定 64 KiB request 上限；格式／缺值錯誤回 400，超過限制依 ASP.NET 邊界拒絕，不支援媒體型態回 415。
- Core loader 驗證買方後才綁定；不存在回 400、買方不符回 403。只讀取必要 BSON 欄位，舊 PaymentId=null 不會提前造成反序列化失敗，仍能抵達既有 409 分支。用途缺欄位為 false，已保存的不合法用途值視為 BUG。
- 為了完成 2.2 錯誤邊界，提前接入 `ExecutionContextInvalidException` 的 500／`execution_context_invalid` 映射與 Error log；完整 OperationResult／headers 仍在 2.6。舊 middleware 的 Token 明文輸出隨替換一併移除，OAuth 其餘路徑仍留 2.6 全面驗證。
- CLI 使用 DI provider 與 `ShopOperationScope`，每次命令及 Semantic Kernel 工具呼叫建立自己的 scope，從 Token 恢復身分。巢狀呼叫結束或拋錯時還原外層 scope；AsyncLocal 只保存目前 scope 的服務入口，不共用 Context 實例。Checkout complete 使用同一 Core loader，CheckoutService 從目前 scope 解析；明確 RequestMember 輸入維持不變。

## 驗證

新增 [ExecutionScopeTests](../../tests/AndrewDemo.NetConf2023.WebContract.Tests/ExecutionScopeTests.cs) 22 筆。HTTP 案例實際啟動 loopback Kestrel、MVC 與本次 middleware／filter，使用隔離暫存 DB；測試專用 probe controller 只存在測試 assembly，不加入正式 API。

| 範圍 | 證據 |
|---|---|
| HTTP-001／012 的 context 部分 | 匿名 Products、受驗證一般 id、會員訂單集合；無交易維持 null，不猜 Cart／其他 id。完整 headers 驗收留 2.6。 |
| HTTP-006／007 | malformed JSON、缺 ID、null、溢位、錯誤型別、大小限制、媒體型態、UTF-16；大小寫、數字字串及重複欄位的 action ID 與交易 ID 一致；Items／DI 同實例。 |
| HTTP-008 | 缺少／錯誤 scheme／未知 Token 回 401；他人 Checkout 回 403、缺交易回 400；沒有訂單副作用。 |
| HTTP-009 的入口部分 | Token／Member 矛盾與交易保存用途損壞回 500 與穩定 code，不輸出用途 header；Core loader 缺會員身分拋用途 BUG。完整 Outcome 留 2.6。 |
| HTTP-011 的 scope 部分 | 20 筆正式／test HTTP request 並行，及後續無交易 request，context 各自正確。 |
| HTTP-014 | 真實 HTTP complete 遇到 PaymentId=null 的舊 Checkout 回 409，保留紀錄且不建立 Order。 |
| CLI scope | 同一 scope 介面與 concrete 同實例、CheckoutService 同 scope 共用／跨 scope 分離；巢狀身分不污染外層；無效 Token 拒絕、匿名 scope 為 null transaction；20 筆不同身分的並行操作隔離；root 不可解析 scoped CheckoutService。 |

2026-10-18 執行：

```sh
dotnet build src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo --verbosity minimal -m:1 -nr:false
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo -m:1 -nr:false
```

- solution 全套 **153 passed、0 failed、0 skipped**：Core 80、WebContract 33、AppleBTS.Extension 19、PetShop.Extension 13、PetShop.API 8。
- solution 與 ConsoleUI build 成功；無新增編譯警告。
- CLI 驗證為實際編譯加共用 operation scope 的自動化驗證，未呼叫外部模型或進行互動式購物 smoke。
- Docker images 未重建，本機使用者 DB 未動；沒有宣稱完整 TiP UI／商業效果隔離已驗收。

## 交接

- 2.3：接續 create 用途判定與成功後 Bind、固定交易內容、Order 繼承與封存；complete 結束保留 Checkout 快照至出口。不可用當前白名單重分類保存交易。
- 2.4／2.5：受保護寫入與事件獨立 scope 尚未交付。現有 singleton product／event services 尚未注入 scoped context，接入時須配合其 lifetime。
- 2.6：OperationResult、所有 headers、完整結構化 log 與 OAuth 明文 Token 清理尚未完成；沿用本次錯誤映射，不另開重複出口。
- 僅更新交接計畫的進度及 docs；未更動 Phase 1 frozen spec／testcases 或 Abstract。
