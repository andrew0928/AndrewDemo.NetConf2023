# Storefront Family Architecture

## 目標

本專案後續要收斂成多套獨立 storefront，但維持相同的技術骨架與開發流程：

- `AndrewDemo.NetConf2023.CommonStorefront`
- `AndrewDemo.NetConf2023.AppleBTS.Storefront`
- `AndrewDemo.NetConf2023.PetShop.Storefront`
  - 目前先視為暫名，待 PetShop 規格正式開啟後再 freeze

共同原則如下：

- storefront 是獨立網站，不是單一網站內用 feature flag 切三種模式
- UI 走 ASP.NET Core server-side rendering / BFF 模式
- browser 不直接持有 bearer token 呼叫 `/api`、`/bts-api`、`/petshop-api`
- storefront 在 server side 呼叫 backend API
- login authority 使用標準 `.API` 提供的 `/oauth/*`
- 對外網站入口由 Azure Front Door 或 nginx 整合 `/*`、`/oauth/*`、`/api/*`、`/bts-api/*`、`/petshop-api/*`
- 後端 container apps 彼此直接互聯

## 建議的專案切分

### 1. Storefront Shared

建議新增共用專案：

- `AndrewDemo.NetConf2023.Storefront.Shared`

責任：

- 共用 layout 與基礎 view model
- OAuth login redirect / callback session flow
- server-side session / cookie 管理
- typed API clients 封裝
- 共用錯誤頁、loading、member navbar、cart summary 等元件

這個 shared project 應只放：

- 與 vertical 無關的 UI shell
- `/api` 共通 contract 的 client 包裝
- 共用 auth/session 邏輯

不應放：

- AppleBTS gift selection 規則
- PetShop reservation 規則

### 2. Common Storefront

- `AndrewDemo.NetConf2023.CommonStorefront`

責任：

- 顯示 `.Core` 通用商品型錄
- member profile / orders
- cart / checkout
- 只整合 `/api`

這套 storefront 應視為 baseline reference implementation。

### 3. AppleBTS Storefront

- `AndrewDemo.NetConf2023.AppleBTS.Storefront`

責任：

- 在 common storefront 骨架上補 AppleBTS 專屬頁面與流程
- server-side 整合 `/api` 與 `/bts-api`
- 顯示 BTS 專屬型錄、教育資格狀態、gift options

### 4. PetShop Storefront

- `AndrewDemo.NetConf2023.PetShop.Storefront`

責任：

- 在 common storefront 骨架上補 PetShop 專屬頁面與流程
- server-side 整合 `/api` 與 `/petshop-api`
- 顯示 reservation / 預約專屬 UI

## 建議的網站拓樸

每個 storefront 都是獨立站點，但沿用同一種 routing 與 BFF 設計。

### Common

```text
Browser
  -> common.example.com/*
     -> CommonStorefront
        -> server-side 呼叫 .API
```

### AppleBTS

```text
Browser
  -> bts.example.com/*
     -> AppleBTS.Storefront
        -> server-side 呼叫 .API
        -> server-side 呼叫 AppleBTS.API
```

### PetShop

```text
Browser
  -> pet.example.com/*
     -> PetShop.Storefront
        -> server-side 呼叫 .API
        -> server-side 呼叫 PetShop.API
```

## Azure Front Door 對外整合

對外 path 應維持一致：

- `/*` -> 對應 storefront
- `/oauth/*` -> 標準 `.API`
- `/api/*` -> 標準 `.API`
- `/bts-api/*` -> AppleBTS API
- `/petshop-api/*` -> PetShop API
- `/bts-api/*` -> `AndrewDemo.NetConf2023.AppleBTS.API`
- `/petshop-api/*` -> `PetShop.API`

但 storefront server side 不需要再透過 Front Door 呼叫 backend。建議改用 ACA 內部 service URL。

原因：

- 避免內部流量再繞外部入口
- 降低延遲與額外 hop
- 避免 session / auth / rate limit 在內外呼叫上混雜

## OAuth / Login Flow

第一版不重刻 login UI，直接使用標準 `.API` 提供的 OAuth namespace：

- `/oauth/authorize`
- `/oauth/token`

storefront 只補：

- `/auth/login`
- `/auth/callback`
- server-side session persistence

流程如下：

1. browser 進 storefront 任一受保護頁面
2. storefront 判定尚未登入，redirect 到 storefront 自己的 `/auth/login`
3. `/auth/login` 302 到 `/oauth/authorize`
4. 使用者在既有 login UI 完成登入
5. `.API` redirect 回 storefront `/auth/callback?code=...`
6. storefront 在 server side 呼叫 `/oauth/token`
7. storefront 取得 bearer token 後，寫入 server-side session 或 secure auth cookie
8. 後續所有頁面由 storefront server side 代呼叫 `/api`、`/bts-api`、`/petshop-api`

## 前端技術選擇

建議：

- ASP.NET Core MVC 或 Razor Pages
- 第一版偏 server-rendered
- 只在必要區塊補少量 JavaScript enhancement

暫不建議：

- Node.js / Next.js 當正式 host
- 純 static SPA 直接從 browser 呼叫 `/api`、`/bts-api`

主要原因：

- 目前後端、部署、設定模型都已經是 `.NET`
- token 與 auth 流程更適合留在 server side
- vertical storefront 的差異在頁面與 BFF orchestration，不在前端 runtime 技術本身

## 建議的 server-side client 分層

每個 storefront 應有自己的 application services，但可共用 typed client。

### Shared clients

- `CoreApiClient`
  - products
  - member
  - carts
  - checkout

### AppleBTS clients

- `AppleBtsApiClient`
  - catalog
  - qualification

### PetShop clients

- `PetShopApiClient`
  - reservations
  - available slots
  - service catalog

## 建議的頁面模組

### Common Storefront

- `/`
- `/products`
- `/products/{id}`
- `/cart`
- `/checkout`
- `/member`
- `/member/orders`
- `/auth/login`
- `/auth/callback`

### AppleBTS Storefront

除了 common 之外，再加：

- `/bts`
- `/bts/qualification`
- `/bts/products/{id}`

### PetShop Storefront

除了 common 之外，再加：

- `/petshop`
- `/petshop/services`
- `/petshop/reservations`
- `/petshop/reservations/{id}`

## 建議的開發順序

1. 先做 `AndrewDemo.NetConf2023.Storefront.Shared`
2. 再做 `AndrewDemo.NetConf2023.CommonStorefront`
3. 以 shared 為基礎做 `AndrewDemo.NetConf2023.AppleBTS.Storefront`
4. 最後再做 `AndrewDemo.NetConf2023.PetShop.Storefront`

這樣的原因是：

- Common storefront 可先驗證 baseline BFF 與 auth/session flow
- AppleBTS 與 PetShop 只需要專注在 vertical-specific UI 與 orchestration

## 驗收策略

storefront 完成條件不應只看：

- build 成功
- unit test 成功

還必須包含：

- 依 `/spec/testcases/storefront-family-ui-and-bff.md` 逐條驗收
- 使用 `agent-browser` 操作實際啟動中的 storefront
- 對應 testcase 驗收完成後，才可視為該 storefront 已交付

若某情境因環境或外部依賴限制無法執行，應明確列為 blocked item。

## 不採用的做法

### 1. 一個 storefront host 內切三種模式

不採用。這會讓 vertical-specific dependency、layout、auth flow、routing 持續交纏。

### 2. 純前端 SPA 直接呼叫 `/api` 與 vertical API

不採用。會讓 token、CORS、雙 API base URL、callback flow 暴露到 browser。

### 3. 為每個 storefront 額外引入 Node.js host

暫不採用。對目前 repo 與 ACA 拓樸沒有必要。
