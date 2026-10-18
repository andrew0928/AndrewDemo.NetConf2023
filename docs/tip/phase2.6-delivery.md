# TiP Phase 2.6 交付紀錄

日期：2026-10-18。分支：`feature-test-in-production`。使用者授權提交的 2.5 已提交為 `64ced22`。本階段完成標準 Core／API／CLI／事件的結果出口與正常 logging；Abstract、frozen 業務規格及既有成功 response body 未變更。

## 實作與使用方式

- 新增 scoped `OperationExecution`。由 HTTP、CLI 或事件邊界開始操作，出口建立不可變 `OperationResult`，分別保留 IsTestBehavior、Outcome 與逐筆 SkippedEffects。只有明確載入交易的操作才使用 transaction 用途；一般操作使用 identity。出口不查白名單。
- `OperationEffects` 在已開始的操作內保留全部實際略過步驟；服務內部的 Clear 不再抹除同一次操作先前的效果。未進入 host operation 的既有直接服務呼叫仍維持每次呼叫清理的行為。
- HTTP middleware 在 OnStarting 輸出小寫 Behavior／Outcome、排序去重的效果代碼及已授權 transaction headers。無交易時省略三個交易 headers；complete 移除作用中交易後仍使用本次 Checkout 快照，不混入事件的 Order context。
- 400／401／403／409／413／415 等拒絕保留原有 HTTP 語意並輸出 rejected；非預期錯誤為 failed。用途 BUG 維持 500／execution_context_invalid，清除不可信 transaction，不輸出 Behavior 或交易 headers。無效／過期 token 與尚未確認身分即發生的基礎設施錯誤也不捏造 normal 分類。
- 已取得可信用途後發生錯誤，結果仍保留該用途與先前實際略過的效果。正常寫入後的失敗不假稱 rollback，也不產生 test 略過紀錄。用途本身不可信時 `OperationExecution.Result` 為 null，但 Outcome、實際效果收集器及診斷證據保留。
- 略過 log 在操作出口以最終 Outcome 輸出，包含 TraceId、ShopId、IdentityKind／MemberId、TransactionKind／Id／IsTest、EffectCode、TargetType／Id、ReasonCode 與 RequestedChange。逐筆使用當時的效果判定快照，不因後續 context 改變而改寫已發生的證據。另記錄操作結束與拒絕／失敗，沒有實際 adapter 或略過時不製造效果 log。
- HTTP TraceId 使用當次 HttpContext.TraceIdentifier；CLI／每次事件產生獨立識別碼。嵌套事件有自己的結果與診斷紀錄，外層 Checkout 的結果不包含內層效果。
- `ShopOperationScope.Complete(outcome)`／`Fail(exception)` 可直接取得結果；`Execution` 保留出口快照。Dispose 只釋放資源，不推測成功。ConsoleUI 的共用 `RunOperation` 包裝命令／Kernel 操作，顯示用途、結果、交易與逐筆略過資訊；bool／nullable 結果與業務例外明確映射拒絕。付款等待改取原始 async 例外，避免 AggregateException 將業務拒絕誤報 failed。
- `DefaultOrderEventDispatcher.DispatchWithResult` 是 Core concrete 便利入口，Completed／Cancelled 成功均可直接取得結果；原 Abstract void Dispatch 保持不變。失敗會先完成該 scope 的結果／log，再重拋原例外；handler 可注入 OperationExecution 觀測。離開事件前再次確認保存用途的快照未被替換，載入用途前失敗不以 System.IsTest=false 推測正式。
- OAuth 改用 ILogger 的固定訊息與 MemberId，移除 token、grant code、完整 request／redirect／state 的 Console 輸出。middleware 不輸出 Authorization 或完整例外訊息；原本 token response／授權 redirect 本身保留，沒有改動認證合約。

CLI 的直接使用範例（受信任程式內，不是新增公開 endpoint）：

```csharp
using var operation = ShopOperationScope.Open(scopeFactory, accessToken);
try
{
    operation.Services.GetRequiredService<ProductMaintenanceService>()
        .UpdatePrice(productId, price, authorize);
    OperationResult? result = operation.Complete();
    // 讀取 result.IsTestBehavior、Outcome、SkippedEffects。
}
catch (Exception error)
{
    operation.Fail(error);
    throw;
}
```

每次操作使用新 scope；HTTP 已由 middleware 自動完成結果，不應由 controller 提早凍結出口。這些是 Core 的內部組裝 API，沒有新增 Abstract contract。

## 驗收證據

[OperationOutputTests](../../tests/AndrewDemo.NetConf2023.WebContract.Tests/OperationOutputTests.cs) 為 ExecutionScopeTests 的 partial 檔，沿用真實 Kestrel／MVC／正式 middleware 及隔離 LiteDB。[OrderEventScopeTests](../../tests/AndrewDemo.NetConf2023.WebContract.Tests/OrderEventScopeTests.cs) 使用正式 dispatcher。

| 規格 | 已驗證內容 |
|---|---|
| HTTP-001／012 | 匿名商品、test Cart、自己的訂單集合沒有單筆交易 headers；訂單逐筆保留用途。 |
| HTTP-002／003／010 | 四組會員／PaymentId 組合；create 成功 body 欄位未變，complete 使用已封存 Checkout 的快照；兩個 SKU 保留兩筆 Result／log，header 只有一次 inventory.deduct；重複完成 rejected。 |
| HTTP-004／016 | 僅註冊在測試 host 的維護探針：商品／SKU 必要驗證、原資料不變、無 transaction、三筆略過／兩種排序去重代碼；成功、拒絕與後續失敗的 Outcome 分開驗證。production 沒有新增維護 API。 |
| HTTP-005 | 庫存不足 400／rejected，交易保留、沒有 Order 或虛構 inventory.deduct；缺 paymentId 仍 400。 |
| HTTP-006／007 | 沿用前階段 MVC 唯一解析的格式、缺值、大小、UTF-16、重複欄位案例；補上正式 header 斷言，保留 Items 與 DI 同實例。 |
| HTTP-008／009／014 | 無效 token 401、雙 test 買方不符 403、不存在交易 400、舊交易 409；不洩漏他人 transaction。矛盾／缺失用途 500、failed 且省略不可信用途。 |
| HTTP-011 | 並行正式／test 的真實 Checkout complete，headers、Result、TraceId 與逐筆 log 分別對應交易；只有正式庫存異動。 |
| HTTP-013 | CLI 無 HttpContext 的成功／拒絕／失敗結果；事件直接回傳結果、System／Order 用途、後續失敗仍保留既有略過與 failed log；既有重送／巢狀／並行測試同步驗證結果。 |
| HTTP-015 | 既有 OperationResultContractTests 維持集合複製／唯讀；新增 CLI 結果完成後不隨 identity 改變，再次 Complete 回傳原快照。 |
| HTTP-017 | OAuth 授權、交換、重複／無效 code、缺登入名稱，正常／無效／過期／用途矛盾 API token；擷取應用 logger 的訊息、屬性、例外與 Console，均無完整 token／授權碼。framework log 使用與正式 appsettings 相同的 Microsoft.AspNetCore=Warning。 |
| 真實效果後失敗 | 正式維護已更新商品／SKU 後拋錯，回 failed、沒有任何 test 略過，資料保留實際已完成異動。 |

測試中的 shipment／statistics 仍只有可計數替身，真實付款／出貨／統計 adapter 不存在。沒有新增 queue、重送 API、取消入口或管理 UI。

## 執行紀錄

```sh
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false --verbosity quiet
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false --verbosity quiet
```

- 最終 solution **327 passed、0 failed、0 skipped**：Core 161、WebContract 126、AppleBTS Extension 19、PetShop Extension 13、PetShop API 8。本階段新增 17 項，並擴充既有出口與事件案例。
- ConsoleUI build 成功，0 warnings／0 errors。最終 solution 僅出現既有 2 項 ShopDatabase obsolete 警告；首輪重新編譯亦有既有 API XML 文件警告，新增 xUnit analyzer 警告已修正。
- 首輪發現過大 body 的 BadHttpRequestException 被通用 catch 改成 500，已改為保留原 StatusCode 並通過回歸。測試曾以 domain Order 反序列化 response、誤將集合 envelope 當陣列，已改用實際 JSON 形狀斷言，沒有為測試修改 response 合約。
- `git diff --check` 通過。沒有更動 Abstract 或 frozen 業務規格；只追加 testcase 證據與階段進度。

本次全部使用隔離資料庫，沒有重建／重啟 Docker、沒有改寫使用者部署 DB。ConsoleUI 的共用 scope／結果已有自動化驗證，互動式 Copilot／外部模型流程未實際操作。2.6 留待 review；後續使用者於 2026-10-18 授權與 2.7 一併 commit／push。

## 後續 2.7

CommonStorefront 明確選擇 test PaymentId、讀取並呈現操作結果；重建本機 images，以正式會員、test 會員及測試付款完成 UI／API／DB 驗收。現有 Docker 映像不包含本輪程式，不能直接拿既有部署宣稱 2.6 headers 已上線。Admin API 保持 Phase 1.1 的獨立認證與結果契約，不混入購物 request 的 context。
