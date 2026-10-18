# Phase 1.2：用 Admin CLI 做 TiP 最後確認

本操作流程使用已實作的 CLI；建置與執行方式見 [AdminCLI 操作說明](admin-cli.md)。使用 .NET `shop-admin`，完整契約見 [Phase 1.2](../../spec/tip-phase1.2.md)。目標是讓查核可以重複執行、保存證據，不再依賴 LiteDB extension 的開檔／快取狀態。

## 先準備

由 CLI 發佈版本的安裝／執行說明取得 `shop-admin`，確認 `--version`。API 必須已啟動並有 admin token；使用本機環境提供 SHOP_ADMIN_BASE_URL／SHOP_ADMIN_ACCESS_TOKEN，或在 working directory 的 `.env` 提供設定，也可用 --env-file 指定其他檔案。程序環境優先，不在聊天、命令列或匯出檔中放 token。

```text
shop-admin --help
shop-admin --skill
shop-admin --skill tip-verify
shop-admin stats --source order --type normal --env-file .local/admin.env
```

後續例子假設已設定程序環境；若 dotenv 位於 working directory 的 `.env`，不必另加參數；其他位置每個查詢加上 --env-file。先建立新的空證據目錄，例如 `.local/tip-final/run-001`；不要覆寫舊一輪。核對期間先停止產生新交易，服務保持啟動。記錄分支 commit、CLI／部署版本、時間與使用的非秘密設定。

## 查會員、統計與明細

```text
shop-admin list --source member --type all --all-pages
shop-admin list --source member --type all --name test
shop-admin stats --source order --type normal
shop-admin stats --source order --type test
shop-admin stats --source order --type all
shop-admin stats --source checkout --type test --state completed --time-field closedAt
shop-admin list --source order --type all --all-pages
shop-admin list --source checkout --type all --all-pages
```

會員依名稱取得真實 ID；正式／測試付款不會把既有 Member 重新分類。type=all 合併兩類，不能稱正式營收。統計採全符合條件，與 list 當頁筆數不同。若資料量大，可加 member-id／日期縮小條件，再用 all-pages 讀完。

日期只適用訂單與結帳，起點含、終點不含；以下日期為範例，須替換成實際驗收範圍：

```text
shop-admin stats --source order --type normal --from 2026-10-18T00:00:00+08:00 --to 2026-10-19T00:00:00+08:00
shop-admin list --source checkout --type test --state completed --time-field closedAt --from 2026-10-18T00:00:00+08:00 --to 2026-10-19T00:00:00+08:00 --all-pages
```

Order 使用 CreatedAt，Checkout 可用 ClosedAt；兩者不保證恰好落在相同時間邊界。核對已知 ID 時先用無日期清單確認，不能因一邊被日期排除就斷言漏資料。未知時間／金額／關聯要保留待確認，不補值。

## 保存基準與比較

```text
shop-admin list --source member --type all --all-pages --format json --output .local/tip-final/run-001/members.json
shop-admin stats --source order --type normal --format json --output .local/tip-final/run-001/normal-stats.json
shop-admin stats --source order --type test --format json --output .local/tip-final/run-001/test-stats.json
shop-admin list --source order --type all --all-pages --format json --output .local/tip-final/run-001/orders.json
shop-admin list --source checkout --type all --all-pages --format json --output .local/tip-final/run-001/checkouts.json
shop-admin list --source order --type all --all-pages --format csv --output .local/tip-final/run-001/orders.csv
```

JSON 含各次 observations／queriedAt 與 rows；CSV 方便人工表格閱讀，無完整查詢 metadata。兩次匯出各自查 API，不宣稱同一快照。exit 非零不當作沒有資料；檔案已存在就換新的 run 目錄。

若要做新的前後比較，先保存 before，再由使用者透過既有 UI 或已授權的測試流程準備下表交易，完成後在新目錄保存 after。Admin CLI 全程唯讀，不代為送單、取消、重送或改庫存。也可只核對已知既有交易，但沒有 before 時不能回推未保存的歷史庫存／總額。

| 已知案例 | CLI 最後核對 |
|---|---|
| 一般會員／一般付款成功 | Member=false，Order／Checkout=false；只有此類訂單增加 normal 總額。 |
| 一般會員／測試付款成功 | Member 仍 false，Order／Checkout=true；Checkout 保存測試 PaymentId；normal 總額不增加。 |
| test 會員／一般付款與測試付款成功 | Member、Order、Checkout=true；保存各自付款；normal 總額不增加。 |
| 庫存不足遭拒絕 | 指定交易為 active／created，沒有關聯訂單；拒絕原因另依實際操作結果確認。 |
| 成功交易封存 | Order.checkoutTransactionId 對到 archived／completed，IsTest、totalPrice 與已知付款一致，時間非未知。不假定 Order.id 等於 Checkout ID。 |

比較要使用相同篩選與已知新增案例。normal 增量等於已知一般訂單金額，test 增量等於已知測試訂單金額；有其他寫入或未知金額時無法據此下完整結論。不得將 Order 與 Checkout 金額再加總。

2.7 的歷史參考：會員 test ID=2、tip27-normal ID=4；一般訂單 #4、測試訂單 #5～8，手機 Order #8 關聯 Checkout #9；拒絕 Checkout #8 保留 active。當時 normal 為 3 筆／226、test 為 5 筆／272。這些只用於找回已知證據，不是後續每次執行必須固定的資料值。

## 整體 TiP 的證據邊界

| 成果 | 最後確認方式 |
|---|---|
| 會員／訂單／結帳用途、付款、狀態、時間、金額與關聯 | 本 CLI 的清單與統計。 |
| 測試訂單不增加正式總額 | 受控 before／after CLI 統計與已知交易清單。 |
| 庫存不扣減、商品／SKU 防寫、Token 繼承、Cart 真實增刪 | 既有 Core／HTTP 自動化；三個 admin API 沒有這些資料。 |
| 買方拒絕、用途 BUG、不可變用途、並行與事件重送／隔離、headers／log | 既有 WebContract／Core 整合測試及其實際輸出；不由查詢成功推論。 |
| 真實金流／出貨／事件統計 adapter、PetShop 專屬 TiP | 依既定範圍標不適用，不能以替身或一般模組測試宣稱實際能力通過。 |

必要時從 repo root 執行既有回歸以補齊當前版本證據：

```sh
dotnet test src/AndrewDemo.NetConf2023.slnx --nologo -m:1 -nr:false -p:UseSharedCompilation=false
```

這是另外的驗證命令，不是 admin CLI 子命令。後續 CLI 的專屬測試亦須納入交付；測試數以當次實際輸出為準。對照 [AC 覆蓋表](ac-test-coverage.md) 記錄每項來源，輸出「CLI 已查核／自動化已驗證／未驗證／不適用」，再由使用者做最後接受確認。
