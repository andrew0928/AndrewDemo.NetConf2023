# Phase 2 Review Notes

這份筆記列的是以 commit `bb5122c9b6285a20f8f40ac96662f0c0b54c6f73` 為準，review 後仍存在的落差或風險，並不代表 phase 2 主體沒有完成，而是提醒你在 freeze / 下一輪規劃時要不要順手一起收斂。

## Findings

### [P2] `CreateCheckout` 標示 201，但實際 action 會回 200

- `CheckoutController.Create(...)` 只回傳 plain `CheckoutCreateResponse`，沒有 `Created(...)`、`CreatedAtRoute(...)` 或明確 `StatusCode(201)`。
- 這表示 runtime 行為與 `[ProducesResponseType(StatusCodes.Status201Created)]` 不一致，API consumer 會看到 `200 OK`。
- 參考：`src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs:40-69`

### [P2] spec 已定義 `BuyerMismatch -> 403`，但目前沒有對應的 API 邊界自動化測試

- `spec/testcases/checkout-correctness-fixes.md` 已明確列出 `TC-CK-FIX-101`，要求 `CheckoutController` 將 `BuyerMismatch` 映射為 `403 Forbidden`。
- 現有自動化測試只覆蓋 `CheckoutService` 的 core 行為，還沒有 controller / integration test 驗證 HTTP mapping。
- 參考：`spec/testcases/checkout-correctness-fixes.md:36-42`、`src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs:104-111`、`tests/AndrewDemo.NetConf2023.Core.Tests/CheckoutServiceTests.cs:13-158`

### [P2] `.http` sample 仍停留在 phase 0 / phase 1 之前的 request shape

- `src/AndrewDemo.NetConf2023.API.http` 仍用 numeric `productId`，且 `checkout/create` 與 `checkout/complete` body 還保留已移除的 `accessToken` 欄位。
- phase 2 的 source code 已明確改成 header-based auth 與 string `ProductId`，sample 若不更新，手動 smoke test 會被誤導。
- 參考：`src/AndrewDemo.NetConf2023.API/AndrewDemo.NetConf2023.API.http:67-68`、`src/AndrewDemo.NetConf2023.API/AndrewDemo.NetConf2023.API.http:98-112`

### [P3] `CheckoutController` 註解仍宣稱 complete 後會清空購物車

- controller XML remarks 仍寫「完成交易時，會將購物車內容轉換成訂單，並且清空購物車」。
- phase 2 實作與 spec 都沒有 cart clearing 行為，這段註解會讓 API 使用者誤判 side effect。
- 參考：`src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs:32-37`

## 本輪 review 的判讀

- phase 2 的主體目標已完成：`CheckoutService` 已落地，buyer mismatch 與 transaction delete timing 已修正，`CheckoutServiceTests` 也把主要 correctness 路徑補齊。
- 目前殘留的問題多半是 API contract 說明、sample 與測試覆蓋層級沒有完全跟上，屬於可以在下一輪一次收口的尾巴。
