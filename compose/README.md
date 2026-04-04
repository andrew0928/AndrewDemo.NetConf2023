# Docker Compose 部署環境

模擬 Azure Container App 的運作方式，使用 init container + emptyDir 模式。

本目錄目前有兩套主要用途：

- `compose.yaml`
  - 舊的標準 API 本機環境
- `applebts.compose.yaml`
  - AppleBTS 專屬本機驗證環境
  - 同時啟動：
    - `applebts-seed`
    - `applebts-api`
    - `applebts-btsapi`
  - 共用單一 AppleBTS 專屬資料庫 volume

## 架構說明

```
┌─────────────────────────────────────────┐
│  Docker Compose Environment (Replica)   │
│                                         │
│  ┌──────────────┐                      │
│  │ Init Container│                      │
│  │   (seed)     │                      │
│  │              │                      │
│  │ 1. 啟動      │                      │
│  │ 2. 複製 DB   │──┐                   │
│  │ 3. 退出      │  │                   │
│  └──────────────┘  │                   │
│                    ▼                   │
│              ┌──────────┐              │
│              │ emptyDir │              │
│              │ (shared) │              │
│              └──────────┘              │
│                    │                   │
│                    │                   │
│  ┌─────────────────▼────────┐          │
│  │   API Container          │          │
│  │                          │          │
│  │ - 讀寫 /data/*.db        │          │
│  │ - 提供 API 服務          │          │
│  └──────────────────────────┘          │
│                                         │
└─────────────────────────────────────────┘
```

## 使用方式

### 1. 建置映像

```bash
cd /home/andrew/code-work/andrew-demo/AndrewDemo.NetConf2023
./build.sh
```

### 2. 啟動環境

```bash
cd compose
docker compose up
```

## AppleBTS 本機驗證

### 啟動 AppleBTS 環境

```bash
cd /Users/andrew/code-work/andrewshop.apidemo
docker compose -f compose/applebts.compose.yaml up --build
```

若要先重建乾淨資料庫：

```bash
cd /Users/andrew/code-work/andrewshop.apidemo
docker compose -f compose/applebts.compose.yaml down -v
docker compose -f compose/applebts.compose.yaml up --build
```

### AppleBTS 本機端點

- 標準 API: `http://localhost:5108`
- AppleBTS API: `http://localhost:5118`
- 標準 Swagger: [http://localhost:5108/swagger](http://localhost:5108/swagger)
- AppleBTS Swagger: [http://localhost:5118/swagger](http://localhost:5118/swagger)

### 使用 VS Code `.http` 測試

請直接開啟：

- [applebts-local.http](/Users/andrew/code-work/andrewshop.apidemo/compose/applebts-local.http)

建議順序：

1. `OAuth authorize`
2. 從 `Location` header 取 `code`
3. `OAuth token exchange`
4. 把 `access_token` 填到 `@accessToken`
5. `讀取 BTS 商品目錄`
6. `送出教育資格驗證`
7. `建立購物車`
8. `加入主商品`
9. 從購物車 response 取主商品 `lineId`
10. 把它填到 `@mainLineId`
11. `加入綁定主商品的贈品`
12. `試算 BTS 折扣`

### 使用 decision table 腳本測試

```bash
cd /Users/andrew/code-work/andrewshop.apidemo
scripts/applebts/run-decision-table.sh
```

## AppleBTS Time Shift 驗證

AppleBTS compose 已支援用環境變數覆蓋 host 的 `TimeProvider` 設定：

- `Time__Mode`
- `Time__ExpectedStartupLocal`
- `Time__TimeZoneId`

### 例 1: 啟動在 BTS 活動期間

`2026-04-04 18:35 (Asia/Taipei)` 應落在 BTS 時段內：

```bash
cd /Users/andrew/code-work/andrewshop.apidemo
docker compose -f compose/applebts.compose.yaml down -v
env \
  Time__Mode=Shifted \
  Time__ExpectedStartupLocal='2026-04-04T18:35:00' \
  Time__TimeZoneId=Asia/Taipei \
  docker compose -f compose/applebts.compose.yaml up --build
```

預期：

- `/bts-api/catalog` 有資料
- 驗證教育資格後，`macbook-air` 試算可拿到 BTS 價

### 例 2: 啟動在 BTS 活動期間外

`2026-01-01 18:35 (Asia/Taipei)` 應落在 BTS 時段外：

```bash
cd /Users/andrew/code-work/andrewshop.apidemo
docker compose -f compose/applebts.compose.yaml down -v
env \
  Time__Mode=Shifted \
  Time__ExpectedStartupLocal='2026-01-01T18:35:00' \
  Time__TimeZoneId=Asia/Taipei \
  docker compose -f compose/applebts.compose.yaml up --build
```

預期：

- `/bts-api/catalog` 為空
- `macbook-air` 試算回原價
- discount record 會回 `Hint`

### 關閉 Time Shift

若要恢復真實系統時間：

```bash
cd /Users/andrew/code-work/andrewshop.apidemo
docker compose -f compose/applebts.compose.yaml down -v
env \
  Time__Mode=System \
  Time__TimeZoneId=Asia/Taipei \
  docker compose -f compose/applebts.compose.yaml up --build
```

### 3. 測試 API

```bash
# 列出產品
curl http://localhost:5108/api/products

# 查看 Swagger UI
open http://localhost:5108/swagger
```

### 4. 停止環境

```bash
docker compose down

# 清除 volume (重置資料庫)
docker compose down -v

# AppleBTS compose
docker compose -f compose/applebts.compose.yaml down
docker compose -f compose/applebts.compose.yaml down -v
```

## 重要特性

### Init Container 模式
- `seed` 容器先執行，完成資料庫初始化
- 執行完成後退出 (`restart: "no"`)
- `api` 容器透過 `depends_on` 等待 seed 完成

### EmptyDir 模擬
- `shared-data` volume 模擬 Azure Container App 的 emptyDir
- 生命週期限於此 compose 環境 (replica 範圍)
- 執行 `docker compose down -v` 會清除資料

### 資料持久化
- 資料庫變更會保存在 volume 中
- 只要不執行 `down -v`，資料會跨 container 重啟保留
- 模擬單一 replica 的資料持久性

## 環境變數

可以透過 `.env` 檔案或環境變數調整:

```bash
# 建立 .env 檔案
cat > .env << EOF
ASPNETCORE_ENVIRONMENT=Production
SHOP_DATABASE_FILEPATH=/data/shop-database.db
EOF

docker compose up
```

AppleBTS compose 也支援直接用環境變數覆蓋 `TimeProvider` host 設定：

```bash
env \
  Time__Mode=Shifted \
  Time__ExpectedStartupLocal='2026-04-04T18:35:00' \
  Time__TimeZoneId=Asia/Taipei \
  docker compose -f compose/applebts.compose.yaml up --build
```

## 疑難排解

### 查看 init container 日誌
```bash
docker compose logs seed

# AppleBTS seed
docker compose -f compose/applebts.compose.yaml logs applebts-seed
```

### 檢查共享資料
```bash
docker compose run --rm api ls -lh /data/

# AppleBTS 共享資料
docker compose -f compose/applebts.compose.yaml run --rm applebts-api ls -lh /data/
```

### 進入 API 容器偵錯
```bash
docker compose exec api sh

# AppleBTS API
docker compose -f compose/applebts.compose.yaml exec applebts-api sh

# AppleBTS BTS API
docker compose -f compose/applebts.compose.yaml exec applebts-btsapi sh
```
