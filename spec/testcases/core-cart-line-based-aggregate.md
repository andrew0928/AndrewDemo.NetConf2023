# `.Core` Cart 採 line-based aggregate 測試案例

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-04-01

## Cart Persistence

### TC-CART-001 相同 `ProductId` 的兩次加入應可保存為兩筆 line

- Given: 一個 cart
- And: 同一個 `ProductId` 被加入兩次
- When: `.Core` 保存 cart
- Then: cart 內應保留兩筆獨立 line
- And: 兩筆 line 有不同 `LineId`

### TC-CART-002 cart line 可依附另一筆 line

- Given: cart 內有一筆主 line
- When: 新增一筆依附 line
- Then: 該 line 可保存 `ParentLineId`
- And: `ParentLineId` 指向主 line 的 `LineId`

### TC-CART-003 raw cart line 不要求先補齊商品名稱與單價

- Given: cart line 已有 `LineId`、`ProductId`、`Quantity`
- When: 尚未建立 `CartContext`
- Then: raw cart line 可不帶 `ProductName` / `UnitPrice`

## Cart Context

### TC-CART-101 `CartContext` 必須帶 `EvaluatedAt`

- Given: 一個 cart
- When: 執行 `CartContextFactory.Create(...)`
- Then: 回傳的 `CartContext` 必須帶 `EvaluatedAt`

### TC-CART-102 `CartContextFactory` 必須保留 line identity

- Given: cart 內已有 `LineId`、`ParentLineId`、`AddedAt`
- When: 建立 `CartContext`
- Then: `CartContext.LineItems` 必須保留這些欄位
- And: 同時補齊 `ProductName` / `UnitPrice`

### TC-CART-103 discount engine 必須能收到未合併的獨立 lines

- Given: cart 內有兩筆相同 `ProductId` 但不同 `LineId` 的 lines
- When: 建立 `CartContext` 並執行 `DiscountEngine`
- Then: rule 可看到兩筆獨立 line

## Core Boundary

### TC-CART-201 line relation 是 `.Core` 通用能力，不綁定 Apple BTS

- Given: `.Core` cart contract
- When: 檢查 `ParentLineId`
- Then: 它表示 generic line relation
- And: 不應被定義成 Apple BTS 專用欄位
