# TiP Phase 1.1 管理查核測試案例

日期：2026-10-18。依據：[管理查核規格](../tip-admin-query.md)。本輪只交付案例；未新增或執行這些自動化測試。認證設定方式已定案；CLI 案例供使用者後續追加工具時驗證，不是 API 完成條件。

## 認證與隔離

| ID | Given／When | Then |
|---|---|---|
| ADMIN-AUTH-001 | 正確預設 admin token 呼叫三個 GET | 可查；不查 member_tokens、不新增會員／Token。 |
| ADMIN-AUTH-002 | 缺少／錯誤／只符合前綴／大寫／帶連字號的 token | 401；查詢服務未執行，無資料外洩。 |
| ADMIN-AUTH-003 | 有效 normal 或 test 會員 token 呼叫 admin | 401；會員用途不賦予管理權。 |
| ADMIN-AUTH-004 | admin token 呼叫會員或 checkout API | 401；不能把 admin 視為 Member。 |
| ADMIN-AUTH-005 | token 只在 URL、cookie、body；或多值 Authorization | 401，不 fallback。 |
| ADMIN-AUTH-006 | 未設定／空設定啟動 API | admin 入口 404；既有商店入口正常。 |
| ADMIN-AUTH-007 | 設定值格式非法 | 啟動失敗；錯誤不包含設定值。 |
| ADMIN-AUTH-008 | 切換設定並重啟 API | 舊 token 無效、新 token 有效；不存在 refresh／簽發入口。 |
| ADMIN-AUTH-009 | 檢查成功／失敗 headers、log、會員資料與 CLI 程序 argv | 不含秘密；回應 no-store；不帶單筆交易 headers。 |

## 訂單與篩選

隔離 fixture：正式 Order A=100、B=200（B 的 FulfillmentStatus=Failed）；測試 Order C=900，C 的 Buyer.IsTest=false；三筆均在同一日期區間且有已知 CreatedAt／TotalPrice。

| ID | Given／When | Then |
|---|---|---|
| ADMIN-ORDER-001 | 不給 purpose，pageSize=1 | totalCount=2、totalAmount=300、knownTotalAmount=300；items 只有 1 筆，B 仍納入。 |
| ADMIN-ORDER-002 | purpose=test／all | 分別 count=1／3、amount=900／1200；不依 Buyer.IsTest 排除 C。 |
| ADMIN-ORDER-003 | 變更會員名稱／白名單再查 | 既有訂單用途與保存買方名稱不重算。 |
| ADMIN-ORDER-004 | 以 memberId 篩選與分頁 | 總額、筆數、items 使用同條件；ID 由大至小。 |
| ADMIN-ORDER-005 | 起點、終點、不同 UTC offset 的等價時間 | 起點含、終點不含；等價 UTC 區間結果相同。 |
| ADMIN-ORDER-006 | 空集合或超出最後一頁 | 空集合金額 0；超頁 items 空但統計維持全部符合值。 |
| ADMIN-ORDER-007 | 非法 purpose、日期、from>=to、pageSize>100、重複／未知參數 | 400 / admin_query_invalid，不靜默忽略。 |

## 舊資料與缺值

| ID | Given／When | Then |
|---|---|---|
| ADMIN-LEGACY-001 | raw BSON 缺少 IsTest | 視為 normal，驗證 typed 預設值與查詢條件一致。 |
| ADMIN-LEGACY-002 | IsTest 存成無效字串等非法型別 | 500 / admin_data_invalid，不作 normal。 |
| ADMIN-LEGACY-003 | 訂單未知時間，無日期／有日期條件 | 無日期可列；有日期排除且 excludedUnknownTimeCount 正確，無現在時間補值。 |
| ADMIN-LEGACY-004 | 符合條件訂單一筆金額缺失、另一筆已知 100 | totalAmount=null、knownTotalAmount=100、unknownAmountCount=1；真實 0 元不算未知。 |
| ADMIN-LEGACY-005 | Checkout 缺時間／付款／金額／狀態 | 分別 null／null／null／unknown，不偽造 0 或 Completed；原 BSON 不變。 |
| ADMIN-LEGACY-006 | 舊訂單沒有 CheckoutTransactionId | 回 null，不捏造連結；無日期條件可列，但不宣稱對帳完成。 |

## Checkout 與會員

| ID | Given／When | Then |
|---|---|---|
| ADMIN-CHECKOUT-001 | active Created、archived Completed／Cancelled、舊 Unknown | 合併後排序分頁，storage 與 state 正確，預設用途 normal。 |
| ADMIN-CHECKOUT-002 | state=completed、timeField=closedAt、日期篩選 | 僅列該期間成交；Created／Cancelled／Unknown 不混入。 |
| ADMIN-CHECKOUT-003 | timeField=closedAt 且有日期，含作用中資料 | ClosedAt=null 者不列入期間，記入 excludedUnknownTimeCount。 |
| ADMIN-MEMBER-001 | 正式與測試會員、缺 IsTest 舊會員 | normal／test／all 分別符合自身用途；只回 id/name/isTest。 |
| ADMIN-MEMBER-002 | name=test，資料庫 collation 具大小寫／文化規則 | 與登入會員查找規則一致；不把 name 數字轉 ID。 |
| ADMIN-MEMBER-003 | name 與 memberId 同時指定，或傳會員日期參數 | 名稱與 ID 取交集；不支援的日期參數回 400。 |

## 一致性與副作用

| ID | Given／When | Then |
|---|---|---|
| ADMIN-READ-001 | 訂單查詢期間另一請求提交訂單 | 同一回應統計與 items 來自同份集合，不混用前後狀態。 |
| ADMIN-READ-002 | 在 active 讀取與 archived 讀取之間，以可控同步點完成 Checkout | 每份清單只見一次該交易：移轉前 active 或移轉後 archived；不得漏列或重複。測試不能只靠隨機並行。 |
| ADMIN-READ-003 | 寫入交易尚未 commit 或之後 rollback | 查詢不暴露未提交訂單／封存，rollback 不留查詢可見的半套資料。 |
| ADMIN-READ-004 | 同一 API 在寫入 commit 前後連續查詢 | 後一次看到最新提交；不要求停止 container 或清除 extension。 |
| ADMIN-READ-005 | 查詢前後比對業務集合，觀察事件與 skipped-effect log | 無業務資料改寫、用途重算、Token 建立、事件或 test 略過紀錄。 |
| ADMIN-READ-006 | DB 讀取故障／資料不可判讀 | 非 2xx，明確錯誤碼；不回舊結果、空成功或秘密。 |

## 後續 CLI（Phase 1.1 歷史參考案例）

2026-10-18：使用者於 Phase 1.2 改採 .NET CLI；下表保留原參考情境，文法／輸出及新案例以 [Phase 1.2 CLI 案例](tip-admin-cli.md) 為準，無 shell 實作交付。

| ID | Given／When | Then |
|---|---|---|
| ADMIN-CLI-001 | orders／checkouts／members 加合法參數 | GET 到指定 admin route，與直接 HTTP 回應一致；不重算金額。 |
| ADMIN-CLI-002 | 名稱空白、加號、中文、單引號與時間 offset | 正確 URL encode，不展開為命令或改寫查詢條件。 |
| ADMIN-CLI-003 | token 缺少、非法子命令、HTTP 400／401／404／500、連線拒絕 | 非零 exit code，stdout 不偽造成功，stderr 說明錯誤且不含秘密。 |
| ADMIN-CLI-004 | HTTP redirect 或遠端明文 HTTP base URL | 不將 token 跟隨轉址或送至遠端明文 HTTP。 |
| ADMIN-CLI-005 | stdout 重導至檔案；檢查 stderr／argv | 檔案只有成功 JSON；不含 token／除錯輸出；token 不在 curl argv。 |
| ADMIN-CLI-006 | 執行所有查詢 | 不開 DB、不控制 container、不修改業務資料。 |

## 交付要求

後續實作需記錄實際測試命令、通過數與未覆蓋範圍；本文件的案例數不等於自動化測試數。API 的認證／統計／並行一致性是必要驗收，不能以 API 回 200 或 shell 能輸出 JSON 替代。

## 2026-10-18 API 實作驗證

新增 AdminQueryTests（22 項）與 AdminQueryHttpTests（44 項）全部通過；solution 共 262 項通過。另以真正 API Program、新程序及資料副本完成設定／停用／token 輪替驗證。驗收對應：AUTH-001～009（不含 CLI argv）、ORDER-001～007、LEGACY-001～006、CHECKOUT-001～003、MEMBER-001～003、READ-001～006；詳細手段與限制見 [交付紀錄](../../docs/tip/phase1.1-delivery.md)。CLI 六項案例留後續追加工具時驗證。Docker daemon 恢復後已成功重建 Common API 映像；使用者依 Compose 啟動及 curl 指令回報 `orders?purpose=normal` 手動測試通過；完整容器案例未重跑。
