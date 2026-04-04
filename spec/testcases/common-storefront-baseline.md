# CommonStorefront Baseline 測試案例

## 狀態

- phase: 1
- status: proposed-for-review
- 日期：2026-04-04

## 驗收原則

- 本文件是 `CommonStorefront` Phase 1 的正式驗收清單
- 完成條件為：
  - 對應功能已實作
  - `agent-browser` 可逐條操作並驗證通過
- 若某情境因環境或外部依賴無法執行，必須標示為 blocked

## Page Routes / Navigation

### TC-CS-001 首頁可作為任務入口

- Given: 使用者進入 `CommonStorefront`
- When: 開啟首頁
- Then: 可看到網站標題、主要導覽、登入入口與購物車入口
- And: 頁面包含 `header`、`nav`、`main`、`footer`

### TC-CS-002 商品列表可導向詳細頁

- Given: 使用者開啟商品列表
- When: 瀏覽列表並選取其中一項商品
- Then: 可進入該商品的詳細頁
- And: 商品列表在手機版不依賴水平捲動作為主要閱讀方式

### TC-CS-003 商品詳細頁可加入購物車

- Given: 使用者進入商品詳細頁
- When: 執行加入購物車
- Then: storefront 在 server side 呼叫標準 `.API`
- And: 使用者之後可在 `/cart` 看到商品

## Auth / Session

### TC-CS-004 未登入進入受保護頁面會走 `/auth/login`

- Given: 使用者尚未登入
- When: 直接開啟 `/cart`
- Then: 會被導向 `/auth/login`
- And: `/auth/login` 會再導向 `/api/login/authorize`

### TC-CS-005 OAuth callback 由 server side 交換 token

- Given: `/api/login/authorize` 完成登入
- When: 使用者被 redirect 回 `/auth/callback?code=...`
- Then: `CommonStorefront` 在 server side 呼叫 `/api/login/token`
- And: token 由 storefront session 或 secure cookie 保存

### TC-CS-006 登出會清除 storefront session

- Given: 使用者已登入
- When: 執行 `/auth/logout`
- Then: storefront 清除登入狀態
- And: 之後重新進入 `/cart` 會再次要求登入

## Cart / Checkout / Member

### TC-CS-007 購物車頁顯示 line items、discounts、hints 與總價

- Given: 購物車內已有商品
- When: 使用者進入 `/cart`
- Then: 頁面可看到 line items、折扣、hint 與總價
- And: hint 以可見文字顯示

### TC-CS-008 結帳頁可完成主要任務

- Given: 使用者已登入且購物車內有商品
- When: 進入 `/checkout` 並完成送單
- Then: storefront 會透過 server side 呼叫 checkout APIs
- And: 完成後可看到結果頁或導回訂單列表

### TC-CS-009 會員資料與訂單列表可讀取

- Given: 使用者已登入
- When: 開啟 `/member` 與 `/member/orders`
- Then: 頁面可顯示 member profile 與 orders

## Accessibility / RWD

### TC-CS-010 鍵盤可完成主要操作

- Given: 使用者只用鍵盤操作網站
- When: 依序操作首頁、商品列表、商品詳細頁、購物車與結帳
- Then: 所有主要互動都可完成
- And: focus 樣式清楚可見

### TC-CS-011 表單欄位具有 label 與錯誤關聯

- Given: 使用者進入任何需要輸入的頁面
- When: 檢視表單
- Then: 每個欄位都有 label
- And: 欄位錯誤可透過文字與輔助技術關聯到欄位

### TC-CS-012 手機版可完成主要任務

- Given: 使用者以手機尺寸瀏覽 `CommonStorefront`
- When: 操作商品列表、商品詳細頁、購物車與結帳
- Then: 可完整完成主要任務
- And: 不需要水平捲動作為主要操作方式

## BFF / Topology

### TC-CS-013 browser 不直接呼叫 `/api/login/token`

- Given: 使用者完成登入流程
- When: 檢查 browser 端行為
- Then: browser 不直接以 JavaScript 呼叫 `/api/login/token`
- And: token exchange 發生在 storefront server side

### TC-CS-014 storefront server side 不繞 Front Door 呼叫 backend

- Given: `CommonStorefront` 需要呼叫 `.API`
- When: 檢視 host 設定與實際呼叫拓樸
- Then: storefront server side 直接呼叫內部 backend service URL
- And: 不應透過對外 Front Door URL 再回呼 `/api`
