# AppleBTS 折扣需拆成主商品與贈品兩筆折扣行

## 狀態

- accepted
- 日期：2026-04-09

## 背景

目前 AppleBTS 在結帳與訂單頁的折扣呈現，會把：

- 主商品 BTS 價差
- 贈品補貼

合併成單一折扣行：

- `BTS 優惠`

但在業務上，這兩者是不同來源的優惠：

- 主商品優惠：主商品原價與 `BtsPrice` 的價差
- 贈品優惠：gift subsidy

若合併成一筆，會讓訂單與收據難以閱讀，也不利於 UI 精確呈現。

## 決策

- `BtsDiscountRule` 若同時存在主商品價差與贈品補貼，必須回傳兩筆 `DiscountRecord`
- 第一筆名稱固定為：
  - `BTS 主商品優惠`
- 第二筆名稱固定為：
  - `BTS 贈品優惠`
- `BTS 主商品優惠`
  - `Amount` 只包含主商品價差
  - `RelatedLineIds` 只包含主商品 line
- `BTS 贈品優惠`
  - `Amount` 只包含 gift subsidy
  - `RelatedLineIds` 只包含合法 gift line
- 若只有主商品價差成立，則只回傳 `BTS 主商品優惠`
- 若 gift subsidy 不成立，則不得產生 `BTS 贈品優惠`
- hint 仍可維持單一 `BTS 優惠` 名稱

## 影響

- `AndrewDemo.NetConf2023.AppleBTS.Extension`
  - `BtsDiscountRule` 改為輸出兩筆折扣
- `AndrewDemo.NetConf2023.AppleBTS.Extension.Tests`
  - 相關 scenario test 需改成驗證兩筆折扣
- storefront / API / `.Core`
  - 不需要擴充 contract
  - 只要沿用既有多筆 `DiscountLine` 顯示即可自然呈現

## 不影響

- 這不是 `.Core` 或 Phase 1 contract 的回頭修正
- 不增加新的 `.API` endpoint
- 不增加新的 storefront 專屬 API model

## 替代方案

### 1. 只在 storefront 把單一 `BTS 優惠` 拆成兩筆顯示

不採用。storefront 並不知道單筆金額內部如何分攤，無法正確重建主商品優惠與贈品優惠。

### 2. 在 API controller 層做二次拆分

不採用。API 只是傳遞 `DiscountRecord` / `OrderDiscountLine`，真正的商業語意來源仍是 `BtsDiscountRule`。

## 後續工作

- 調整 `BtsDiscountRule`
- 更新 AppleBTS extension tests
- 更新 AppleBTS / BTS campaign 規格與驗收案例
- 更新 storefront smoke 驗證腳本
