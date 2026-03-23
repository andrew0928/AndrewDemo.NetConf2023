# Discount 與 Cart 邊界重新檢討

## 目的

這份文件是依照目前最新原則，重新檢查現有 Phase 1 設計是否過度設計、是否把 contract 放錯層。

本次 review 的前提是：

- `discount` domain 只負責「試算折扣」
- `discount` domain 的直接 contract 只保留：
  - `IDiscountEngine`
  - `IDiscountRule`
  - `DiscountRecord`
- 購物車內容、消費者身分、商店啟動結果，應視為 `cart` domain 或更外層 orchestration 的責任
- contract 只放必要設計，可有可無的一律先移除

## 現況問題

### 1. `DiscountContracts` 目前仍然超出 discount domain 邊界

目前 [DiscountContracts.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Abstract/Discounts/DiscountContracts.cs) 除了 `IDiscountEngine`、`IDiscountRule`、`DiscountRecord` 以外，還包含：

- `DiscountEvaluationContext`
- `DiscountConsumerSnapshot`
- `CartLineItem`

這代表 discount contract 不只描述「折扣規則」，還開始描述：

- 購物車內有什麼
- 消費者是誰
- 該用哪種 context 傳資料

這些都不是 discount domain 必須擁有的核心概念。

### 2. `DefaultDiscountEngine` 仍直接依賴 `IShopRuntimeContext`

目前 [DefaultDiscountEngine.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Discounts/DefaultDiscountEngine.cs) 會直接讀取：

- `IShopRuntimeContext`
- `Manifest.EnabledDiscountRuleIds`

這代表 discount engine 不只是「執行規則」，還順便承擔了：

- 目前是哪個 shop
- 該 shop 啟用了哪些規則

這會讓 engine 同時扮演：

- 折扣執行器
- shop runtime 感知元件
- rule selector

這三個責任混在一起，邊界過重。

### 3. `DiscountEvaluationContextFactory` 是一個補洞用的工廠，不是必要抽象

目前 [DiscountEvaluationContextFactory.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Discounts/DiscountEvaluationContextFactory.cs) 的存在原因是：

- `Cart`
- `Member`
- `IShopDatabaseContext`
- `shopId`

這些資料散落在不同地方，所以需要一個 factory 在結帳前臨時拼出 `DiscountEvaluationContext`。

這不是一個有獨立語意的 domain service，比較像是因為 contract 邊界放錯，才多長出來的 adapter。

### 4. `Cart` domain 與 `Discount` contract 目前同時各自定義 line item

目前：

- [Cart.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Cart.cs) 有 `Cart.CartLineItem`
- [DiscountContracts.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Abstract/Discounts/DiscountContracts.cs) 有 `CartLineItem`

兩者名稱接近，但一個是購物車內部項目，一個是折扣 contract 專用項目。

這通常表示目前的 abstraction boundary 不乾淨：

- 若 discount domain 真的需要自己的 line item 契約，表示它在 process boundary 外
- 若 discount domain 只是同一個 solution 內的規則計算，則重複定義通常沒有必要

### 5. Controller 與 Cart 仍直接參與 discount orchestration

目前：

- [CartsController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CartsController.cs) 直接拿 `IShopRuntimeContext` 與 `IDiscountEngine` 做試算
- [CheckoutController.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs) 自己建立折扣明細
- [Cart.cs](/Users/andrew/code-work/andrewshop.apidemo/src/AndrewDemo.NetConf2023.Core/Cart.cs) 內含 `EstimateDiscounts` / `EstimatePrice`

這表示目前沒有一個明確的 `CartContext` 或 cart pricing orchestration 來承接：

- 商品快照整理
- 消費者身分整理
- 啟用規則整理
- 原價 / 折扣 / 總額組裝

## 重新整理後的最小設計

### Discount domain 應只保留

- `IDiscountEngine`
- `IDiscountRule`
- `DiscountRecord`

它的責任只有：

- 接受某個 cart-side context
- 執行已註冊且已啟用的 `IDiscountRule`
- 回傳負項折扣明細 `DiscountRecord[]`

它不應直接知道：

- 資料庫怎麼查
- shop runtime 怎麼解析
- cart 是怎麼建立的
- controller 怎麼輸出 HTTP response

### Cart domain 應持有試算輸入

建議將下列概念移回 cart domain：

- `LineItem`
- `CartContext`
- 消費者資訊
- 商店啟用規則資訊

也就是說，未來真正代表「立即結帳時的購物清單狀態」的物件，應該是 `CartContext`，而不是 `DiscountEvaluationContext`。

### Discount engine 應只吃 cart-side contract

若 Phase 1 只求最小必要設計，建議往這個方向收斂：

```csharp
public interface IDiscountEngine
{
    IReadOnlyList<DiscountRecord> Evaluate(CartContext context);
}

public interface IDiscountRule
{
    string RuleId { get; }
    int Priority { get; }
    IReadOnlyList<DiscountRecord> Evaluate(CartContext context);
}
```

重點不是 `CartContext` 一定要叫這個名字，而是：

- 試算輸入應該屬於 cart domain
- discount domain 只依賴它，不重新定義一套 discount 專用 context

### 是否需要 `CartContextSnapshot`

目前看不到 Phase 1 必須保留 `CartContextSnapshot` 的理由。

只有在下列情境，才值得另外切 snapshot contract：

1. 折扣規則將來要在獨立 process / 獨立服務中執行
2. discount plugin 將來不允許直接相依 cart domain assembly
3. 試算需要把 cart state 序列化成固定 payload，跨程序或跨版本傳輸

若目前規劃仍是：

- 同一個 solution
- 同一個 process
- 啟動時載入 rule assemblies

那麼 `CartContextSnapshot` 多半不是必要設計，可以先移除。

## 建議的責任切分

### `IShopRuntimeContext`

不建議讓 `IDiscountEngine` 直接依賴它。

比較合理的位置是：

- API 啟動階段
- cart context 建立階段
- application/cart service orchestration

也就是說，`shop runtime` 應先被轉譯成 cart-side 可用的資訊，再交給 discount engine，而不是讓 engine 自己去抓 runtime。

### `IShopDatabaseContext`

不建議出現在 discount contract。

它可以存在於：

- cart context builder
- cart service
- checkout service

但不應存在於 discount domain 的公開契約中。

### `DiscountEvaluationContextFactory`

若未來引入 `CartContext`，這個 factory 應該：

- 消失

或改名並改責任成：

- `CartContextBuilder`

這樣語意才正確。

## 對 Phase 1 的直接結論

### 應移除或下沉

- `DiscountEvaluationContext`
- `DiscountConsumerSnapshot`
- `DiscountContracts` 內的 `CartLineItem`
- `DiscountEvaluationContextFactory`
- `DefaultDiscountEngine -> IShopRuntimeContext` 的直接相依

### 應保留

- `IDiscountEngine`
- `IDiscountRule`
- `DiscountRecord`
- `ShopManifest`
- `IShopManifestResolver`

### 建議新引入但放在 cart/application 邊界

- `CartContext`
- `CartContextBuilder` 或等價 service
- cart pricing / checkout orchestration service

## 建議採用的下一步

1. 先在 Phase 1 freeze 前，把 `.Abstract` 中 discount contract 收縮到最小集合。
2. 將試算輸入模型移到 cart domain，至少先用單一 `CartContext` 承接。
3. 讓 rule enablement 在 engine 外決定，或把 enablement 資訊內聚到 `CartContext`。
4. 等折扣邊界乾淨後，再往後定義 ProductService plugin 邊界，否則後面會再複製一次同樣的混亂。
