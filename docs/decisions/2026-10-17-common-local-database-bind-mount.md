# Common 本機開發資料庫使用實體目錄

## 狀態

accepted

## 背景

為支援 TiP 業績與核帳資料的開發驗證，使用者要求讓 VS Code 等本機工具直接存取 LiteDB 檔案。本調整支援目前驗證流程，不新增正式查詢功能。

## 決策

- 正在使用的 `compose/common.site-dev.compose.yaml` 改採 bind mount。
- canonical 本機路徑為 `<repo>/.local/data/common-site-dev/shop-database.db`，容器內維持 `/data/shop-database.db`。
- seed 與 API 共用目錄；此組態僅在 DB 不存在時執行 seed，避免重新啟動覆寫資料。
- 遷移前正常停止 API，完整複製 `/data`；原 named volume 保留作備份。
- `.local/` 不進 Git 或 Docker build context。

## 影響

- `down -v` 不再重置 common site-dev 資料；重置前應停止網站並搬移資料目錄作備份。
- 本機查詢須先停止 API，查詢完成關閉 DB 後再啟動；亦可停止後複製資料目錄並查詢副本。
- 僅調整 common site-dev；其他 compose 模式及站點維持原設定。
- 不影響 `.Core`、`.Abstract` 或 frozen spec。

## 替代方案

保留 named volume 並每次匯出副本亦可，但不符合使用者希望 DB 直接放在本機實體目錄的操作方式。

## 後續工作

Phase 2 完成交易資料寫入後，再提供開發用業績與核帳查詢。
