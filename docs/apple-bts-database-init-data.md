# AppleBTS DatabaseInit 資料說明

這份文件說明目前 `AndrewDemo.NetConf2023.AppleBTS.DatabaseInit` 初始化到資料庫中的內容、資料來源與整理原則。

本文的目的主要是讓人快速理解：

- 目前 AppleBTS 測試環境到底 seed 了什麼資料
- 這些資料是從哪裡來的
- 這些資料是直接寫在 code，還是由外部檔案匯入
- 目前資料內容有哪些刻意簡化

## 1. 資料來源

目前 AppleBTS 的資料來源是：

1. 本次設計討論中確認的業務規則
2. 本次 session 內，依 **2026-04-02 當下 Apple 台灣官網主要產品線頁面** 所整理出的展示用產品線快照
3. 後續再依 POC 需求，手動精簡成適合測試的 seed 清單

也就是說，這份資料：

- **不是**執行時從 Apple 官網即時抓取
- **不是**由外部 JSON / CSV 匯入
- 而是根據本次討論時確認過的產品線與活動規則，**人工整理後直接寫在 code**

本次 session 內已確認採用的產品線方向是：

- 一般 catalog：Apple 主要產品線
- BTS 主商品：Mac / iPad 主線
- BTS 贈品：AirPods / Apple Pencil / Magic Trackpad 等少數配件

## 2. DatabaseInit 的資料來源方式

目前 `AndrewDemo.NetConf2023.AppleBTS.DatabaseInit` 的資料來源方式是：

- **直接寫在 code 內**
- 並由 `Program.cs` 讀取這份靜態 seed 清單後寫入資料庫

主要位置如下：

- [AppleBtsSeedData.cs](../src/AndrewDemo.NetConf2023.AppleBTS.DatabaseInit/AppleBtsSeedData.cs)
- [Program.cs](../src/AndrewDemo.NetConf2023.AppleBTS.DatabaseInit/Program.cs)

寫入流程是：

1. `Program.cs` 啟動後先刪除既有 DB 檔
2. 依 `AppleBtsSeedData.Products` 建立：
   - `products`
   - `skus`
   - `inventory_records`
3. 透過 `AppleBtsAdminService` 寫入：
   - `bts_campaigns`
   - `bts_main_offers`
   - `bts_gift_options`
   - `member_education_verifications`

也就是說：

- 商品主檔、SKU、庫存：由 `DatabaseInit` 直接寫入
- BTS campaign / offer / gift option / 教育驗證 sidecar：透過 `AppleBtsAdminService` 寫入

目前**沒有**額外的外部 seed 檔案作為資料來源。

## 3. 資料原則與補充說明

目前這份 seed 資料採以下原則：

### 3.1 展示用途優先

這份資料是為了：

- 本機開發
- Docker Compose 測試環境
- Storefront / API / BTS API 驗證

所以它是 **POC / demo seed**，不是正式商品主檔來源。

### 3.2 產品線採「主線簡化版」

目前只保留主要產品線，不建立細部規格型號，例如：

- 不區分 RAM / SSD / 顏色
- 不區分各種組態 SKU
- 每個商品只保留一個最基礎、最容易辨識的展示版本

### 3.3 BTS 活動資料是人工整理的測試資料

目前的：

- `BtsPrice`
- `GiftGroup`
- `MaxGiftSubsidyAmount`
- 可選贈品清單

都是為了符合本次討論中已確認的 decision table 與 UI 驗證情境而整理的 seed。

它們的用途是：

- 驗證 `BtsDiscountRule`
- 驗證型錄顯示
- 驗證主商品 + 贈品加入購物車
- 驗證訂單折扣顯示

### 3.4 活動時間窗以 Asia/Taipei 自然日定義，再轉成 UTC 儲存

目前 campaign 是：

- 2026-04-01 00:00:00 `Asia/Taipei`
- 到 2026-10-31 23:59:59 `Asia/Taipei`

在 seed 時會先以台北時間定義，再轉成 UTC 寫入 `BtsCampaignRecord`。

### 3.5 教育驗證 seed 只放最少測試資料

目前只有一筆預先建立的 member 驗證資料，主要用途是：

- 驗證「已過期資格」情境

一般 `.edu.xx` 驗證流程仍是透過 AppleBTS API 執行時建立。

### 3.6 這份資料不是後續資料維護格式

目前 seed 清單雖然直接寫在 code，但它的角色是：

- 提供可重建的測試資料庫
- 提供 demo / compose 的穩定資料集

它**不是**之後營運人員直接維護資料的格式。

若後續需要更高維護性，才再考慮把 seed 拆成外部檔案。

## 4. 目前 seed 進去的資料內容

### 4.1 Campaign

- 名稱：`Apple BTS 2026`
- 期間：`2026-04-01` 到 `2026-10-31`
- 狀態：啟用

### 4.2 一般商品主線

目前會建立的 Apple 一般商品主線包含：

- MacBook Air
- MacBook Pro
- iMac
- Mac mini
- Mac Studio
- Mac Pro
- iPad Pro
- iPad Air
- iPad
- iPad mini
- iPhone 17 Pro
- iPhone Air
- iPhone 17
- iPhone 17e
- iPhone 16
- Apple Watch Series 11
- Apple Watch SE 3
- Apple Watch Ultra 3

### 4.3 配件 / 贈品商品

目前會建立的配件商品包含：

- AirPods 4
- AirPods Pro 3
- AirPods Max
- Apple Pencil Pro
- Apple Pencil (USB-C)
- Magic Trackpad

## 5. 目前 code 內直接定義的產品 / 贈品 / 優惠清單

以下清單是給人閱讀與理解用途，不是 DB 重建格式。

### 5.1 一般商品清單

| 類別 | 商品 | 一般售價 |
|---|---|---:|
| Mac | MacBook Air | NT$ 35,900 |
| Mac | MacBook Pro | NT$ 54,900 |
| Mac | iMac | NT$ 42,900 |
| Mac | Mac mini | NT$ 19,900 |
| Mac | Mac Studio | NT$ 67,900 |
| Mac | Mac Pro | NT$ 229,900 |
| iPad | iPad Pro | NT$ 32,900 |
| iPad | iPad Air | NT$ 19,900 |
| iPad | iPad | NT$ 11,900 |
| iPad | iPad mini | NT$ 15,900 |
| iPhone | iPhone 17 Pro | NT$ 36,900 |
| iPhone | iPhone Air | NT$ 29,900 |
| iPhone | iPhone 17 | NT$ 25,900 |
| iPhone | iPhone 17e | NT$ 21,900 |
| iPhone | iPhone 16 | NT$ 25,900 |
| Watch | Apple Watch Series 11 | NT$ 13,900 |
| Watch | Apple Watch SE 3 | NT$ 8,900 |
| Watch | Apple Watch Ultra 3 | NT$ 27,900 |

### 5.2 配件 / 贈品商品清單

| 類別 | 商品 | 一般售價 |
|---|---|---:|
| Audio | AirPods 4 | NT$ 5,990 |
| Audio | AirPods Pro 3 | NT$ 7,990 |
| Audio | AirPods Max | NT$ 17,900 |
| Accessory | Apple Pencil Pro | NT$ 4,500 |
| Accessory | Apple Pencil (USB-C) | NT$ 2,690 |
| Accessory | Magic Trackpad | NT$ 4,500 |

### 5.3 BTS 主商品與優惠清單

| 主商品 | 一般售價 | BTS 價格 | 贈品補貼上限 | 可選贈品 |
|---|---:|---:|---:|---|
| MacBook Air | NT$ 35,900 | NT$ 31,400 | NT$ 5,990 | AirPods 4、AirPods Pro 3、Magic Trackpad |
| MacBook Pro | NT$ 54,900 | NT$ 50,400 | NT$ 5,990 | AirPods 4、AirPods Pro 3、Magic Trackpad |
| iMac | NT$ 42,900 | NT$ 38,400 | NT$ 5,990 | AirPods 4、AirPods Pro 3、Magic Trackpad |
| Mac mini | NT$ 19,900 | NT$ 18,400 | 無 | 無 |
| iPad Pro | NT$ 32,900 | NT$ 28,400 | NT$ 4,500 | AirPods 4、Apple Pencil Pro |
| iPad Air | NT$ 19,900 | NT$ 18,400 | NT$ 4,500 | AirPods 4、Apple Pencil Pro |
| iPad | NT$ 11,900 | NT$ 10,400 | NT$ 4,500 | AirPods 4、Apple Pencil (USB-C) |
| iPad mini | NT$ 15,900 | NT$ 14,400 | NT$ 4,500 | AirPods 4、Apple Pencil (USB-C) |

### 5.4 預先建立的 member 測試資料

目前只預先建立一筆已過期教育資格的測試 member：

| 會員名稱 | 驗證信箱 | 狀態 | 用途 |
|---|---|---|---|
| `bts-expired-user` | `expired-user@campus.edu.tw` | Verified，但已過期 | 驗證資格過期情境 |

## 6. 對這份 seed 的解讀方式

可以把目前這份資料理解成：

- 它是一份 **人工整理的 AppleBTS 展示用資料快照**
- 它足夠支援：
  - catalog 顯示
  - 教育資格驗證
  - gift 選擇
  - discount / hint
  - storefront / API / compose 驗證
- 但它不是正式商品主檔，也不是 Apple 官網的完整鏡像資料

若後續 seed 規模變大或要讓營運資料可維護，再考慮把這份資料從 code 抽到外部檔案格式。
