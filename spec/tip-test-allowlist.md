# TiP 測試白名單

## 狀態

- 日期：2026-10-18。
- 狀態：紅燈檢查已由使用者確認，白名單查詢已實作。
- 依據：[白名單與 TDD 決策](../docs/decisions/2026-10-17-tip-allowlist-and-tdd-checkpoint.md)。

## Core 介面

`AndrewDemo.NetConf2023.Core.Testing.TestAllowlist` 為 sealed concrete class。

```csharp
public TestAllowlist();
public TestAllowlist(IEnumerable<string> memberNames, IEnumerable<int> paymentIds);
public bool IsTestMember(string memberName, LiteDB.Collation? collation = null);
public bool IsTestPayment(int paymentId);
```

- 無參數建構式使用 code 中的預設清單：Member.Name `test`、PaymentId `990001`。
- 明確傳入兩份清單時取代預設清單；Member.Name 與 PaymentId 分別判定，命中為 true，未命中為 false。
- 空清單表示全部未命中；名稱與付款 ID 不交叉匹配。
- 建構式複製輸入集合，不公開可變清單；不同實例可擁有不同清單。
- 此類別只判定名稱或付款 ID 是否命中，不直接建立或修改 Member、Token、Checkout、Order。
- 清單建立後唯讀，後續可註冊為 singleton；已接入標準 host 與 MemberService。

- 名稱比對沿用會員查找的 LiteDB collation；MemberService 必須傳入作用中資料庫的 Collation。未指定時使用 LiteDB Collation.Default。不得額外 trim、轉數字或以 MemberId 補判；預設 IgnoreCase 資料庫中 `test`／`TEST` 等價，使用不同 collation 時依該資料庫規則。
- 2026-10-18 使用者明確重新開啟會員白名單規格，依 [Name 決策](../docs/decisions/2026-10-18-tip-member-name-allowlist.md) 取代原 ID 判定；Abstract 不需異動。

## 用途判定時機

| 流程 | 規則 |
|---|---|
| Member 註冊 | 以 Name 判定並初始化 Member.IsTest。 |
| Token 簽發 | 繼承 Member.IsTest，不重新查白名單。 |
| Checkout create | 使用 Member.IsTest 或 PaymentId 命中結果決定用途。 |
| Complete／Order／事件 | 承接已保存用途，不重新套用清單。 |

既有資料 IsTest 為 false 的政策不變；後續變更清單不得重新分類已存在物件。

## TDD 檢查

新增行為案例見 [testcases](testcases/tip-test-allowlist.md)。紅燈階段的正確結果為 build 成功、目標行為測試失敗；測試不跳過、不使用 Assert.Throws 將未實作視為成功。使用者已確認紅燈檢查點並授權補上實作；目前查詢依各自的集合回傳是否命中，測試以原有行為斷言驗證。

## Phase 1 基準

本規格已納入 [Phase 1 定案清單](tip-phase1.md)。後續 Phase 2 依此接入流程；規格與 Abstract 合約如需改動，須由使用者明確重新開啟規格階段。
