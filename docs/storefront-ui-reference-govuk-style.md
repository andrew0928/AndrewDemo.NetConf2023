# Storefront UI Reference - GOV.UK Style

## 目標

storefront UI 的主要目的是：

- 展示與操作流程清楚
- 不追求品牌感很強的視覺表現
- 優先滿足無障礙與行動裝置友善
- 技術實作越簡單越好

因此，建議的 UI 參考風格是：

- GOV.UK Design System
- USWDS

這兩種風格的共同特徵是：

- 版面非常克制
- 文字層級明確
- 表單與錯誤提示清楚
- 對鍵盤操作與螢幕閱讀器友善
- 在 RWD 上偏實用而非花俏

## 視覺方向

### 整體基調

- 淺底色
- 深色文字
- 單一主色
- 單一警示色
- 不使用大量漸層與陰影
- 不使用 carousel、浮動促銷視窗、複雜動畫

### 字體

建議第一版直接使用系統字體，不額外引入 web font。

原因：

- 載入簡單
- 中文顯示穩定
- 對展示用 PoC 足夠

### 版面密度

- 手機版優先單欄
- 桌面版最多兩到三欄
- 卡片間距與段落間距固定
- 以文字與任務完成為優先，不用把版面塞滿

## 資訊架構

### 共用頁面

- 首頁
- 商品列表
- 商品詳細頁
- 購物車
- 結帳
- 會員資料
- 訂單列表

### AppleBTS 額外頁面

- BTS 首頁
- 教育資格頁
- BTS 商品詳細頁

## 代表性版型

### 1. 首頁

首頁不做大型 hero banner，建議只放：

- 網站標題
- 簡短說明
- 主要入口
  - 商品列表
  - BTS 專區
  - 購物車
- 目前登入狀態

### 2. 商品列表

建議用任務導向列表，不做高度視覺化電商牆。

每個商品卡片顯示：

- 商品名稱
- 簡短說明
- 價格
- 是否有活動
- 前往詳情按鈕

桌面版兩到三欄；手機版單欄。

### 3. 商品詳細頁

建議結構：

- 商品名稱
- 價格
- 商品描述
- 若為 BTS：
  - 顯示 BTS 說明
  - 顯示資格提示
  - 顯示可選贈品區塊
- 主要 CTA：
  - 加入購物車

### 4. 購物車

這頁是最重要的資訊清楚頁面，建議以 table-like list 呈現：

- line item 名稱
- 單價
- 數量
- 小計
- 折扣 / hint
- 總計

如果有 hint，要放在總價附近，不能藏在 tooltip。

### 5. 結帳頁

建議不要塞多段流程 wizard。

第一版就做成單頁確認：

- 買家資訊
- 商品摘要
- 折扣摘要
- 總金額
- 送出訂單

## 無障礙設計基準

基準建議：

- WCAG 2.2 AA

### 必做清單

- 每頁有 skip link
- 使用 `header` / `nav` / `main` / `footer`
- 所有表單欄位都有 `label`
- required / error / hint 有文字，不只靠顏色
- 表單錯誤有 summary 區塊
- 所有互動元件可用鍵盤操作
- focus ring 明顯可見
- 所有 icon 若承載資訊，需有文字或 aria 標記
- 行動裝置不依賴 hover 才能看懂
- 放大 200% 到 400% 仍可操作

### 實作細節建議

- 錯誤提示放在欄位上方或下方，並以 `aria-describedby` 連結
- checkbox / radio 使用 `fieldset` + `legend`
- page title 與 `h1` 要一致或高度接近
- loading 不做會閃爍的 spinner 動畫，簡單文字即可

## RWD 原則

### Mobile-first

第一版直接以手機尺寸開始設計：

- 單欄
- 觸控按鈕大於等於 44px 高
- 導覽列優先簡化成少量文字連結

### 建議 breakpoints

- `< 640px`: mobile
- `640px - 1023px`: tablet
- `>= 1024px`: desktop

### RWD 行為

- 商品列表：1 欄 -> 2 欄 -> 3 欄
- 商品詳細頁：永遠保持主內容優先，不做左右過度切割
- 購物車：小螢幕改成 stacked summary，不強迫橫向表格捲動

## 元件集合

第一版元件應盡量少：

- Header
- Footer
- Breadcrumb
- Page heading
- Notification banner
- Error summary
- Product card
- Price summary
- Form field
- Primary button / Secondary button
- Simple list / Description list

避免第一版就做：

- modal
- drawer
- mega menu
- carousel
- floating CTA

## 技術建議

### UI framework

不建議導入大型前端 UI framework。

建議：

- ASP.NET Core Razor Pages
- 1 個 layout
- 幾個 partials / tag helpers
- 自家輕量 CSS

### CSS 策略

建議採固定 token：

- spacing scale
- color scale
- font scale
- container width

先用很小的 CSS foundation 即可，不需要完整 design token engine。

## 為什麼不直接做 Apple 官網風格

Apple 官網風格的重點是：

- 品牌視覺
- 大圖與情境敘事
- 動態展示

但你的需求重點是：

- 展示用 PoC
- 清楚可操作
- 無障礙
- RWD
- 簡單實作

所以 GOV.UK / USWDS 風格更合適。

## 後續實作建議

1. 先以 `CommonStorefront` 做 baseline
2. 先完成：
   - layout
   - product list
   - product detail
   - cart
   - checkout
3. 再把 AppleBTS 頁面套進同一個 UI grammar

換句話說，AppleBTS UI 不應該發明另一套視覺語言，而是沿用 common storefront 的元件與版型，只加上 BTS 專屬資訊區塊。
