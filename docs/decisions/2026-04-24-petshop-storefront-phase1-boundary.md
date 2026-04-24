# PetShop Storefront Phase 1 邊界

## 狀態

- accepted
- 日期：2026-04-24

## 背景

PetShop API 與 local compose topology 已完成後，下一步需要讓使用者能透過 storefront 完成美容預約、把預約結果加入購物車，並在 checkout 後看到 reservation confirmed。

這個階段牽涉到頁面 flow、BFF client、標準 cart / checkout 頁面重用、member reservation 狀態顯示與 browser smoke。若全部集中在單一階段，review package 會過大，也容易把 spec、UI 與 topology 問題混在一起。

## 決策

- M4-P3 拆成三個子階段：
  - M4-P3A：PetShop Storefront Spec / Skeleton
  - M4-P3B：Reservation Flow Pages
  - M4-P3C：Member / Order Integration 與 Browser Smoke
- `AndrewDemo.NetConf2023.PetShop.Storefront` 必須沿用 storefront family 的 server-side BFF 模式。
- browser 不直接持 bearer token 呼叫 `/api` 或 `/petshop-api`。
- PetShop 專屬 backend 呼叫集中在 `PetShopApiClient`。
- 標準 product、cart、checkout、member order 能力繼續使用 `Storefront.Shared` 的 `CoreApiClient` 與 shared pages grammar。
- `AndrewDemo.NetConf2023.PetShop.Storefront` 必須擁有自己的 vertical route surface，不以 `CommonStorefront` runtime 代替；CommonStorefront 只作為頁面模式與 UI grammar 來源。
- `compose/petshop-storefront.compose.yaml` 的 storefront target 必須啟動 `AndrewDemo.NetConf2023.PetShop.Storefront`，並由 nginx edge 統一代理 `/api/*`、`/petshop-api/*` 與 storefront routes。
- `Storefront.Shared` 統一註冊 `TimeProvider`，讓 Common、AppleBTS、PetShop storefront 都遵守可測試時間來源規則，不在 page model 直接使用 current-time API。
- 第一版 PetShop route 固定為：
  - `/petshop`
  - `/petshop/reservations/new`
  - `/petshop/reservations`
  - `/petshop/reservations/{id}`
- create hold 成功後，頁面顯示 `holding` / 「預約確認中」，並提供將 reservation hidden product 加入標準 cart 的 server-side action。
- checkout 本身不建立 PetShop 專屬 checkout endpoint；仍導向標準 `/cart` 與 `/checkout`。
- checkout completed 後，PetShop storefront 透過 reservation list/detail 顯示 `confirmed`，實際狀態轉移仍由標準 `.API` 的 `IOrderEventDispatcher` 驅動。
- Pet profile、staff/admin 排班、confirmed 後取消/改期、durable notification、SPA client state management 不納入 M4-P3 第一版。

## 影響

- P3A 需要新增 `spec/petshop-storefront-baseline.md` 與 `spec/testcases/petshop-storefront-baseline.md`。
- P3A 可以建立最小 `PetShop.Storefront` skeleton 與 typed client contract，但不在此階段完成所有 reservation pages。
- P3B 才開始實作主要 reservation flow pages。
- P3C 才切換 compose storefront target 並做 browser smoke。
- P3B/P3C 完成後，PetShop Storefront 第一版 consumer-facing flow 已可由 `http://localhost:5238` 驗證。

## 替代方案

### 1. 直接複製 AppleBTS Storefront 並一次完成全部頁面

不採用。AppleBTS flow 是 campaign / qualification / gift bundle；PetShop flow 是 reservation lifecycle / hidden product bridge。直接複製會加快初期速度，但容易保留錯誤的 route、用語與測試邊界。

### 2. 把 PetShop UI 放進 CommonStorefront

不採用。CommonStorefront 是標準系統 baseline，不應吸收 PetShop vertical-specific reservation flow。

### 3. 讓 browser 直接呼叫 PetShop API

不採用。這會破壞 storefront family 已確認的 BFF / token 邊界，並讓 browser 直接持有 bearer token 進行 domain API 呼叫。

## 後續工作

- M4-P3A / P3B / P3C 已完成。
- 後續若要擴充 confirmed reservation 取消/改期、staff/admin UI、pet profile 或 durable notification，應另開新階段，不混入 M4-P3 第一版。
