# AppleBTS 過程中的 Phase 1 / `.Core` 回頭修正追蹤

這份文件用來追蹤：在 AppleBTS 擴充過程中，哪些原本以為是 Phase 2 實作細節，最後被證明其實是需要回頭修正的 **Phase 1 contract / `.Core` 基礎決策**。

後續 AppleBTS 擴充正式收尾時，應以這份清單回報總數。

## 標記規則

- 若某決策會影響 `.Core` 的通用設計，decision 本文必須明確標示：
  - `重大決策`
  - `影響 .Core`
- 若某決策代表需要回頭修正 Phase 1、`/spec`、或 `.Abstract` contract，decision 本文必須明確標示：
  - `回頭修正 Phase 1`
  - `影響 .Abstract / spec`
- 凡是被標記為上述類型，且起因可追溯到 AppleBTS 擴充過程，就要納入這份追蹤清單
- AppleBTS 結案摘要時，以這份文件作為唯一統計來源

## 目前累計

- 累計數量：6

## 清單

| 編號 | 日期 | 類型 | 決策 | 說明 |
|---|---|---|---|---|
| 1 | 2026-04-01 | `.Core` + `.Abstract` | [Line-based Cart 屬於 `.Core` 主線重構](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-01-line-based-cart-is-core-refactor.md) | AppleBTS 需要主商品 / 贈品關聯時，才暴露 `ProdQtyMap` 無法保留 line identity，最後回頭重開 cart contract。 |
| 2 | 2026-04-01 | `.Core` + `.Abstract` | [SKU 與 Inventory 屬於 `.Core` 的標準能力](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-01-sku-and-inventory-are-core-standard-capabilities.md) | AppleBTS 討論商品與贈品時，才確認 SKU / inventory 不能當 extension 專屬能力，而必須升格回 `.Core`。 |
| 3 | 2026-04-01 | Phase 1 contract | [DiscountRecord 擴充為 discount/hint 單一型別](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-01-discount-record-kind-and-related-lines.md) | AppleBTS 需要表達活動失效與資格不足提示，才回頭修正 `.Abstract.Discounts`。 |
| 4 | 2026-04-02 | `.Core` + host cross-cutting | [TimeProvider 化的時間平移與 Time Mock 遷移方向](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-02-timeprovider-based-time-shift-and-mock.md) | AppleBTS decision table 需要驗證活動時間窗，才暴露目前系統沒有統一可控的時間來源，需回頭補基礎時間抽象。 |
| 5 | 2026-04-05 | `.Core` 語意修正 | [BuyerSatisfaction 改為 Nullable 語意](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-05-buyer-satisfaction-nullable-semantics.md) | 在 storefront 實作過程中進一步暴露：`0` 不能同時代表「未判讀」與「最低分」，因此回頭修正 `.Core` 的訂單語意。 |
| 6 | 2026-04-05 | `.Core` + Phase 1 cart baseline | [Cart Line 刪除與子項目 Cascade](/Users/andrew/code-work/andrewshop.apidemo/docs/decisions/2026-04-05-cart-line-removal-and-child-cascade.md) | 在 AppleBTS bundle 與 Common storefront 購物車 UI 實作時，才明確暴露 line-based cart 仍缺少刪除與 child cascade 的通用能力，因此回頭補上 `.Core` 與 baseline spec。 |

## 統計原則

- 只計入在 AppleBTS 擴充過程中被重新識別出的基礎決策缺口
- 不計入單純的 host wiring、seed data、compose、測試腳本等實作工作
- 若同一個主題只是後續 implementation refinement，不重複計數

## 後續使用方式

- 若後續又出現新的回頭修正項目，直接追加一列並更新「目前累計」
- AppleBTS 擴充完成回報時，直接引用這份文件中的總數與清單
