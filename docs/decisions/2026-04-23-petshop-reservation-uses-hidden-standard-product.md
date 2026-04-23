# PetShop Reservation 採 Hidden Standard Product Projection

## 狀態

accepted

## 背景

PetShop 預約美容需要讓 reservation hold 結果加入既有 cart 並與一般商品一起 checkout。早期草案把這個 bridge 稱為 `dynamic-product`，並設計成 PetShop 自有 entity，包含 `Active`、`Consumed`、`Expired`、`Cancelled` 等獨立狀態。

重新檢查既有 product contract 後，`Product` 已有 `IsPublished` 欄位，且標準 product service 的公開列表與 id lookup 已可支援「不公開瀏覽，但可用 id 解析」的 hidden product 需求。因此，把 `dynamic-product` 做成獨立 entity 會讓 reservation 與 product 各自持有 lifecycle state，增加同步成本。

## 決策

PetShop P1A 不建立獨立 `dynamic-product` entity / status。

reservation hold 成功時，PetShop 在同一個 transaction / critical section 內建立：

- `PetShopReservationRecord`
- 標準 `Product` record，且 `IsPublished = false`

`PetShopReservationRecord.ProductId` 指向 hidden `Product.Id`。hidden product 的可購買狀態不另存 status，而是由 reservation 狀態推導：

- `Holding` 且未過期：`PetShopProductService.GetProductById(productId)` 回傳 `Product`
- `Holding` 但已過期：lazy expire reservation，回傳 `null`
- `Confirmed` / `Expired` / `Cancelled`：回傳 `null`

`PetShopProductService` 採 decorator 方式包住標準 `IProductService`：

- `GetPublishedProducts()` 委派給標準 product service
- `GetProductById(productId)` 先讀標準 `Product`，再套用 reservation product policy

## 影響

- `.Core` contract 不需要新增 buyer-aware validator 或 reservation-aware product type。
- PetShop Extension source 移除 `PetShopDynamicProductRecord` 與 `PetShopDynamicProductStatus`。
- `PetShopReservationRepository.TryCreateHold` 改為建立 reservation，並寫入 `.Core` 標準 Products collection 的 hidden `Product`。
- `PetShopReservationService` 保留 reservation 為 lifecycle source of truth，`ConfirmFromOrder` / `CancelHold` / `ExpireHold` 只更新 reservation。
- docs / spec / tests 的 canonical term 改為 hidden standard `Product` 或 reservation product projection。

## 替代方案

### 1. 保留獨立 `dynamic-product` entity

不採用。它能完整表達 product lifecycle，但在 P1A 會讓 product status 與 reservation status 雙軌同步，增加交易一致性與測試負擔。

### 2. 只用 `DefaultProductService`，不建立 PetShopProductService

不採用。`DefaultProductService.GetProductById(productId)` 可讀到 hidden `Product`，但無法根據 reservation `Expired` / `Cancelled` / `Confirmed` 阻擋後續 checkout。PetShop 仍需要自己的 product service decorator 套用 reservation lifecycle policy。

### 3. 在 `.Core` 增加通用 Product metadata 或 validation hook

不採用。P1A 尚未證明這是跨 shop 穩定需求，提前擴充 `.Core` 會降低核心穩定性。若未來多個 vertical 都需要 owner-aware 或 quantity-aware cart validation，再重新開啟 `.Core` contract 決策。

## 後續工作

- P1B 定義 PetShop API 時，必須確保 hidden product id 只回傳給 reservation owner。
- P2 生產化資料庫 repository 時，`CreateHold` 必須保留 reservation / product 成對建立與 slot conflict check 的 transaction boundary。
- 若未來要支援 checkout 後取消交易，應先建立 confirmed reservation cancellation 的狀態機，再決定 product lookup 是否重新開放或永遠不復活。
