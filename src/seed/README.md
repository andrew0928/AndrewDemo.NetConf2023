# Seed Init Container

此目錄用於建置 init container，負責將資料庫檔案部署到 Container App 的共享儲存 (emptyDir)。

## 檔案說明

- `Dockerfile`: Init container 映像定義
- `seed-entrypoint.sh`: 初始化執行 script (含錯誤處理和日誌)
- `shop-database.db`: 要部署的資料庫檔案

## 使用方式

### 1. 準備資料庫檔案

**選項 A: 使用 DatabaseInit 產生新的初始資料庫**
```bash
cd /home/andrew/code-work/andrew-demo/AndrewDemo.NetConf2023
dotnet run --project src/AndrewDemo.NetConf2023.DatabaseInit
cp src/AndrewDemo.NetConf2023.DatabaseInit/bin/Debug/net10.0/shop-database.db src/seed/
```

**選項 B: 使用現有的資料庫 snapshot**
```bash
cp /path/to/your/snapshot.db src/seed/shop-database.db
```

### 2. 建置映像

```bash
# 在專案根目錄執行
./build.sh

# 或手動建置
docker build -t andrewdemo-netconf2023-seed:develop src/seed/
```

### 3. 測試

```bash
# 本地測試 init container
docker run --rm -v $(pwd)/test-data:/data andrewdemo-netconf2023-seed:develop
ls -lh test-data/shop-database.db
cat test-data/.seed_done
```

## 環境變數

- `SEED_DEST`: 目標掛載點 (預設: `/data`)
- `SEED_CHOWN_UID`: 設定檔案擁有者 UID (選填)
- `SEED_CHOWN_GID`: 設定檔案擁有者 GID (選填)

## 注意事項

- Init container 使用 ENTRYPOINT 模式，確保 Azure Container Apps 正確執行
- 包含錯誤處理機制 (`set -euo pipefail`)，任何錯誤都會導致容器失敗
- 執行過程會輸出帶時間戳記的日誌，方便追蹤和除錯
- 完成後會建立 `.seed_done` 旗標檔案，可用於驗證執行狀態
- 資料庫檔案會被複製到 `/data/shop-database.db` (emptyDir mount point)
- 檔案權限設定為 666，目錄權限 777，確保 API container 可以讀寫
- 此容器執行完成後即退出 (init container 模式)
