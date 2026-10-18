# 會員登入與 LiteDB Explorer 查詢差異

日期：2026-10-18。使用者回報以名稱 `900001` 成功登入，但 VS Code LiteDB Explorer 的 `SELECT $ FROM members;` 仍顯示 local-environment-smoke、andrew、test 三筆。

## 查核結果

1. API 的 `/data` bind mount 指向 repo 的 `.local/data/common-site-dev`，登入流程以表單 `name` 呼叫 MemberService.GetOrCreate，再簽發 Token。沒有把數字名稱解讀成 ID 的分支。
2. 停止 API 後複製 DB 至 `/tmp/tip-member-investigation/before.db`，原檔與副本 SHA-256 同為 `964a1b32453050bf7bdfa0078926d3f8e78d446251ad73b94e90121b26b1d5d1`。
3. 以獨立 LiteDB 5.0.17 讀取副本，會員集合只有 `_id=1, Name="900001", IsTest=false`，該會員已有 1 筆 Token。診斷沒有列出 Token 值，也沒有新增會員。
4. 再使用已安裝 LiteDB Explorer 1.1.1 自帶的 LiteDbBridge，開新程序對同一副本執行原 SQL，也只回傳上述 1 筆。SQL 本身與登入會員保存未發現錯誤。
5. Explorer 原有 Bridge log 在 03:41～03:42 及 03:51:50 對同一路徑仍回傳舊的三筆會員。VS Code 畫面也顯示三筆；關閉資料庫後結果表仍留在畫面。因此「仍看到那張表」不代表已重新讀取目前 DB。
6. 已操作 Explorer 關閉並重新選擇正確路徑；Bridge log 有 close／open 成功，但本次 UI 觀測尚未確認新結果表刷新成功。API 已恢復執行。

## 結論與限制

已確認目前 DB 中存在名稱 `900001`，並非目前會員未保存。異常落在 Explorer 原有連線／結果與目前檔案狀態不一致；使用新程序讀取可取得正確資料。尚無足夠證據區分舊 file handle、資料庫曾被替換或快取失效的具體機制，不宣稱已重現或修復 extension 內部問題。

查詢時應先停止 API，關閉 Explorer DB 連線後重新開啟，再執行 SELECT；仍出現舊結果時重載 VS Code window／extension，或使用停機副本與新連線。關閉 DB 不會自動清空舊結果畫面。

## 白名單修正另案

使用者已明確要求會員白名單以可辨識的 Name 判定，而非數值 ID。此次異常查核沒有改動白名單、Member.IsTest、Token 或交易用途。預設 Name 清單與字串比較規則確認後，再同步正式規格與案例；不得因本次更正重新分類既有會員。

後續：使用者已確認預設 Name=test、比對沿用資料庫 collation；規格與實作修訂見 [決策](../decisions/2026-10-18-tip-member-name-allowlist.md)。上述查核結果保留為修改前證據。
