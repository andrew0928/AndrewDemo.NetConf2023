# Storefront Family UI and BFF Testcases

## 驗收原則

- 本文件中的 test cases 是 storefront Phase 1 的正式驗收清單
- storefront 完成條件為：
  - 對應功能實作完成
  - `agent-browser` 可逐條操作並驗證通過
- 驗收時應以實際啟動中的 storefront 為對象，而不是只檢查靜態畫面或 unit test
- 若某 testcase 因環境或外部依賴無法完成，必須標註為 blocked，而不是直接忽略
- 後續若新增 storefront 頁面或 vertical-specific workflow，應先把 testcase 補進本文件或對應 vertical 的 testcase 文件

## TC-UI-01 Common Storefront 首頁與導覽

- Given: 使用者開啟 `CommonStorefront`
- When: 進入首頁
- Then: 可看到網站標題、主要導覽、登入入口與購物車入口
- And: 頁面包含 `header`、`nav`、`main`、`footer`
- And: 可使用鍵盤移動到主要導覽連結

## TC-UI-02 商品列表在手機版可操作

- Given: 使用者以手機尺寸開啟商品列表頁
- When: 瀏覽頁面
- Then: 商品列表以單欄或可讀的多欄方式呈現
- And: 不需要以水平捲動作為主要閱讀方式
- And: 每個商品都能進入詳細頁

## TC-UI-03 商品詳細頁可完成加入購物車

- Given: 使用者進入商品詳細頁
- When: 點選加入購物車
- Then: storefront 在 server side 呼叫對應 backend API
- And: browser 不需要直接持 bearer token 呼叫 `/api`

## TC-UI-04 購物車頁顯示折扣與 hint

- Given: 購物車內已有商品與 discount evaluation 結果
- When: 使用者進入購物車頁
- Then: 頁面可看到 line items、折扣、hint 與總價
- And: hint 必須以可見文字呈現，不能只存在 icon 或 tooltip

## TC-UI-05 結帳頁在手機版可完成主要任務

- Given: 使用者以手機尺寸進入結帳頁
- When: 檢視與送出訂單
- Then: 頁面仍可完整閱讀與操作
- And: 送出按鈕易於觸控點擊

## TC-UI-06 OAuth login authority 沿用 `/api/login`

- Given: 使用者尚未登入
- When: 進入受保護頁面
- Then: storefront redirect 到自身 `/auth/login`
- And: `/auth/login` 再導向 `/api/login/authorize`
- And: storefront 本身不直接提供另一套 authority login form

## TC-UI-07 OAuth callback 與 server-side token exchange

- Given: `/api/login/authorize` 完成登入並 redirect 回 storefront callback
- When: storefront 收到 `code`
- Then: storefront 在 server side 呼叫 `/api/login/token`
- And: token 儲存在 server-side session 或 secure cookie
- And: browser 不直接以 JavaScript 呼叫 `/api/login/token`

## TC-UI-08 AppleBTS Storefront 顯示 BTS 專屬頁面

- Given: 使用者進入 `AppleBTS.Storefront`
- When: 開啟 BTS 首頁或 BTS 商品頁
- Then: storefront 在 server side 呼叫 `/bts-api`
- And: 可看到 BTS 專屬型錄、資格狀態或 gift options

## TC-UI-09 AppleBTS 與 Common 使用相同 UI grammar

- Given: 使用者分別瀏覽 `CommonStorefront` 與 `AppleBTS.Storefront`
- When: 比較版面與元件
- Then: 兩者使用相同的 layout、spacing、form style 與 navigation grammar
- And: AppleBTS 只在內容區塊上追加 vertical-specific UI

## TC-UI-10 無障礙表單要求

- Given: 使用者進入登入、資格驗證、結帳等表單頁
- When: 檢視表單結構
- Then: 每個欄位都有 `label`
- And: 必填與錯誤訊息以文字方式呈現
- And: 欄位錯誤可透過輔助技術關聯到對應欄位

## TC-UI-11 鍵盤操作

- Given: 使用者只用鍵盤操作網站
- When: 依序操作導覽、商品頁、購物車與結帳
- Then: 所有主要互動都可完成
- And: focus 樣式清楚可見

## TC-UI-12 Server-side BFF 呼叫拓樸

- Given: storefront 部署於 Front Door 後方
- When: storefront 需要讀取資料
- Then: browser 對外只需呼叫 storefront 網站
- And: storefront server side 直接呼叫內部 backend service
- And: storefront server side 不應再繞經 Front Door 呼叫 `/api` 或 vertical API
