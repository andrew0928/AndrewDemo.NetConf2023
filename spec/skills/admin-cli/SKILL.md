---
name: shop-admin-query
description: 使用 shop-admin 唯讀查詢會員、結帳與訂單的統計或明細，匯出證據，並核對標準 TiP 保存用途與正式總額；適用於 admin 查核，不用於登入、交易、修改資料或部署。
---

# Shop Admin 查核指引

先依任務讀取一個 topic；不需載入全部文件。CLI 入口為 `shop-admin`，人用說明可執行 `shop-admin --help`。本指引由 CLI 內嵌提供，不依賴 repo、外部網站或其他 skills。

| topic-id | 何時使用 | 取得完整指引 |
|---|---|---|
| configure | 首次設定、選擇服務或 dotenv | `shop-admin --skill configure` |
| query-stats | 查總筆數、正式訂單總額、未知金額 | `shop-admin --skill query-stats` |
| query-details | 查會員／訂單／結帳與關聯 | `shop-admin --skill query-details` |
| export | 保存 TXT／CSV／JSON 或完整分頁結果 | `shop-admin --skill export` |
| tip-verify | 做 TiP 最後查核並整理證據範圍 | `shop-admin --skill tip-verify` |
| troubleshoot | 命令、設定、HTTP、回應或輸出失敗 | `shop-admin --skill troubleshoot` |

每次報告保留 source、type、有效條件與 queriedAt。預設 type=normal；查測試會員時須明示 test 或 all。會員不接受日期。跨頁／跨來源不是原子快照，CLI 的成功退出碼也不是完整 TiP 通過證明。

不要要求使用者把 token 貼入聊天，不執行會員登入、checkout 或容器修改來解決查詢問題。若任務超出三個唯讀 API，說明缺少的證據或能力，再沿已授權範圍處理。

維護來源：本目錄的 `references/<topic-id>.md`。每個 topic 同樣可用 CLI 命令獨立取得。
