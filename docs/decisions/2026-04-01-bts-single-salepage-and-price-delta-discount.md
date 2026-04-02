# Apple BTS 採單一 SalePage 與價差型折扣

## 狀態

- accepted
- 日期：2026-04-01

## 背景

先前 BTS 技術提案曾假設一般入口與 BTS 入口會拆成不同 `SalePage`，且 checkout 需有專屬 validation service 來阻擋活動過期的結帳。但目前 `.Core.CheckoutService` 並沒有 generic business validation hook，可直接插入 campaign-specific validation service。

若維持雙 `SalePage` 與 checkout blocking，實作成本會明顯擴大，且會先推動 `.Core` checkout contract 變更。

## 決策

- Apple BTS 目前採單一公開 `SalePage`
- `Product.Price` 維持一般售價
- `bts-price` 放在 sidecar campaign 資料
- `BtsMainOfferRecord.MaxGiftSubsidyAmount` 定義主商品對應 gift 的補貼上限
- 由 `BtsDiscountRule` 根據 `bts-price`、gift subsidy、member qualification、活動時間窗與 `ParentLineId` gift relation 來計算價差
- gift subsidy 是否成立，以 gift line 是否綁定 `ParentLineId` 且符合 gift group 規則判定
- gift subsidy 只作用在 gift line，最低補貼到 `0`，剩餘額度不得轉移到主商品
- 若結帳當下活動失效或資格不符：
  - 不阻擋 checkout
  - 不套用 BTS 折扣
  - 可回傳 `Hint` 提示優惠已失效

## 影響

- 不需要先為 AppleBTS 重構 `.Core` checkout validation hook
- 不需要額外保留 `BtsCartLineRecord` 或其他 cart provenance sidecar
- 收據與總價以「原價商品行 + BTS 優惠折扣行」表達最終成交結果
- 若未來真的需要「活動過期必須阻擋結帳」，需另外重開 checkout contract 討論

## 替代方案

### 1. 一般入口與 BTS 入口拆成不同 `SalePage`

不採用。現況下仍需要 checkout blocking / cart migration 等額外機制，導致整體變更面過大。

### 2. 活動過期時直接禁止結帳

本輪不採用。`.Core` 目前沒有適當的 generic validation hook；若硬做，會把 AppleBTS 特例壓進 checkout 主流程。

## 後續工作

1. 更新 BTS 正式 spec 與 testcase，使其對齊單一 `SalePage` 與 `ParentLineId` gift relation 模式。
2. AppleBTS implementation 先聚焦在 `BtsDiscountRule` 與 sidecar records。
3. checkout blocking 若未來仍有必要，再另外重開 Phase 1。
