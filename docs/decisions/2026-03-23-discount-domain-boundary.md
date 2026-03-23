# Discount Domain 與 Cart Domain 邊界

## 狀態

- accepted
- 日期：2026-03-23

## 背景

在 Phase 1 初稿中，折扣相關 contract 除了 `IDiscountRule`、`DiscountRecord` 之外，還包含：

- `DiscountEvaluationContext`
- `DiscountConsumerSnapshot`
- `CartLineItem`

同時 `DefaultDiscountEngine` 也直接依賴 `IShopRuntimeContext` 來選擇啟用的規則。

這讓 discount domain 同時承擔：

- 折扣規則執行
- 購物車資料輸入格式定義
- 消費者資料投影
- shop runtime 感知
- rule enablement 選擇

職責過重，且違反 contract 只放必要設計的原則。

## 決策

### 1. Discount contract 只保留兩個公開 contract 與一個輸出模型

- `IDiscountRule`
- `DiscountRecord`

`DiscountEngine` 保留為 `.Core` 內的 concrete service，不放在 `.Abstract`。

discount domain 的責任只限於：

- 根據當下可結帳的購物清單狀態進行折扣試算
- 回傳負項折扣明細

### 2. 購物車與消費者相關資料改歸 Cart domain

下列概念不再視為 discount domain 的公開 contract：

- `LineItem`
- `CartContext`
- 消費者身分或會員投影
- shop runtime 轉譯後的啟用規則資訊

這些概念應歸在 cart domain 或更外層 orchestration service。

### 3. Discount engine 不直接依賴 runtime 與 database

`DiscountEngine` / `IDiscountRule` 不應直接依賴：

- `IShopRuntimeContext`
- `IShopDatabaseContext`

shop runtime 與 database 的使用位置應往外移到：

- cart context 建立階段
- cart service
- checkout service

### 4. 非必要 snapshot contract 先移除

除非出現以下情境，否則不建立 `CartContextSnapshot` 一類的額外 contract：

- 跨 process 執行折扣規則
- discount plugin 不允許直接相依 cart domain assembly
- 需要固定的序列化 payload 跨服務傳遞

目前 Phase 1 不符合以上條件，因此 snapshot contract 不列入必要設計。

## 影響

- `.Abstract` 的 discount contract 需要收縮
- `DiscountEvaluationContextFactory` 應移除或轉型為 `CartContextFactory`
- `DefaultDiscountEngine` 需要移除對 `IShopRuntimeContext` 的直接依賴，並簡化為 `DiscountEngine`
- `Cart`、試算流程、checkout orchestration 需要新增 cart-side input model
- `spec` 與測試案例需改寫成 cart context 驅動的折扣試算描述

### 5. `LineItem` 在 `Cart` 與 `CartContext` 間共用

- `LineItem` 改為唯讀型別
- raw `Cart.LineItems` 可只帶 `ProductId`、`Quantity`
- `CartContext.LineItems` 由 `CartContextFactory` 補齊 `ProductName`、`UnitPrice`

## 替代方案

### 替代方案 A：保留 `DiscountEvaluationContext`

優點：

- 目前改動較小

缺點：

- 折扣 domain 持續持有不屬於自己的輸入模型
- 容易再複製出更多 discount 專用 snapshot 類型

結論：

- 不採用

### 替代方案 B：讓 discount engine 繼續直接讀 `IShopRuntimeContext`

優點：

- engine 可自行決定啟用規則

缺點：

- engine 同時承擔 rule execution 與 runtime selection
- 之後 testing 與替換 cart orchestration 會更混亂

結論：

- 不採用

## 後續工作

1. 在 Phase 1 freeze 前調整 `.Abstract` 的 discount contract。
2. 補一份 cart-side 試算輸入模型草案。
3. 將既有 sequence diagram 與 spec 改寫為 cart context 驅動版本。
