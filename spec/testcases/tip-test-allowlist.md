# TiP 白名單測試案例

## 狀態

- 日期：2026-10-17。
- 狀態：紅燈檢查已確認，查詢已補實作；沿用原有 10 筆行為案例。
- 規格：[TiP 測試白名單](../tip-test-allowlist.md)。
- 自動化：[TestAllowlistTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/TestAllowlistTests.cs)，標記 `Feature=TiP` 僅供篩選，不跳過測試。

| 案例 | 前提與操作 | 預期 |
|---|---|---|
| TC-TIP-LIST-001 | 預設清單查 MemberId 900001／990001／42 | 分別 true／false／false，共 3 筆。 |
| TC-TIP-LIST-002 | 預設清單查 PaymentId 990001／900001／42 | 分別 true／false／false，共 3 筆。 |
| TC-TIP-LIST-003 | 傳入 MemberId 17 與 PaymentId 23 | 各自命中、不交叉匹配，且不帶入預設 ID。 |
| TC-TIP-LIST-004 | 傳入兩份空清單 | 預設示範 ID 亦不命中。 |
| TC-TIP-LIST-005 | 建立後清空、改寫原輸入集合 | 已建立白名單維持原資料。 |
| TC-TIP-LIST-006 | 建立兩個不同清單的實例 | 各自判定，無共用可變全域清單。 |

共 10 筆行為測試；紅燈階段因查詢方法尚未實作而失敗。既有物件持久化案例仍應通過。

## 紅燈實測

2026-10-17：10 項全部因 TestAllowlist 查詢骨架的 NotImplementedException 失敗；編譯成功，沒有跳過測試。

## 綠燈實測

2026-10-17：使用者確認後補上實作，原有 10 項行為案例全部通過，未修改斷言或跳過測試。完整 solution 編譯成功（0 errors、5 個既有 warnings），全套 96 項測試全部通過。
