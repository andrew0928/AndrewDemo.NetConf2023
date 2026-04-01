# Shop Runtime 維持 Tenant Isolation Mode

## 狀態

- accepted
- 日期：2026-04-01

## 背景

在確認目前系統結構時，已明確觀察到：

- `ShopManifest` 內含 `DatabaseFilePath`
- host 啟動時先解析 `ShopId`，再依 manifest 建立資料庫連線
- `ShopDatabaseContext` 直接連到單一 database file
- 現有 collection 沒有 row-level `ShopId` discriminator

使用者要求：

- 維持目前的 tenant isolation mode 設計
- 並把這個結構寫進 `/spec`
- 讓後續開發專案能理解目前主線

## 決策

### 1. 目前主線維持 Tenant Isolation Mode

canonical 結論：

- 每個 `ShopId` 對應一份 `ShopManifest`
- 每份 `ShopManifest` 對應一個專屬 `DatabaseFilePath`
- 每個 host process 只連向該 shop 的專屬資料庫

### 2. 目前不採 Tenant Share Mode

下列設計不屬於目前主線：

- 同一個 physical database 內裝多個 shop 的資料
- 以 row-level `ShopId` 當 tenant discriminator
- shared master data + shop runtime data 同庫共存

若後續要朝這個方向演進，必須重開 Phase 1。

### 3. `ShopId` 的目前語意

目前 `ShopId` 代表：

- 啟動時選擇哪份 manifest
- 啟用哪個 product service
- 啟用哪些 discount rules
- 使用哪個 database file

目前 `ShopId` 不代表：

- 資料列層級的 tenant key

### 4. `/spec` 應以目前結構為基準

後續所有開發專案在未重開 Phase 1 前，都應以：

- `spec/shop-runtime-data-isolation-mode.md`

作為 runtime data topology 的正式基準。

## 影響

- 後續開發不能把現有 `ShopDatabaseContext` 誤解為 shared DB / row-filter 模式
- 若要做 shared physical database，不能當成小修正直接混入 phase 2
- 現有 `DatabaseFilePath` 仍是 runtime 邊界的一部分

## 替代方案

### 替代方案 A：直接把目前結構解讀成 Tenant Share Mode

缺點：

- 與實際程式碼不符
- 會讓後續開發誤判 schema 與資料隔離方式

結論：

- 不採用

### 替代方案 B：現在就把 shared physical database 當主線

缺點：

- 與使用者本輪要求衝突
- 會把未定案的結構誤寫成正式 spec

結論：

- 不採用

## 後續工作

1. 把 tenant isolation mode 寫入 `/spec` 與 `/spec/testcases`。
2. 後續若出現 shared DB 需求，先明確標記為 reopen Phase 1。
3. 舊有 shared-DB 探索文件需視為替代方向，不作為目前主線。
