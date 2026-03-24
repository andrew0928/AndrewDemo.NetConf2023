# Testcase Overview

這裡整理的是我認為最能代表 commit `cb466d49cc2f971fd20917f397ac8ab7bdd99c08` 的 4 條主案例。

## 挑選原則

- 優先涵蓋 API 的主流程：登入、會員識別、購物車、結帳。
- 優先選擇有實作入口、HTTP sample 或測試程式可交叉驗證的情境。
- `seed init container` 與 `DatabaseInit` 屬於部署初始化流程，主要放在 C4 文件中說明，不另外拆成 testcase。

## 一覽表

| ID | 情境 | 主要證據 | 備註 |
| --- | --- | --- | --- |
| TC-01 | OAuth2-like 登入與 access token 交換 | `LoginController.cs`, `signin.html`, `oauth2.http` | 驗證授權碼與 token 交換流程 |
| TC-02 | 會員辨識與商店註記更新 | `MemberController.cs`, `MemberPersistenceTests.cs` | 驗證 token -> member 的解析與 notes 持久化 |
| TC-03 | 建立購物車、加入商品、試算折扣 | `CartsController.cs`, `Cart.cs`, `DiscountEngine.cs`, `CartPersistenceTests.cs` | 驗證 cart persistence 與估價規則 |
| TC-04 | 建立 checkout transaction、完成結帳、查詢訂單 | `CheckoutController.cs`, `Order.cs`, `AndrewDemo.NetConf2023.API.http` | 驗證 order 生成與 member order history |

## 文件連結

- [TC-01 OAuth2-like 登入與 access token 交換](./tc01-oauth2-login-and-token.md)
- [TC-02 會員辨識與商店註記更新](./tc02-member-profile-and-shop-notes.md)
- [TC-03 建立購物車、加入商品、試算折扣](./tc03-cart-add-items-and-estimate.md)
- [TC-04 建立 checkout transaction、完成結帳、查詢訂單](./tc04-checkout-and-order-history.md)

## 補充

- `ProductPersistenceTests.cs` 沒有單獨拆成 testcase，因為它主要支撐 TC-03 與 TC-04 的商品資料存在前提。
- 這版沒有 API 層級的自動化測試；很多流程只能從 controller 程式與 `.http` sample 交叉比對。
