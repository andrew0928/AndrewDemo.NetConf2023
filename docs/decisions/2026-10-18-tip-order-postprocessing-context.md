# TiP 訂單後置處理的 Transaction Context

## 狀態

accepted，2026-10-18。使用者確認 Phase 2.2 endpoint 清單並授權開始實作。

## 背景

目前只有 Checkout create／complete 涉及單筆交易。使用者要求留下未來訂單出貨等 checkout 後置處理的用途傳遞規則，現階段不建立相關任務。

## 決策

- 目前 `POST /api/checkout/create` 入口沒有 transaction，成功後綁定 Checkout；`POST /api/checkout/complete` 使用 body 的 transactionId，驗證買方後綁定 Checkout。
- 未來若新增 `/api/orders/{id}/xxx`，例如出貨等訂單後置處理，仍必須附帶 current transaction：Kind=Order、Id 為該訂單 ID、IsTest 取自保存的 Order；執行身分不會解除測試訂單的效果限制。
- endpoint 須明確宣告 OrderId 來源並驗證存取權限；若轉為事件／任務處理，獨立 scope 仍從 Order 恢復用途。
- 系統目前沒有上述 endpoint 或工作，因此本次只留下文件註記，不新增出貨、工作排程或其他後置處理能力。

## 影響

延續已定案的 HTTP 與 Context 規格，不修改 frozen spec 或 Abstract。本次 HTTP 解析以 metadata 與 MVC action filter 共用已綁定 DTO，避免重複解析 body；scope 與身分由 middleware 建立。

## 替代方案

僅使用操作者 identity 無法代表訂單保存的用途，因此後置處理不得藉由正式／System 身分將 test Order 當成正式交易。

## 後續工作

未來實際新增單筆 Order endpoint／後置任務時，依本註記與既定規格補上權限、用途恢復、效果控制與測試。Phase 2.5 仍只處理現有事件邊界。
