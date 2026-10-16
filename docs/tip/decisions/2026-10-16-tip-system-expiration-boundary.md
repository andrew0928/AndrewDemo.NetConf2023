# TiP 測試查詢與系統到期整理的責任邊界

## 狀態

accepted

確認日期：2026-10-16。使用者要求依「系統應正常處理真正逾期的預約」方向修正並保留 [AC-TIP-012](../tip-acceptance-criteria.md#ac-tip-012)，修訂版已接受。現行 PetShop 不符合的執行邊界已列入[架構變更事項](../tip-architecture-notes.md#petshop-expiration-change)，留待實作；事件、排程、執行身分與 context 的具體設計尚未定案。

## 背景

AC-TIP-012 原草案將 test Member 查詢引發的預約過期更新，也視為必須略過的測試寫入。使用者指出：正式會員 A 的預約已經逾期，測試會員 B 的查詢只是發現事實；真正執行清理的角色應是系統，不應因發現者為 test 而停止正常到期整理。

使用者選擇：B 可以發出不以 B 為執行主體的到期通知，由系統正常清理。只有另將事件本身定義為受保護對象時，才會禁止測試產生事件；本案例不採此分支。此決策承接[領域物件分類](2026-10-16-tip-domain-object-classification.md)，補充行為歸屬邊界，不改變物件分類。

### 現行程式查核

2026-10-16 靜態檢查 source code、既有規格與測試內容，未執行 runtime 測試：

1. [ProductsController](../../../src/AndrewDemo.NetConf2023.API/Controllers/ProductsController.cs) 依 product ID 呼叫查詢，未傳入查詢者的 Member／Token。
2. [PetShopProductService](../../../src/AndrewDemo.NetConf2023.PetShop.Extension/Products/PetShopProductService.cs) 將 Product 與 `TimeProvider` 的當下時間交給 `ApplyReservationProductPolicy`。
3. [PetShopReservationService](../../../src/AndrewDemo.NetConf2023.PetShop.Extension/Services/PetShopReservationService.cs) 判斷 `Holding` 且 `HoldExpiresAt <= evaluatedAt`，同步呼叫 `ExpireHold`。
4. `ExpireHold` 改寫 `Status = Expired`、`UpdatedAt`，再由 [PetShopReservationRepository](../../../src/AndrewDemo.NetConf2023.PetShop.Extension/Services/PetShopReservationRepository.cs) 直接 `Reservations.Update`。Reservation 與 Product 均保留，沒有刪除紀錄。
5. 這條路徑沒有過期事件、queue 或獨立 worker。現有 [PetShopOrderEventDispatcher](../../../src/AndrewDemo.NetConf2023.PetShop.Extension/Reservations/PetShopOrderEventDispatcher.cs) 處理訂單事件，直接同步呼叫 `ConfirmFromOrder`；不是查詢發出的過期清理事件。
6. 時段占用判定直接使用 `HoldExpiresAt`；已逾期的 `Holding` 不占位，不必等待狀態寫回 `Expired`。

因此，目前符合「查詢觸發到期整理、更新不依賴查詢者身分」的部分語意，但尚未具備使用者描述的非同步系統工作邊界，也未實作 TiP 模式判定。

## 決策

1. 正式預約確實已逾期時，系統到期整理應正常執行；B 是測試會員不構成阻止清理的理由。
2. B 的查詢與系統的到期整理分開判定責任；本案例允許 B 通知系統發現逾期，再由系統依正式資料與時間規則處理。
3. 「正常清理」的現行對應是 `Holding -> Expired`，不是實體刪除 Reservation 或 Product。本決策不增加資料刪除需求。
4. AC-TIP-012 依使用者修正保留並接受：B 正常得到商品不可用結果，到期通知正常發出；等待系統清理完成後，驗證 A 的預約已保存為 `Expired`，預約與 Product 紀錄仍保留。原草案的「因 test 略過到期寫入並留略過 log」不採用。
5. 既有測試改價、扣庫存、建立預約等限制仍成立；本決策只釐清系統到期整理，不將所有無會員資訊的事件一律視為可修改正式資料。

## 影響

- **重大決策；影響 .Core**：後續通用執行模式設計須區分發現者、業務操作主體與系統維護工作，不能僅由最初呼叫者推定全部後續工作的用途。
- PetShop 的系統到期整理需有明確責任邊界；目前同步呼叫不能作為非同步隔離已完成的證據。
- PetShop 需補上到期通知與系統處理邊界；非同步清理的完成判定、重複通知與狀態競爭留待架構階段處理。目前不選定 transport、message contract 或 worker 實作。
- **回頭修正 Phase 1；影響 .Abstract / spec**：實作前須補齊正式到期整理規格及測試案例；是否需要調整 `.Abstract` 與具體 contract 形狀，留待 Phase 1 確認。
- 本次僅更新 `/docs`；未重新開啟 Phase 1，未變更 `/spec`、`.Abstract` 或 source code。

## 替代方案

- 一律將測試查詢後的到期整理略過：不採用，會將系統原本應執行的生命週期整理歸為 B 的測試操作。
- 將本案例事件本身列為受保護對象，禁止 B 發出通知：使用者本次不選此方案。
- 直接沿用目前查詢同步寫入、沒有獨立系統處理邊界的結構：不足以驗收修訂後的 AC-TIP-012，列為待處理架構差距。

## 後續工作

- AC-TIP-012 已修訂並接受；後續實作須同時驗證通知正常發出與系統清理完成的持久化結果。
- 依 [PetShop 架構變更事項](../tip-architecture-notes.md#petshop-expiration-change) 補上事件判定、系統執行身分、context 邊界及處理完成的觀察方式。
- 設計時區分「通知需要重新檢查」與「直接要求改成某狀態」，並確認處理當下仍符合到期條件；此為待評估設計，尚未凍結 contract。
