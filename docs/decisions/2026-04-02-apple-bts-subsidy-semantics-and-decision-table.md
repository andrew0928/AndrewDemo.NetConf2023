# Apple BTS 贈品補貼語意與情境決策表

## 狀態

- accepted
- 日期：2026-04-02

## 背景

AppleBTS 在先前討論中，主商品 `bts-price`、gift option、gift 上限與 cart relation 已大致收斂，但「gift 金額到底是折扣、補貼，還是可轉移額度」仍有歧義。

若這一點不先定清楚，後續實作 `BtsDiscountRule` 時會出現兩種完全不同的行為：

1. 把 gift 的未使用額度轉移到主商品
2. 只把 gift 補到 `0`，未使用額度直接作廢

這兩種行為對結帳結果完全不同，必須先定案。

## 決策

- `BtsMainOfferRecord` 的欄位命名定為 `MaxGiftSubsidyAmount`
- `MaxGiftSubsidyAmount` 代表主商品對其 gift 的補貼上限
- 這個補貼只作用在 gift line
- gift 最低只補到 `0`
- 未使用的 gift subsidy 額度不得轉移到主商品
- 若使用者不選 gift，主商品除了 `BtsPrice` 之外，不再有額外折扣
- `BtsGiftOptionRecord` 只負責列出 gift options，不再保存金額欄位

gift subsidy 公式定義為：

- `GiftSubsidy = min(MainOffer.MaxGiftSubsidyAmount, Gift.UnitPrice)`

主商品成交價定義為：

- `MainProductFinalPrice = MainOffer.BtsPrice`

## 完整情境決策表

| ID | 類型 | 情境 | 決策結果 |
|---|---|---|---|
| `M-01` | Main | Campaign=Y, Qualification=Y, MainOffer=Y | 主商品採 `BtsPrice` |
| `M-02` | Main | Campaign=Y, Qualification=N, MainOffer=Y | 主商品回原價，回 `Hint` |
| `M-03` | Main | Campaign=N, Qualification=-, MainOffer=- | 整套 BTS 不啟用，主商品回原價，可回 `Hint` |
| `M-04` | Main | Campaign=Y, Qualification=Y, MainOffer=N | 主商品回原價 |
| `G-01` | Gift | 主商品已成立 BTS，有 GiftGroup，未選 gift | 主商品只採 `BtsPrice`，gift subsidy = 0 |
| `G-02` | Gift | 主商品已成立 BTS，gift 有 `ParentLineId`，且屬於合法 group | `gift subsidy = min(MaxGiftSubsidyAmount, GiftPrice)` |
| `G-03` | Gift | gift 無 `ParentLineId` | 不成立 gift subsidy |
| `G-04` | Gift | gift 有 `ParentLineId`，但不在合法 group | 不成立 gift subsidy |
| `G-05` | Gift | 主商品沒有 `GiftGroupId` | gift 邏輯全部忽略，gift subsidy = 0 |
| `P-01` | Pricing | `macbook-air + airpods4`，cap=5990，gift=5990 | `31400 + 5990 - 5990 = 31400` |
| `P-02` | Pricing | `macbook-air + apple-pencil`，cap=5990，gift=4500 | `31400 + 4500 - 4500 = 31400` |
| `P-03` | Pricing | `macbook-air + airpods-pro-3`，cap=5990，gift=7990 | `31400 + 7990 - 5990 = 33400` |
| `P-04` | Pricing | `macbook-air` 不選 gift | 主商品成交價 = `BtsPrice`，不得移轉未使用補貼 |
| `C-03` | Corner | checkout / estimate 時活動過期 | 主商品回原價，gift subsidy 失效，回 `Hint` |
| `C-04` | Corner | checkout / estimate 時資格過期 | 主商品回原價，gift subsidy 失效，回 `Hint` |
| `C-05` | Corner | 同一主商品選超過 1 個 gift，`MaxGiftQuantity = 1` | 回 `Hint` |
| `C-06` | Corner | 主商品只有特價沒有贈品 | 只套 `BtsPrice`，不進 gift 邏輯 |

補充：

- `C-01` 主商品移除時連帶移除子商品，已回歸 `.Core` 的 cart 規格，因此不列入 AppleBTS 專屬 decision table

## 影響

- `BtsDiscountRule` 的實作必須先判定主商品是否成立 BTS，再判定 gift subsidy
- `BtsOfferRepository` 需能回傳：
  - active campaign
  - main offer
  - gift options
- unit tests 應直接依上述 decision table 展開，不再自行發明額外金額語意

## 替代方案

### 1. 把 gift 未使用額度轉移到主商品

不採用。這會讓主商品除了 `BtsPrice` 之外還有第二層額外折扣，和目前業務規則不符。

### 2. 把 gift 金額上限掛在 `BtsGiftOptionRecord`

不採用。使用者已確認 gift 補貼上限跟著主商品或 gift group 走，不跟著單一 gift option 走。

## 後續工作

1. 以這張 decision table 為基準展開 unit tests。
2. `BtsDiscountRule` 與 repository implementation 完成後，tests 必須逐列對應驗證。
