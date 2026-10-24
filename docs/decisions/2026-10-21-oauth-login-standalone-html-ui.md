# OAuth 登入頁保留獨立 HTML 並沿用 Storefront UI 風格

## 狀態

- accepted
- 日期：2026-10-21

## 背景

使用者要求快速調整 OAuth 登入表單，明確保留簡單 HTML 與由 API 程式回傳頁面的既有方式。本次屬於 storefront 共用登入體驗的補齊，沿用已確認的 GOV.UK／USWDS 類型 UI 風格與無障礙方向。

## 決策

- 維持 `LoginController.GetAuthorize()` 回傳 `wwwroot/signin.html`，不改為 Razor Pages、SPA 或另一個 host。
- 登入頁以獨立 HTML 與內嵌 CSS 呈現，沿用 storefront 的配色、表單與焦點風格，不依賴 storefront 靜態資源或外部 CDN。
- 補上繁體中文語言與頁面標題、語意區域、skip link、欄位 label／說明、原生必填檢查與可見焦點；允許瀏覽器縮放，支援窄螢幕。
- 保留名稱登入／自動註冊、密碼忽略，以及原有 OAuth POST 欄位。JavaScript 僅以 `URLSearchParams` 將 `client_id`、`redirect_uri`、`state` 帶入 hidden fields。

## 影響

- 變更 API 的登入 HTML；OAuth endpoint、Core、Abstract 與正式合約不變。
- 版型只沿用視覺與互動規則，不引入 Storefront.Shared 的 runtime dependency。
- 本機驗證沿用 Docker Compose；API 映像需重新建置。

## 替代方案

- 改用 Razor／SPA：不符合使用者保留簡單 HTML 的要求。
- 引用 storefront 的 CSS 路徑：會讓獨立 OAuth host 依賴 storefront 路由與資源發布，因此不採用。

## 後續工作

- 日後調整登入表單時，持續檢查鍵盤操作、必填提示、手機版面與登入回跳；局部驗證不等同完整無障礙認證。

## 驗證

- 2026-10-21 在 Docker Compose 的 `http://localhost:5128` 以 Edge 驗證：頁面標題與語言、可聚焦 skip link、欄位 label／說明、空白名稱的原生必填提示。
- 只用鍵盤完成測試會員登入，OAuth callback 後顯示「登入成功」。
- 320px viewport 下內容寬度為 320px，沒有橫向溢出；驗證後還原瀏覽器尺寸。
- 主要文字、提示文字、按鈕與頁首文字的色彩對比分別為 19.59、7.07、6.21、12.40。
