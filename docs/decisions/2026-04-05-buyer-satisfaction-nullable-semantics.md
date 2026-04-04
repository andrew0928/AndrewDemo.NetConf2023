# BuyerSatisfaction 改為 Nullable 語意

## 狀態

- accepted
- 日期：2026-04-05

## 背景

在 `CommonStorefront` 實作與驗證過程中，發現 checkout contract 目前將 `Satisfaction` 視為非 nullable 整數，因此 storefront 若不提供這個欄位，就只能以 `0` 代替。

但這會混淆兩種完全不同的語意：

- `null`：沒有判讀、沒有來源、沒有提供
- `0`：有明確判讀，而且是最低分

這不只是 `CommonStorefront` UI 的問題，而是 `.Core` 訂單資料語意本身過於粗糙。

本決策明確標記為：

- `重大決策`
- `影響 .Core`

## 決策

- `CheckoutCompleteCommand.Satisfaction` 改為 `int?`
- `Order.OrderShopNotes.BuyerSatisfaction` 改為 `int?`
- API request DTO 的 `Satisfaction` 改為 `int?`
- `null` 的 canonical 語意為：
  - 沒有提供
  - 沒有判讀
  - 不適用於該流程
- `0` 的 canonical 語意為：
  - 已明確判讀
  - 且結果是最低分
- `CommonStorefront` checkout 第一版固定送出：
  - `Satisfaction = null`
  - `ShopComments = null`
- `ConsoleUI` 或未來 AI 對話流程若有明確判讀結果，仍可傳入具體分數

## 影響

- `.Core` 的訂單語意更精確，不再把「未判讀」誤記成「極度不滿」
- `CommonStorefront` 可以用正確語意完成送單，而不需要填入假資料
- 後續若要做 AI 對話摘要、客服情緒判讀、體驗回饋分析，資料會更可信

## 替代方案

### 1. 維持 `int`，以 `0` 代表未提供

不採用。這會讓資料分析與後續商業判讀失真。

### 2. 移除 `Satisfaction` 欄位

暫不採用。`ConsoleUI` 與未來對話式流程仍有可能產生有效分數，欄位本身仍有保留價值。

## 後續工作

- 補 core test，驗證未提供滿意度時會正確保存為 `null`
- 後續若有 storefront 或其他 host 顯示訂單備註，需依 nullable 語意顯示
