# TiP 研究文件

本目錄集中收錄 AndrewShop 的 Testing in Production（TiP）說明、接受條件與專屬決策。文件供需求討論與確認使用，尚未代表正式規格已凍結或系統已完成實作。

## 文件索引

| 文件 | 內容 |
|---|---|
| [TiP 設計原則](tip-guidelines.md) | 大前提、領域物件分類、略過紀錄與商業統計規則。 |
| [TiP 接受條件](tip-acceptance-criteria.md) | 逐條確認的 AC 與驗收結果要求。目前已接受 AC-TIP-001、AC-TIP-003、AC-TIP-004、AC-TIP-005、AC-TIP-006、AC-TIP-007、AC-TIP-008、AC-TIP-009、AC-TIP-010、AC-TIP-011、AC-TIP-012、AC-TIP-013、AC-TIP-014、AC-TIP-015。 |
| [TiP 架構設計筆記](tip-architecture-notes.md) | 已確認的 context、效果控制與責任分工，以及後續應用規格、contract／實作及擴充事項。 |
| [不產生真實商業效果](decisions/2026-10-16-tip-no-real-business-effects.md) | TiP 大前提的決策紀錄。 |
| [領域物件分類與不可變用途](decisions/2026-10-16-tip-domain-object-classification.md) | 物件分類、已接受案例及使用者補充決策。 |
| [系統到期整理的責任邊界](decisions/2026-10-16-tip-system-expiration-boundary.md) | AC-TIP-012 已修正接受：測試查詢可以喚起正常到期整理；PetShop 架構差距留待實作。 |
| [研究範圍與架構規範邊界](decisions/2026-10-17-tip-contract-and-context-boundaries.md) | 以 prototype 歸納 best practice；確認不可變契約、事件／任務交接、出口表達、正常 log 與 schema 用途定義，具體保證機制交由實作端。同日補充 current identity／current transaction 兩類 context 定義。 |

## 維護方式

本輪 14 條核心 AC 與支撐 TiP 體系的架構原則已全部確認；AC-TIP-002 保留為信用卡示意，沒有實作信用卡支付時為 N/A。原則收斂不代表正式規格已凍結或系統已實作。

- TiP 專屬文件以本目錄為 canonical 位置，不保留舊路徑副本。
- TiP 決策收錄於 `decisions/`，沿用[共用決策文件規範](../decisions/README.md)，並同步維護該處的決策索引。
- 跨主題的 [project roadmap](../project-roadmap.md) 留在原位置，以連結指向本目錄。
- 本輪以可重用規範為範圍收斂；完整應用政策矩陣、具體 contract／實作與額外來源擴充保留到對應階段，不自動視為已接受。
