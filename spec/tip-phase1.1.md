# TiP Phase 1.1：管理查核 API

日期：2026-10-18。分支：feature-test-in-production。

## 範圍與狀態

使用者接受正式查核入口，指定 `/api/admin/*` 採獨立認證、預先指定的小寫無連字號 `admin_` 前綴 access token，由使用者後續追加 shell CLI 讀取。本輪只完成 Phase 1.1 規格，實作留待後續；需求只存在 API，沒有 Web UI。

本輪交付正式查詢規格、案例與決策；不新增 API、CLI 或 UI 實作，不變更持久化 schema、會員認證、既有 TiP 交易契約或 Abstract。使用者已確認未設定 token 即停用；認證採 Authorization Bearer 與 SHOP_ADMIN_ACCESS_TOKEN 環境變數，Phase 1.1 規格定案。

| 文件 | 用途 |
|---|---|
| [管理查核規格](tip-admin-query.md) | 認證、API、查詢口徑、回應、CLI 與一致性要求。 |
| [管理查核案例](testcases/tip-admin-query.md) | 後續實作驗收基準，不代表本輪測試已通過。 |
| [追加決策](../docs/decisions/2026-10-18-tip-admin-query.md) | 授權、範圍與替代方案。 |

## 與既有階段的關係

這是 Phase 1 的追加規格，支援 TiP 正式／測試資料隔離驗證，並承接原已保留的核帳查詢方向；不取代 Phase 2.1～2.7，不將尚未完成的 Phase 2.4～2.7 視為完成。

原交接所稱「正式報表／查詢 API 不在本輪」仍適用原 Phase 2 清單；此次追加以本文件獨立交接，後續獲授權再實作。既有離線 `query-litedb.sh` 是診斷工具，不是本次 admin CLI 的實作。

## 合約放置

新增的是 HTTP／CLI adapter 合約與查詢語意，正式 schema 見管理查核規格。第一版查詢服務可放 Core，HTTP DTO 放 API，不新增共用業務介面或為 admin 增加 Abstract 身分類型。Admin 認證獨立於會員 IdentityContext；清單不綁定單筆 TransactionContext。

若後續發現需要共用 Abstract 介面或改變既有 context 語意，須重新提出具體契約，不將本次規格授權擴張至其他 frozen contract。

## 後續實作交接

1. 實作 Core 查詢服務及三個 admin API；驗證讀取一致性，不能只宣稱 BeginTrans 足以跨集合一致。
2. 實作 admin token 認證與部署設定，與會員 token 雙向隔離。
3. 完成 API 案例中的資料／認證／並行驗證，再提供部署與交付紀錄。Web UI 不在需求內，也不列為待做項目。
4. 使用者後續追加 shell CLI；文件中的 CLI 契約與案例供該次工作參考，不是 API 交付的前置條件。所有統計仍由 API 計算。

本輪不執行部署、不設定實際秘密、不新增登入帳號，不宣稱新增 endpoints 可用。
