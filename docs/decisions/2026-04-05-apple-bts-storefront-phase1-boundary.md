# 2026-04-05 AppleBTS Storefront Phase 1 邊界

## 狀態

- accepted

## 背景

`CommonStorefront` 已作為 storefront family 的 baseline implementation 落地，但 AppleBTS 還需要：

- BTS 型錄入口
- 教育資格頁
- gift 選擇與加入流程

同時，這些需求原則上不應新增 AppleBTS 專屬的 `.Core` 或標準 checkout contract。

## 決策

- `AndrewDemo.NetConf2023.AppleBTS.Storefront` 以 `CommonStorefront` 為骨架實作
- 一般官網流程維持與 `CommonStorefront` 相同
- 只追加 AppleBTS 專屬頁面：
  - `/bts`
  - `/bts/qualification`
  - `/bts/products/{id}`
- storefront 在 server side 同時整合：
  - `/api`
  - `/bts-api`
- gift 加入流程採 server-rendered 確認步驟
  - 同意後才加入主商品與 gift
  - gift line 以 `ParentLineId` 綁到該主商品 line
  - 取消時購物車維持原狀

## 影響

- 新增 `AndrewDemo.NetConf2023.AppleBTS.Storefront`
- 新增 AppleBTS storefront 專屬 compose 與 nginx 驗證拓樸
- 新增 AppleBTS storefront spec / testcase

這個決策本身：

- 不新增 AppleBTS 專屬 `.Core` contract
- 不直接重開 `.Abstract`

但在實作期間若暴露通用 storefront / cart 缺口，仍應另立 decision 並納入 AppleBTS backtracking count。

## 替代方案

### 1. 直接在 CommonStorefront 內用 feature flag 切出 BTS 頁面

不採用。會讓 storefront family 失去獨立網站邊界。

### 2. 以 client-side JavaScript confirm 處理 gift 加入

不採用。第一版要求維持 server-rendered、低複雜度與無障礙優先。

### 3. 為 gift 加入流程新增 `.Core` 專屬 contract

不採用。現有 `/api/carts/{id}/items` 加上 `ParentLineId` 已足以表達需求。

## 後續工作

- 完成 `AppleBTS.Storefront` 實作
- 完成本機 compose 驗證
- 依 AppleBTS storefront testcases 做 browser 驗收
