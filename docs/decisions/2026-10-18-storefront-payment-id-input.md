# Storefront 改用 PaymentID 輸入

## 狀態

accepted。2026-10-18 使用者明確要求 UI 直接輸入 PaymentID，不再直接選擇測試付款；要求與 AdminCLI 分兩批 commit。

## 背景

既有 normal/test radio 將付款映射為 0／990001，邏輯雖正確，卻把測試用途變成消費者選項。使用者希望以模擬卡號呈現輸入，再由系統判定，支援 TiP 主線的人工作業驗證。

## 決策

- 保留現行 Core 白名單與用途判定，僅調整 CommonStorefront。
- PaymentID 預設空白且必填，接收既有 int 值；create 原值傳入，不建立 PaymentMode 相容路徑，complete 不傳付款 ID。
- 次要色提示預設測試 ID 990001 與「測試會員或測試 ID 皆建立測試交易」規則；實際用途取 API 結果。
- 保留模擬用途提示、合法性檢查、失敗輸入與購物車；不接收真實信用卡資訊。
- 先提交 AdminCLI，再獨立提交本次 UI。

## 影響

**回頭修正 Phase 1**，**影響 .Abstract / spec**：使用者明確重新確認本項 UI 規格，因此同步 CommonStorefront baseline 與案例；不修改 Abstract、Core 或 HTTP 契約。PaymentID 沿用 int，不引入信用卡字串模型或額外範圍限制。

## 替代方案

保留測試付款 radio 或自動隨機生成 ID 均不符合此次需求；由 UI 自行判定 test 會造成兩套規則，不採用。

## 後續工作

完成頁面與表單測試，重建 CommonStorefront 供手動確認；交付結果見 [紀錄](../tip/payment-id-ui-delivery.md)。未另行擴充 AppleBTS／PetShop UI。
