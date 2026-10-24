# troubleshoot：診斷查詢失敗

先看 exit code、HTTP status 與已知 API code；不要把失敗解釋為沒有資料。

| exit | 處理 |
|---|---|
| 2 | 查 `--help`；檢查 source 適用選項、type、日期時區、重複參數、page／all-pages 衝突。member 不可給日期。 |
| 3 | 查 `--skill configure`；確認 dotenv 存在、環境優先序與 token 格式。不印出設定值。 |
| 4 | 確認目標可達、TLS 或逾時；不要改用 insecure、轉送 token 或自動重試。 |
| 5 | 401 為認證失敗；404/admin_disabled 為服務端停用，普通 404 可能 route 不存在；400 為 API 拒絕條件；500 不改成空集合。3xx 不跟隨。 |
| 6 | schema 不符或跨頁漂移。保存無秘密的錯誤資訊；先確認服務版本／是否仍在寫入，不能以舊結果替代。 |
| 7 | 輸出檔已存在或 I/O 失敗；選新檔名或確認本機權限，不刪除既有證據。 |
| 130 | 使用者取消；不得當成成功，剩餘查詢已停止。 |

若查詢成功但找不到 test，先明示 `--type all`，檢查 page／totalCount、member-id 與日期。若 totalAmount=null，閱讀 unknownAmountCount；這是成功回傳的未知資料，不是網路錯誤。

調查只在本次查詢授權範圍內；不要回顯 token、dotenv 原文、完整錯誤 body 或堆疊，也不自動修改 API、部署或資料庫。無法用三個唯讀 API 解釋的問題，指出缺少的證據。
