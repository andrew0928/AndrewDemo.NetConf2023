# 2026-04-05 Cart Line 刪除與子項目 Cascade

## 狀態

- accepted

## 標記

- 重大決策
- 影響 .Core
- 回頭修正 Phase 1
- 影響 .Abstract / spec

## 背景

在 `CommonStorefront` 與 `AppleBTS.Storefront` 實作購物車 UI 時，使用者需要能從 `/cart` 直接移除指定購物車項目。

AppleBTS 又進一步暴露一個通用需求：gift line 是透過 `ParentLineId` 綁定主商品 line。若刪除主商品後 gift 仍留在購物車，會形成不合法的孤兒子項目。

因此，這不是單純的 storefront UI 行為，而是 `.Core` 的 line-based cart aggregate 尚未補齊的基礎操作。

## 決策

- `.Core.Cart` 必須提供以 `LineId` 為單位的刪除能力
- 刪除主商品 line 時，所有 `ParentLineId` 指向該 line 的子 line 必須一併刪除
- 這個 cascade 規則屬於通用 cart aggregate 規格，不是 AppleBTS 專屬邏輯
- 標準 `.API` 必須提供對應的 cart line delete endpoint
- `CommonStorefront` 與 `AppleBTS.Storefront` 都應使用這個通用 endpoint，而不是各自實作私有刪除語意

## 影響

- `.Core`
  - `Cart.RemoveLine(lineId)` 成為 line-based cart 的正式基礎能力
- `.API`
  - 標準 cart controller 提供刪除指定 line 的 HTTP endpoint
- `/spec`
  - `CommonStorefront` baseline 補上 cart line delete 與 cascade 驗收條件
- AppleBTS 回頭修正追蹤
  - 此案列入 AppleBTS 過程中的通用設計回補項目

## 替代方案

### 1. 只在 storefront 層做 UI 隱藏，不提供刪除

- 不可接受
- 使用者無法修正購物車內容

### 2. 只刪除指定 line，不 cascade 子 line

- 不可接受
- 會留下失去主商品關聯的 gift line

### 3. 在 AppleBTS.Storefront 私下實作 gift cleanup

- 不可接受
- 這會把通用 cart 一致性責任錯放到 vertical-specific host

## 後續工作

- `CommonStorefront` 與 `AppleBTS.Storefront` 以相同 cart delete grammar 驗收
- AppleBTS 結案時，將本案納入 `.Core / Phase 1` 回頭修正總結
