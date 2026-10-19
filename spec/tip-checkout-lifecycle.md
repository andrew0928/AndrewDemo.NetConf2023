# TiP Checkout 狀態與固定交易內容

2026-10-19 修訂：交易 collection 隔離與指定身分限制已同步至本規格並獲使用者確認，見 [Phase 3 review](../docs/tip/phase3-spec-review.md)。業務原則已確認；新 storage 與改價拒絕尚未實作。既有 Phase 1／2 的交付數字及紀錄僅代表當時版本。

## 狀態與範圍

2026-10-17 使用者明確追加並確認：Checkout 保存 State、獨立金額與明細，**create 時固定**，不保存商品資訊快照；終態完整搬移到封存區後凍結。依據：[決策](../docs/decisions/2026-10-17-checkout-state-and-transaction-details.md)。本文件納入 Phase 1 定案，取代原先 complete 時重讀 Cart／重新計價的流程規則。

Phase 2 已交付 Core 生命週期流程；Phase 3 在同用途分區維持相同規則。取消 API、真實金流與商品資訊快照不在本次範圍；查詢依既有 admin 規格調整存取，Abstract 簽章不變。

## 儲存與命名

- normal／test 各有作用中與封存 collection，完整命名以 [交易儲存規格](tip-transaction-storage.md) 為準，避免第二份對照表漂移。
- 四區使用同一 CheckoutTransactionRecord；生命週期與交易用途是獨立維度，active → archived 只可在原用途分區內移動。
- 終態搬移保留所有欄位與原 TransactionId，不能縮減成摘要、產生新 ID 或改變用途。Order 亦寫入同用途分區。

## 狀態機

`CheckoutState` 位於 Core.Checkouts，與呼叫結果 CheckoutCompleteStatus 分開：

| 值 | 語意 | ClosedAt | 儲存區 |
|---|---|---|---|
| Unknown = 0 | 舊資料未保存 State；不是可執行的正常狀態 | 舊值／null | 不自行推斷或補造 |
| Created = 1 | 已成功建立、用途及金額明細已固定 | null | 作用中 |
| Completed = 2 | 交易完成，對應 Order 已成立 | UTC 結束時間 | 封存 |
| Cancelled = 3 | 交易取消，未因該 Checkout 建立 Order | UTC 結束時間 | 封存 |

合法業務轉移只有 `Created → Completed`、`Created → Cancelled`。Unknown 不可完成；不得將終態改回 Created 或互換 Completed／Cancelled。庫存不足、買方不符等拒絕維持 Created，並非取消。Fulfillment 失敗不改 Completed、不修改封存。

本輪先定義取消的資料與狀態轉移規則；取消執行入口後續再做。已完成 Order 的取消、退款、沖銷不是把封存 Checkout 改成 Cancelled，另案設計。

## 獨立交易內容

`CheckoutTransactionRecord` 追加：

| 欄位 | 型別 | 語意 |
|---|---|---|
| State | CheckoutState | 生命週期；僅由服務執行合法轉移。 |
| TotalPrice | decimal?，init | create 時計算的最終總額；新交易必須有值，0 是有效金額，null 只代表舊資料缺少金額。 |
| ProductLines | List<CheckoutProductLine>，init | 建立時的購買項目，獨立於 Cart 與 Order 儲存。 |
| DiscountLines | List<CheckoutDiscountLine>，init | 建立時實際套用的折扣結果。 |

### CheckoutProductLine

- `LineId`、`ParentLineId?`：保留來源 Cart line 識別與關係；同商品的多筆 line 不合併丟失來源。
- `ProductId`、`SkuId?`：商品與庫存識別，不是商品內容快照。
- `UnitPrice: decimal`、`Quantity: int`、`LineAmount: decimal`：該筆交易的價格與數量，LineAmount = UnitPrice × Quantity。
- 不保存 ProductName、商品描述、規格、圖片或完整 Product。

### CheckoutDiscountLine

- `RuleId`、`Name`、`Description`、`Amount: decimal`、`RelatedLineIds`。
- 名稱與說明是已套用折扣的紀錄，沿用既有折扣結果語意；不複製 Product 主檔。
- 只保存 DiscountRecordKind.Discount；Hint 不作為成交折扣。
- 折扣 Amount 沿用現行負值表示減價，TotalPrice = 商品 LineAmount 合計 + 折扣 Amount 合計。

明細成員使用 init，但集合本身仍為持久化模型的 List；凍結保證必須在服務／儲存邊界實作，不能以模型語法當作防寫保證。

## Create：決定交易內容

1. 驗證明確 RequestMember，載入 Cart 與當下商品資料。
2. 以此次建立時間執行價格／折扣計算，複製交易明細與折扣結果，不保留 Cart／Product 的可變物件參考。必要資料不存在時拒絕 create，不留下部分交易；API 沿用業務拒絕 400。
3. 同次保存 CreatedAt、State=Created、ClosedAt=null、MemberId、CartId、PaymentId、IsTest 與完整金額明細。
4. 本次報價不保留庫存、不扣庫存。Complete 仍須驗證庫存是否足夠。
5. 之後修改 Cart、商品價格、折扣設定或白名單，不改變既有 Checkout 的內容或用途。要變更內容必須重新 create，不提供就地重新計價。

## Complete：沿用固定內容

1. 驗證買方、作用中交易與 State=Created；既有缺 PaymentId 的 409 規則維持。
2. 舊交易沒有 State 或完整固定交易內容時，要求重新建立，回 409／checkout_recreate_required，不用目前 Cart、商品現價或 Order 反填歷史資料。此新增缺資料分支由 Phase 2 接入，與既有 PaymentId 缺少分支使用相同 code、各自清楚的 message。
3. 使用保存的 ProductLines 決定商品與數量，保存的單價／折扣／TotalPrice 決定成交金額；不重讀 Cart 決定交易內容，也不重新執行折扣引擎。
4. 商品存在性、可購買性、買方與庫存等必要檢查照常；已保存 SkuId 不得被當前商品關聯偷偷替換，關聯不符時拒絕。庫存依保存的 SkuId／數量檢查，test 只略過扣減，不略過驗證。
5. 建立 Order 時複製交易數值、折扣及用途，明確填入 CheckoutTransactionId；Order 既有商品名稱可取自完成時的商品讀取，僅供顯示，不參與重新計價。Order 的明細集合不得與 Checkout 共用可變參考。
6. 同一資料庫 transaction 內再次確認可完成，執行適用庫存異動、Order 建立、State=Completed／ClosedAt 設定、完整封存與作用中紀錄移除；任一步失敗全部 rollback。
7. Order.CreatedAt、Checkout.ClosedAt 與完成回應採同一 UTC 業務完成時間。Commit 後派送事件，履約結果只更新 Order，不修改封存 Checkout。

保存內容互相矛盾（例如總額不等於明細合計）不得默默重算後放行，須停止並記錄資料完整性錯誤。已知舊格式缺資料以 409 重建處理；非預期資料損壞沿用內部失敗處理。

## 終態搬移與凍結

- Completed／Cancelled 均保留 create 時相同金額與明細；取消時保留的是原交易條件，不代表已成交，不建立 Order。
- 保存終態與 ClosedAt、寫入完整封存及移除作用中紀錄必須在同一個資料庫 transaction 提交。
- 封存只允許首次新增與讀取；正常應用操作不得 update、upsert 覆寫、刪除或重新啟用。即使沿用可變模型，服務／儲存入口仍須拒絕修改。
- 交易 ID 跨 normal／test、active／archived 四區唯一；重複 complete 維持作用中不存在的 400，不重新派送事件。
- Phase 2 已建立用途／封存保護；Phase 3 必須擴及兩種用途及所有應用寫入，驗證不能以 raw collection 或另一分區繞過。

## 業績與核帳原則

- 正式成交交易使用 `State=Completed` 且 `IsTest=false`，依 ClosedAt 歸期；Created、Cancelled、Unknown 不列為成交。
- Order 依自身 IsTest=false 與 CreatedAt 查詢；與關聯 Checkout 對照 ID、用途、總額及交易明細，不因 Buyer 為正式而納入 test Order。
- 只有明確要求時列 test；交易與訂單兩個視角不得重複加總。取消可獨立查核原內容，但不是成交額。
- 舊資料未知時間／金額／狀態不能補造或以 0 代替；歷史缺 IsTest 仍採已確認 false 政策。
- 管理查詢依 [admin 規格](tip-admin-query.md)；此 schema 支援本專案的交易／訂單核對，不新增外部金流、退款或沖銷帳務。

## 驗收

見 [狀態與固定交易內容測試案例](testcases/tip-checkout-lifecycle.md)。持久化測試僅證明欄位可保存；create 固定、complete 沿用、原子封存與防寫能力須在 Phase 2 依真實入口驗證。
