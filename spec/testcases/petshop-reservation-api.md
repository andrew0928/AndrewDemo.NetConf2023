# PetShop Reservation API 測試案例

## 狀態

- phase: M4-P1B
- status: accepted
- 日期：2026-04-24

## 範圍

本文件對應 [PetShop Reservation API Spec](../petshop-reservation-api.md) 的第一版 API 行為。

測試重點：

- API route 與 auth boundary
- service / availability 查詢
- reservation hold 建立
- reservation owner-only 查詢
- checkout 前取消 hold
- hidden product id 只在有效 `holding` 狀態回傳給 owner

本文件不測：

- Core cart / checkout API 本身
- checkout 後取消交易
- confirmed reservation 取消 / 改期
- durable notification delivery

## Decision Table

| Case | API | 是否登入 | 是否 owner | service 是否存在 | slot 是否可用 | reservation 狀態 | hold 是否過期 | 預期 HTTP | 預期結果 |
|---|---|---:|---:|---:|---:|---|---:|---:|---|
| API-D1 | `GET /services` | N | - | - | - | - | - | 200 | 回傳 service catalog |
| API-D2 | `GET /availability` | N | - | Y | Y | - | - | 200 | 回傳 available slot |
| API-D3 | `GET /availability` | N | - | Y | N | - | - | 200 | 被占用 slot 不在回應內 |
| API-D4 | `POST /reservations/holds` | N | - | Y | Y | - | - | 401 | 不建立 reservation/product |
| API-D5 | `POST /reservations/holds` | Y | Y | Y | Y | - | - | 201 | 建立 `holding`，回傳 `checkoutProductId` |
| API-D6 | `POST /reservations/holds` | Y | Y | N | - | - | - | 404 | `service-not-found` |
| API-D7 | `POST /reservations/holds` | Y | Y | Y | N | - | - | 409 | `slot-unavailable`，不建立新資料 |
| API-D8 | `GET /reservations` | Y | - | - | - | mixed | mixed | 200 | 只回傳目前會員自己的 reservations |
| API-D9 | `GET /reservations/{id}` | Y | Y | - | - | `holding` | N | 200 | 回傳 `holding` 與 `checkoutProductId` |
| API-D10 | `GET /reservations/{id}` | Y | N | - | - | `holding` | N | 403 | 不回傳 reservation detail |
| API-D11 | `GET /reservations/{id}` | Y | Y | - | - | `holding` | Y | 200 | lazy expire 後回傳 `expired`，`checkoutProductId = null` |
| API-D12 | `POST /reservations/{id}/cancel-hold` | Y | Y | - | - | `holding` | N | 200 | `holding -> cancelled`，`checkoutProductId = null` |
| API-D13 | `POST /reservations/{id}/cancel-hold` | Y | Y | - | - | `holding` | Y | 409 | `hold-expired` |
| API-D14 | `POST /reservations/{id}/cancel-hold` | Y | Y | - | - | `confirmed` | - | 409 | `reservation-not-cancellable` |
| API-D15 | `GET /reservations/{id}` | Y | Y | - | - | `confirmed` | - | 200 | 回傳 `confirmed`，`checkoutProductId = null` |

## Scenario

### API-D1 查詢服務清單不需要登入

- Given: PetShop service catalog 內有 `grooming-basic`
- When: client 呼叫 `GET /petshop-api/services`
- Then: 回傳 `200 OK`
- And: response 包含 `serviceId`、`name`、`price`、`durationMinutes`

### API-D2 查詢可用時段不需要登入

- Given: `grooming-basic` 存在
- Given: `2026-05-01T02:00:00Z` slot 沒有 active reservation
- When: client 呼叫 `GET /petshop-api/availability?serviceId=grooming-basic&date=2026-05-01`
- Then: 回傳 `200 OK`
- And: response 包含 `isAvailable = true` 的 slot

### API-D3 已被 hold 的時段不可預約

- Given: 同 `StartAt + EndAt + VenueId + StaffId` 已有未過期 `Holding` reservation
- When: client 呼叫 availability API
- Then: 回傳 `200 OK`
- And: 該 slot 不出現在可用清單

### API-D4 未登入不能建立 hold

- Given: request 沒有有效 Bearer token
- When: client 呼叫 `POST /petshop-api/reservations/holds`
- Then: 回傳 `401 Unauthorized`
- And: 不建立 reservation
- And: 不建立 hidden product

### API-D5 建立 hold 成功會回傳 checkoutProductId

- Given: 會員 `101` 已登入
- Given: `grooming-basic` 存在
- Given: slot 可用
- When: client 呼叫 `POST /petshop-api/reservations/holds`
- Then: 回傳 `201 Created`
- And: response status 為 `holding`
- And: response 包含 `reservationId`
- And: response 包含 `checkoutProductId`
- And: `checkoutProductId` 可被 Core cart API 加入購物車

### API-D6 service 不存在時不可建立 hold

- Given: 會員 `101` 已登入
- Given: `serviceId = unknown-service`
- When: client 呼叫 `POST /petshop-api/reservations/holds`
- Then: 回傳 `404 Not Found`
- And: error code 為 `service-not-found`

### API-D7 slot 衝突時不可建立 hold

- Given: 會員 `101` 已登入
- Given: 同 slot 已有 active reservation
- When: client 呼叫 `POST /petshop-api/reservations/holds`
- Then: 回傳 `409 Conflict`
- And: error code 為 `slot-unavailable`
- And: 不建立新的 reservation
- And: 不建立新的 hidden product

### API-D8 reservation list 只回傳目前會員自己的資料

- Given: 會員 `101` 已登入
- Given: 會員 `101` 與 `202` 都各自有 reservation
- When: client 呼叫 `GET /petshop-api/reservations`
- Then: 回傳 `200 OK`
- And: response 只包含會員 `101` 的 reservations
- And: 若其中某筆 `holding` 已過期，應在 response 中呈現 `expired`

### API-D9 owner 查詢 holding reservation 會取得 checkoutProductId

- Given: 會員 `101` 已登入
- Given: `res-001` 屬於會員 `101`
- Given: `res-001` 狀態為 `holding` 且未過期
- When: client 呼叫 `GET /petshop-api/reservations/res-001`
- Then: 回傳 `200 OK`
- And: response status 為 `holding`
- And: response 包含 `checkoutProductId`

### API-D10 非 owner 不可查詢 reservation detail

- Given: 會員 `202` 已登入
- Given: `res-001` 屬於會員 `101`
- When: client 呼叫 `GET /petshop-api/reservations/res-001`
- Then: 回傳 `403 Forbidden`
- And: 不回傳 reservation detail
- And: 不回傳 `checkoutProductId`

### API-D11 查詢已過期 hold 會 lazy expire

- Given: 會員 `101` 已登入
- Given: `res-001` 屬於會員 `101`
- Given: `res-001` 狀態為 `holding`
- Given: `HoldExpiresAt` 已早於 API evaluation time
- When: client 呼叫 `GET /petshop-api/reservations/res-001`
- Then: 回傳 `200 OK`
- And: response status 為 `expired`
- And: `checkoutProductId = null`

### API-D12 checkout 前取消 hold 成功

- Given: 會員 `101` 已登入
- Given: `res-001` 屬於會員 `101`
- Given: `res-001` 狀態為 `holding` 且未過期
- When: client 呼叫 `POST /petshop-api/reservations/res-001/cancel-hold`
- Then: 回傳 `200 OK`
- And: response status 為 `cancelled`
- And: `checkoutProductId = null`

### API-D13 hold 已過期時取消失敗

- Given: 會員 `101` 已登入
- Given: `res-001` 屬於會員 `101`
- Given: `res-001` 狀態為 `holding`
- Given: `HoldExpiresAt` 已早於 API evaluation time
- When: client 呼叫 `POST /petshop-api/reservations/res-001/cancel-hold`
- Then: 回傳 `409 Conflict`
- And: error code 為 `hold-expired`

### API-D14 confirmed reservation 不可用 checkout 前取消 API

- Given: 會員 `101` 已登入
- Given: `res-001` 屬於會員 `101`
- Given: `res-001` 狀態為 `confirmed`
- When: client 呼叫 `POST /petshop-api/reservations/res-001/cancel-hold`
- Then: 回傳 `409 Conflict`
- And: error code 為 `reservation-not-cancellable`

### API-D15 confirmed reservation 查詢不回傳 checkoutProductId

- Given: 會員 `101` 已登入
- Given: `res-001` 屬於會員 `101`
- Given: `res-001` 狀態為 `confirmed`
- When: client 呼叫 `GET /petshop-api/reservations/res-001`
- Then: 回傳 `200 OK`
- And: response status 為 `confirmed`
- And: `confirmedOrderId` 有值
- And: `checkoutProductId = null`

## Executable Tests

目前對應的 controller tests 位於：

- `tests/AndrewDemo.NetConf2023.PetShop.API.Tests/PetShopApiControllerTests.cs`

目前已對應：

| decision | test |
|---|---|
| API-D1 | `GetServices_ReturnsConfiguredCatalog` |
| API-D3 | `GetAvailability_WhenSlotOccupied_OmitsUnavailableSlot` |
| API-D4 | `CreateHold_WithoutAccessToken_ReturnsUnauthorized` |
| API-D5 | `CreateHold_WithAuthenticatedMember_ReturnsCreatedReservation` |
| API-D8 | `GetReservations_ReturnsOnlyCurrentMembersReservationsAndLazyExpiresHold` |
| API-D10 | `GetReservation_WhenOwnerMismatch_ReturnsForbidden` |
| API-D14 | `CancelHold_WhenConfirmed_ReturnsConflict` |
