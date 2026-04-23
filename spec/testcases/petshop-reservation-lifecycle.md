# PetShop Reservation / Hidden Product Projection Lifecycle 測試案例

## 狀態

- phase: M4-P1A
- status: accepted
- 日期：2026-04-23

## 範圍

本文件只驗證 `reservation` 與 hidden standard `Product` projection 的生命週期連動。

本階段不驗證：

- checkout 後取消交易
- 預約服務折扣
- Storefront UI
- 通知可靠度
- background expiration worker

本文件中的 `CancelHold` 只代表 checkout 前取消尚未結帳的 hold，不代表 `OrderCancelledEvent` 或訂單成立後取消交易。

## 測試基準資料

除非 testcase 另有指定，皆使用以下資料：

- `ServiceId = grooming-basic`
- `StartAt = 2026-05-01T02:00:00Z`
- `EndAt = 2026-05-01T03:00:00Z`
- `VenueId = room-a`
- `StaffId = staff-amy`
- `HoldDuration = 30 min`

## Decision Table

| Case | 建立預約時是否衝突? | checkout 前是否取消 hold? | 是否在 hold 時效內 checkout? | 是否為同一 order event 重送? | 預期 Reservation | 預期 Product Projection | 對應 testcase |
|---|---:|---:|---:|---:|---|---|---|
| D1 | Y | - | - | - | 不建立 | 不建立新 hidden `Product` | TC-PET-RSV-002 |
| D2 | N | Y | N | - | `Holding -> Cancelled` | product record 保留，lookup 回傳 `null` | TC-PET-RSV-003 |
| D3 | N | N | N | - | `Holding -> Expired` | product record 保留，lookup 回傳 `null` | TC-PET-RSV-004 |
| D4 | N | N | Y | N | `Holding -> Confirmed` | product record 保留，checkout 後 lookup 回傳 `null` | TC-PET-RSV-005 |
| D5 | N | - | - | - | 新 reservation `Holding` | 建立新 hidden `Product` | TC-PET-RSV-006 |
| D6 | N | N | Y | Y | 保持 `Confirmed` | lookup 維持 `null` | TC-PET-RSV-007 |

## Executable Tests

目前對應的 unit tests 位於：

- `tests/AndrewDemo.NetConf2023.PetShop.Extension.Tests/PetShopReservationLifecycleTests.cs`

對應關係：

| testcase | unit test |
|---|---|
| TC-PET-RSV-001 | `TC_PET_RSV_001_CreateHold_CreatesReservationAndHiddenProduct` |
| TC-PET-RSV-002 | `TC_PET_RSV_002_CreateHold_WhenActiveOccupantExists_ReturnsSlotUnavailable` |
| TC-PET-RSV-003 | `TC_PET_RSV_003_CancelHold_BeforeCheckout_HidesProductAndReleasesSlot` |
| TC-PET-RSV-004 | `TC_PET_RSV_004_GetProductById_AfterHoldExpires_LazyExpiresReservationAndHidesProduct` |
| TC-PET-RSV-005 | `TC_PET_RSV_005_OrderCompletedEvent_WithinHold_ConfirmsReservationAndHidesProduct` |
| TC-PET-RSV-006 | `TC_PET_RSV_006_HistoricalExpiredOrCancelled_DoesNotBlockNewHold` |
| TC-PET-RSV-007 | `TC_PET_RSV_007_DuplicateOrderCompletedEvent_IsIdempotent` |

## Testcases

### TC-PET-RSV-001 建立 hold 會同時建立 reservation 與 hidden Product

- Given: 目前沒有相同 `StartAt + EndAt + VenueId + StaffId` 的 active occupant
- When: 會員 `101` 在 `2026-05-01T01:00:00Z` 呼叫 `CreateHold`
- Then: 建立 `reservation`
- And: 建立一筆對應的 standard `Product`
- And: `Product.IsPublished = false`
- And: `PetShopProductService.GetProductById(productId)` 在 hold 未過期時回傳 `Product`

狀態異動：

| sn | time | event | reservation-status | product-record | product-resolution | description |
|---:|---|---|---|---|---|---|
| 1 | `2026-05-01T01:00:00Z` | `CreateHoldRequested` | `res-001: 無 -> 無` | `pet-rsv-prod-001: 無 -> 無` | `GetProductById -> null` | 尚未建立資料。 |
| 2 | `2026-05-01T01:00:00Z` | `SlotAvailabilityChecked` | `res-001: 無 -> 無` | `pet-rsv-prod-001: 無 -> 無` | `GetProductById -> null` | 沒有 `Holding` 未過期或 `Confirmed` occupant。 |
| 3 | `2026-05-01T01:00:00Z` | `CreateHoldCommitted` | `res-001: 無 -> Holding` | `pet-rsv-prod-001: 無 -> Hidden` | `GetProductById -> Product` | 同 transaction 建立 reservation 與 hidden `Product`；`HoldExpiresAt = 01:30Z`。 |

### TC-PET-RSV-002 建立 hold 時遇到 active occupant 會被拒絕

- Given: `res-001` 已存在且狀態為 `Holding`
- And: `res-001.HoldExpiresAt = 2026-05-01T01:30:00Z`
- When: 會員 `202` 在 `2026-05-01T01:10:00Z` 對同 slot 呼叫 `CreateHold`
- Then: 回傳 `slot-unavailable`
- And: 不建立新 reservation
- And: 不建立新 hidden `Product`

狀態異動：

| sn | time | event | reservation-status | product-record | product-resolution | description |
|---:|---|---|---|---|---|---|
| 1 | `2026-05-01T01:00:00Z` | `ExistingHoldCommitted` | `res-001: 無 -> Holding` | `pet-rsv-prod-001: 無 -> Hidden` | `GetProductById -> Product` | 會員 `101` 已持有同 slot，`HoldExpiresAt = 01:30Z`。 |
| 2 | `2026-05-01T01:10:00Z` | `CreateHoldRequested` | `res-001: Holding -> Holding` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> Product` | 會員 `202` 對同 slot 呼叫 `CreateHold`。 |
| 3 | `2026-05-01T01:10:00Z` | `SlotConflictDetected` | `res-001: Holding -> Holding` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> Product` | active occupant 命中。 |
| 4 | `2026-05-01T01:10:00Z` | `CreateHoldRejected` | `res-002: 無 -> 無` | `pet-rsv-prod-002: 無 -> 無` | `GetProductById -> null` | 回傳 `slot-unavailable`；不建立新資料。 |

### TC-PET-RSV-003 checkout 前取消 hold 會隱藏 reservation product

- Given: `res-001` 是 `Holding`
- And: `pet-rsv-prod-001` 是 `Product(IsPublished=false)`
- When: 會員在 checkout 前呼叫 `CancelHold`
- Then: `res-001` 變成 `Cancelled`
- And: product record 保留
- And: `PetShopProductService.GetProductById(pet-rsv-prod-001)` 回傳 `null`
- And: 同 slot 不再被佔用

狀態異動：

| sn | time | event | reservation-status | product-record | product-resolution | description |
|---:|---|---|---|---|---|---|
| 1 | `2026-05-01T01:00:00Z` | `CreateHoldCommitted` | `res-001: 無 -> Holding` | `pet-rsv-prod-001: 無 -> Hidden` | `GetProductById -> Product` | hold 建立成功。 |
| 2 | `2026-05-01T01:15:00Z` | `CancelHoldRequested` | `res-001: Holding -> Holding` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> Product` | 會員在 checkout 前取消 hold。 |
| 3 | `2026-05-01T01:15:00Z` | `HoldCancelled` | `res-001: Holding -> Cancelled` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | product record 保留，reservation 狀態阻擋後續 lookup；slot 釋放。 |
| 4 | `2026-05-01T01:15:00Z` | `ProductLookupAfterCancel` | `res-001: Cancelled -> Cancelled` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | `GetProductById` 回傳 `null`。 |

### TC-PET-RSV-004 hold 過期後 product lookup 會 lazy expire

- Given: `res-001` 是 `Holding`
- And: `res-001.HoldExpiresAt = 2026-05-01T01:30:00Z`
- And: `pet-rsv-prod-001` 是 hidden `Product`
- When: `2026-05-01T01:31:00Z` 呼叫 `GetProductById(pet-rsv-prod-001)`
- Then: `res-001` 變成 `Expired`
- And: product record 保留
- And: `GetProductById` 回傳 `null`

狀態異動：

| sn | time | event | reservation-status | product-record | product-resolution | description |
|---:|---|---|---|---|---|---|
| 1 | `2026-05-01T01:00:00Z` | `CreateHoldCommitted` | `res-001: 無 -> Holding` | `pet-rsv-prod-001: 無 -> Hidden` | `GetProductById -> Product` | `HoldExpiresAt = 01:30Z`。 |
| 2 | `2026-05-01T01:31:00Z` | `ProductLookupRequested` | `res-001: Holding -> Holding` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> pending` | 開始解析 product。 |
| 3 | `2026-05-01T01:31:00Z` | `LazyExpireApplied` | `res-001: Holding -> Expired` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | reservation lazy expire。 |
| 4 | `2026-05-01T01:31:00Z` | `ProductLookupRejected` | `res-001: Expired -> Expired` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | `GetProductById` 回傳 `null`。 |

### TC-PET-RSV-005 hold 在時效內 checkout 成功會 confirmed reservation

- Given: `res-001` 是 `Holding`
- And: `pet-rsv-prod-001` 是 hidden `Product`
- When: 會員在 `2026-05-01T01:12:00Z` 完成 checkout
- And: `PetShopOrderEventDispatcher` 收到 `OrderCompletedEvent(OrderId=9001, ProductId=pet-rsv-prod-001)`
- Then: `res-001` 變成 `Confirmed`
- And: `res-001.ConfirmedOrderId = 9001`
- And: product record 保留但 `GetProductById` 回傳 `null`

狀態異動：

| sn | time | event | reservation-status | product-record | product-resolution | description |
|---:|---|---|---|---|---|---|
| 1 | `2026-05-01T01:00:00Z` | `CreateHoldCommitted` | `res-001: 無 -> Holding` | `pet-rsv-prod-001: 無 -> Hidden` | `GetProductById -> Product` | hold 建立成功。 |
| 2 | `2026-05-01T01:12:00Z` | `CheckoutProductLookup` | `res-001: Holding -> Holding` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> Product` | `GetProductById` 回傳 `Product` snapshot。 |
| 3 | `2026-05-01T01:12:00Z` | `OrderCreated` | `res-001: Holding -> Holding` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> Product` | `.Core` 建立 `OrderId = 9001`。 |
| 4 | `2026-05-01T01:12:00Z` | `OrderCompletedDispatched` | `res-001: Holding -> Confirmed` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | `ConfirmedOrderId = 9001`。 |

### TC-PET-RSV-006 歷史 Expired / Cancelled 不阻擋重新預約

- Given: 同 slot 只有歷史 reservation
- And: 歷史 reservation 狀態是 `Expired` 或 `Cancelled`
- When: 會員對同 slot 呼叫 `CreateHold`
- Then: 建立新的 reservation
- And: 建立新的 hidden `Product`
- And: 不復活舊 reservation

狀態異動：

| sn | time | event | reservation-status | product-record | product-resolution | description |
|---:|---|---|---|---|---|---|
| 1 | `2026-05-01T01:31:00Z` | `HistoricalExpiredExists` | `res-001: 無 -> Expired` | `pet-rsv-prod-001: 無 -> Hidden` | `GetProductById -> null` | 同 slot 只有歷史 expired 紀錄，不佔位。 |
| 2 | `2026-05-01T01:31:00Z` | `CreateHoldRequestedAfterExpired` | `res-001: Expired -> Expired` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | 舊資料不復活，slot 檢查不衝突。 |
| 3 | `2026-05-01T01:31:00Z` | `CreateHoldCommittedAfterExpired` | `res-002: 無 -> Holding` | `pet-rsv-prod-002: 無 -> Hidden` | `GetProductById -> Product` | 建立新的 reservation 與新的 hidden product。 |
| 4 | `2026-05-01T01:31:00Z` | `HistoricalCancelledExists` | `res-010: 無 -> Cancelled` | `pet-rsv-prod-010: 無 -> Hidden` | `GetProductById -> null` | 同 slot 只有歷史 cancelled 紀錄時，也不佔位。 |
| 5 | `2026-05-01T01:31:00Z` | `CreateHoldCommittedAfterCancelled` | `res-011: 無 -> Holding` | `pet-rsv-prod-011: 無 -> Hidden` | `GetProductById -> Product` | 建立新的 reservation 與新的 hidden product。 |

### TC-PET-RSV-007 同一 OrderCompletedEvent 重送必須 idempotent

- Given: `res-001` 已是 `Confirmed`
- And: `res-001.ConfirmedOrderId = 9001`
- And: `pet-rsv-prod-001` 是 hidden `Product`
- When: 再次收到同一筆 `OrderCompletedEvent(OrderId=9001, ProductId=pet-rsv-prod-001)`
- Then: reservation 狀態保持不變
- And: 不建立第二筆 confirmation side effect
- And: `GetProductById` 維持回傳 `null`

狀態異動：

| sn | time | event | reservation-status | product-record | product-resolution | description |
|---:|---|---|---|---|---|---|
| 1 | `2026-05-01T01:12:00Z` | `OrderAlreadyConfirmed` | `res-001: 無 -> Confirmed` | `pet-rsv-prod-001: 無 -> Hidden` | `GetProductById -> null` | 初始資料已是 checkout 成功後結果，`ConfirmedOrderId = 9001`。 |
| 2 | `2026-05-01T01:13:00Z` | `DuplicateOrderCompletedEventReceived` | `res-001: Confirmed -> Confirmed` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | 同一 order id 的 `OrderCompletedEvent` 重送。 |
| 3 | `2026-05-01T01:13:00Z` | `IdempotentConfirmSkipped` | `res-001: Confirmed -> Confirmed` | `pet-rsv-prod-001: Hidden -> Hidden` | `GetProductById -> null` | 回傳 success，但不重複改資料。 |
