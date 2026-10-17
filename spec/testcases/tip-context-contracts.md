# TiP Context 合約測試案例

## 狀態

- 日期：2026-10-17。
- 狀態：紅燈檢查已確認，Core context 已補實作；沿用原有 11 筆行為案例。
- 規格：[Context 合約](../tip-context-contracts.md)。
- 自動化：[ExecutionContextTests](../../tests/AndrewDemo.NetConf2023.Core.Tests/ExecutionContextTests.cs)，標記 `Feature=TiP`，不跳過測試。

| 案例 | 操作 | 預期 |
|---|---|---|
| TC-TIP-CTX-001 | 建立全新的兩個 context | identity 為匿名，MemberId 為 null；transaction 為 null。 |
| TC-TIP-CTX-002 | 初始化正式／測試會員 identity | Kind、MemberId、IsTest 正確，transaction 仍為 null，共 2 筆。 |
| TC-TIP-CTX-003 | 單獨讀取新建 transaction context | Current 為 null。 |
| TC-TIP-CTX-004 | 綁定 Checkout／Order 的正式／測試快照 | Kind、Id、IsTest 正確，共 4 筆。 |
| TC-TIP-CTX-005 | 正式 identity 搭配 test transaction，再清除交易 | 交易恢復 null，identity 不變；先前讀取的交易快照仍保留原 ID 與用途。 |
| TC-TIP-CTX-006 | 系統 identity 處理 test Order | identity 為系統且 false，transaction 仍為 test。 |
| TC-TIP-CTX-007 | 建立並操作不同實例 | 新實例不繼承外層資料，操作內層不覆寫外層資料。 |

共 11 筆案例。這些案例不宣稱已驗證 DI lifetime、事件 dispatcher、HTTP middleware 或商業效果控制；對應整合測試隨後續實作補上。

## 紅燈實測

2026-10-17：11 項全部因 Core context 骨架的 NotImplementedException 失敗；編譯成功，沒有跳過測試。全 solution 其餘 75 項既有測試通過。

## 綠燈實測

2026-10-17：使用者確認後補上實作，原有 11 項行為案例全部通過，未修改斷言或跳過測試。完整 solution 編譯成功（0 errors、5 個既有 warnings），全套 96 項測試全部通過。
