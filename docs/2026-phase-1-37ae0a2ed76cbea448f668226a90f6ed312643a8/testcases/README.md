# Testcase Overview

這裡整理的是 phase 1 這個 commit 最具代表性的 4 條 testcase。

## 挑選原則

- 優先選擇 phase 1 新增或重構的行為，而不是單純沿用 phase 0 的會員查詢與 OAuth 基本流程。
- 以 `spec/testcases/` 為主，再用實作與 `Core.Tests` 驗證是否真的落地。
- testcase 的重點是 phase 1 的邊界變化：`shop runtime`、`CartContext + DiscountRule`、`IProductService`、`order fulfillment`。

## 一覽表

| ID | 情境 | 對應規格 | 主要程式 |
| --- | --- | --- | --- |
| TC-P1-01 | 啟動時解析 `ShopManifest` 並組裝 runtime | `TC-RT-001 ~ 004`, `TC-PR-001 ~ 002` | `Program.cs`, `ConfigurationShopManifestResolver.cs` |
| TC-P1-02 | 由 `CartContextFactory` 與 `DiscountEngine` 進行折扣試算 | `TC-CT-001 ~ 002`, `TC-DC-001 ~ 004`, `TC-API-001` | `CartContextFactory.cs`, `DiscountEngine.cs`, `Product1SecondItemDiscountRule.cs`, `CartsController.cs` |
| TC-P1-03 | `IProductService` 列 published product，並驗證加入購物車的商品 | `TC-PR-101 ~ 103`, `TC-PR-201 ~ 202` | `ProductsController.cs`, `CartsController.cs`, `DefaultProductService.cs` |
| TC-P1-04 | 完成 checkout，建立 product event 與 fulfillment status | `TC-API-002`, `TC-PR-301 ~ 305` | `CheckoutController.cs`, `Order.cs`, `ProductOrderEventFactory.cs` |

## 文件連結

- [TC-P1-01 啟動時解析 ShopManifest 並組裝 runtime](./tc-p1-01-shop-runtime-bootstrap.md)
- [TC-P1-02 CartContext 與 DiscountEngine 折扣試算](./tc-p1-02-cart-context-and-discount-engine.md)
- [TC-P1-03 IProductService 商品查詢與購物車驗證](./tc-p1-03-product-service-and-cart-validation.md)
- [TC-P1-04 Checkout、Product Event 與 Fulfillment Status](./tc-p1-04-checkout-fulfillment-and-product-event.md)

## 補充

- OAuth / member 基本流程在 phase 1 沒有本質性改寫，因此不再重複拆成主要 testcase。
- 若你後續要做 phase 1 freeze review，我會把 `review-notes.md` 裡的落差一併視為這些 testcase 的殘餘風險。
