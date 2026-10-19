# TiP Context 合約

Phase 3 補充：IdentityContext 用於認證、授權及指定操作限制；TransactionContext 由受控 storage locator 載入保存用途，用於該交易的分區與效果政策。新交易才依會員／PaymentId 規則判定；既有交易不可用目前 Token 或 System identity 重新分類。兩者用途不同不構成通用拒絕條件，買方驗證仍適用。collection／ID 對照見 [交易儲存規格](tip-transaction-storage.md)。本次沿用現有 Abstract 快照與事件簽章，不新增合併 context 或 mode 參數。

2026-10-19 修訂：交易 collection 隔離與指定身分限制已同步至本規格並獲使用者確認，見 [Phase 3 review](../docs/tip/phase3-spec-review.md)。業務原則已確認；新 storage 與改價拒絕尚未實作。既有 Phase 1／2 的交付數字及紀錄僅代表當時版本。

## 狀態與範圍

- 日期：2026-10-17。
- 狀態：Abstract 合約維持，Core 初始化、綁定與清除已實作。
- 依據：[Context 分離決策](../docs/decisions/2026-10-17-tip-standard-scope-and-context.md)。
- Phase 2 已接入 host、DI、middleware 與事件；Phase 3 需將載入入口改為用途分區的受控 locator，單純快照測試不足以證明 storage 路由。

## 唯讀介面

Namespace：`AndrewDemo.NetConf2023.Abstract.Contexts`。

```csharp
public interface IIdentityContext
{
    IdentitySnapshot Current { get; }
}

public interface ITransactionContext
{
    TransactionSnapshot? Current { get; }
}
```

介面獨立注入，不提供合併的 IShopExecutionContext；一般讀取端不能透過介面初始化或覆寫 context。

## 身分快照

`IdentitySnapshot` 為不可變資料，所有屬性只讀：

| 欄位 | 型別 | 語意 |
|---|---|---|
| Kind | IdentityKind | Anonymous、Member 或 System。 |
| MemberId | int? | 只有會員身分有值；匿名與系統為 null。 |
| IsTest | bool | 會員取自已保存的用途；匿名與系統快照為 false。 |

建立入口為 `IdentitySnapshot.Anonymous`、`IdentitySnapshot.System`、`IdentitySnapshot.ForMember(memberId, isTest)`。目前的初始化資料必須由可信執行邊界提供；此模型不負責驗證 Token。

## 交易快照

`TransactionSnapshot` 為不可變資料，所有屬性只讀：

| 欄位 | 型別 | 語意 |
|---|---|---|
| Kind | TransactionKind | Checkout 或 Order。 |
| Id | int | 對應種類的持久化物件 ID。 |
| IsTest | bool | 直接取自該物件已保存的用途。 |

建立時明確提供 kind、id、isTest。無交易使用 null；不能以 ID = 0 或 IsTest = false 代替「沒有交易」。既定需要交易的操作若資料遺失，須由載入邊界依規格處理錯誤，不能偽裝成正常無交易。

## Core 寫入入口

Namespace：`AndrewDemo.NetConf2023.Core.Contexts`。

- `IdentityContext : IIdentityContext`：`Initialize(IdentitySnapshot identity)` 提供可信身分；未初始化時 Current 為匿名快照。
- `TransactionContext : ITransactionContext`：`Bind(TransactionSnapshot transaction)` 綁定目前交易；`Clear()` 結束目前綁定，Current 恢復 null。
- Initialize／Bind 接收非 null 快照，替換各自的 Current；傳入 null 時拋出 ArgumentNullException，不覆寫原狀態。Clear 是解除交易綁定的入口。
- 清除交易不影響 identity；已讀取的快照不因 context 綁定改變而變動。
- 不同 context 實例不自動繼承彼此的狀態；跨 request／事件須明確恢復資料。
- 系統 identity 的 IsTest = false 不解除 test Order 的限制。Identity 與 transaction 分開提供政策判定資料。

DI 採 scoped；同一 HTTP request 可共用 request scope，事件建立自己的 scope。白名單為獨立唯讀資料，可共用 singleton。Core context 不依賴 HttpContext。

## 訂單事件介面

`IOrderEventDispatcher` 的兩個 Dispatch 簽章不變：處理端建立自己的 scope，依 OrderId 從持久化 Order 恢復用途；即使同步呼叫，也不可依賴原 HTTP request 存活。Phase 2 已接入 dispatcher；Phase 3 保持此合約，透過跨用途唯一 Order ID 定位正確分區。

## 驗證

見 [測試案例](testcases/tip-context-contracts.md)。紅燈階段已確認測試因 NotImplementedException 失敗；使用者授權後已補上快照保存與清除實作，使用相同的行為斷言驗證轉綠。

## Phase 1 基準

本規格已納入 [Phase 1 定案清單](tip-phase1.md)。此次使用者授權更新交易邊界規格；Phase 3 沿用 Abstract，若後續仍需改公開合約，須另外提出具體差異。
