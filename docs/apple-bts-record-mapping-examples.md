# Apple BTS Record 關係與案例對應

## 狀態

- confirmed
- 日期：2026-04-02

## 目的

這份文件只解釋目前 AppleBTS extension 最小設計下，`*Record` 之間的關係，以及它們如何表達實際業務案例。

本文件不處理：

- class diagram
- sequence diagram
- controller / API 設計
- 具體折扣演算法實作細節

## 目前保留的 Record

### 1. `BtsCampaignRecord`

代表一檔 BTS 活動本身。

用途：

- 定義活動名稱
- 定義活動起訖時間
- 定義活動是否啟用

關鍵欄位：

- `CampaignId`
- `Name`
- `StartAt`
- `EndAt`
- `IsEnabled`

### 2. `BtsMainOfferRecord`

代表「某個主商品」在某檔 BTS 活動中的優惠設定。

用途：

- 指出哪個 `ProductId` 是 BTS 主商品
- 定義該主商品的 `BtsPrice`
- 指出它可搭配哪個贈品群
- 限制最多可搭配幾個贈品
- 定義該主商品搭配贈品時的最高補貼金額

關鍵欄位：

- `OfferId`
- `CampaignId`
- `MainProductId`
- `BtsPrice`
- `GiftGroupId`
- `MaxGiftQuantity`
- `MaxGiftSubsidyAmount`

目前語意：

- `MaxGiftQuantity = 1` 代表「最多只能選 1 個贈品」
- `MaxGiftSubsidyAmount` 代表「這個主商品所搭配贈品的補貼上限」
- 這個補貼只作用在贈品 line
- 贈品最低只會被補貼到 `0`
- 若贈品原價低於補貼上限，差額直接作廢，不可轉移到主商品
- 若主商品只有特價、沒有贈品，則 `GiftGroupId = null`

### 3. `BtsGiftOptionRecord`

代表某個贈品群中的一個可選贈品。

用途：

- 指出該贈品群有哪些可選商品

關鍵欄位：

- `OptionId`
- `CampaignId`
- `GiftGroupId`
- `GiftProductId`

### 4. `MemberEducationVerificationRecord`

代表 member 是否具備有效 `.edu` 驗證。

用途：

- 定義 member 是否有 BTS 資格
- 定義資格的有效期限

關鍵欄位：

- `VerificationId`
- `MemberId`
- `Email`
- `Status`
- `VerifiedAt`
- `ExpireAt`
- `Source`

## Record 之間的關係

### 活動設定關係

- `BtsCampaignRecord` 1 對多 `BtsMainOfferRecord`
- `BtsMainOfferRecord` 0 或 1 個 `GiftGroupId`
- `GiftGroupId` 1 對多 `BtsGiftOptionRecord`

也就是說：

- 一檔活動下面可以有多個主商品
- 每個主商品最多只對應一個贈品群
- 每個贈品群下面可以有多個贈品選項
- 但該主商品實際最多只能選 1 個贈品，這由 `MaxGiftQuantity = 1` 表達

### 資格關係

- `MemberEducationVerificationRecord` 不直接掛在 `CampaignId`
- 它是 member 自己的資格 sidecar
- BTS 折扣是否成立時，再同時看：
  - member 驗證是否有效
  - campaign 是否仍在活動時間內
  - main product / gift option 是否對得上

## 實際案例對應

以下先用同一檔活動 `bts-2026` 說明。

---

## 案例

需求：

- 主商品是 `macbook-air`
- `BtsPrice = 31400`
- 該主商品最多只能選 1 個贈品
- 該主商品的贈品補貼上限是 `5990`
- 可選贈品有：
  - `airpods-4`
  - `apple-pencil`

### 需要建立的設定 records

#### 1. 活動 record

```json
{
  "CampaignId": "bts-2026",
  "Name": "Apple BTS 2026",
  "StartAt": "2026-07-01T00:00:00Z",
  "EndAt": "2026-09-30T23:59:59Z",
  "IsEnabled": true
}
```

#### 2. 主商品 offer record

```json
{
  "OfferId": "offer-macbook-air",
  "CampaignId": "bts-2026",
  "MainProductId": "macbook-air",
  "BtsPrice": 31400,
  "GiftGroupId": "gift-group-macbook-air",
  "MaxGiftQuantity": 1,
  "MaxGiftSubsidyAmount": 5990
}
```

說明：

- `MainProductId = "macbook-air"` 代表這個公開 `Product`
- `BtsPrice = 31400` 代表主商品的 BTS 特價
- `GiftGroupId = "gift-group-macbook-air"` 代表它可以搭配某一組贈品
- `MaxGiftQuantity = 1` 代表最多只能選 1 個贈品
- `MaxGiftSubsidyAmount = 5990` 代表這個主商品搭配贈品時，補貼上限是 `5990`
- 這個補貼只用來把贈品售價往下補，最低到 `0`

#### 3. 贈品選項 records

```json
{
  "OptionId": "gift-option-macbook-air-airpods4",
  "CampaignId": "bts-2026",
  "GiftGroupId": "gift-group-macbook-air",
  "GiftProductId": "airpods-4"
}
```

```json
{
  "OptionId": "gift-option-macbook-air-apple-pencil",
  "CampaignId": "bts-2026",
  "GiftGroupId": "gift-group-macbook-air",
  "GiftProductId": "apple-pencil"
}
```

說明：

- `airpods-4` 與 `apple-pencil` 都是同一個主商品的 gift options
- 真正的「最高折 `5990`」不跟在 gift option，而是跟在主商品 offer

### 實際情境 1

購物車內容：

- `macbook-air`
- `airpods-4`

runtime cart lines：

- 主商品 line
  - `ProductId = "macbook-air"`
- 贈品 line
  - `ProductId = "airpods-4"`
  - `ParentLineId = 主商品 line 的 LineId`

結帳語意：

- 主商品金額：`31400`
- 贈品原價：`5990`
- 贈品補貼：`min(5990, 5990) = 5990`
- 最終結果：`31400 + 5990 - 5990 = 31400`

### 實際情境 2

購物車內容：

- `macbook-air`
- `apple-pencil`

runtime cart lines：

- 主商品 line
  - `ProductId = "macbook-air"`
- 贈品 line
  - `ProductId = "apple-pencil"`
  - `ParentLineId = 主商品 line 的 LineId`

結帳語意：

- 主商品金額：`31400`
- 贈品原價：`4500`
- 贈品補貼：`min(5990, 4500) = 4500`
- 最終結果：`31400 + 4500 - 4500 = 31400`

### 實際情境 3

購物車內容：

- `macbook-air`
- 不選贈品

結帳語意：

- 主商品金額：`31400`
- 贈品補貼：`0`
- 最終結果：`31400`

## 這個案例的關鍵結論

- `MaxGiftSubsidyAmount` 跟著 `macbook-air` 主商品走
- `airpods-4` 與 `apple-pencil` 只是同一個 gift group 底下的可選商品
- 真正的 gift 補貼公式應是：
  - `min(MainOffer.MaxGiftSubsidyAmount, GiftProduct.UnitPrice)`
- 補貼只作用在 gift line，不可移轉到主商品
- 若沒有選 gift，主商品除了 `BtsPrice` 之外，不再有額外折扣

## 若主商品只有特價沒有贈品，怎麼表達

例如 `mac-mini` 只有 BTS 特價，沒有贈品：

```json
{
  "OfferId": "offer-mac-mini",
  "CampaignId": "bts-2026",
  "MainProductId": "mac-mini",
  "BtsPrice": 16400,
  "GiftGroupId": null,
  "MaxGiftQuantity": 1,
  "MaxGiftSubsidyAmount": null
}
```

目前建議語意：

- `GiftGroupId = null` 就代表「沒有贈品群」
- 此時不需要任何 `BtsGiftOptionRecord`
- `MaxGiftQuantity` 可保留預設值，但實際上不會被使用

若你希望資料更明確，也可以在實作時加上規則：

- `GiftGroupId = null` 時，視同 `MaxGiftQuantity = 0`

## 目前這版設計的重點

- `BtsCampaignRecord` 管活動
- `BtsMainOfferRecord` 管主商品與 BTS 價格
- `BtsMainOfferRecord.MaxGiftSubsidyAmount` 管主商品對應的贈品補貼上限
- `BtsGiftOptionRecord` 只管贈品選項有哪些商品可選
- `MemberEducationVerificationRecord` 管 member 資格
- gift 的「已被選取」不是額外的 extension record
- gift 是否成立，是由 cart line 的 `ParentLineId` 加上上述 sidecar records 共同判定
