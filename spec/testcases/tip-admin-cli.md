# TiP Phase 1.2 Admin CLI 驗收案例

日期：2026-10-18。依據：[CLI 合約](../tip-admin-cli.md)。狀態：案例已定案；實際執行與覆蓋見 [實作交付紀錄](../../docs/tip/phase1.2-delivery.md)，不得把案例列數當作通過測試數。Phase 1.1 舊 ADMIN-CLI-001～006 的查詢、編碼、失敗、秘密與唯讀要求由本表承接，輸出／文法改按 Phase 1.2。

## 命令與組態

| ID | 情境 | 預期 |
|---|---|---|
| CLI-001 | stats／list 分別查 member／checkout／order | 僅 GET 對應 admin endpoint，source／type 正確映射，預設 normal。 |
| CLI-002 | 未知或重複選項、缺 source、非法枚舉／分頁／時間、全頁搭配 page | exit 2；無 HTTP、無輸出檔。 |
| CLI-003 | member 帶 from 或 to；order 帶 state；checkout 帶 name | 本地拒絕，不默默忽略。 |
| CLI-004 | 中文、空白、+、&、=、引號與命令樣式的 name；offset +08:00 | 完整編碼為單一值，不新增參數、不執行字串；900001 仍是名稱。 |
| CLI-005 | UTC／offset 等價、單側範圍、from=to、缺時區 | 等價範圍同條件，合法單側可用；非法時間本地拒絕；API 邊界仍為含起點不含終點。 |
| CLI-006 | 無 env-file，cwd 存在／不存在 .env | 預設讀 cwd 的 .env；不存在則略過，環境優先；明確路徑覆寫預設。 |
| CLI-007 | 同 key 同時有環境與 dotenv；環境空值 | 環境優先，空值不 fallback；缺／空 token exit 3，不呼叫 HTTP。 |
| CLI-008 | dotenv BOM／空行／註解／單雙引號／其他 key | 按契約解析，只套用兩個已知 key，不修改父環境。 |
| CLI-009 | 指定檔缺失、無權限、重複 key、破損引號、export／多行 | exit 3；錯誤不包含原行／秘密，沒有部分設定後繼續查詢。 |
| CLI-010 | 遠端 HTTP、userinfo、額外 path／query／fragment、偽 localhost 前綴 | 不送 token；本機三種 host 及遠端 HTTPS 可用，根 slash 正規化。 |
| CLI-011 | 觀察程序 argv、stdout／stderr、暫存與輸出；token 模擬錯誤 | 沒有 token；不參考會員登入或 Docker 設定；不直接開 DB。 |

## 統計、明細與輸出

隔離 fixture：normal 訂單 100、200（履約 failed），test 訂單 900（Buyer=false）；另備未知金額／時間、空字串／null、Unicode／控制字元與 CSV 特殊字元，不直接修改使用者部署資料。

| ID | 情境 | 預期 |
|---|---|---|
| CLI-101 | order stats normal／test／all | 一次 HTTP page=1/pageSize=1；count=2/1/3、amount=300/900/1200，使用 API 統計；無逐頁加總。 |
| CLI-102 | member／checkout stats | 僅適用筆數／排除時間指標；checkout state／timeField 保持，沒有虛構營收。 |
| CLI-103 | order list pageSize=1 | 一筆 rows，API totalCount／總額仍完整；不把 200 當 300。 |
| CLI-104 | 空集合、超最後頁、未知金額、0 元、null 關聯／時間 | 區分本頁空與集合空；未知不補值，0／false 保留；totalAmount=null 仍 exit 0。 |
| CLI-105 | 已有會員／訂單用途不同 | 逐列使用自身 IsTest，不依 Buyer 或名字重新分類。 |
| CLI-106 | TXT 長名稱、控制字元、狀態、UTC、decimal | 完整欄位可讀、不截斷、不執行 ANSI；精度不變、不加貨幣或本地時間假設。 |
| CLI-107 | 三來源 × stats/list × txt/csv/json | 同份查詢結果投影一致；JSON schemaVersion／filters／pagination／observations／rows 合約正確；CSV 欄序固定。 |
| CLI-108 | CSV 逗號、引號、換行、Unicode、公式起始字串、負數金額 | UTF-8 BOM／CRLF／合法 quoting；文字公式保護，負數數值不變；null 空欄且 JSON 可保留原值。 |
| CLI-109 | stdout／output、格式與副檔名矛盾、目標存在 | 只發布指定格式；存在檔不覆寫、矛盾 exit 2；輸出失敗 exit 7，舊檔不變。 |
| CLI-110 | 中途 HTTP／JSON／磁碟錯誤或取消 | 不發布成功檔，清理暫存；無 API 半份結果到 stdout；底層 stdout I/O 失敗非零。 |
| CLI-111 | 從不同 cwd 執行 published CLI | HTTP／輸出行為相同，help／skill 不依 repo 位置；無秘密檔被複製到 publish。 |

## 全頁、HTTP 與錯誤

| ID | 情境 | 預期 |
|---|---|---|
| CLI-201 | list --all-pages，0／1／多頁 | 從第 1 頁完整讀取，按 ID 降冪串接，記錄每頁 queriedAt，不自動合併 stats 金額。 |
| CLI-202 | 全頁過程 count／金額改變、重複 ID、提前空頁、順序／筆數不符 | exit 6，不發布部分結果，不宣稱原子快照或自動重試。 |
| CLI-203 | 不可偵測的相同 count 更新 | 文件與輸出仍明示跨頁非原子；測試不能以漂移檢查宣稱完整 snapshot 保證。 |
| CLI-204 | HTTP 3xx／400／401／404／500 | exit 5，不跟轉址或重試；stderr status／安全 code，stdout 空；停用與未知 route 區分。 |
| CLI-205 | DNS／連線／TLS／30 秒逾時、Ctrl+C | exit 4／130，取消剩餘頁面；不切 insecure 或舊結果。 |
| CLI-206 | 200 空 body、非法 JSON、缺欄位、錯型別、重複 key | exit 6；不偽造空 rows／0／normal；合法額外欄位不造成失敗。 |
| CLI-207 | 執行所有來源與模式前後觀察服務 | 只讀取 API，無 Token／交易／DB／容器異動；admin 無單筆 transaction 不解讀為失敗。 |

## 自我描述與 Agent 指引

| ID | 情境 | 預期 |
|---|---|---|
| CLI-301 | 無 token、網路離線且 cwd 無 repo，執行 help／version／skill | exit 0；不讀 dotenv、不 HTTP、不寫檔。人用 help 有短例、預設值與會員日期限制。 |
| CLI-302 | --skill 無 topic／六個合法 topic／未知 topic | 索引／各自完整指引；未知 exit 2 列合法 ID，不輸出無關全量內容。 |
| CLI-303 | 發佈版本與指引 | 同一 binary 內嵌 canonical Markdown，索引 topic 可全部解析；沒有指向執行環境不存在路徑的必要依賴。 |
| CLI-304 | 指引任務：「查 test 為何看不到」 | 導向 type=all/test 與 filters/page，不把 name 當 ID、不重分類資料。 |
| CLI-305 | 指引任務：「查當月正式營收與全部明細」 | 指出保存訂單金額非實收，帶明確時區／normal，明細匯出明示 all-pages，保留未知／非原子限制。 |
| CLI-306 | 指引任務：「用結果證明 TiP 全部通過」 | 分清可查核與不可推論證據；不由清單宣稱庫存／事件／Token／所有 AC 已通過，不自行新增交易。 |
| CLI-307 | 指引任務：「401 幫我修到好」 | 說明認證與組態優先序，不索取聊天秘密、不自行重啟容器或替換 token。 |

301～303 為可自動化的內容／打包行為；304～307 為指引使用情境 review，不以精確文案字串比對冒充 agent 行為驗證。測試命令、實際通過數與限制由後續實作交付記錄。

## 最後驗收

在標準部署中執行 [最終驗收流程](../../docs/tip/phase1.2-final-verification.md)，保存版本、命令、條件、各次 queriedAt 與 JSON。只讀既有已知案例，或由使用者另外完成已授權的交易準備；不得讓 CLI 自行建立資料。歷史 2.7 結果可當交叉參考，不能假設 ID／總額永遠不變。範圍外 AC 與不存在 adapter 明示不適用。
