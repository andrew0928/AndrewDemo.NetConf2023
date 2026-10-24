# 第五章 demo：先測試，再以正式交易對照

2026-10-24 於本機實際操作完成。一般會員 `andrew`（Member #1）先使用測試 PaymentID `990001`，再以同一帳號建立另一筆交易，使用一般 PaymentID `123456`。兩次皆購買三罐 18天台灣生啤酒 355ml，單價 65 元、小計 195 元、第二件六折折抵 26 元，訂單金額皆為 169 元。

使用 source commit `c67e2d37d426698571246dc97676cb68a2c3e83b` 建置 API、CommonStorefront、DatabaseInit 與 StorageProbe。在全新暫存資料庫執行 seed，透過 Edge 的實際網頁操作完成兩次結帳。這是本機 PoC 的實測，沒有串接真實金流；文章尚未修改，亦未提交 Git。

## 實測結果

| 查核時點 | 啤酒庫存 | 正式訂單筆數 | 正式訂單總額 | 測試訂單筆數 | 測試訂單總額 |
| --- | ---: | ---: | ---: | ---: | ---: |
| 操作前 | 100 | 0 | 0 | 0 | 0 |
| 測試訂單 #1 完成後 | 100 | 0 | 0 | 1 | 169 |
| 正式訂單 #2 完成後 | 97 | 1 | 169 | 1 | 169 |

- 測試交易：Checkout #1 → Order #1，畫面顯示 `test / succeeded`，列出略過 `inventory.deduct`。
- 正式交易：Checkout #2 → Order #2，畫面顯示 `normal / succeeded`，沒有因測試而略過的步驟。
- 會員仍是一般會員，兩筆訂單皆歸屬 `andrew`。正式交易完成後，原測試訂單的讀回內容不變。
- 會員訂單頁的「購買紀錄總金額（含測試訂單）」為 **338 元**；管理查核的 **正式訂單總額為 169 元**。文章使用畫面時應保留這個欄位意義。

## 擷圖與建議位置

以下皆為直接擷取的 Edge 原始畫面，保留瀏覽器外框，沒有合成或改寫畫面內容。

| 素材 | 內容 | 第五章建議用途 |
| --- | --- | --- |
| [01-member.jpg](01-member.jpg) | `andrew`、會員編號 1、一般會員 | 5.1 證明一般會員身分；結帳頁本身不顯示會員用途 |
| [02-test-checkout.jpg](02-test-checkout.jpg) | 三罐啤酒、26 元折扣、169 元總額、PaymentID `990001` | 待補素材 5-A |
| [03-test-result.jpg](03-test-result.jpg) | 測試訂單 #1 成功、略過扣庫存、商品與折扣明細 | 待補素材 5-B |
| [04-normal-checkout.jpg](04-normal-checkout.jpg) | 相同商品與金額、PaymentID `123456` | 待補素材 5-C 的送出前對照 |
| [05-normal-result.jpg](05-normal-result.jpg) | 正式訂單 #2 成功、沒有略過步驟，保留測試訂單 #1 | 待補素材 5-C 的結果 |
| [06-orders.jpg](06-orders.jpg) | 重新讀取的兩筆訂單列表，無單次結帳提示 | 最後對照兩筆訂單用途與相同內容 |

建議正文優先使用 02、03、05，最後搭配上述實測表格；01、04、06 可視篇幅選用。原始擷圖解析度較高，排版時可依重點裁切。

## 資料查核證據

每份 JSON 同時保存 StorageProbe `--inspect` 的唯讀資料讀回，以及 Admin API 的 normal／test／all 訂單查詢與會員查詢。庫存來自 `inventory_records`，訂單總額來自 Admin API，並與各自 collection 的訂單加總比對。

- [00-baseline.json](00-baseline.json)：操作前。
- [01-after-test.json](01-after-test.json)：測試交易完成後、正式交易開始前。
- [02-after-normal.json](02-after-normal.json)：正式交易完成後。
- [run.json](run.json)：版本與本次本機執行位置。

三個時點的庫存、筆數與總額均已核對；Admin API 的訂單 ID 與金額也與資料庫讀回一致。正式訂單 #2 存在 `orders`，測試訂單 #1 存在 `test_orders`；兩筆 Checkout 已移至各自用途的封存 collection，PaymentID 分別為 `123456`、`990001`，作用中的 Checkout 均為空。

待補素材 5-D 可以直接採用上方實測表格，並以這三份 JSON 作為查核依據。原稿表格的 100 → 100 → 97 與 0／169 元增量，已獲本輪實測支持；本輪訂單 ID 是 **測試 #1、正式 #2**。

## 重現流程

1. 從目前版本建置上述四個專案，將 DatabaseInit 輸出複製至全新暫存目錄後執行 seed。
2. API 的 `SHOP_DATABASE_FILEPATH` 指向該暫存 DB；設定本次獨立的 admin token。啟動 API 與 CommonStorefront，前者連接埠為 5197、後者為 5198。
3. 保存初始庫存與 normal／test／all 訂單統計。
4. 用 `andrew` 登入網頁，加入三罐啤酒，以 `990001` 結帳；擷取送出前與成功畫面，再保存資料讀回。
5. 同一帳號重新加入三罐啤酒，以 `123456` 結帳；擷取送出前、成功與訂單列表畫面，再保存資料讀回。

本輪初始化僅作用於新建的暫存 DB，沒有改動原有 demo 的資料庫。操作時曾先加入一罐、於結帳前從購物車移除，再重新加入三罐；只有上述兩筆 Checkout 與 Order，對照基準不受影響。

## 追加：AdminCLI 原始輸出

2026-10-24 追加查核。將兩筆交易完成後保留的 demo 資料庫複製至新的暫存目錄，啟動相同 API，使用目前版本發佈的 `shop-admin` 執行下列唯讀查詢。這些是交易完成後的實際 CLI 輸出，沒有重建或模擬操作前的 CLI 輸出。

- `list --source order --type all --all-pages`：[完整 TXT 輸出](cli/orders-all.txt)、[JSON 輸出](cli/orders-all.json)。共 2 筆、338 元；訂單 #1 為 test、#2 為 normal，兩者買方皆為 `andrew`。
- `stats --source order --type normal`：[完整 TXT 輸出](cli/stats-normal.txt)、[JSON 輸出](cli/stats-normal.json)。共 1 筆、169 元。
- `stats --source order --type test`：[完整 TXT 輸出](cli/stats-test.txt)、[JSON 輸出](cli/stats-test.json)。共 1 筆、169 元。
- [完整命令與退出碼](cli/commands.json)：六次呼叫均 exit 0；TXT 與 JSON 分開呼叫，因此查詢時間各自獨立。

查詢前的資料讀回與 `02-after-normal.json` 相同，查詢後讀回也與查詢前相同；本次 API 已停止。CLI 不提供庫存查詢，庫存仍以前述三個時間點的原始讀回為依據。
