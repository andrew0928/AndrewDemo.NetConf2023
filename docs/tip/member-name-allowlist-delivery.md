# 會員 Name 白名單修訂交付

日期：2026-10-18。分支：feature-test-in-production。

## 決策與實作

使用者確認 [查詢差異](2026-10-18-member-query-investigation.md) 後，同意白名單改以 Name 判定，預設 test。見 [決策](../decisions/2026-10-18-tip-member-name-allowlist.md)。

- TestAllowlist 接受 IEnumerable<string> memberNames，移除會員數值 ID 判定。PaymentId 預設仍為 990001。
- MemberService 首次建立以 Name 與作用中資料庫 Collation 查白名單，與會員查找一致；不額外 trim 或轉數字。
- 既有會員不重新分類，Token 繼承保存用途。ID 配發、原子保存與併發控制保留。
- 正式規格、案例、Core、HTTP 測試與架構說明同步修訂，Abstract 不變。歷史決策與交付紀錄附取代註記。

## 驗證

- Core 118 項、HTTP 合約 38 項全部通過，無跳過。
- 最後將新增 collation 測試改為隔離記憶體資料庫後，會員與白名單 28 項重驗通過。
- 新增驗證：數字名稱／ID 900001 不命中；test 首次建立命中；既有正式 test 不重分類；IgnoreCase、區分大小寫與土耳其語 collation 與會員查找一致。
- 原有併發、rollback、Token、用途不可變及 Checkout 保存用途案例維持通過。
- Docker API publish 成功，只有既存 obsolete 與 XML 文件警告；git diff --check 通過。
- 本次未重新執行 AppleBTS／PetShop 全套測試，未變更其領域政策。

## 本機部署

已重建 common-api 映像並重新建立 API 容器，保留原資料目錄，未執行 seed 或新增登入測試帳號。API 5108/api/products、代理 5128/api/products 與 5128/products 均回應 200。

新建會員使用名稱 test 即命中；名稱 900001 不命中。若 test 已存在且 IsTest=false，重新登入仍為 false，這是用途不可變的預期結果。

```sql
SELECT $ FROM members WHERE Name = 'test';
```

Explorer 舊連線顯示舊資料的問題，本次未宣稱修復 extension；應關閉資料庫連線並重新開啟作用中的檔案再查詢，必要時重載 extension。詳細證據見上述查核紀錄。
