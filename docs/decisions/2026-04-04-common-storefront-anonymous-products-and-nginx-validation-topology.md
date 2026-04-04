# CommonStorefront 匿名商品 API 與 nginx 驗證拓樸

## 狀態

- accepted
- 日期：2026-04-04

## 背景

`CommonStorefront` Phase 1 已確認：

- 商品列表 `/products`
- 商品詳細頁 `/products/{id}`

必須支援匿名瀏覽。

但既有 `.API` host middleware 原本會對所有 `/api/*` 套用 access token 檢查，因此 `GET /api/products` 與 `GET /api/products/{id}` 雖然 controller 本身沒有標註 authorize，實際上仍會被擋成 `401 Unauthorized`。

另外，`CommonStorefront` 的 OAuth 第一版沿用 `/api/login`，若本機驗證環境沒有同源 path routing，`/auth/login -> /api/login/authorize -> /auth/callback` 這條流程會變得不自然，也不利於後續對齊 Azure Front Door 的最終站點拓樸。

## 決策

- `.API` middleware 必須允許匿名 `GET/HEAD /api/products*`
- `CommonStorefront` 的 local validation environment 採 nginx reverse proxy
- nginx 對外提供單一入口：
  - `/*` -> `CommonStorefront`
  - `/api/*` -> `.API`
- nginx 必須保留原始 `Host:port`，避免 OAuth callback 產生錯誤的 public origin
- `CommonStorefront` 的 server-side `CoreApiClient` 仍直接呼叫 backend internal URL
- browser 端登入與頁面流轉則走 nginx 提供的同源 public path

## 影響

- `CommonStorefront` 可以維持 spec 中的匿名商品瀏覽行為
- `/auth/login` 與 `/api/login` 的第一版整合可以在本機 compose 內直接驗證
- local validation topology 與最終 Front Door 對外 path 拓樸更接近
- 後續 `AppleBTS.Storefront` 與 `PetShop.Storefront` 也可沿用相同模式

## 替代方案

### 1. 讓 `/products` 也要求登入

不採用。這違反既有 `CommonStorefront` spec，也不符合 storefront 作為展示型網站的基本需求。

### 2. 不做 reverse proxy，直接用不同 port 測試 UI 與 API

暫不採用作為正式 local validation topology。雖然技術上可行，但無法自然覆蓋 `/auth/login -> /api/login/authorize -> /auth/callback` 的同源流程。

### 3. 使用非 nginx 的其他 reverse proxy

本輪不採用。使用者已明確偏好 nginx 作為驗證環境的 reverse proxy。

## 後續工作

- 補 `CommonStorefront` compose / nginx 設定
- 補 storefront smoke test 與後續 `agent-browser` 驗收
- 將相同驗證模式沿用到其他 vertical storefront
