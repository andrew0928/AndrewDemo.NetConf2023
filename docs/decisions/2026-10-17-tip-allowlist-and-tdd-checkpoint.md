# TiP 白名單位置與 TDD 紅燈檢查點

## 狀態

accepted，確認日期：2026-10-17。

**重大決策；影響 .Core。回頭修正 Phase 1；影響 .Abstract / spec。** 使用者接受白名單集中於 Core 的方向，要求先準備介面與測試，確認可以編譯且測試失敗，再檢查後進入實作。

## 背景

會員註冊與 Checkout 建立須共用測試用途來源。API、CLI 與初始化程式不可各自維護清單；此設計延續標準 Core 與 host 分工的主線。

## 決策

- 白名單位於 `src/AndrewDemo.NetConf2023.Core/Testing/TestAllowlist.cs`，採 concrete class，第一版不新增白名單的 Abstract interface。
- 分開提供 `IsTestMember(int memberId)` 與 `IsTestPayment(int paymentId)`；相同整數值在兩種清單中獨立判定。
- 預設值集中於 code；亦提供接收兩份清單的建構式，建立時複製資料，呼叫端後續改動集合不影響已建立的白名單。
- 白名單建立後唯讀，可由 singleton 共用，不捕捉 identity、transaction 或 request 狀態。
- Member 註冊時判定用途；Token 簽發時繼承 Member；Checkout create 查 PaymentId；後續流程使用保存的用途。
- 先停在可編譯的 TDD 紅燈檢查點供使用者確認；使用者後續已確認並要求補上實作。新增測試驗證目標行為，不以斷言 NotImplementedException 作為驗收成功。

## 具體化選擇

- 標準示範預設 MemberId 為 `900001`，PaymentId 為 `990001`，集中於 TestAllowlist；未命中為正式。
- 接收明確清單的建構式取代預設值；空清單表示不將任何 ID 分類為測試用途。
- 查詢方法於紅燈階段保留 NotImplementedException；使用者確認後已補上查詢，服務接入與 DI 註冊留待後續。
- 示範 ID 不表示自動建立會員；註冊時的 ID 配發與 init 欄位初始化仍須在實作階段一起處理。

## 影響

- Core 白名單已由骨架補成可查詢的實作，沿用既有測試。
- 依既有 Context 分離設計新增 Abstract 的 IIdentityContext／ITransactionContext 與不可變快照；Core 的 IdentityContext.Initialize、TransactionContext.Bind／Clear 已完成快照保存與清除，詳見 [Context 合約](../../spec/tip-context-contracts.md)。
- IOrderEventDispatcher 簽章維持，補上獨立 scope 與恢復 Order 用途的契約註解。
- 紅燈階段確認白名單與 Context 行為測試失敗；補實作後以相同行為斷言驗證轉綠，既有資料模型與回歸測試仍須通過。
- 本批未修改既有會員／Checkout 流程，不宣稱已啟用 TiP。

## 替代方案

- 每個 host 各寫一份清單：容易產生不同用途判定，改為共用 Core 定義。
- 第一版存 database 或建立白名單管理 API：目前先以 code 固定值滿足範圍，日後需要再更換載入來源。

## 後續工作

- 白名單查詢與 Context 基礎實作已補上；後續接入 host、DI、middleware 與事件執行邊界。
- 依 [Core 欄位決策](2026-10-17-tip-core-fields-and-explicit-member.md) 接入註冊、簽發與 Checkout 建立流程。

## 驗證結果

2026-10-17：紅燈檢查的 21 項案例已全部轉綠，既有 75 項測試亦通過。完整 solution 編譯成功，0 errors、5 個既有 warnings。
