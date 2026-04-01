# Shop Runtime Data Isolation Mode 測試案例

## 狀態

- phase: 1
- status: draft-for-freeze
- 日期：2026-04-01

## Runtime Topology

### TC-IS-001 `ShopId` 先解析 manifest，再決定 database path

- Given: `SHOP_ID=shop-a`
- And: appsettings 內存在 `shop-a` 的 `ShopManifest`
- When: 啟動 API
- Then: 先解析 `shop-a` 的 manifest
- And: 再使用該 manifest 的 `DatabaseFilePath`

### TC-IS-002 單一 process 只連一個 database file

- Given: host 啟動完成
- When: 檢查 `ShopDatabaseContext`
- Then: 只建立一個 `LiteDatabase` 連線
- And: 該 process 不會在 request 期間依 `ShopId` 切換資料庫

### TC-IS-003 collection 不依賴 row-level `ShopId` filter

- Given: `ShopDatabaseContext` 已建立
- When: 讀取 `members`、`products`、`orders`
- Then: 直接讀固定 collection 名稱
- And: 不存在依 `ShopId` 加上的內建 row filter

### TC-IS-004 每個 shop 使用自己的專屬 database

- Given: `shop-a` 與 `shop-b` 各自設定不同 `DatabaseFilePath`
- When: 分別啟動兩個 host
- Then: 各自連到自己的 database file
- And: 系統不假設兩者資料自動共享

### TC-IS-005 tenant share mode 不屬於目前規格

- Given: 有開發者希望在同一個 database 內放多個 shop 的資料
- When: 檢查目前 `/spec`
- Then: 這不屬於目前 canonical 設計
- And: 必須重開 Phase 1 才能調整
