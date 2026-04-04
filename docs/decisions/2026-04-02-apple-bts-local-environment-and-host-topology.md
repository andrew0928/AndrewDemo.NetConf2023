# AppleBTS 本機測試環境與 Host 拓樸

## 狀態

- accepted
- 日期：2026-04-02

## 背景

Apple BTS 已確認不是獨立商店，而是同一個 shop 內的 campaign。雖然如此，為了本機驗證與後續 ACA 部署，需要把標準 API、BTS 專屬 API 與 seed/init 拆成獨立容器。

另外，本輪也已確認：

- 標準 cart / checkout 仍由既有 `.API` 提供
- `Product Catalog Service` 與 `Education Qualification Service` 由 BTS 專屬 API 提供
- 兩個 host 必須共用同一份 AppleBTS 專屬資料庫
- reverse proxy 不是必須，本機可先以不同 port 區隔
- campaign 過期情境的 time mock 先 deferred，不阻擋主要開發與建置

## 決策

- AppleBTS 本機測試環境使用獨立 compose：
  - `applebts-seed`
  - `applebts-api`
  - `applebts-btsapi`
- 三者共用同一個 AppleBTS 專屬資料庫 volume
- 兩個長駐 host 連到同一個 LiteDB 檔案時，必須使用 `Connection=Shared`
- `applebts-seed` 使用 `AndrewDemo.NetConf2023.AppleBTS.DatabaseInit`
  - 全量重建資料庫
  - 只 seed Apple 商品與 AppleBTS sidecar collections
- `applebts-api` 使用既有 `AndrewDemo.NetConf2023.API`
  - 透過 `appsettings.AppleBts.json` 啟用 `apple-bts` 模組
  - cart / checkout API 路徑維持 `/api/*`
- `applebts-btsapi` 使用 `AndrewDemo.NetConf2023.AppleBTS.API`
  - 專責 `/bts-api/*`
  - 第一版只提供 catalog 與 education qualification
- 本機先不加 reverse proxy
  - `applebts-api` 對外 port `5108`
  - `applebts-btsapi` 對外 port `5118`
- gift 與 main product 的關聯仍回歸標準 `/api/carts/{id}/items`
  - 由 `ParentLineId` 表達
  - 不把這個行為做成 BTS.API 專屬 cart endpoint
- API 驗證腳本使用 `shell + curl`
  - 依 decision table 驗證主要情境
  - `M-03` / `C-03` 因 time mock 尚未定案，暫列 skip

## 影響

- 本機只要啟動 AppleBTS compose，就能同時測：
  - OAuth/login
  - 標準 cart / checkout
  - BTS catalog / qualification
- ACA 部署時也可直接對應為：
  - app container
  - extension api container
  - seed/init container
- 標準 API 與 BTS API 共用 bearer token 與同一份 member/product/order/cart 資料

## 替代方案

### 1. 只保留單一 API host，所有 BTS endpoint 全塞回既有 `.API`

不採用。這會讓標準 API 與 extension-specific endpoint 混在一起，host boundary 不清楚。

### 2. 用 reverse proxy 強制把 `/api` 與 `/bts-api` 合到單一 port

暫不採用。對本機測試不是必要複雜度，後續若前端整合需要再補。

### 3. 讓 BTS gift relation 只在 BTS.API 專屬 endpoint 處理

不採用。`ParentLineId` 屬於 `.Core` cart line 語意，標準 API 應同步承接。

## 後續工作

- 補 AppleBTS compose 與 Dockerfiles
- 補 `.http` 與 `shell + curl` 本機驗證資產
- 後續若要驗證活動過期情境，再重開 time mock 設計
