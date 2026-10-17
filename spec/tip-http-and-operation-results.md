# TiP HTTP 與操作結果合約

## 狀態與邊界

本文件定義 Phase 1 的對外契約與 Phase 2 驗收要求；HTTP middleware、結果收集與 log 串接尚未實作。Core 結果模型位於 `.Abstract/Operations`，不依賴 ASP.NET。

IdentityContext 與 TransactionContext 維持獨立。操作結果只描述本次執行，不把兩種 context 合併；HTTP adapter 在出口讀取當次已授權的 context 與結果。

## 操作結果模型

`OperationResult` 是不可變快照：

| 欄位 | 型別 | 語意 |
|---|---|---|
| IsTestBehavior | bool | 依標準流程用途來源表判定的本次操作性質。 |
| Outcome | OperationOutcome | Succeeded／Rejected／Failed。 |
| SkippedEffects | IReadOnlyList<SkippedEffect> | 實際因 test 而略過的效果；空集合表示沒有這類略過。 |

- Succeeded：必要檢查通過，依政策完成應做的步驟。可能包含實際寫入，也可能包含 test 略過。
- Rejected：權限、輸入或業務規則拒絕，包括買方不符、庫存不足、舊 Checkout 要求重建。
- Failed：非預期執行錯誤或 context 遺失／矛盾。已有步驟結果不得因失敗而被偽造或抹除。
- 不以 IsTestBehavior 推斷成功；也不以成功推斷每個效果都執行。
- 每個 SkippedEffect 保存 EffectCode、TargetType、TargetId、ReasonCode。集合與項目均不可由讀取端改寫；建構時複製集合。
- 結果收集器留在 Core 的操作範圍內，Phase 2 可採 concrete class；不為此合併 Context，也不要求額外公開介面。

### 穩定代碼

| EffectCode | TargetType | 使用條件 |
|---|---|---|
| inventory.deduct | inventory | 到達扣庫存步驟且因 test 略過。 |
| product.update | product | 到達商品更新步驟且因 test 略過。 |
| sku.update | sku | 到達 SKU 更新步驟且因 test 略過。 |
| payment.charge | checkout | 實際存在付款 adapter，且該效果因 test 略過。 |
| shipment.create | order | 實際存在出貨 adapter，且該效果因 test 略過。 |
| statistics.update | order | 實際存在事件統計更新，且該效果因 test 略過。 |

ReasonCode 固定為 `test_behavior`。尚未提供的能力不得產生假的 skipped effect；必要檢查失敗、尚未執行到的步驟也不得標示略過。其他受保護寫入如新增／刪除遵循相同命名規則，但本輪不新增管理 API。

## HTTP request 與 context

1. Routing 之後以 endpoint metadata 明確宣告 transaction 種類及 ID 來源。一般 request 不猜測 `id`，CartId 不視為交易 ID。
2. 驗證 token 後載入 Member 與 Token 保存的用途，初始化 IdentityContext。匿名入口使用匿名 identity。
3. `/api/checkout/create` 入口沒有交易；成功建立後才綁定 Checkout 快照。
4. `/api/checkout/complete` 從 JSON body 的 transactionId 載入作用中 Checkout。若 middleware 讀 body，必須限制大小、採與 MVC 一致的 JSON 規則、讀完 rewind；格式錯誤回 400，不可讓 middleware 與 controller 解出不同 ID。
5. 解析交易亦須驗證可存取範圍；未授權／買方不符不得輸出他人交易的用途 header。Core 仍獨立驗證明確 RequestMember。
6. 明確的 Order endpoint 從指定 OrderId 解析。訂單集合查詢不綁定單一交易；本輪不為 context 新增 Order endpoint。
7. `HttpContext.Items` 的 `access-token` key 沿用；身分與交易入口分別使用 Core context 型別作 key，value 引用 DI scope 中同一個 context 實例，不複製另一份可變狀態。
8. 新 event scope 或 CLI scope 不讀取舊 HttpContext.Items，從明確資料恢復 identity／transaction。

## HTTP response headers

既有成功 response body 形狀維持，由 middleware 在 response 開始前輸出 headers。對外值一律採下表小寫格式。

| Header | 值 | 出現條件 |
|---|---|---|
| X-Shop-Behavior | normal / test | 已取得可信的 OperationResult。 |
| X-Shop-Outcome | succeeded / rejected / failed | 已形成操作結果。 |
| X-Shop-Skipped-Effects | 以逗號分隔的 EffectCode，排序且去重 | 確實有 skipped effects；無則省略。 |
| X-Shop-Transaction-Type | checkout / order | 存在已授權的 current transaction。 |
| X-Shop-Transaction-Id | 十進位 ID | 同上。 |
| X-Shop-Transaction-Is-Test | true / false | 同上。 |

沒有交易時，三個 transaction headers 全部省略；物件表示的 transaction 為 null。這與一般操作的用途／結果表達分開，例如 test 會員修改商品可沒有 transaction，但仍有 test 行為及略過結果。

出口不重新查白名單分類。Checkout 完成後即使作用中紀錄已移除，仍使用本次的 Checkout 快照；事件端則使用 Order 快照。

Header 不帶 token、完整會員資料或 skipped effect 的逐筆目標明細。HTTP status 保留原業務成功／失敗意義，test 不強制回 200；CLI 與事件可直接讀取 OperationResult，無須依賴 HTTP。

## 錯誤與缺失

| 情境 | HTTP／結果 | 副作用 |
|---|---|---|
| create JSON 沒有 paymentId | 400，Rejected | 不建立交易。 |
| Token 無效／過期 | 401，Rejected | 不執行受驗證操作。 |
| Checkout 買方不符 | 403，Rejected | 不修改交易，不輸出他人 transaction headers。 |
| Checkout 不存在／重複完成 | 400，Rejected | 不從封存重做，不重送事件。 |
| 舊 Checkout 未保存 PaymentId | 409，Rejected | 保留原紀錄，要求重新 create，不補值。 |
| 舊 Checkout 未保存 State／固定交易內容 | 409，Rejected | 同樣使用 checkout_recreate_required，要求重新 create，不依現在的 Cart／價格補資料；Phase 2 接入。 |
| 業務檢查失敗，例如庫存不足 | 沿用既有 400，Rejected | 未到達效果步驟不記錄略過。 |
| 預期有 context 卻遺失，或 Token／Member 等既定用途矛盾 | 500，Failed，code=execution_context_invalid | 在受保護效果前停止，記錄 Error；不以匿名／false 放行。 |

缺少 PaymentId 的 409 body 固定為：

```json
{
  "code": "checkout_recreate_required",
  "message": "Checkout 未保存 PaymentId，請重新建立 Checkout。"
}
```

舊格式缺少 State／固定交易內容的 409 使用相同 code，message 為「Checkout 未保存建立時的狀態、金額或明細，請重新建立 Checkout。」；不得改用舊相容計價。

一般錯誤不在本輪全面改成新 envelope。用途 BUG 的 500 使用 `code` 與可供閱讀的 `message`；對無法信任的用途不得輸出 X-Shop-Behavior 或 transaction headers。建立結果前發生的基礎設施故障不偽造成功／正式分類。

## 正常 log

沿用 host 的 logging 機制，不另建專用 log 儲存。略過效果採結構化 Info log，至少包含：

- TraceId／CorrelationId（同次操作可追蹤），ShopId。
- IdentityKind、適用時的 MemberId；TransactionKind、TransactionId、TransactionIsTest（無交易時為 null）。
- EffectCode、TargetType、TargetId、ReasonCode、Outcome。
- 商品／SKU 更新額外保留被略過的必要變更內容，避免只記錄「成功」。

不記錄 access token 或認證秘密；Log 中的用途與出口結果取自同一份快照及實際判定。正常業務拒絕沿用正常紀錄；context BUG 為 Error。真實效果已發生後的失敗要如實記錄，不以 test 略過掩蓋。

## 驗收

見 [HTTP／結果案例](testcases/tip-http-and-operation-results.md)。目前只有結果模型與合約測試可驗證；middleware、HTTP headers、log 及 event scope 的整合驗證列入 Phase 2。

## 明文 token 清理範圍（Phase 2）

2026-10-17 使用者明確追加後續工作：移除既有 API middleware 與 OAuth 登入／token exchange 中的 access token 明文輸出。驗收見 TC-TIP-HTTP-017；本次時間欄位補充不修改 logging 程式。
