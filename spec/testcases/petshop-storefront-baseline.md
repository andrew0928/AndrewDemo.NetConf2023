# PetShop Storefront Baseline 測試案例

## 狀態

- phase: M4-P3
- status: accepted; implemented; browser-smoke-passed
- 日期：2026-04-24

## 驗收原則

- 本文件是 `AndrewDemo.NetConf2023.PetShop.Storefront` 第一版的正式驗收清單。
- P3A 完成條件為 spec / testcase / skeleton 可 build。
- P3B 完成條件為 reservation flow pages 可依本文件主要案例操作。
- P3C 完成條件為 `petshop-storefront.compose.yaml` 可啟動並用 browser smoke 驗收主要 happy path。
- 若某 testcase 因時間控制或環境限制無法執行，必須標示為 blocked。

## 驗收結果

- 2026-04-24：`dotnet build src/AndrewDemo.NetConf2023.slnx -m:1` 通過。
- 2026-04-24：`dotnet test src/AndrewDemo.NetConf2023.slnx -m:1` 通過。
- 2026-04-24：`petshop-storefront.compose.yaml` 已切換為啟動 `AndrewDemo.NetConf2023.PetShop.Storefront`。
- 2026-04-24：使用者以 browser 驗證 PetShop storefront flow，結果 OK。

## Decision Table

| Case | 已登入 | 可用 slot | create hold 成功 | hold 期限內加入 cart | 同次結帳有一般商品 > 1000 | 完成 checkout | 預期結果 | 覆蓋階段 |
|---|---|---|---|---|---|---|---|---|
| D1 | Y | Y | Y | Y | Y | Y | reservation 從 `holding` 變 `confirmed`，cart/checkout 顯示 `-100` 折扣 | P3B/P3C |
| D2 | N | Y | N/A | N/A | N/A | N/A | create hold action 導向登入，未建立 reservation | P3B |
| D3 | Y | N | N | N/A | N/A | N/A | 無可選 slot，或 API conflict 後顯示錯誤 | P3B |
| D4 | Y | Y | Y | N | N/A | N/A | reservation 保持 `holding`，detail 可取消 hold | P3B |
| D5 | Y | Y | Y | Y | N | Y | reservation confirmed，但沒有 PetShop 滿額折扣 | P3C |
| D6 | Y | Y | Y | cancelled | N/A | N/A | reservation `cancelled`，不再顯示加入 cart action | P3B |
| D7 | Y | Y | Y | expired | N/A | N/A | add-to-cart 失敗並顯示 hold expired / product unavailable | P3C |

## Page Routes / Navigation

### TC-PET-UI-001 PetShop 首頁顯示美容服務目錄

- Given: 使用者開啟 `PetShop.Storefront`
- When: 進入 `/petshop`
- Then: 可看到美容服務清單
- And: 每筆服務顯示名稱、說明、價格與服務時間
- And: 每筆服務都有建立預約入口

### TC-PET-UI-002 建立預約頁可查詢可用 slot

- Given: 使用者進入 `/petshop/reservations/new`
- When: 選擇服務與日期
- Then: storefront 透過 server side 呼叫 `/petshop-api/availability`
- And: 頁面只顯示 PetShop API 回傳的可用 slot
- And: slot 顯示時間、場地與服務人員

### TC-PET-UI-003 建立 hold 需要登入

- Given: 使用者尚未登入
- When: 在 `/petshop/reservations/new` 選擇 slot 並送出
- Then: storefront 導向 `/auth/login`
- And: 登入完成後可回到建立預約流程
- And: 未登入狀態不會建立 reservation

### TC-PET-UI-004 建立 hold 成功後顯示預約確認中

- Given: 使用者已登入，且選擇一個可用 slot
- When: 送出建立 reservation hold
- Then: storefront 在 server side 呼叫 `/petshop-api/reservations/holds`
- And: 頁面顯示 reservation detail
- And: 狀態顯示為「預約確認中」
- And: 頁面顯示 hold 到期時間
- And: 頁面提供加入購物車與取消 hold action

### TC-PET-UI-005 slot 不可用時顯示錯誤

- Given: 使用者已登入
- When: 選定的 slot 在送出前已被其他 reservation hold 或 confirmed
- Then: PetShop API 回傳 conflict
- And: storefront 顯示可理解的錯誤
- And: 不建立 cart line

## Cart / Checkout Flow

### TC-PET-UI-006 holding reservation 可加入購物車

- Given: 使用者已有一筆 `holding` reservation
- When: 在 reservation detail 點選加入購物車
- Then: storefront server side 讀取 owner-visible reservation detail
- And: storefront 不在 UI 顯示 `checkoutProductId`
- And: storefront 使用 `CoreApiClient` 將 hidden product 加入標準 cart
- And: 成功後導向 `/cart`

### TC-PET-UI-007 reservation 搭配一般商品滿額可顯示折扣

- Given: 使用者購物車包含一筆有效 reservation line
- And: 購物車也包含一般商品金額大於 1000
- When: 使用者開啟 `/cart`
- Then: cart estimate 顯示 `PetShop 預約購買滿額折扣`
- And: 折扣金額為 `-100`

### TC-PET-UI-008 沒有一般商品滿額時不顯示 PetShop 折扣

- Given: 使用者購物車包含一筆有效 reservation line
- And: 一般商品金額小於或等於 1000
- When: 使用者開啟 `/cart`
- Then: 不顯示 PetShop 滿額折扣

### TC-PET-UI-009 checkout completed 後 reservation 顯示已預約

- Given: 使用者購物車包含一筆有效 reservation line
- When: 使用者完成 `/checkout`
- Then: 標準 `.API` dispatch `OrderCompletedEvent`
- And: PetShop reservation 被標記為 `confirmed`
- And: 使用者重新進入 `/petshop/reservations/{id}` 看到「已預約」

## Reservation Status / Member

### TC-PET-UI-010 我的預約列表顯示目前會員 reservations

- Given: 使用者已登入且有 reservation
- When: 開啟 `/petshop/reservations`
- Then: storefront server side 呼叫 `/petshop-api/reservations`
- And: 頁面列出目前會員的 reservations
- And: 每筆 reservation 顯示服務、時間、場地、服務人員與狀態

### TC-PET-UI-011 reservation detail 只顯示目前會員資料

- Given: 使用者已登入
- When: 開啟 `/petshop/reservations/{id}`
- Then: storefront server side 呼叫 `/petshop-api/reservations/{id}`
- And: 若 API 回傳 forbidden 或 not found，頁面顯示無權限或找不到
- And: 不顯示其他會員 reservation detail

### TC-PET-UI-012 checkout 前可取消 hold

- Given: 使用者已有一筆 `holding` reservation
- When: 在 detail 頁取消 hold
- Then: storefront server side 呼叫 `POST /petshop-api/reservations/{id}/cancel-hold`
- And: 頁面顯示 `cancelled`
- And: 不再顯示加入購物車 action

### TC-PET-UI-013 expired hold 不可加入購物車

- Given: 使用者已有一筆已過期的 hold
- When: 嘗試加入購物車
- Then: storefront 顯示 hold 已失效或商品不可加入
- And: 不建立 cart line

## BFF / UI Grammar

### TC-PET-UI-014 browser 不直接呼叫 `/petshop-api`

- Given: 使用者完成 reservation flow
- When: 檢查 browser 端行為
- Then: browser 不直接以 JavaScript 呼叫 `/petshop-api`
- And: PetShop API 呼叫發生在 storefront server side

### TC-PET-UI-015 PetShop 與 Common 使用相同 UI grammar

- Given: 使用者分別瀏覽 `CommonStorefront` 與 `PetShop.Storefront`
- When: 比較版面與主要元件
- Then: 兩者沿用相同 layout、navigation、form、button、notification 與 error summary grammar
- And: PetShop 只在內容區塊上追加 reservation-specific UI
