# TiP 測試白名單

## 狀態

- 日期：2026-10-17。
- 狀態：紅燈檢查已由使用者確認，白名單查詢已實作。
- 依據：[白名單與 TDD 決策](../docs/decisions/2026-10-17-tip-allowlist-and-tdd-checkpoint.md)。

## Core 介面

`AndrewDemo.NetConf2023.Core.Testing.TestAllowlist` 為 sealed concrete class。

```csharp
public TestAllowlist();
public TestAllowlist(IEnumerable<int> memberIds, IEnumerable<int> paymentIds);
public bool IsTestMember(int memberId);
public bool IsTestPayment(int paymentId);
```

- 無參數建構式使用 code 中的預設清單：MemberId `900001`、PaymentId `990001`。
- 明確傳入兩份清單時取代預設清單；MemberId 與 PaymentId 分別判定，命中為 true，未命中為 false。
- 空清單表示全部未命中；兩種 ID 不交叉匹配。
- 建構式複製輸入集合，不公開可變清單；不同實例可擁有不同清單。
- 此類別只判定 ID 是否命中，不直接建立或修改 Member、Token、Checkout、Order。
- 清單建立後唯讀，後續可註冊為 singleton；本批尚未接入 DI 或服務流程。

## 用途判定時機

| 流程 | 規則 |
|---|---|
| Member 註冊 | 以確定的 MemberId 判定並初始化 Member.IsTest。 |
| Token 簽發 | 繼承 Member.IsTest，不重新查白名單。 |
| Checkout create | 使用 Member.IsTest 或 PaymentId 命中結果決定用途。 |
| Complete／Order／事件 | 承接已保存用途，不重新套用清單。 |

既有資料 IsTest 為 false 的政策不變；後續變更清單不得重新分類已存在物件。

## TDD 檢查

新增行為案例見 [testcases](testcases/tip-test-allowlist.md)。紅燈階段的正確結果為 build 成功、目標行為測試失敗；測試不跳過、不使用 Assert.Throws 將未實作視為成功。使用者已確認紅燈檢查點並授權補上實作；目前查詢依各自的集合回傳是否命中，測試以原有行為斷言驗證。

## Phase 1 基準

本規格已納入 [Phase 1 定案清單](tip-phase1.md)。後續 Phase 2 依此接入流程；規格與 Abstract 合約如需改動，須由使用者明確重新開啟規格階段。
