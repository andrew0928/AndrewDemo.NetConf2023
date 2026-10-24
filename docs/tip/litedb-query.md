# 查核工具遷移至 AdminCLI

2026-10-18 使用者確認移除 DatabaseQuery 與 `scripts/query-litedb.sh`，未來使用 AdminCLI＋Admin API。舊的離線 SQL 指令已不再提供，LiteDB extension 也不是日常管理查核入口。

請依 [AdminCLI 操作說明](admin-cli.md) 建置與查詢，或依 [TiP 最後驗收流程](phase1.2-final-verification.md) 保存證據。API 保持運作即可查會員、結帳、訂單與統計，不必為查詢停止容器。

Admin API 未公開的庫存／Token 等資料不屬於目前 CLI 範圍；改由既有 Core／HTTP 自動化補足證據，未來擴充須另行確認契約。ConsoleUI 保留購物／AI demo，用途與唯讀管理查核不同。

決策依據見 [Phase 1.2 工具取代決策](../decisions/2026-10-18-tip-admin-cli.md#工具取代決策)。歷史交付曾使用離線工具查核，仍是當時的證據，不表示現行指令可用。
