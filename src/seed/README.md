# Seed Init Container

此目錄用於建置 init container，負責將資料庫檔案部署到 Container App 的共享儲存 (emptyDir)。

## 檔案說明

- `Dockerfile`: Init container 映像定義
- `shop-database.db`: 要部署的資料庫檔案

## 使用方式

### 1. 準備資料庫檔案

**選項 A: 使用 DatabaseInit 產生新的初始資料庫**
```bash
cd /home/andrew/code-work/andrew-demo/AndrewDemo.NetConf2023
dotnet run --project src/AndrewDemo.NetConf2023.DatabaseInit
cp src/AndrewDemo.NetConf2023.DatabaseInit/bin/Debug/net9.0/shop-database.db src/seed/
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
```

## 注意事項

- 資料庫檔案會被複製到 `/data/shop-database.db` (emptyDir mount point)
- 檔案權限設定為 666，確保 API container 可以讀寫
- 此容器執行完成後即退出 (init container 模式)
