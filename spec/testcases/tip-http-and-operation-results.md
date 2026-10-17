# TiP HTTP 與操作結果驗收案例

依據：[HTTP 與結果合約](../tip-http-and-operation-results.md)。除下方模型測試外，均為 Phase 2 待執行驗收。

| ID | Given／When | Then |
|---|---|---|
| TC-TIP-HTTP-001 | 一般無交易 request | 三個 transaction headers 全部省略，transaction 物件為 null。 |
| TC-TIP-HTTP-002 | create 成功建立 test Checkout | 出口有 checkout、正確 ID、true 的 transaction headers；body 維持原成功形狀。 |
| TC-TIP-HTTP-003 | complete 後作用中交易已移至封存 | 出口仍使用該次 Checkout 快照，不因作用中紀錄消失而漏掉用途。 |
| TC-TIP-HTTP-004 | test 商品更新被略過 | Behavior=test、Outcome=succeeded、Skipped-Effects 含 product.update；無 transaction headers，讀取商品仍原值。 |
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

## 已有模型驗證

`OperationResultContractTests` 覆蓋 TC-015 與 TC-016，共 4 筆。Controller 的舊交易 409 mapping 另有 `CheckoutContractTests`；HTTP server／middleware、headers、log 與事件範圍仍待 Phase 2 驗證。

## TC-TIP-HTTP-017 移除 access token 明文 log（Phase 2）

- Given：使用可辨識的測試 token 完成 OAuth 登入、token exchange、受驗證 API request 及失敗路徑。
- When：擷取各路徑的應用 log。
- Then：任何訊息、結構化屬性或例外輸出均不包含完整 access token；保留不依賴秘密的診斷資訊。
- 範圍包含 API Program middleware、LoginController 的登入及 token 回應輸出；本次僅追加案例，尚未修改程式。
