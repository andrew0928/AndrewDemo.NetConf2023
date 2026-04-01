# `.Core` Cart 採 line-based aggregate 規格

## 狀態

- phase: 1
- status: draft-for-review
- 日期：2026-04-01

## 範圍

本規格只涵蓋以下主題：

1. `.Core` 的 cart persistence model 應改為 line-based aggregate
2. `CartContext` 與 `LineItem` contract 應如何支撐 line identity
3. `CartContextFactory` 應保留哪些 line-level 資訊

本規格暫不涵蓋：

- Apple BTS qualification 與活動規則
- API route / request / response shape
- checkout 的庫存 transaction 細節
- line role / promotion type taxonomy

## 目標

- 讓相同 `ProductId` 的多次加入不再被被動合併
- 讓 `.Core` 能穩定保存 line-to-line relation
- 讓 discount estimate 與 checkout 共用同一種 line-based context
- 將這個能力明確建模為 `.Core` 主線，不是 Apple BTS 專案專用修正

## Canonical 術語

- `Cart`: 購物車 aggregate root
- `CartLine`: cart 內的一筆獨立 line
- `LineId`: line 的穩定識別碼
- `ParentLineId`: 某筆 line 所依附的上層 line，可為 null
- `AddedAt`: 該 line 被加入 cart 的時間
- `CartContext`: discount / checkout 使用的試算輸入快照
- `EvaluatedAt`: 建立 `CartContext` 的時間

## 規格

### 1. `Cart` 不應再以 `ProdQtyMap<ProductId, Qty>` 作為 canonical persistence model

原因：

- 相同 `ProductId` 會被自動合併
- line identity 會消失
- line-to-line relation 無法保存
- 未來 inventory / promotion / bundle / offer pairing 都會失真

canonical 結論：

- `.Core` cart persistence 應改為 line-based aggregate

### 2. 每筆 cart line 都必須有自己的 identity

每筆 `CartLine` 至少應保存：

- `LineId`
- `ProductId`
- `Quantity`
- `AddedAt`
- `ParentLineId`

規則：

- `LineId` 必須在 cart 內穩定且唯一
- `ParentLineId` 可為 `null`
- `ParentLineId != null` 代表該 line 是依附其他 line 而存在

### 3. 相同 `ProductId` 的不同加入行為，不得被自動合併

規則：

- 若使用者兩次加入同一個 `ProductId`
- 除非呼叫端明確指定 merge policy
- 否則 `.Core` 應保存為兩筆獨立 line

### 4. `CartContext` 必須保留 line identity 與 evaluation time

`CartContext` 至少應包含：

- `ShopId`
- `ConsumerId`
- `ConsumerName`
- `EvaluatedAt`
- `LineItems`

### 5. `LineItem` contract 應補齊 line-level 資訊

`LineItem` 至少應包含：

- `LineId`
- `ParentLineId`
- `AddedAt`
- `ProductId`
- `Quantity`
- `ProductName`
- `UnitPrice`

其中：

- raw cart line 可暫時不帶 `ProductName` / `UnitPrice`
- `CartContextFactory` 應補齊 `ProductName` / `UnitPrice`
- `CartContextFactory` 不得丟失 `LineId` / `ParentLineId` / `AddedAt`

### 6. line relation 是 `.Core` 的通用能力，不預設綁定 Apple BTS

canonical 結論：

- `ParentLineId` 只是 generic line relation
- 它不是 Apple BTS 專用欄位
- 未來其他 extension 也可用來表示 bundle、加購、依附項目等關聯

### 7. 本階段不強制定義 line role taxonomy

本階段先不把 `LineRole` / `PromotionType` 寫進 `.Abstract`。

原因：

- 目前真正穩定且必要的是 line identity 與 parent relation
- role taxonomy 仍可能因不同 extension 類型而演化

## 對 `.Abstract` contract 的直接影響

### CartContext

- 新增 `EvaluatedAt`

### LineItem

- 新增 `LineId`
- 新增 `ParentLineId`
- 新增 `AddedAt`

## 非目標

- 本規格不要求 `.Abstract` 直接加入 `LineRole`
- 本規格不定義 API 是否允許 client 直接指定 `LineId`
- 本規格不定義 cart merge policy 的完整策略表
