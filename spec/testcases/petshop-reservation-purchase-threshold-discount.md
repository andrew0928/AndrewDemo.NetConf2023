# PetShop Reservation Purchase Threshold Discount 測試案例

## 狀態

- phase: M4-P4
- status: accepted
- 日期：2026-04-23

## 範圍

本文件驗證 PetShop reservation 與一般商品同次結帳時的滿額折扣。

規則命名固定為 `PetShopReservationPurchaseThresholdDiscountRule`，避免與後續其他 PetShop 折扣混淆。

## 規則

- 同一次 cart evaluation 只要包含至少一筆有效 PetShop reservation line，即具備折扣資格。
- 有效 PetShop reservation line 必須對應狀態為 `Holding` 且尚未超過 `HoldExpiresAt` 的 reservation。
- 折扣門檻只計算非 reservation 的一般商品 line 小計。
- 一般商品 line 小計以 `CartContext.LineItems` 的 `UnitPrice * Quantity` 計算，不扣除其他 discount rule 之後的 net amount。
- 一般商品 line 小計必須大於 `1000`，才折抵 `100`。
- 一次 cart evaluation 第一版只輸出一筆折扣，不因 reservation 筆數增加，也不做每滿 `1000` 累加。

## Decision Table

| Case | 是否有有效 reservation line? | 有效 reservation 筆數 | 一般商品金額是否大於 1000? | 一般商品金額 | 預約金額 | 預期折扣 | 預期結帳金額 | 覆蓋狀態 |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| D1 | N | 0 | Y | 1500 | 0 | 0 | 1500 | covered |
| D2 | Y | 1 | Y | 1500 | 2000 | -100 | 3400 | covered |
| D3 | Y | 2 | Y | 3000 | 4000 | -100 | 6900 | covered |
| D4 | Y | 1 | N，等於門檻 | 1000 | 2000 | 0 | 3000 | covered |
| D5 | Y | 1 | N，沒有一般商品 | 0 | 2000 | 0 | 2000 | covered |
| D6 | N，reservation 已過期 | 0 | Y | 1500 | 2000 | 0 | 3500 | covered |

## Scenario

### D1 沒有預約，即使一般商品金額大於 1000 也不折扣

- Given: cart 只有一般商品 `1500`
- When: 執行 `PetShopReservationPurchaseThresholdDiscountRule`
- Then: 不輸出 discount record
- Then: 結帳金額為 `1500`

### D2 一筆有效預約，且一般商品金額大於 1000，折扣一次

- Given: cart 有一筆有效 reservation line `2000`
- Given: cart 有一般商品 `1500`
- When: 執行 `PetShopReservationPurchaseThresholdDiscountRule`
- Then: 輸出一筆 `-100` discount record
- Then: 結帳金額為 `2000 + 1500 - 100 = 3400`

### D3 多筆有效預約，且一般商品金額大於 1000，仍只折扣一次

- Given: cart 有兩筆有效 reservation line，各 `2000`
- Given: cart 有一般商品 `3000`
- When: 執行 `PetShopReservationPurchaseThresholdDiscountRule`
- Then: 輸出一筆 `-100` discount record
- Then: 結帳金額為 `2000 * 2 + 3000 - 100 = 6900`

### D4 一筆有效預約，但一般商品金額等於 1000，不折扣

- Given: cart 有一筆有效 reservation line `2000`
- Given: cart 有一般商品 `1000`
- When: 執行 `PetShopReservationPurchaseThresholdDiscountRule`
- Then: 不輸出 discount record
- Then: 結帳金額為 `3000`

### D5 只有預約，沒有一般商品，不折扣

- Given: cart 有一筆有效 reservation line `2000`
- Given: cart 沒有一般商品 line
- When: 執行 `PetShopReservationPurchaseThresholdDiscountRule`
- Then: 不輸出 discount record
- Then: 結帳金額為 `2000`

### D6 預約已過期，即使一般商品金額大於 1000 也不折扣

- Given: cart 有一筆 reservation line `2000`
- Given: 該 reservation 已超過 `HoldExpiresAt`
- Given: cart 有一般商品 `1500`
- When: 執行 `PetShopReservationPurchaseThresholdDiscountRule`
- Then: 不輸出 discount record
- Then: 結帳金額為 `3500`

## Executable Tests

目前對應的 unit tests 位於：

- `tests/AndrewDemo.NetConf2023.PetShop.Extension.Tests/PetShopReservationPurchaseThresholdDiscountRuleTests.cs`

對應關係：

| Decision | unit test |
|---|---|
| D1 | `D1_WithoutReservation_DoesNotDiscountProductPurchaseAboveThreshold` |
| D2 | `D2_WithOneReservationAndProductPurchaseAboveThreshold_ReturnsSingleDiscount` |
| D3 | `D3_WithMultipleReservationsAndProductPurchaseAboveThreshold_ReturnsSingleDiscount` |
| D4 | `D4_WithReservationAndProductPurchaseAtThreshold_DoesNotDiscount` |
| D5 | `D5_WithReservationOnly_DoesNotDiscount` |
| D6 | `D6_WithExpiredReservationAndProductPurchaseAboveThreshold_DoesNotDiscount` |
