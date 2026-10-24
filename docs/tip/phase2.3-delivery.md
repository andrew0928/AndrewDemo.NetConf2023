# TiP Phase 2.3 交付紀錄

日期：2026-10-18。分支：`feature-test-in-production`。Phase 2.3 完成，待 review；下一階段為 2.4。範圍為標準 Core／API／CLI 的 Checkout 固定報價、Order、庫存與封存；前兩階段未提交的變更保留在同一工作樹。

## 實作與邊界

- Create 讀取保存的 Member，確認明確 RequestMember 的用途一致；依 Member.IsTest 與 PaymentId 白名單固定 Checkout.IsTest。以同一次商品讀取及建立時間計價，保存 Created、付款、買方、金額、商品明細、line 關係及折扣 RelatedLineIds；只保存 Discount，不保存 Hint 或商品內容快照。此時不扣庫存。
- Create 缺商品／SKU／會員等必要資料時拒絕，不保存半筆交易；API 使用既定 400，計價內部錯誤仍為失敗。Core 以實作層 CheckoutRejectedException 傳遞業務拒絕，沒有修改 command、response 或 Abstract。成功 commit 後才綁定 scoped Checkout context。
- Complete 不讀 Cart、不重新計價或執行折扣；商品名稱可取完成時的顯示值，但成交價格、數量、SKU 與折扣沿用保存內容。商品能否解析仍由 IProductService 決定，hidden product 可用 ID 購買，不把 IsPublished 當成可購買旗標。
- Complete 驗證買方、會員、State、必要固定內容、總額一致性、商品／SKU 關聯與庫存；同 SKU 的多筆 line 合併驗證需求。舊資料缺 State／金額／明細等回 409／checkout_recreate_required；原先 PaymentId 缺值／null 的規則保留。非預期內容矛盾停止並記錄 Error，不重算掩蓋問題。
- 在同一筆同步 LiteDB transaction 中取得寫入鎖後重新載入 Checkout，再完成庫存、Order Insert、完整封存與作用中紀錄移除。沒有在資料庫交易內 await；拒絕外層巢狀交易。任一步失敗 rollback，commit 前不派送事件。
- Create／complete 共用資料庫寫入鎖，支援不同服務 instance 及 Shared connection。TransactionId 取作用中與封存兩區最大 ID 後配發；Order ID 獨立配發並使用 Insert，不以交易 ID Upsert 覆寫其他來源的訂單。
- Order 保存 CheckoutTransactionId、IsTest 與 CreatedAt；Checkout 保存 Completed／ClosedAt。兩筆紀錄與完成回應使用同一 UTC 時間，對齊 LiteDB 毫秒精度。
- 封存以原 BSON 搬移，只改 State／ClosedAt，保留所有已知及擴充欄位；不補造舊 IsTest 缺值。正常應用透過受保護的 typed collections 讀取：Checkout 拒絕 Update／Upsert，封存另拒絕 Delete／DeleteMany／DeleteAll；作用中入口拒絕封存 ID 重新啟用。Order 拒絕整筆覆寫／刪除，履約由 Core 僅更新 FulfillmentStatus，避免用途被重設。
- `IShopDatabaseContext.Database` 仍是低階基礎設施入口，供 Core 原子編排、索引、遷移及測試注入使用；它不是任意 SQL 或資料庫管理員的防竄改安全邊界。標準應用不得繞過上述 Core／typed collection 入口。既有 schema 測試的手動狀態寫入明確改走低階入口，與服務凍結測試分開。
- Test 交易照常驗證全部庫存，僅略過扣減；新增 scoped CheckoutEffects 收集實際 `inventory.deduct`／`inventory`／SKU／`test_behavior`，並留 log。驗證未通過時沒有 skipped effect；已到達略過步驟但後續寫入失敗，仍保留實際略過證據，資料庫全部 rollback。
- Commit 後才 dispatch；履約失敗保留 Order 與 Completed 封存，只將 Order.FulfillmentStatus 設為 Failed。Complete 出口仍保留 Checkout snapshot，不改綁 Order。事件獨立 scope 與重送用途驗收接續 2.5。

## 驗證對照

新增 `CheckoutLifecycleTests` 33 筆，以及 `ExecutionScopeTests` 5 筆真實 HTTP 案例。既有 CheckoutServiceTests 改由 create 建立有效固定報價，再測 complete；不再將缺資料的舊紀錄當成可完成交易。

| 定案案例 | 本階段證據 |
|---|---|
| CHECKOUT-101／102；FLOW-101～104、106、107、109 | 四種用途組合、100×2−20 報價；Cart 刪除、商品改價、折扣引擎改為拋錯後仍以 180 成交；保存付款、白名單改變不重分類，舊缺 IsTest 為 false。 |
| CHECKOUT-103／104；FLOW-204／205 | 商品刪除、SKU 換綁／刪除、庫存不足／紀錄缺失、同 SKU 合併數量檢查；create 失敗不保存；既有買方拒絕與 HTTP 403 回歸。 |
| CHECKOUT-105；FLOW-201～203、301 | 正式與 test Order／封存用途與固定數值正確、同一完成時間；正式扣庫存、test 留實際略過紀錄，並行時只有正式扣減。 |
| CHECKOUT-106；FLOW-206 | 庫存 Update、Order Insert、封存 Insert、作用中 Delete 後及 commit 前注入故障；全部 rollback、零事件，重試成功。Create commit 失敗也不綁 context、不留半筆資料。 |
| CHECKOUT-107；FLOW-207／208 | 同交易 12 次並行 complete 最多成功一次；16 次並行 create ID 唯一；兩個 Shared connection 配發與完成；已有非 Checkout 訂單不被覆寫、封存 ID 不重用。 |
| CHECKOUT-108；FLOW-401／402 | typed collection 拒絕修改、upsert、批次更新、刪除／重啟；正式與 test 兩個方向皆驗證用途不可替換，拒絕後原 BSON 不變；合法履約欄位更新保留用途與交易內容。 |
| CHECKOUT-109；FLOW-209 | dispatch 拋錯後交易維持 Completed，完整封存不變，Order.FulfillmentStatus=Failed。事件重送及 FLOW-210 的事件部分留 2.5。 |
| CHECKOUT-110／111；FLOW-108 | 缺 State、TotalPrice、ProductLines、DiscountLines、明細單價或 Unknown 回重建；矛盾總額停止並留 Error；HTTP 409 保留原紀錄。 |
| HTTP context／業務錯誤 | 真實 create 成功後綁定、complete 成功後仍為 Checkout；夾帶不同 paymentId 無法改寫；缺商品 400、不綁 transaction；重送 400、僅一筆 Order。 |

## 執行紀錄

2026-10-18 最終 solution 回歸：**191 passed、0 failed、0 skipped**，其中 Core 113、WebContract 38、AppleBTS.Extension 19、PetShop.Extension 13、PetShop.API 8。相較 2.2 新增 38 筆。

Solution 與 ConsoleUI build 成功，最終增量 build 均為 0 warnings／0 errors。測試重新編譯 Core 時仍有既有 ShopDatabase obsolete 警告，無新增警告。`git diff --check` 通過。

開發中先修正 HTTP 測試誤用 Core Order 作為反序列化 DTO 的問題，改為檢查實際 JSON；另以 HTTP 與 DB 比對確認毫秒時間精度差異，完成上述修正後重跑全套通過。

```sh
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo --verbosity minimal -m:1 -nr:false
dotnet build src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false
dotnet build src/AndrewDemo.NetConf2023.ConsoleUI/AndrewDemo.NetConf2023.ConsoleUI.csproj --no-restore --nologo -m:1 -nr:false
```

測試使用隔離暫存 DB；HTTP 測試啟動本機 loopback Kestrel。Docker images 未重建、使用者本機 DB 未修改。未進行外部模型 CLI 互動或完整 UI smoke；不宣稱 2.7 已完成。

## 後續交接

- 2.4：將 Checkout 的 test 扣庫存判斷整理到共用受保護效果控制，擴及 Product／SKU；沿用實際略過收集資訊，避免長期兩套判斷。
- 2.5：事件獨立 scope、System identity、從 Order 恢復用途與重送／巢狀隔離。
- 2.6：將 CheckoutEffects 接入 OperationResult、headers、CLI／事件出口及完整結構化 log；目前未新增正式對外 headers，也未虛構付款／出貨／統計 adapter。
- 2.7：CommonStorefront、Docker 重建與完整正式／test UI 驗收。
- 保留未來 `/api/orders/{id}/xxx` 後置處理必須攜帶 current Order transaction 的既定註記；未新增後置 endpoint 或任務。
- Frozen spec／testcases 與 Abstract 未修改；只更新 Phase 2 交接計畫進度及 docs。未 commit／push。

## 後續本機部署（2026-10-18）

使用者另行授權重建部署及重新啟動 container，已完成 `common.site-dev` 的 images build 與 `up -d --no-build --force-recreate`。此紀錄更新前述「交付時 Docker 未重建」的環境狀態，不代表 Phase 2.7 完整 UI 驗收完成。

- 停止 API／Storefront／edge 後備份至 `.local/backups/common-site-dev-20261018-031952/`；原 DB 與備份 SHA-256 均為 `b86bb50d062f95b187690b6be2e034983e271edf181ac1874b584fde0f2f574e`。
- 沿用 `.local/data/common-site-dev`，seed 明確輸出 `Existing database found; skipping seed.`，exit code 0。
- API／Storefront／nginx 均為 Up；網站 `http://localhost:5128/products`、直連 API `http://localhost:5108/api/products` 與 edge API `http://localhost:5128/api/products` 均回 200。
- 未建立測試會員、未送出交易或修改既有資料用途；UI 仍為隨機 PaymentId，明確選擇 test payment 留待後續 UI 工作。

> 2026-10-18 後續修訂：會員白名單已改為 Name（預設 test），本文原 ID 白名單敘述保留為當時交付證據；現行規則見 [決策](../decisions/2026-10-18-tip-member-name-allowlist.md)。
