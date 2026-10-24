# TiP 白名單測試案例

## 狀態

- 日期：2026-10-18。
- 狀態：紅燈檢查已確認，查詢已補實作；會員判定已依 Name 決策修訂。
- 規格：[TiP 測試白名單](../tip-test-allowlist.md)。
- 自動化：[TestAllowlistTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/TestAllowlistTests.cs)，標記 `Feature=TiP` 僅供篩選，不跳過測試。

| 案例 | 前提與操作 | 預期 |
|---|---|---|
| TC-TIP-LIST-001 | 預設清單查 Member.Name test／900001／normal | 分別 true／false／false，共 3 筆。 |
| TC-TIP-LIST-002 | 預設清單查 PaymentId 990001／900001／42 | 分別 true／false／false，共 3 筆。 |
| TC-TIP-LIST-003 | 傳入 Member.Name "17" 與 PaymentId 23 | 各自命中、不交叉匹配，且不帶入預設值。 |
| TC-TIP-LIST-004 | 傳入兩份空清單 | 預設示範名稱及付款 ID 亦不命中。 |
| TC-TIP-LIST-005 | 建立後清空、改寫原輸入集合 | 已建立白名單維持原資料。 |
| TC-TIP-LIST-006 | 建立兩個不同清單的實例 | 各自判定，無共用可變全域清單。 |

共 10 筆行為測試；紅燈階段因查詢方法尚未實作而失敗。既有物件持久化案例仍應通過。

## 紅燈實測

2026-10-17：10 項全部因 TestAllowlist 查詢骨架的 NotImplementedException 失敗；編譯成功，沒有跳過測試。

## 綠燈實測

2026-10-17：使用者確認後補上實作，原有 10 項行為案例全部通過，未修改斷言或跳過測試。完整 solution 編譯成功（0 errors、5 個既有 warnings），全套 96 項測試全部通過。

## Name 規格修訂驗證（2026-10-18）

- 數字名稱 `900001` 與 ID `900001` 均不構成預設會員白名單命中。
- 預設名稱 `test` 首次建立為 test，Token 繼承保存值。
- 既有正式 `test` 及舊資料登入後不重新分類。
- MemberServiceTests 驗證 IgnoreCase、區分大小寫與土耳其語 collation，白名單結果與會員名稱查找一致。
- 併發／失敗重試仍維持唯一 ID，但用途只依名稱判定。
