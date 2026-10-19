# TiP 交易用途與 collection 隔離

日期：2026-10-19。業務邊界已由使用者確認；本文件是追加 [Phase 3](tip-phase3-plan.md) 的規格修訂，具體 collection 命名、遷移與交付切分已由使用者確認。尚未實作，不代表現行資料庫已分流。

依據：[交易邊界決策](../docs/decisions/2026-10-19-tip-identity-storage-and-access-boundaries.md)。與 [標準流程](tip-standard-flow.md)、[生命週期](tip-checkout-lifecycle.md)、[Context](tip-context-contracts.md)、[管理查核](tip-admin-query.md) 共同使用；本文件是交易 collection 對照與遷移規則的 canonical 來源。

## 業務不變條件

1. 嚴格隔離邊界是交易。Checkout 建立時固定 `IsTest = Member.IsTest || IsTestPayment(PaymentId)`；後續完成、Order、封存、事件與重試保持原用途及 storage 分區，禁止 live／test 互轉。
2. live 對應既有 API、header、CLI 的 `normal`／`IsTest=false`；對外不新增 `live` 枚舉，也不更名既有欄位。test 對應 `IsTest=true`。
3. Member、Token、Cart、Product、SKU、Inventory 共用；Token.IsTest 繼承 Member.IsTest 的認證規則維持。共用不解除權限或物件歸屬檢查。
4. 正式會員／Token 可以建立、完成自己的 test 交易。不得以 `identity.IsTest == transaction.IsTest` 作為通用授權條件，也不得以當前 Token 決定既有交易的 storage。
5. test identity 禁止商品改價；其他限制逐項明訂，不能將所有共用資料操作一律拒絕。SKU 維護維持既有「驗證後略過寫入」政策，這次不擴張禁止清單。
6. test transaction 不產生任何實際業務效果。必要業務驗證照常執行；test Checkout／Order／封存與 log 可以保存。庫存扣減、真實付款、出貨、正式商業統計等效果必須在發生前阻止；System identity 不解除此限制。

獨立商品維護與交易效果屬不同操作；不能在 test 交易內改呼叫一般維護入口，或清除 transaction context，藉此執行該交易本來禁止的實際效果。

## 同一 shop database 的配置

本次沿用一個 `ShopManifest.DatabaseFilePath` 與同一 LiteDB transaction；不拆 database、連線、登入流程或 API。未來實體分庫須另行評估原子性與共享資料讀取，本次不宣稱已具備分庫能力。

| 根物件／生命週期 | normal／live | test |
|---|---|---|
| Order | `orders` | `test_orders` |
| 作用中 Checkout | `checkout_transactions` | `test_checkout_transactions` |
| 封存 Checkout | `archived_checkout_transactions` | `test_archived_checkout_transactions` |

三組使用原有模型，不建立 test 專用模型。保留 IsTest 作為不可變用途及 storage 一致性檢查，不再依賴每個業務查詢記得排除另一用途的 row。normal 舊名稱保留作為正式分區，不保留混合資料的相容 alias 或雙寫入口。

`members`、`member_tokens`、`carts`、`products`、`skus`、`inventory_records` 不拆分。Member 清單仍依會員自身 IsTest 篩選；交易清單則依交易用途選 collection，兩者不同。

## 受控存取與識別

- Core 集中管理 collection 對照、單筆定位、集合查詢與交易寫入。業務服務、controller、CLI、event loader 不自行拼 collection 名稱，也不持有可繞過用途驗證的 raw transaction collection。
- Shop database 的共用實例不綁定某個 request 的可變 identity。新增交易使用建立時可信的用途；既有交易使用持久化資料恢復的用途；集合查詢使用明確的 normal／test／all 條件。
- Checkout ID 在同一 shop 的四個 active／archived、normal／test collections 間唯一；Order ID 在兩個 Order collections 間唯一。不同物件種類可以使用相同數字；不要求跨 shop 或 Checkout／Order 全域唯一，也不改公開 ID 型別。
- ID 配發與保存必須有相同的寫入協調邊界，包含非 Checkout 來源的既有 Order；不能各 collection 自行取 max+1。不得重用封存 ID、覆寫其他分區物件。
- 只收到 ID 的既有 API／事件由受控 locator 在該 shop 的適用分區辨識唯一物件，驗證原始用途與所在 collection，再做授權／買方檢查，授權成功後才暴露 transaction context。locator 可內部檢查兩種用途，不能把資料合併後「第一筆找到就用」。
- complete 只接受作用中 Checkout；封存僅供查詢及關聯，不能因 locator 找到封存就重新執行。不存在／重複完成維持既有 400；他人交易維持 403，不暴露交易用途。會員只取得自己的 Order，查詢包含自己的兩種用途。
- 初始化、遷移與刻意製造損壞的測試可使用受控 raw BSON 入口；不得形成一般應用端的長期繞道。

## 用途一致性與失敗處理

1. 新資料必須明確保存 bool IsTest，且與目標 collection 一致；已保存物件不可改用途、跨區搬移或複製成另一用途的同一交易。
2. 舊資料完全缺少 IsTest 依既定政策為 false，只能位於 normal 分區；明確 null／錯誤型別為異常，不能當 false。test 分區缺用途同樣屬異常。
3. ID 重複、用途與 collection 不符、已存在關聯的 Checkout／Order 用途矛盾，都必須停止相關操作並記錄錯誤，不能靜默 fallback、去重、重分類或修復。
4. 交易操作的用途矛盾沿用 `execution_context_invalid`／HTTP 500／Failed；管理查核沿用 `admin_data_invalid`。未知用途不得輸出可信用途 headers，也不得執行效果。
5. 缺少歷史 PaymentId／State／固定交易內容的 Checkout 仍按既有 409 重建政策；遷移不補造資料。Order 的歷史 null CheckoutTransactionId 保留 null，不捏造封存或關聯。

## 生命週期、事件與讀取一致性

- create 只寫入既定用途的 active collection。complete 在同一 LiteDB transaction 中完成適用庫存效果、同用途 Order、同用途完整封存、active 刪除；任一步失敗全部 rollback。
- 所有 normal／test 寫入共用 ID／Checkout 寫入協調機制；並行同筆 complete 最多成功一次。封存凍結及原報價不變規則延續。
- commit 後才派送事件。新的事件 scope 以 OrderId 定位並恢復保存用途；重送、System identity、FulfillmentStatus 更新均維持該 Order 分區，不依呼叫者或白名單重算。
- Order 查詢依 normal／test 讀一個 collection；all 明確合併兩個。Checkout 依用途合併 active／archived；all 合併四個。先完成一致讀取、驗證及篩選，再統計、排序、分頁。
- 多 collection 查詢必須與寫入協調，保證一次回應只見提交前或提交後的完整狀態；不能依賴各 collection 獨立讀取就假設得到共同快照。沿用／擴充現有 checkout 寫入鎖或具等價保證的機制，驗證跨程序共用 DB 的情境。
- Admin 預設 normal；purpose、DTO、日期／未知值規則、summary 口徑與 CLI 格式維持。Checkout 回應的 `storage=active/archived` 只描述生命週期，不改成 collection 名稱。會員 Order history 維持授權後的兩用途合併；Cart 仍可正常寫入。

## 既有資料遷移與切換

遷移是 storage 位置變更，並非交易用途改變；只依已保存 IsTest 分流，禁止重新查會員／Token／白名單決定歷史用途。

1. 提供明確指定 database 路徑的離線遷移入口，先停止所有 writers、取得可還原備份。不得以現有會刪除輸出 DB 的 seed 初始化路徑執行搬移，也不在 API 查詢／登入時自動搬資料。
2. 遷移前檢查資料版本、重複 ID、無法判讀用途及已存在關聯的用途矛盾。未完成遷移但 test 目標已有不明資料時停止，不猜測上次搬到哪裡、不覆寫衝突資料。
3. 以原始 BSON 保留 ID、所有已知／未知欄位、時間與明細；將原三個 mixed collections 中 IsTest=true 的文件移至對應 test collection，false／缺欄位留原處。不得重新序列化而補造舊欄位或流失未知欄位。
4. 六個 collections 的必要索引、搬移與 storage 版本標記須形成可驗證的完成狀態；資料搬移與完成標記同一 transaction 提交。任一衝突或故障 rollback，原資料仍完整，不留下可被新 host 誤認完成的狀態。
5. 遷移完成後核對各用途、生命週期的筆數、ID、保存內容與 Order／Checkout 關聯，再開放新版本 writers。重跑已完成且一致的資料庫為 no-op；發現版本或內容矛盾則停止。
6. 新建空 DB／seed 直接建立新配置與版本標記。新 host 對未遷移的有資料 DB 明確拒絕服務並提示遷移，不能繼續混寫；已完成遷移也不能讓舊版 writer 接回。回退部署須配合停寫及一致備份還原，不宣稱直接換回舊 binary 即可。

版本標記的 Core 內部模型與遷移命令語法留給 Phase 3.1／3.5 實作選定；不得擴大為通用 migration framework。新 seed、測試 fixture 與部署操作說明須同步，既有使用中的 DB 不在本次規格更新中操作。

## 驗收與範圍

新增 [交易隔離案例](testcases/tip-transaction-storage.md)，並同步既有流程、HTTP、物件、生命週期與管理查核案例。改價拒絕的具體契約見 [HTTP 與結果](tip-http-and-operation-results.md)。

現有 Abstract 的獨立 contexts、int ID、OrderId 事件及 OperationOutcome.Rejected 足以表達本次要求；新增 storage／migration 邊界留 Core，無需修改 Abstract。實際分庫、OAuth 搬移、付款／出貨 adapter、新維護 API、取消／退款及 AppleBTS／PetShop 的 TiP 領域政策均不在本次範圍；共用變更仍須跑其回歸。
