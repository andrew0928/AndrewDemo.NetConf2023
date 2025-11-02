# Docker Compose 部署環境

模擬 Azure Container App 的運作方式，使用 init container + emptyDir 模式。

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

## 疑難排解

### 查看 init container 日誌
```bash
docker compose logs seed
```

### 檢查共享資料
```bash
docker compose run --rm api ls -lh /data/
```

### 進入 API 容器偵錯
```bash
docker compose exec api sh
```
