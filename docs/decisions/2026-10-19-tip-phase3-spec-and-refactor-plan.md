# TiP Phase 3 規格修訂與重構計畫

## 狀態

accepted：使用者已確認交易隔離的業務原則，並要求「確認需求規格都已經更新，追加 phase 3 來進行這部份的重構，完成後給我確認」。

accepted：具體 collection 命名、HTTP 拒絕碼、遷移策略與六段重構計畫已由使用者回覆「OK」確認，並明確要求提交 docs／spec。**規格／案例已更新；Phase 3 尚未實作，Abstract 與現有資料庫未修改。**

日期：2026-10-19。

## 背景

前輪 [評估範圍](2026-10-19-tip-collection-isolation-assessment-scope.md) 限定 PoC 同庫拆 collection；後續 [交易邊界決策](2026-10-19-tip-identity-storage-and-access-boundaries.md) 已明確保留共用認證與非交易資訊，固定交易用途，並要求 test identity 改價拒絕、test transaction 不產生實際業務效果。原 spec 與 AC-TIP-005 仍包含成功略過改價及混合用途 collection，必須同步。

本項延續 roadmap 的共用交易與 ProductService／DiscountRule 模組化主線，讓模組依一致 context 與 storage 邊界運作。

## 決策

1. **回頭修正 Phase 1；影響 .Abstract / spec。** 使用者最新指示授權更新此項需求規格與 testcase；既有 Abstract 已能表達獨立 contexts、唯一 int ID 及 Rejected，實際無需修改 Abstract。
2. **重大決策；影響 .Core。** 新增 [交易儲存規格](../../spec/tip-transaction-storage.md)，集中定義六區、用途不可變、單筆定位／授權、唯一 ID、原子性、一致查詢與離線遷移。
3. normal 保留原 orders／checkout_transactions／archived_checkout_transactions；test 採 test_ 前綴，公開 API 的 normal／test／all 維持。此命名已隨本次規格確認。
4. 改價拒絕使用既有 Rejected；HTTP 拒絕採 403／test_identity_forbidden，不新增正式維護 API。僅商品價格列入本次禁止清單；SKU 保留現行驗證後略過政策。
5. 以 [Phase 3.1～3.6](../../spec/tip-phase3-plan.md) 接續既有 Phase 2，涵蓋受控 storage、交易／事件、身分限制、查詢、遷移與整體驗收。禁止以只新增 collections 或舊測試綠燈視為完成。
6. 本次交付為已確認的文件與規格提交；不修改 source、不執行現有 DB 遷移或部署。後續實作須同步 source／docs／demo 與已確認的 canonical 規格，不長期保留 mixed collection fallback。

## 影響

- spec：物件、生命週期、標準流程、Context、HTTP、admin 與 shop-local isolation 同步；Phase 1 索引與 Phase 2 歷史狀態標記更新。
- testcase：新增 STORE-001～026，修訂 FLOW-303／HTTP-004，獨立追蹤 SKU 的 FLOW-309／HTTP-018；保留舊證據的歷史版本意義。
- docs：修訂 AC-TIP-005、guidelines、architecture notes、coverage、索引與 roadmap，新增 [review 說明](../tip/phase3-spec-review.md)。
- 未實作的新要求明確標示待 Phase 3；未擴大為資料庫分離、OAuth 搬移或擴充模組的 TiP 業務實作。

## 替代方案

- 只記錄決策、不更新 spec：會留下改價成功略過與新版拒絕互相衝突，不採用。
- 把新工作併入已交付 Phase 2：會混淆歷史驗證證據，依使用者要求另列 Phase 3。
- 只拆 collections、不集中存取／處理舊資料：會漏掉 raw 查詢、事件與歷史 test rows，不足以達成交易隔離。
- 同時重寫 Abstract、拆 DB 或導入通用 migration framework：超出本次 PoC 必要範圍，不採用。

## 後續工作

本次規格及執行切分已確認；後續進入 Phase 3 時依序完成實作、自我驗證與交付。本輪只提交文件，不啟動重構。實際使用中的 DB 遷移需先列明目標、停寫與備份還原步驟，不能把此次文件要求當作已執行資料切換。
