# PetShop P1A 採 Concrete-First 內部邊界

## 狀態

accepted

## 背景

PetShop P1A 目前正在確認 `reservation` 與 hidden standard `Product` projection 的核心生命週期。第一版 source skeleton 曾先建立：

- `IPetShopIdGenerator`
- `IPetShopReservationStore`
- `CreatePetShopReservationHoldCommand`
- `ConfirmPetShopReservationFromOrderCommand`
- `CancelPetShopReservationHoldCommand`

這些設計能表達擴充點，但也會讓尚未穩定、尚未出現第二種實作的內部細節過早變成 interface / command pattern。

## 決策

PetShop P1A 採 concrete-first：

- 移除 `IPetShopIdGenerator`。
- 移除 `IPetShopReservationStore`。
- 保留 concrete `PetShopReservationRepository` 作為 persistence boundary。
- `PetShopReservationService` 直接依賴 `PetShopReservationRepository`。
- `ReservationId` / reservation `ProductId` 第一版由 `PetShopReservationService` 內部產生。
- 移除 `PetShopReservationCommands` 命名與 command pattern。
- `CreateHold` 因輸入欄位較多，保留 `CreatePetShopReservationHoldRequest`。
- `CancelHold` / `ConfirmFromOrder` 使用 method parameters。

此決策不影響 `.Core` contract，也不影響 `.Abstract`。正式跨邊界擴充點仍維持：

- `IProductService`
- `IOrderEventDispatcher`

## 影響

- PetShop extension source 更接近實際第一版 implementation，不會為尚未需要替換的內部策略建立 interface。
- `CreateHold` 的 transaction boundary 仍由 repository 承接，不把 slot conflict check 分散到 service 或 UI。
- 測試與後續 API spec 應以 service method 語意為準，而不是 command object 名稱。
- 若未來需要 SQLite / in-memory / remote reservation service 多實作並存，再抽 repository interface。

## 替代方案

### 1. 保留 `IPetShopIdGenerator`

不採用。ID 產生目前只是內部策略，尚未形成穩定替換需求。若未來要改成 ULID、database sequence 或跨節點 ID，再局部重構即可。

### 2. 保留 `IPetShopReservationStore`

不採用。必要的是 persistence boundary 與 transaction owner，不是 interface 形式。目前只有一種 repository implementation，先以 concrete class 表達即可。

### 3. 全部操作都使用 command pattern

不採用。`CancelHold` 與 `ConfirmFromOrder` 參數少且語意清楚，command object 只會增加間接層。`CreateHold` 因欄位較多，使用 request DTO 已足夠。

## 後續工作

- 若 P1B 開始定義 HTTP API，API request / response DTO 應與 service method 區分，不回頭引入 application command pattern。
- 若 P2 實作資料庫 repository，必須保留 `TryCreateHold` 的 atomic slot conflict check 與 create pair guarantee。
- 若出現第二種 persistence implementation，再以實際替換需求抽 interface。
