# PetShop 本機測試環境與 Host 拓樸

## 狀態

- accepted
- 日期：2026-04-24

## 背景

PetShop reservation flow 需要同時使用兩個 host：

- 標準 `.API`：負責 OAuth、product list、cart、checkout、order event dispatch。
- `PetShop.API`：負責 `/petshop-api/*` 的美容服務目錄、availability、reservation hold、reservation 查詢與 checkout 前取消 hold。

reservation hold 成功後會建立 hidden standard `Product`，因此兩個 host 必須共用同一份 shop database；checkout completed 後也必須由標準 `.API` 載入 PetShop module，透過 `PetShopOrderEventDispatcher` 將 reservation 標記為 confirmed。

## 決策

- PetShop 本機 API 驗證環境使用 `compose/petshop.compose.yaml`：
  - `petshop-seed`
  - `petshop-api`
  - `petshop-reservationapi`
- 三者共用單一 PetShop 專屬資料庫 volume。
- 兩個長駐 host 連到同一個 LiteDB 檔案時，必須使用 `Connection=Shared`。
- `petshop-seed` 使用 `AndrewDemo.NetConf2023.PetShop.DatabaseInit`：
  - 全量重建資料庫
  - 只 seed PetShop 的一般商品、SKU 與 inventory
  - reservation 與 hidden product 由 runtime flow 動態建立
- `petshop-api` 使用既有 `AndrewDemo.NetConf2023.API`：
  - 透過 `appsettings.PetShop.json` 啟用 `petshop` 模組
  - `ProductServiceId` 指向 `petshop-product-service`
  - `OrderEventDispatcherId` 指向 `petshop-order-event-dispatcher`
  - cart / checkout API 路徑維持 `/api/*`
- `petshop-reservationapi` 使用 `AndrewDemo.NetConf2023.PetShop.API`：
  - 專責 `/petshop-api/*`
  - 第一版只提供 service catalog、availability、reservation hold/list/detail/cancel-hold
- API 本機驗證先用不同 port 區隔：
  - `petshop-api` 對外 port `5208`
  - `petshop-reservationapi` 對外 port `5218`
- PetShop frontend / reverse proxy 驗證環境使用 `compose/petshop-storefront.compose.yaml`：
  - `petshop-storefront` 第一版先沿用 `CommonStorefront`
  - `petshop-edge` 使用 nginx 對外 port `5238`
  - `/api/*` proxy 到標準 `.API`
  - `/petshop-api/*` proxy 到 `PetShop.API`
  - `/` proxy 到 storefront
- PetShop 專屬 reservation UI 尚未進入 M4-P3；storefront compose 先驗證 host topology 與 reverse proxy 整合，不代表 PetShop Storefront 已完成。

## 影響

- 本機可用 `petshop-local.http` 驗證：
  - OAuth/login
  - PetShop service catalog / availability
  - reservation hold 建立 hidden product
  - hidden product 加入 cart
  - reservation + 一般商品滿額折扣
  - checkout completed 後 reservation confirmed
- 標準 API 需要直接 reference `PetShop.Extension`，因為 host runtime 必須依 manifest 組裝 `PetShopProductService` 與 `PetShopOrderEventDispatcher`。
- PetShop API 與標準 API 共用 bearer token、member、product、cart、order 與 reservation sidecar data。

## 替代方案

### 1. 只啟動 `PetShop.API`，不啟動標準 `.API`

不採用。reservation 的商業需求是進入標準 cart / checkout，單獨啟動 PetShop API 無法驗證 hidden product bridge 與 order event confirmation。

### 2. 把 `/petshop-api/*` endpoint 直接放進標準 `.API`

不採用。PetShop vertical API 是 extension-specific endpoint，應維持獨立 host boundary，避免標準 API controller 持續膨脹。

### 3. 立刻建立 `AndrewDemo.NetConf2023.PetShop.Storefront`

暫不採用。M4-P3 尚未開始；本階段先以 `CommonStorefront` 驗證 product/cart/checkout baseline 與 reverse proxy topology，PetShop reservation UI 後續再以專屬 storefront 補上。

## 後續工作

- 進入 M4-P3 時建立 `AndrewDemo.NetConf2023.PetShop.Storefront`，並替換 `petshop-storefront.compose.yaml` 的 storefront build target。
- 若後續需要 browser smoke script，再補 PetShop edge port 的端對端驗證腳本。
