# Storefront Family 與 BFF 架構

## 狀態

- accepted
- 日期：2026-04-04

## 背景

目前 backend 已收斂為：

- 標準 `.API` 提供 `/api/*`
- AppleBTS 專屬 API 提供 `/bts-api/*`

後續還會新增：

- Common storefront
- AppleBTS storefront
- PetShop storefront

這三套網站最終都會獨立部署，但希望沿用相同的設計與開發流程，避免每個 vertical 各自長出不同的 UI host 架構。

另外，本輪也已確認：

- login UI 第一版沿用 `/api/login`
- 對外 path 由 Azure Front Door 整合
- backend containers 在 ACA 內部直接互聯

## 決策

- storefront 採 family 架構，而不是單一網站切多模式
- 每個 storefront 都使用 ASP.NET Core server-side BFF 模式
- browser 不直接呼叫 `/api`、`/bts-api`、`/petshop-api`
- storefront 在 server side 呼叫 backend APIs
- login UI 第一版沿用既有 `/api/login/authorize`
- storefront 自己只處理：
  - `/auth/login`
  - `/auth/callback`
  - session / secure cookie
- 建議新增 shared project：
  - `AndrewDemo.NetConf2023.Storefront.Shared`
- storefront 專案規劃如下：
  - `AndrewDemo.NetConf2023.CommonStorefront`
  - `AndrewDemo.NetConf2023.AppleBTS.Storefront`
  - `AndrewDemo.NetConf2023.PetShop.Storefront`
- `PetShop.Storefront` 目前可先作為 provisional name，等 PetShop spec 正式開啟後再 freeze

## 影響

- Common storefront 可成為 baseline implementation
- AppleBTS / PetShop 只需在 shared + baseline 上補 vertical-specific pages 與 orchestration
- token 與 auth 流程可留在 server side，避免暴露到 browser
- 對外部署時可維持一致的 Front Door path policy：
  - `/*`
  - `/api/*`
  - `/bts-api/*`
  - `/petshop-api/*`
- storefront server side 應直接呼叫 ACA 內部 backend service URL，不繞 Front Door

## 替代方案

### 1. 單一 storefront host 內用 feature flag 支援所有 vertical

不採用。vertical-specific page、workflow 與 dependency 會快速交纏，最終變成一個過重的 host。

### 2. 純 static SPA 直接從 browser 呼叫 `/api` 與 vertical API

不採用。這會讓 token、CORS、雙 backend base URL 與 callback flow 複雜度回到 browser 端。

### 3. 使用 Node.js 作為正式 storefront host

暫不採用。現有 backend、部署、設定模型都已以 `.NET` 為主，額外引入 Node.js host 只會增加 runtime 與維運複雜度。

### 4. 立即重刻 login UI

暫不採用。第一版應先打通既有 `/api/login` authority 與 storefront callback/session flow；品牌化 login UI 可在後續 refinement 再處理。

## 後續工作

- 補 storefront family 的正式 spec 與 shared contract
- 先實作 `AndrewDemo.NetConf2023.CommonStorefront`
- 之後依同一骨架擴充 `AndrewDemo.NetConf2023.AppleBTS.Storefront`
- PetShop 規格正式啟動時，再 freeze `PetShop.Storefront` canonical name 與 UI boundary
