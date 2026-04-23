# PetShop 預約購買滿額折扣與 PoC 通知

## 狀態

accepted

## 背景

PetShop 預約美容需求除了 reservation / checkout bridge 之外，還需要：

- 同一次結帳只要有預約，且商品部分結帳金額大於 `1000`，折 `100`
- checkout 成功後通知服務人員與消費者

目前仍是 PoC 專案，通知不需要先導入可靠 delivery、outbox 或 notification channel。

## 決策

折扣規則命名為 `PetShopReservationPurchaseThresholdDiscountRule`。

命名語意：

- `PetShop`: vertical boundary
- `Reservation`: 折扣資格必須來自 PetShop reservation line
- `PurchaseThreshold`: 一般商品購買滿額門檻折扣

第一版規則：

- 同一次 cart evaluation 必須至少有一筆有效 PetShop reservation line。
- 有效 reservation line 必須對應狀態為 `Holding` 且未過期的 reservation。
- 門檻只計算非 reservation 的一般商品 line 小計。
- 一般商品 line 小計以 `CartContext.LineItems` 的 `UnitPrice * Quantity` 計算，不扣除其他 discount rule 之後的 net amount。
- 一般商品 line 小計必須大於 `1000`，才輸出一筆 `-100` discount。
- 第一版不因 reservation 筆數增加，也不做每滿 `1000` 累加。

代表案例：

- 沒預約，一般商品 `1500`：不折扣，結帳 `1500`。
- 一筆預約 `2000`，一般商品 `1500`：折 `100`，結帳 `3400`。
- 兩筆預約各 `2000`，一般商品 `3000`：仍只折 `100`，結帳 `6900`。

PoC 通知附加在 `PetShopOrderEventDispatcher.Dispatch(OrderCompletedEvent)`：

- `ConfirmFromOrder` 若讓 reservation 新轉為 `Confirmed`，輸出一行 console log，代表通知服務人員與消費者。
- 同一 `OrderCompletedEvent` 重送時不重複輸出通知 log。
- 不引入 notification sender interface，也不處理 durable retry。

## 影響

- `.Core` 不需要變更，仍沿用 `IDiscountRule` 與 `IOrderEventDispatcher`。
- PetShop extension 新增 `PetShopReservationPurchaseThresholdDiscountRule`。
- `PetShopReservationService.ConfirmFromOrder` 改回傳 confirmation result，讓 dispatcher 能分辨新 confirmation 與 duplicate event。
- 後續若要導入正式通知，只需替換 dispatcher 內的 PoC console log side effect。

## 替代方案

### 1. 命名為 `PetShopDiscountRule`

不採用。名稱過於中性，未來 PetShop 若有會員折扣、服務人員折扣、套票折扣會衝突。

### 2. 通知先抽 `IPetShopNotificationSender`

不採用。PoC 只需要可觀察 side effect，提前抽 notification interface 會讓目前設計過重。

### 3. 以 reservation 金額作為滿額門檻

不採用。重新對照商業需求後，reservation 只作為折扣資格條件；滿額門檻看的是同次結帳的一般商品金額。
