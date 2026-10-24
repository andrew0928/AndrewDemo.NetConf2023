# TiP HTTP 與操作結果驗收案例

2026-10-19 修訂：與 [交易隔離案例](tip-transaction-storage.md) 共同驗收 Phase 3。下方 Phase 1／2 證據是當時版本；本次新增／變更要求尚未實作或驗證。

依據：[HTTP 與結果合約](../tip-http-and-operation-results.md)。除下方模型測試外，均為 Phase 2 待執行驗收。

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-HTTP-001 | 一般無交易 request | 三個 transaction headers 全部省略，transaction 物件為 null。 |
| TC-TIP-HTTP-002 | create 成功建立 test Checkout | 出口有 checkout、正確 ID、true 的 transaction headers；body 維持原成功形狀。 |
| TC-TIP-HTTP-003 | complete 後作用中交易已移至封存 | 出口仍使用該次 Checkout 快照，不因作用中紀錄消失而漏掉用途。 |
| TC-TIP-HTTP-004 | 具一般權限的 test identity 嘗試商品改價 | HTTP 403／test_identity_forbidden、Behavior=test、Outcome=rejected；無 skipped effects／transaction headers，重讀原價，拒絕 log 含目標與原因。 |
| TC-TIP-HTTP-005 | test Checkout 因庫存不足被拒絕 | Outcome=rejected，沒有尚未執行的 inventory.deduct 略過；HTTP 沿用 400。 |
| TC-TIP-HTTP-006 | body 格式錯誤／缺 ID／超過大小限制 | 依既有 HTTP 邊界拒絕；不出現成功交易 header、不產生副作用；middleware 不消耗後續 body。 |
| TC-TIP-HTTP-007 | middleware 與 controller 解析同一 payload | 使用一致 JSON 規則與同一 ID；Items 與注入的 context 引用同一物件。 |
| TC-TIP-HTTP-008 | 無效 token 或他人 Checkout | 401／403，不暴露他人 transaction headers、不產生效果。 |
| TC-TIP-HTTP-009 | 用途矛盾或必要 context 遺失 | 500、code=execution_context_invalid、Outcome=failed；不輸出不可信用途、不執行受保護寫入。 |
| TC-TIP-HTTP-010 | 同一 request 多個庫存目標被略過 | Result／log 保留逐筆目標；header 只輸出去重且排序的 EffectCode。 |
| TC-TIP-HTTP-011 | 正式、test HTTP request 並行 | 各自 headers、結果與 log 能對應自身交易，不交叉覆寫。 |
| TC-TIP-HTTP-012 | 查詢自己的訂單集合 | 無單筆 transaction headers，各筆 Order 的用途仍正確。 |
| TC-TIP-HTTP-013 | CLI 與事件處理相同 test 效果 | 可直接讀 OperationResult，不需 HttpContext；log 能對應事件／交易。 |
| TC-TIP-HTTP-014 | 舊 Checkout 沒有 PaymentId | 409 與固定 code；前端提示重新建立，不自動回填或重送舊付款參數。 |
| TC-TIP-HTTP-015 | 建立結果後改動原 skipped effects 集合 | 已建立 OperationResult 不變，讀取端不能修改集合。 |
| TC-TIP-HTTP-016 | IsTestBehavior=true，操作分別成功／拒絕／失敗 | Outcome 正確且獨立，沒有略過時集合為空，不由 test 自動產生虛構效果。 |
| TC-TIP-HTTP-018 | test identity 合法修改 SKU 規格 | Behavior=test、Outcome=succeeded、Skipped-Effects=sku.update；無 transaction headers，規格不變。 |

## 已有模型驗證

`OperationResultContractTests` 覆蓋 TC-015 與 TC-016，共 4 筆。Controller 的舊交易 409 mapping 另有 `CheckoutContractTests`；HTTP server／middleware、headers、log 與事件範圍仍待 Phase 2 驗證。

## TC-TIP-HTTP-017 移除 access token 明文 log（Phase 2）

- Given：使用可辨識的測試 token 完成 OAuth 登入、token exchange、受驗證 API request 及失敗路徑。
- When：擷取各路徑的應用 log。
- Then：任何訊息、結構化屬性或例外輸出均不包含完整 access token；保留不依賴秘密的診斷資訊。
- 範圍包含 API Program middleware、LoginController 的登入及 token 回應輸出；本次僅追加案例，尚未修改程式。

## Phase 2.6 執行證據（2026-10-18）

本節保留 2026-10-18 的驗收證據；新版 HTTP-004 的拒絕要求尚未驗證，HTTP-018 用於獨立追蹤未變更的 SKU 政策。當時 HTTP-001～017 的標準 Core／API／CLI／事件出口已由 ExecutionScopeTests（含 OperationOutputTests partial）、OrderEventScopeTests 與原有 OperationResultContractTests 覆蓋；完整方法與限制對照見 [2.6 交付紀錄](../../docs/tip/phase2.6-delivery.md)。solution 327 項通過，ConsoleUI build 成功。

維護 HTTP 案例採測試組件專用探針，沒有新增正式維護 API；真實商業 adapter 不適用。HTTP-014 的 409 與固定 code 已驗證；前端重新建立提示、付款選擇與操作結果呈現仍須在 2.7 完成 UI 整合驗收。現有 Docker 尚未包含此批程式；不將自動化出口證據視為 UI／部署完成。
