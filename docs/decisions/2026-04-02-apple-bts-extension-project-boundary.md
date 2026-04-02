# AppleBTS Extension 最小邊界與骨架

## 狀態

- accepted
- 日期：2026-04-02

## 背景

Apple BTS 已確認不是獨立 `ShopId`，而是同一個 shop 內的限期 campaign。`.Core` 已承接通用的 cart、SKU、inventory 與 checkout correctness，因此 BTS 不應再把通用責任帶回 extension。

另外，本輪已確認：

- `Product.Price` 維持一般售價
- `bts-price` 與 gift subsidy 落在 sidecar
- `BtsDiscountRule` 透過 `DiscountRecord` 輸出價差與 hint
- gift line 是否為 BTS 組合，改以 `ParentLineId` 表達，不再追蹤「是否從 BTS 入口進來」的 cart provenance
- `DefaultProductService` 足以作為主商品服務，不需要另做 `AppleBtsProductService`

## 決策

- 擴充專案名稱定為 `AndrewDemo.NetConf2023.Extension.AppleBTS`
- 第一版只建立可編譯的 skeleton，不直接實作業務邏輯
- 第一版 skeleton 採 concrete-first，不為 extension 內部 repository / service 額外建立 interface 配對
- `AppleBTS Extension` 只擁有以下責任：
  - member `.edu` 驗證 sidecar
  - BTS campaign / offer / gift option sidecar
  - BTS qualification、offer lookup、catalog query、admin 設定的 service 邊界
  - `BtsDiscountRule`，作為接入 `.Core` `DiscountEngine` 的主要入口
- `SKU`、inventory、通用 cart、通用 checkout 仍屬於 `.Core`
- 最小 class 集合為：
  - `BtsDiscountRule`
  - `BtsOfferRepository`
  - `MemberEducationVerificationRepository`
  - `MemberEducationQualificationService`
  - `AppleBtsCatalogService`
  - `AppleBtsAdminService`
- 以下 class 不保留在新骨架：
  - `BtsDiscountEvaluator`
  - `BtsOfferService`
  - `BtsCheckoutValidationService`
  - `BtsCartEntryService`
  - `BtsCampaignRepository`
  - `BtsCartLineRepository`
  - `BtsCartLineRecord`

## 影響

- 可先用 skeleton code 與 unit test 檢視 extension boundary，不必一次做完 BTS 業務實作
- 後續 host 若要啟用 AppleBTS，主要接點會是：
  - DI 註冊 `AndrewDemo.NetConf2023.Extension.AppleBTS`
  - 啟用 `BtsDiscountRule`
  - BTS 專屬頁面以 `AppleBtsCatalogService` 讀取 campaign / offer / gift option
  - BTS 設定介面以 `AppleBtsAdminService` 寫入 sidecar
- 一般 cart 與 checkout 流程完全沿用 `.Core`
- 若 gift line 需要被視為 BTS 組合，host 應在加入 gift 時補上 `ParentLineId`

## 替代方案

### 1. 直接在 `.Core` 實作 BTS 規則

不採用。這會把 campaign-specific 規則污染進通用 checkout / cart domain。

### 2. 讓 AppleBTS 直接取代 `IProductService`

不採用。BTS 目前更像 campaign rule，而不是整個 product domain 的 owner。

### 3. 追蹤 BTS 入口 provenance 或獨立 cart-line sidecar

不採用。這會增加 cart 與 extension 的耦合；目前以 `ParentLineId` 表達 gift relation 已足夠支撐折扣計算。

## 後續工作

- 補 `AndrewDemo.NetConf2023.Extension.AppleBTS` 的正式 spec 與 testcases
- 補最小骨架各 repository / service / rule 的實作
- 最後再決定 host / API 是否需要新入口
