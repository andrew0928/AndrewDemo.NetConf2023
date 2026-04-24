# PetShop API Phase 1 邊界

## 狀態

accepted

## 背景

PetShop reservation lifecycle 與 hidden standard `Product` projection 已在 extension 內定案，接下來需要建立 `AndrewDemo.NetConf2023.PetShop.API` 作為 vertical API server，提供 storefront 或其他 client 使用的預約入口。

在 API contract 草案確認過程中，仍有三個會直接影響路由語意與 response shape 的問題需要固定：

- availability 是否要列出 unavailable slot
- cancel hold endpoint 是否採 resource-style `DELETE`
- 第一版是否需要 reservation list endpoint

## 決策

### 1. PetShop vertical API route prefix 固定為 `/petshop-api`

PetShop API 是 vertical API，不混入既有 Core cart / checkout route。

第一版由 `AndrewDemo.NetConf2023.PetShop.API` 提供：

- `GET /petshop-api/services`
- `GET /petshop-api/availability`
- `POST /petshop-api/reservations/holds`
- `GET /petshop-api/reservations`
- `GET /petshop-api/reservations/{reservationId}`
- `POST /petshop-api/reservations/{reservationId}/cancel-hold`

Core cart / checkout 仍沿用既有 `.API`：

- `POST /api/carts/create`
- `POST /api/carts/{cartId}/items`
- `POST /api/checkout/create`
- `POST /api/checkout/complete`

### 2. Availability API 只回傳可用 slot

第一版 `GET /petshop-api/availability` 不回傳 unavailable slot，也不透過 `isAvailable=false` 做負向表達。

原因：

- storefront 第一版只需要「現在可選哪些 slot」
- 避免 API response 混入不必要的 unavailable explainability shape
- slot 真正一致性仍由 `CreateHold` 的 transaction / conflict check 保證

因此 response contract 只保留可被建立 hold 的 slot。

### 3. Cancel hold 採 POST command endpoint

`POST /petshop-api/reservations/{reservationId}/cancel-hold` 視為業務操作 API，不採 `DELETE /.../hold` 的 resource CRUD 語意。

原因：

- `cancel-hold` 是明確的 domain command，不只是刪除資料
- 操作結果是 reservation state transition：`Holding -> Cancelled`
- 後續若要補 command audit、operator metadata 或 command idempotency，比較容易沿用同一語意

### 4. 第一版必須提供目前會員的 reservation list endpoint

新增：

- `GET /petshop-api/reservations`

用途：

- storefront 會員中心查看目前與歷史 reservation
- checkout 前回到預約流程時可重新取得有效 `holding` reservation 與 `checkoutProductId`

list endpoint 僅回傳目前登入會員自己的 reservations。

### 5. Service catalog 與 availability 先採 configuration-backed concrete service

第一版不建立 staff / venue / schedule 的資料庫 schema，也不建立 admin 維護 API。

先以 configuration-backed concrete service 提供：

- PetShop service catalog
- daily recurring availability templates
- hold duration policy

這符合 thin first pass 原則，後續若需要 seed / database model，再由 M4-P2C 擴充。

## 影響

- `spec/petshop-reservation-api.md` 與 `spec/testcases/petshop-reservation-api.md` 轉為 accepted baseline。
- `AndrewDemo.NetConf2023.PetShop.API` 需依上述 route 與 command semantics 實作。
- `.Abstract` 與 `.Core` 不需要因本決策變更 contract。
- PetShop API implementation 必須確保 hidden product id 只回傳給 reservation owner。

## 替代方案

### 1. Availability 回傳全部 slot，並以 `isAvailable` 標示

不採用。這會讓第一版 response shape 變大，也會把 unavailable reason 建模提前帶進來。

### 2. Cancel hold 採 `DELETE /petshop-api/reservations/{reservationId}/hold`

不採用。這會弱化 domain command 語意，讓 API 看起來像純 CRUD。

### 3. 第一版不做 reservation list endpoint

不採用。這會讓 storefront 會員中心與重新取得有效 `holding` reservation 的流程缺少正式 API。

## 後續工作

- 實作 `AndrewDemo.NetConf2023.PetShop.API`。
- 補齊 PetShop API 的 executable tests。
- 下一階段再把主 `.API` 的 PetShop runtime wiring 與 host topology 接上。
