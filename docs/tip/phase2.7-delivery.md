# TiP Phase 2.7 交付紀錄

日期：2026-10-18。分支：`feature-test-in-production`。完成 CommonStorefront 付款選擇、操作摘要與本機 Docker／Edge／資料庫整合驗證。Abstract、frozen 業務規格及成功 response body 不變；本次僅追加 testcase 證據與階段進度。使用者於 2026-10-18 授權 commit／push，2.6、2.7 一併收錄於本次提交。

## 實作

- 結帳明確選擇「一般示範付款」（PaymentId=0）或「測試付款」（990001），取代隨機付款 ID。create 保存付款，complete 不傳入付款；非法表單選擇不呼叫 Checkout API。
- Shared client 從本次 HTTP response headers 建立 `ApiOperationSummary`，成功與拒絕／失敗均保留摘要；不以會員或畫面選項自行重算用途，不使用跨請求的 LastResult。不完整的用途／交易 headers 不假造 normal 或交易。
- 成功透過 TempData 帶到訂單頁，顯示用途、Outcome、交易識別與實際略過代碼。header 沒有逐筆目標，因此摘要不假造完整 SkippedEffect。不存在的付款／出貨 adapter 不列為略過。
- 成功訊息使用回傳的 OrderDetail.Id，避免誤把 Checkout ID 當成訂單 ID。會員頁顯示保存用途；訂單逐筆顯示用途，會員購買紀錄總額明示包含測試訂單，正式營收仍由 admin 查詢排除 test。
- 400、409、500 保留購物車及 API 摘要；舊交易 409 引導重新確認購物車與付款後重建，不補造舊資料、不自動重試。欄位有 label／fieldset 與錯誤關聯，結果區以 aria-live 呈現。

## 自動化證據

[StorefrontOperationTests](../../tests/AndrewDemo.NetConf2023.WebContract.Tests/StorefrontOperationTests.cs) 新增 8 項：三種付款／API 用途組合、實際訂單 ID、成功清理 session、400／409／500 保留購物車且無自動重試、缺 headers 不假造結果、非法付款選擇拒絕。這些是 PageModel／client 測試，真實 HTTP headers 另由 2.6 Kestrel 整合測試覆蓋。

```sh
dotnet test src/AndrewDemo.NetConf2023.slnx --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false --verbosity quiet
dotnet build src/AndrewDemo.NetConf2023.CommonStorefront/AndrewDemo.NetConf2023.CommonStorefront.csproj --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false --verbosity quiet
docker compose -f compose/common.site-dev.compose.yaml build common-api common-storefront
```

- solution **335 passed、0 failed、0 skipped**：Core 161、WebContract 134、AppleBTS Extension 19、PetShop Extension 13、PetShop API 8。
- CommonStorefront build：0 warnings／0 errors；兩個 Docker images 建置成功並已重新建立 API、Storefront、edge 容器。
- `git diff --check` 通過；測試與 build log 位於本機 `/tmp/tip27-*.log`，不提交到 Git。

## Edge 與部署資料查核

使用者指定 Edge；以 Computer Use 操作獨立 InPrivate 視窗，網站 `http://localhost:5128`。環境未提供 agent-browser，以下為 Edge 實際操作證據，不宣稱執行了該工具。保留現有部署資料，與自動化使用隔離 DB 的案例分開記錄。

測試商品為可樂（ProductId=2、SKU-COKE-350、單價 18），新增一般會員 `tip27-normal`（ID=4、IsTest=false），沿用 `test`（ID=2、IsTest=true）。

| 會員／付款 | 數量／金額 | Checkout／Order | 畫面結果 |
|---|---|---|---|
| 一般／0 | 1／18 | 4／4 | normal、succeeded、無略過 |
| 一般／990001 | 2／36 | 5／5 | test、succeeded、inventory.deduct |
| test／0 | 1／18 | 6／6 | test、succeeded、inventory.deduct |
| test／990001 | 1／18 | 7／7 | test、succeeded、inventory.deduct |
| test／0，庫存不足 | 100／1800 | 8／無 | test、rejected、無略過；購物車保留 |

已操作登入、商品列表／詳細、加入購物車、兩種付款、成功跳轉、會員用途、一般／測試訂單，以及拒絕後返回購物車並刪除本輪測試項目。拒絕的 Checkout #8 仍維持 Created，沒有 Order／封存／ClosedAt；沒有替使用者取消或刪除交易紀錄。

手機尺寸補驗已完成：使用者授權讀取 Edge 視窗以定位商店後，解除先前焦點不明造成的阻擋。使用 Edge DevTools Responsive **400 × 611 CSS px**，從商品列表／綠茶詳細頁、購物車、付款選擇到送單與結果頁完成主要任務，過程不需水平捲動。數量欄位與付款 radio 可用鍵盤操作，Tab 可到達送單按鈕，Return 成功提交；focus 樣式可見。已檢視窄版付款欄位、成功通知、操作結果、略過代碼及訂單總額換行。此為 TC-CS-012 的本次 Edge 模擬尺寸證據，不代表所有實體手機或整套舊 baseline 的鍵盤驗收。

手機案例使用 `test` 會員、PaymentId=990001、綠茶 2 件／50 元，成功建立 **Checkout #9／Order #8**。成功通知正確顯示訂單 8，操作摘要顯示結帳 9，證明兩種 ID 不同時仍呈現正確。用途為 test、結果 succeeded、實際略過 inventory.deduct。

桌面驗證完成時，admin API 與停止 writer 後取得的 LiteDB 一致性快照交叉核對：

- 原有 Order #1～3 未變；新訂單與完整 Completed 封存的用途、付款、商品數量、金額、時間、CheckoutTransactionId 一致；成功交易已從作用中移除。
- 可樂庫存 **100 → 99**，只有一般交易扣一件；其餘 SKU 不變。
- 正式訂單 **2 筆／208 → 3 筆／226**；三筆測試交易未增加正式統計。測試訂單含既有資料為 **4 筆／222**。
- admin members 查到 `tip27-normal=false`、`test=true`；會員用途沒有因測試付款改寫。
- HTTP／PageModel 的 409、用途 BUG 500、並行及事件重送以自動化驗證，未破壞部署資料來製造這些錯誤。

手機補驗後再次核對 admin 與一致性 LiteDB 快照：Order #8 關聯 Completed 封存 #9，PaymentId=990001、用途 test、數量 2、總額 50 與關閉時間均正確；既有 Order #1～7、作用中 Checkout #8、所有 SKU 庫存完全不變。正式統計仍為 **3 筆／226**，測試統計增為 **5 筆／272**。證據保存在 `.local/tip27/mobile`，與桌面驗證快照分開。API 已重新啟動，網站與商品 API 均回 200。

## 部署與保留資料

- 作用中 DB：`.local/data/common-site-dev/shop-database.db`，容器 `/data/shop-database.db`。沿用現有 admin token，未輸出或提交秘密。
- 部署前停止 API 後備份至 `.local/backups/tip27-before-20261018-213257`；DB SHA-256：`278cdbc81f7d5ef4869428f196dda208ae9f7fcb78b10ee333d788bb85e0cd67`。seed 確認跳過既有 DB。
- 本輪 admin 回應、前後查詢與驗證腳本位於 Git 忽略的 `.local/tip27`。讀取快照前停止 API，複製完成即重新啟動；不讓 extension 快取或同時寫入影響查核。
- 若自行重建容器，仍需提供既有 `SHOP_ADMIN_ACCESS_TOKEN`；未設定時 admin API 依契約停用。一般交易測試已實際改變本機庫存，未回滾這些驗收資料。

真實付款／出貨／事件統計 adapter 不存在，標示不適用；PetShop 的 AC-007／008／012／015 與 AppleBTS TiP 不在本輪。完整 AC 邊界見 [覆蓋對照](ac-test-coverage.md)。

## 後續付款輸入調整

上述 radio 操作為本階段交付時的歷史證據。使用者後續要求改為直接輸入 PaymentID，現行版本依 [PaymentID UI 交付](payment-id-ui-delivery.md)；既有用途判定與 HTTP 契約維持。
