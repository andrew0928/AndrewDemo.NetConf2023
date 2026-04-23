# PetShop Reservation P1A 範圍界定

## 狀態

accepted

## 背景

PetShop 預約美容需求會透過 `reservation` 與 hidden standard `Product` 連接既有 cart / checkout。checkout 成功後，PetShop 需要透過 `IOrderEventDispatcher.Dispatch(OrderCompletedEvent)` 將 reservation 由 `Holding` 推進為 `Confirmed`。

同時，訂單成立後的取消交易會牽涉 refund、order cancellation semantics、服務人員通知撤回、slot 是否釋放、以及 confirmed reservation 取消 / 改期規則。這些議題超出 M4-P1A 的核心模型確認範圍。

## 決策

M4-P1A 只處理 reservation 與 hidden standard `Product` projection 的核心生命週期：

- `CreateHold`: 建立 `Holding` reservation 與 `Product(IsPublished=false)`。
- `CancelHold`: checkout 前取消尚未結帳的 hold，reservation 轉為 `Cancelled`，product record 保留但不可解析。
- `ExpireHold`: hold 超時後 reservation 轉為 `Expired`，product record 保留但不可解析。
- `ConfirmFromOrder`: checkout 成功後，透過 `OrderCompletedEvent` 將 reservation 轉為 `Confirmed`，product record 保留但不可再次 checkout。

M4-P1A 不處理 checkout 後取消交易。現階段 `PetShopOrderEventDispatcher.Dispatch(OrderCancelledEvent)` 不改 reservation 狀態。

`Cancelled` 在 P1A 的語意只代表 checkout 前 hold 被取消；confirmed reservation 的取消 / 改期 API 是未來延伸需求。

## 影響

- `docs/project-roadmap.md` 將 checkout 後取消交易列為 P1A 不處理項目。
- `docs/petshop-reservation-lifecycle-draft.md` 的狀態圖不包含 `Confirmed -> Cancelled` 主流程，且 hidden product 不再有獨立狀態圖。
- `spec/testcases/petshop-reservation-lifecycle.md` 的 decision table 不展開 post-checkout cancellation case。
- `AndrewDemo.NetConf2023.PetShop.Extension` skeleton 只提供 checkout 前 `CancelHold` 與 `OrderCompletedEvent` confirmation path。

此決策不影響 `.Core` contract，也不回頭修正 `.Abstract`。

## 替代方案

- 在 P1A 直接處理 `OrderCancelledEvent`：不採用。這會把 refund / slot release / notification compensation 提前納入，超出本階段「核心模型」目標。
- 讓 `Cancelled` 同時代表 checkout 前 hold cancel 與 checkout 後 order cancel：不採用。這會讓狀態語意過載，未來驗證 testcase 難以判斷 cancellation 發生在交易前或交易後。

## 後續工作

- 若後續要支援 checkout 後取消交易，應先建立 confirmed reservation cancellation 的狀態機與 decision table。
- 後續設計需明確定義 `OrderCancelledEvent` 是否釋放 slot、是否建立補償通知、以及是否與 refund transaction 綁定。
