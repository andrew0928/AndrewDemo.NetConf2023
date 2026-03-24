# 2026 Phase 0 文件集

這份目錄是依照 commit `cb466d49cc2f971fd20917f397ac8ab7bdd99c08` 反推整理的版本文件，目的是先把當時的系統結構、主要業務情境與已知落差固定下來，方便你 review 後再繼續往下一個 commit hash 比對。

## 範圍與判讀原則

- 觀察來源以該 commit 的 source code、`README`、`compose`、`API.http`、`oauth2.http`、`Core.Tests` 為主。
- 若註解、README、HTTP sample 與實作不一致，這份文件一律以實際程式碼行為為準。
- 發現的不一致與風險，另外整理在 [review-notes.md](./review-notes.md)。
- 這是一份 phase-0 reverse engineering 文件，不回填後續 Phase 1 / Phase 2 的 contract 與命名。

## 文件索引

- [c4-model.md](./c4-model.md)
- [testcases/README.md](./testcases/README.md)
- [review-notes.md](./review-notes.md)

## 這版系統摘要

- `AndrewDemo.NetConf2023.API` 是主要 HTTP 入口，包含 OAuth2-like 登入、商品查詢、購物車、會員與結帳 API。
- `AndrewDemo.NetConf2023.ConsoleUI` 是另一個互動入口，整合 `Semantic Kernel` 與 Azure OpenAI，讓使用者用命令列或 copilot 方式操作商店。
- `AndrewDemo.NetConf2023.Core` 提供共同的 domain model、LiteDB context、折扣邏輯與 checkout waiting room。
- `AndrewDemo.NetConf2023.DatabaseInit` 會產生初始 `shop-database.db`；`src/seed` 則把這份 snapshot 複製到 compose / ACA 的 shared volume。
- 主要折扣規則只有一條：商品 `Id = 1` 的「第二件六折」。

## 主要 testcase 一覽

| ID | 情境 | 主要模組 | 文件 |
| --- | --- | --- | --- |
| TC-01 | OAuth2-like 登入與 access token 交換 | `LoginController`, `Member`, `MemberAccessTokenRecord` | [TC-01](./testcases/tc01-oauth2-login-and-token.md) |
| TC-02 | 會員辨識與商店註記更新 | `MemberController`, `Member`, `MemberAccessTokenRecord` | [TC-02](./testcases/tc02-member-profile-and-shop-notes.md) |
| TC-03 | 建立購物車、加入商品、試算折扣 | `CartsController`, `Cart`, `DiscountEngine`, `Product` | [TC-03](./testcases/tc03-cart-add-items-and-estimate.md) |
| TC-04 | 建立 checkout transaction、完成結帳、查詢訂單 | `CheckoutController`, `Order`, `CheckoutTransactionRecord`, `MemberController` | [TC-04](./testcases/tc04-checkout-and-order-history.md) |

## 這版文件的使用方式

建議閱讀順序：

1. 先看 [c4-model.md](./c4-model.md) 了解這版 system / project / runtime 的切分。
2. 再看 [testcases/README.md](./testcases/README.md) 了解我挑選哪些情境代表這個版本。
3. 最後看 [review-notes.md](./review-notes.md) 判斷哪些差異是這版實作本身的問題，哪些只是文件與程式碼沒有同步。
