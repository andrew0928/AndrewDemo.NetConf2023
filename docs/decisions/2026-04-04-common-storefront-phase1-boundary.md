# CommonStorefront Phase 1 邊界

## 狀態

- proposed
- 日期：2026-04-04

## 背景

storefront family 已確認：

- 採 ASP.NET Core server-side BFF
- login UI 第一版沿用 `/api/login`
- browser 不直接持 bearer token 呼叫 backend APIs

在開始實作 `CommonStorefront` 前，需要先把 baseline implementation 的 page routes、auth/session、typed client 與 page model 邊界固定下來。

## 決策

- `AndrewDemo.NetConf2023.CommonStorefront` 作為 storefront family 的 baseline implementation
- 第一版技術基準採 Razor Pages 或 MVC server-rendered page
- `CommonStorefront` 只整合標準 `.API`
- `CommonStorefront` 必須提供：
  - `/`
  - `/products`
  - `/products/{id}`
  - `/cart`
  - `/checkout`
  - `/member`
  - `/member/orders`
  - `/auth/login`
  - `/auth/callback`
  - `/auth/logout`
- 建議新增 `AndrewDemo.NetConf2023.Storefront.Shared`
  - 擁有 auth/session、`CoreApiClient`、共用 partial 與共用 view models
- `CommonStorefront` 應以 `agent-browser + testcases` 作為正式驗收方式

## 影響

- `CommonStorefront` 可作為後續 `AppleBTS.Storefront` 與 `PetShop.Storefront` 的 code structure 參考
- auth/session 與 `CoreApiClient` 不需要在每個 vertical storefront 重複發明
- CommonStorefront 若先完成，可用來驗證 storefront family 的最小骨架是否成立

## 替代方案

### 1. 直接先做 AppleBTS.Storefront

不採用。這會讓 baseline 與 vertical-specific 責任混在一起，難以判斷哪些是通用 UI 能力，哪些是 AppleBTS 專屬能力。

### 2. 第一版就拆很多 typed clients 與 page services

暫不採用。第一版應先維持最小複雜度，避免在 CommonStorefront 邊界未穩定前先做過度分層。

### 3. 直接在 browser 端打 `.API`

不採用。這會違反 storefront family 已確認的 server-side BFF 決策。

## 後續工作

- review `CommonStorefront` spec 與 testcases
- freeze `CommonStorefront` page / auth / BFF 邊界
- 之後再 scaffold `Storefront.Shared` 與 `CommonStorefront`
