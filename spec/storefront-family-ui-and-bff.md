# Storefront Family UI and BFF

## 狀態

- phase 1 draft

## 目的

定義 storefront family 的正式 UI / host 基準，供後續：

- `AndrewDemo.NetConf2023.CommonStorefront`
- `AndrewDemo.NetConf2023.AppleBTS.Storefront`
- `AndrewDemo.NetConf2023.PetShop.Storefront`

共同遵循。

## 正式規格

### 1. Storefront 模式

- storefront 必須採 ASP.NET Core server-side BFF 模式
- browser 不直接持 bearer token 呼叫 backend APIs
- storefront 由 server side 呼叫：
  - `/api/*`
  - `/bts-api/*`
  - `/petshop-api/*`

### 2. 專案切分

- `AndrewDemo.NetConf2023.Storefront.Shared`
- `AndrewDemo.NetConf2023.CommonStorefront`
- `AndrewDemo.NetConf2023.AppleBTS.Storefront`
- `AndrewDemo.NetConf2023.PetShop.Storefront`

### 3. Common Storefront 功能範圍

必須提供：

- 首頁
- 商品列表
- 商品詳細頁
- 購物車
- 結帳
- 會員資訊
- 訂單列表
- `/auth/login`
- `/auth/callback`

### 4. AppleBTS Storefront 額外功能

除 common 功能外，必須提供：

- BTS 首頁
- BTS 商品列表或活動型錄入口
- 教育資格頁
- BTS 商品詳細頁

### 5. OAuth / Login

- login authority 使用標準 `.API` 提供的 `/oauth/authorize`
- storefront 不重刻 login authority UI
- storefront 必須自行處理 callback 與 session

### 6. 對外路由

對外網站 path 應遵循：

- `/*` -> storefront
- `/oauth/*` -> `.API`
- `/api/*` -> `.API`
- `/bts-api/*` -> `AndrewDemo.NetConf2023.AppleBTS.API`
- `/petshop-api/*` -> `PetShop.API`

### 7. Storefront 與 backend 的呼叫方式

- storefront server side 應直接呼叫內部 backend service URL
- 不應由 storefront server side 再繞經 Azure Front Door

### 8. UI 設計基準

- 視覺風格採 GOV.UK / USWDS 類型的任務導向極簡風格
- 不以品牌敘事型大型 hero / 動畫 / carousel 為主
- 以清楚、可讀、低複雜度為優先

### 9. Accessibility 基準

- UI 必須以 WCAG 2.2 AA 為基準
- 每頁必須有：
  - skip link
  - `header`
  - `nav`
  - `main`
  - `footer`
- 所有互動元件必須支援鍵盤操作
- 所有表單欄位必須有明確 label
- 錯誤訊息不可只靠顏色表達

### 10. RWD 基準

- UI 必須 mobile-first
- 商品列表、商品詳情、購物車、結帳在手機版必須可直接操作
- 不可要求使用者在手機上以橫向 scroll 作為主要操作方式

### 11. 技術最小化原則

- 第一版 storefront 應優先採 Razor Pages 或 MVC server-rendered page
- 不應先導入 Node.js host
- 不應先導入 SPA framework 作為必要依賴
- JavaScript 應維持最小，只處理必要互動

### 12. 驗收方式

- storefront 完成條件不得只以 build 或 unit test 通過判定
- 必須依本規格對應的 `/spec/testcases/storefront-family-ui-and-bff.md` 逐條驗收
- 驗收執行方式以 `agent-browser` 為主
- `agent-browser` 必須對實際啟動中的 storefront 進行操作與觀察
- 只有在對應 test case 驗收完成後，才可視為 storefront 交付完成
- 若某情境因外部依賴或環境限制無法驗收，必須明確列為 blocked item，不得默認略過

## 非目標

- 第一版不追求 Apple 官網式品牌視覺
- 第一版不追求 client-side SPA
- 第一版不重刻 OAuth authority UI

## 後續實作順序

1. `Storefront.Shared`
2. `CommonStorefront`
3. `AppleBTS.Storefront`
4. `PetShop.Storefront`
