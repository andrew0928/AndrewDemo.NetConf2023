# TiP collection 隔離評估範圍

## 狀態

accepted（僅評估範圍）

同日後續更新：使用者已接受 collection 拆分建議，最終確認以交易作為嚴格邊界、認證與非交易資訊共用，不採 token 分區授權。完整決定另記於 [TiP 身分、儲存與存取邊界](2026-10-19-tip-identity-storage-and-access-boundaries.md)。以下保留初次範圍確認紀錄。

確認日期：2026-10-19。使用者確認：「andrew-shop 是 poc 用途，拆 collection 就足以代表隔離性，替我評估拆 collection 的變更範圍就好」。

本次未授權實作，也未重新開啟 Phase 1；拆分對象、collection 命名、存取介面與遷移策略仍待討論。

## 背景

既有 TiP 透過保存的 IsTest、執行 context 與受保護效果政策區分測試交易。使用者原先要求查證 Stripe 是否揭露 database／table 隔離，並評估本專案改採明確儲存分區的影響，明確指定先不改 code。

此議題延續 [roadmap](../project-roadmap.md) 的 TiP 與模組化主線。

## 決策

- 本專案以 PoC 為目標，同一 shop-local LiteDB 內拆 collection 已足以代表本次研究要求的隔離性。
- 本輪只評估 collection 拆分，不展開獨立 database、主機或基礎設施隔離方案。
- 本輪維持純評估；僅依 AGENTS.md 記錄確認範圍，不修改 code、spec 或 Abstract。
- 不將此範圍確認解讀為已接受任何特定 collection 清單或重構方案。

## 影響

**重大決策；影響 .Core。** 後續評估涵蓋持久化入口、用途路由、ID 唯一性、結帳原子性、事件用途恢復與查詢合併；保留既有商業效果政策作為評估基準。

若後續採納方案需修改 frozen spec，須明確標記 **回頭修正 Phase 1；影響 .Abstract / spec**；是否需要修改 Abstract 取決於正式方案，本次不預先決定。

## 替代方案

- database 分離：不在本次 PoC 評估範圍。
- 維持現有同 collection 的 IsTest 標記：作為比較基準；本次尚未批准替換既有實作。

## 後續工作

1. 提供 collection 拆分的變更範圍與驗證重點。
2. 區分交易資料必需範圍與會員／Token 等追加範圍。
3. 待使用者採納具體方案後，再確認需要重新開啟的規格項目與實作階段。
