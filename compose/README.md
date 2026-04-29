# Docker Compose 組態

本目錄的 compose 檔案以「示範站點」與「使用意圖」命名：

```text
compose/{site}.{mode}.compose.yaml
```

`site` 固定為：

- `common`: 標準網站，包含 core API 與 CommonStorefront
- `applebts`: Apple BTS 教育方案示範站，包含 core API、BTS API 與 AppleBTS Storefront
- `petshop`: PetShop 寵物美容預約示範站，包含 core API、reservation API 與 PetShop Storefront

`mode` 固定為：

- `api-dev`: API 開發、驗證與除錯；最小相依性，不啟動 storefront 或 reverse proxy
- `site-dev`: 本地完整網站驗證；使用 local build，保留最大可觀測性，backend ports 也可直接連
- `site-prod`: 本地端驗證 build.sh 發佈到 registry 的 production image；不 on-the-fly build，只公開 nginx edge

## 檔案清單

| 檔案 | 用途 |
|---|---|
| `common.api-dev.compose.yaml` | common core API 開發與 debug |
| `common.site-dev.compose.yaml` | common 完整網站本地整合驗證 |
| `common.site-prod.compose.yaml` | common registry image / production baseline 驗證 |
| `applebts.api-dev.compose.yaml` | Apple BTS core API + BTS API 開發與 debug |
| `applebts.site-dev.compose.yaml` | Apple BTS 完整網站本地整合驗證 |
| `applebts.site-prod.compose.yaml` | Apple BTS registry image / production baseline 驗證 |
| `petshop.api-dev.compose.yaml` | PetShop core API + reservation API 開發與 debug |
| `petshop.site-dev.compose.yaml` | PetShop 完整網站本地整合驗證 |
| `petshop.site-prod.compose.yaml` | PetShop registry image / production baseline 驗證 |

## api-dev

`api-dev` 用於 API-focused development，目標是最小相依性與最大可觀測性。

- 不啟動 seed
- 不啟動 storefront
- 不啟動 nginx
- 直接 publish API ports
- 使用 local build
- 資料庫使用 container writable layer 或 shared local volume，不保證資料持久

```bash
docker compose -f compose/common.api-dev.compose.yaml up --build
docker compose -f compose/applebts.api-dev.compose.yaml up --build
docker compose -f compose/petshop.api-dev.compose.yaml up --build
```

端點：

| site | core API | vertical API |
|---|---|---|
| common | `http://localhost:5108` | N/A |
| applebts | `http://localhost:5108` | `http://localhost:5118` |
| petshop | `http://localhost:5208` | `http://localhost:5218` |

## site-dev

`site-dev` 用於完整 website 本地驗證，目標是接近實際網站拓樸但保留偵錯便利性。

- 啟動 seed
- 啟動 core API / vertical API
- 啟動 storefront
- 啟動 nginx edge
- 使用 local build
- backend ports 也 publish 到 host，方便直接打 API 與 Swagger
- 這個 mode 不作為 security baseline

```bash
docker compose -f compose/common.site-dev.compose.yaml up --build
docker compose -f compose/applebts.site-dev.compose.yaml up --build
docker compose -f compose/petshop.site-dev.compose.yaml up --build
```

若要重置資料庫：

```bash
docker compose -f compose/common.site-dev.compose.yaml down -v
docker compose -f compose/applebts.site-dev.compose.yaml down -v
docker compose -f compose/petshop.site-dev.compose.yaml down -v
```

端點：

| site | edge | storefront direct | core API direct | vertical API direct |
|---|---|---|---|---|
| common | `http://localhost:5128` | `http://localhost:5129` | `http://localhost:5108` | N/A |
| applebts | `http://localhost:5138` | `http://localhost:5139` | `http://localhost:5108` | `http://localhost:5118` |
| petshop | `http://localhost:5238` | `http://localhost:5239` | `http://localhost:5208` | `http://localhost:5218` |

## site-prod

`site-prod` 用於本地端驗證 build / push 後的 production image。

- 不使用 `build:`
- 只使用 `build.sh` 發佈到 registry 的 image
- 只 publish nginx edge port
- backend API 與 storefront 只在 compose internal network 可達
- nginx 只公開 storefront routes 與 `/oauth/*`
- `/api/*`、`/bts-api/*`、`/petshop-api/*` 與 swagger 不預設公開

production image repository 採 site-prefixed 命名，common baseline 也必須補上 `common` 前綴：

| role | image repository |
|---|---|
| common core API | `andrewdemo-shop-common-api` |
| common seed | `andrewdemo-shop-common-seed` |
| common storefront | `andrewdemo-shop-common-storefront` |
| AppleBTS seed | `andrewdemo-shop-applebts-seed` |
| AppleBTS vertical API | `andrewdemo-shop-applebts-btsapi` |
| AppleBTS storefront | `andrewdemo-shop-applebts-storefront` |
| PetShop seed | `andrewdemo-shop-petshop-seed` |
| PetShop vertical API | `andrewdemo-shop-petshop-reservationapi` |
| PetShop storefront | `andrewdemo-shop-petshop-storefront` |

先發佈 images：

```bash
./build.sh --push
```

或只發佈單一 image：

```bash
./build.sh --push --image common-api
./build.sh --push --image common-storefront
```

啟動 production baseline：

```bash
docker compose -f compose/common.site-prod.compose.yaml up
docker compose -f compose/applebts.site-prod.compose.yaml up
docker compose -f compose/petshop.site-prod.compose.yaml up
```

預設使用：

- `REGISTRY=andrew0928.azurecr.io`
- `IMAGE_TAG=develop`

可覆蓋：

```bash
REGISTRY=andrew0928.azurecr.io IMAGE_TAG=20260429 docker compose -f compose/common.site-prod.compose.yaml up
```

預設 edge ports：

| site | env override | default |
|---|---|---:|
| common | `COMMON_SITE_PORT` | `8080` |
| applebts | `APPLEBTS_SITE_PORT` | `8081` |
| petshop | `PETSHOP_SITE_PORT` | `8082` |

## Time Shift

AppleBTS 與 PetShop compose 支援用環境變數覆蓋 host 的 `TimeProvider` 設定：

- `Time__Mode`
- `Time__ExpectedStartupLocal`
- `Time__TimeZoneId`

AppleBTS 活動期間範例：

```bash
env \
  Time__Mode=Shifted \
  Time__ExpectedStartupLocal='2026-04-04T18:35:00' \
  Time__TimeZoneId=Asia/Taipei \
  docker compose -f compose/applebts.site-dev.compose.yaml up --build
```

PetShop 預約情境範例：

```bash
env \
  Time__Mode=Shifted \
  Time__ExpectedStartupLocal='2026-05-01T09:00:00' \
  Time__TimeZoneId=Asia/Taipei \
  docker compose -f compose/petshop.site-dev.compose.yaml up --build
```

## `.http` 測試檔

- [applebts-local.http](/Users/andrew/code-work/andrewshop.apidemo/compose/applebts-local.http)
- [petshop-local.http](/Users/andrew/code-work/andrewshop.apidemo/compose/petshop-local.http)

## 常用指令

查看 logs：

```bash
docker compose -f compose/applebts.site-dev.compose.yaml logs applebts-api
docker compose -f compose/petshop.site-dev.compose.yaml logs petshop-reservationapi
```

檢查資料 volume：

```bash
docker compose -f compose/applebts.site-dev.compose.yaml run --rm applebts-api ls -lh /data/
docker compose -f compose/petshop.site-dev.compose.yaml run --rm petshop-api ls -lh /data/
```

進入容器：

```bash
docker compose -f compose/applebts.site-dev.compose.yaml exec applebts-api sh
docker compose -f compose/petshop.site-dev.compose.yaml exec petshop-reservationapi sh
```

## Appendix: Container 架構圖

圖中的每個 container 盡量以獨立方框表示。port mapping 會把 host port 放在 host 端，container 原生 port 放在 container 端。多個 path、port、volume mount 會分行標記。

### api-dev

```text
common.api-dev

+------------------+          publish          +------------------------------+
| host             | ------------------------> | common-api                   |
| port: 5108       |                           | container port: 8080         |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | data: container layer        |
                                               +------------------------------+
```

```text
applebts.api-dev

+------------------+          publish          +------------------------------+
| host             | ------------------------> | applebts-api                 |
| port: 5108       |                           | container port: 8080         |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
                                                          |
                                                          | mount: /data
                                                          v
                                               +------------------------------+
                                               | applebts-api-dev-data        |
                                               | file: shop-database.db       |
                                               +------------------------------+
                                                          ^
                                                          | mount: /data
+------------------+          publish          +------------------------------+
| host             | ------------------------> | applebts-btsapi              |
| port: 5118       |                           | container port: 8080         |
+------------------+                           | path: /bts-api/*             |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
```

```text
petshop.api-dev

+------------------+          publish          +------------------------------+
| host             | ------------------------> | petshop-api                  |
| port: 5208       |                           | container port: 8080         |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
                                                          |
                                                          | mount: /data
                                                          v
                                               +------------------------------+
                                               | petshop-api-dev-data         |
                                               | file: shop-database.db       |
                                               +------------------------------+
                                                          ^
                                                          | mount: /data
+------------------+          publish          +------------------------------+
| host             | ------------------------> | petshop-reservationapi       |
| port: 5218       |                           | container port: 8080         |
+------------------+                           | path: /petshop-api/*         |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
```

### site-dev

```text
common.site-dev

+------------------+          publish          +------------------------------+
| host             | ------------------------> | common-edge                  |
| port: 5128       |                           | container port: 80           |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | path: /*                     |
                                               +------------------------------+
                                                 | path: /oauth/*
                                                 | path: /api/*
                                                 | path: /swagger/*
                                                 v
+------------------+          publish          +------------------------------+
| host             | ------------------------> | common-api                   |
| port: 5108       |                           | container port: 8080         |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
                                                 ^
                                                 | mount: /data
+------------------+          seed data        +------------------------------+
| common-seed      | ------------------------> | common-site-dev-data         |
| volume: /data    |                           | file: shop-database.db       |
+------------------+                           +------------------------------+

                                                 | path: /*
                                                 v
+------------------+          publish          +------------------------------+
| host             | ------------------------> | common-storefront            |
| port: 5129       |                           | container port: 8080         |
+------------------+                           | server: common-api           |
                                               | server port: 8080            |
                                               +------------------------------+
```

```text
applebts.site-dev

+------------------+          publish          +------------------------------+
| host             | ------------------------> | applebts-edge                |
| port: 5138       |                           | container port: 80           |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | path: /bts-api/*             |
                                               | path: /bts-swagger/*         |
                                               | path: /*                     |
                                               +------------------------------+
                                                 | path: /oauth/*
                                                 | path: /api/*
                                                 | path: /swagger/*
                                                 v
+------------------+          publish          +------------------------------+
| host             | ------------------------> | applebts-api                 |
| port: 5108       |                           | container port: 8080         |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
                                                 ^
                                                 | mount: /data
+------------------+          seed data        +------------------------------+
| applebts-seed    | ------------------------> | applebts-site-dev-data       |
| volume: /data    |                           | file: shop-database.db       |
+------------------+                           +------------------------------+
                                                 ^
                                                 | mount: /data
                                                 |
+------------------+          publish          +------------------------------+
| host             | ------------------------> | applebts-btsapi              |
| port: 5118       |                           | container port: 8080         |
+------------------+                           | path: /bts-api/*             |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
                                                 ^
                                                 | path: /bts-api/*
                                                 | path: /bts-swagger/*
                                                 |
+------------------+          publish          +------------------------------+
| host             | ------------------------> | applebts-storefront          |
| port: 5139       |                           | container port: 8080         |
+------------------+                           | server: applebts-api         |
                                               | server port: 8080            |
                                               | server: applebts-btsapi      |
                                               | server port: 8080            |
                                               +------------------------------+
                                                 ^
                                                 | path: /*
```

```text
petshop.site-dev

+------------------+          publish          +------------------------------+
| host             | ------------------------> | petshop-edge                 |
| port: 5238       |                           | container port: 80           |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | path: /petshop-api/*         |
                                               | path: /petshop-swagger/*     |
                                               | path: /*                     |
                                               +------------------------------+
                                                 | path: /oauth/*
                                                 | path: /api/*
                                                 | path: /swagger/*
                                                 v
+------------------+          publish          +------------------------------+
| host             | ------------------------> | petshop-api                  |
| port: 5208       |                           | container port: 8080         |
+------------------+                           | path: /oauth/*               |
                                               | path: /api/*                 |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
                                                 ^
                                                 | mount: /data
+------------------+          seed data        +------------------------------+
| petshop-seed     | ------------------------> | petshop-site-dev-data        |
| volume: /data    |                           | file: shop-database.db       |
+------------------+                           +------------------------------+
                                                 ^
                                                 | mount: /data
                                                 |
+------------------+          publish          +------------------------------+
| host             | ------------------------> | petshop-reservationapi       |
| port: 5218       |                           | container port: 8080         |
+------------------+                           | path: /petshop-api/*         |
                                               | path: /swagger/*             |
                                               | volume: /data                |
                                               +------------------------------+
                                                 ^
                                                 | path: /petshop-api/*
                                                 | path: /petshop-swagger/*
                                                 |
+------------------+          publish          +------------------------------+
| host             | ------------------------> | petshop-storefront           |
| port: 5239       |                           | container port: 8080         |
+------------------+                           | server: petshop-api          |
                                               | server port: 8080            |
                                               | server: reservationapi     |
                                               | server port: 8080            |
                                               +------------------------------+
                                                 ^
                                                 | path: /*
```

### site-prod

`site-prod` 的 backend API 與 storefront 不 publish 到 host，只能透過 compose internal network 由 nginx 或 storefront server-side 呼叫。

```text
common.site-prod

+------------------------------+      publish      +------------------------------+
| host                         | ----------------> | common-edge                  |
| port: 8080 default          |                   | container port: 80           |
| env: COMMON_SITE_PORT       |                   | path: /oauth/*               |
+------------------------------+                   | path: /*                     |
                                                   +------------------------------+
                                                     | path: /oauth/*
                                                     v
+------------------------------+      internal     +------------------------------+
| common-seed                  | ----------------> | common-api                   |
| image: common-seed           |                   | image: common-api            |
| volume: /data                |                   | container port: 5108         |
+------------------------------+                   | path: /oauth/*               |
          |                                        | path: /api/*                 |
          | seed data                              | path: /swagger/*             |
          v                                        | volume: /data                |
+------------------------------+                   +------------------------------+
| common-site-prod-data        |                              ^
| file: shop-database.db       |                              | server: common-api
|                              |                              | server port: 5108
+------------------------------+                              |
                                                     +------------------------------+
                                                     | common-storefront            |
                                                     | image: common-storefront    |
                                                     | container port: 8080         |
                                                     +------------------------------+
                                                     ^
                                                     | path: /*
```

```text
applebts.site-prod

+------------------------------+      publish      +------------------------------+
| host                         | ----------------> | applebts-edge                |
| port: 8081 default          |                   | container port: 80           |
| env: APPLEBTS_SITE_PORT     |                   | path: /oauth/*               |
+------------------------------+                   | path: /*                     |
                                                   +------------------------------+
                                                     | path: /oauth/*
                                                     v
+------------------------------+      internal     +------------------------------+
| applebts-seed                | ----------------> | applebts-api                 |
| image: applebts-seed        |                    | image: common-api            |
| volume: /data                |                   | container port: 5108         |
+------------------------------+                   | path: /oauth/*               |
          |                                        | path: /api/*                 |
          | seed data                              | path: /swagger/*             |
          v                                        | volume: /data                |
+------------------------------+                   +------------------------------+
| applebts-site-prod-data      |                              ^
| file: shop-database.db       |                              | server: applebts-api
|                              |                              | server port: 5108
+------------------------------+                              |
          ^                                           +------------------------------+
          | mount: /data                              | applebts-storefront          |
+------------------------------+                      | image: applebts-storefront |
| applebts-btsapi              |                      | container port: 8080         |
| image: applebts-btsapi       |                      | server: applebts-btsapi    |
|                              |                      | server port: 5109          |
| container port: 5109         |                      +------------------------------+
| path: /bts-api/*             |                              ^
| path: /swagger/*             |                              | path: /*
| volume: /data                |                              |
+------------------------------+                              |
```

```text
petshop.site-prod

+------------------------------+      publish      +------------------------------+
| host                         | ----------------> | petshop-edge                 |
| port: 8082 default          |                   | container port: 80           |
| env: PETSHOP_SITE_PORT      |                   | path: /oauth/*               |
+------------------------------+                   | path: /*                     |
                                                   +------------------------------+
                                                     | path: /oauth/*
                                                     v
+------------------------------+      internal     +------------------------------+
| petshop-seed                 | ----------------> | petshop-api                  |
| image: petshop-seed         |                    | image: common-api            |
| volume: /data                |                   | container port: 5108         |
+------------------------------+                   | path: /oauth/*               |
          |                                        | path: /api/*                 |
          | seed data                              | path: /swagger/*             |
          v                                        | volume: /data                |
+------------------------------+                   +------------------------------+
| petshop-site-prod-data       |                              ^
| file: shop-database.db       |                              | server: petshop-api
|                              |                              | server port: 5108
+------------------------------+                              |
          ^                                           +------------------------------+
          | mount: /data                              | petshop-storefront           |
+------------------------------+                      | image: petshop-storefront |
| petshop-reservationapi       |                      | container port: 8080         |
| image: reservationapi       |                      | server: reservationapi     |
|                              |                      | server port: 5109          |
| container port: 5109         |                      +------------------------------+
| path: /petshop-api/*         |                              ^
| path: /swagger/*             |                              | path: /*
| volume: /data                |                              |
+------------------------------+                              |
```
