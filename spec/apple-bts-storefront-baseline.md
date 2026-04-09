# AppleBTS Storefront Baseline

## 狀態

- phase 1 accepted

## 目的

定義 `AndrewDemo.NetConf2023.AppleBTS.Storefront` 在 `CommonStorefront` 之上的最小擴充範圍。

## 正式規格

### 1. AppleBTS Storefront 的基底

- 必須沿用 `CommonStorefront` 的 page grammar、auth/session flow 與 server-side BFF 模式
- browser 不直接持 bearer token 呼叫 `/api` 或 `/bts-api`
- 一般商品、購物車、結帳、會員頁維持通用 storefront 行為

### 2. 追加的 AppleBTS 專屬頁面

必須提供：

- `/bts`
- `/bts/qualification`
- `/bts/products/{id}`

### 3. BTS 型錄頁

`/bts` 必須顯示：

- 主商品名稱
- 一般售價
- BTS 活動價
- `MaxGiftSubsidyAmount`
- gift option 清單
- 每個 gift 的原價
- 每個 gift 在目前主商品規則下的最高可折金額

### 4. 教育資格頁

`/bts/qualification` 必須：

- 要求登入
- 顯示目前資格狀態
- 提供 `.edu.xxx` 驗證表單
- 在 server side 呼叫 `/bts-api/qualification/*`

### 4A. 會員資料頁需顯示 BTS 教育資格摘要

- `/member` 必須額外顯示目前的 Apple BTS 教育資格摘要
- 摘要至少包含：
  - 是否通過驗證
  - 驗證信箱
  - 到期時間
  - 或未通過原因
- 頁面應提供前往 `/bts/qualification` 的入口
- `/member/orders` 若訂單包含 BTS 折扣，必須列出折扣名稱、說明與金額
- 若同時存在主商品價差與贈品補貼，應分別顯示：
  - `BTS 主商品優惠`
  - `BTS 贈品優惠`

### 5. BTS 商品詳細頁

`/bts/products/{id}` 必須顯示：

- 主商品名稱與說明
- 一般售價與 BTS 活動價
- gift option 清單
- 只加入主商品的按鈕
- 每個 gift 的專屬加入入口

### 6. gift 加入確認流程

- 使用者選擇 gift 時，storefront 必須先顯示確認步驟
- 使用者同意後，storefront 才可真正修改購物車
- 確認加入時，storefront 必須：
  - 先加入主商品
  - 再加入 gift
  - gift line 必須帶 `ParentLineId = 主商品 line id`
- 使用者取消時，購物車必須維持原狀

### 7. 本機驗證拓樸

AppleBTS storefront 的本機 browser 驗證入口固定為：

- `http://localhost:5138`

## 非目標

- 第一版不做品牌視覺強化
- 第一版不做 client-side bundle builder
- 第一版不新增 AppleBTS 專屬的 `.Core` 或 `.API` contract
