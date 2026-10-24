# TiP storage transaction 由 Core 原子操作擁有

## 狀態

accepted，2026-10-19。使用者授權執行 Phase 3，並在併發驗證期間建議「只在最外圍啟動 transaction；內部關鍵操作檢驗 transaction 狀態」，同時授權採用更合適的具體方案。此處採納其原則，將最外圍定於 Core 的同步原子操作，而非整個 HTTP request。**重大決策；影響 .Core**，沒有改動 frozen spec 或 Abstract。

## 背景

新增 storage helper 與既有 Checkout／Admin 邊界巢狀呼叫 `BeginTrans()`，在 LiteDB 5.0.21 Shared mode 造成鎖無法完整釋放；Shared connection 測試逾時。官方 [SharedEngine 原始碼](https://github.com/litedb-org/LiteDB/blob/v5.0.21/LiteDB/Client/Shared/SharedEngine.cs) 的 BeginTrans 每次取得 mutex，且會改寫 transactionRunning，因此不能把回傳 false 當作可安全重入的偵測方法。

## 決策

- Core `CheckoutService` 的 create 寫入區段與同步 `CompleteTransaction` 擁有 begin／commit／rollback。等待排隊及提交後事件派送在 DB transaction 外。
- 內部 `StorageTransaction` 以同執行緒、同 database instance 的受控狀態辨識既有交易；內層沿用，不再次呼叫 LiteDB BeginTrans。配發 Order ID、封存等關鍵步驟先驗證外層交易存在。
- 交易區段不跨 await。使用 thread-local 狀態配合 LiteDB 的 thread-bound transaction；它不是 IdentityContext／TransactionContext，也不承載交易的 normal／test 用途。
- 查詢、獨立寫入、初始化與遷移各有自己的短交易邊界。跨 collection 查詢先取得共同名稱鎖；同步讀完、materialize 後才釋放。
- 應用不得自行從 raw Database 開外層 transaction 再呼叫受控 store；需要組合的原子操作由 Core 定義，避免出現第二套交易擁有者。

## 影響

Core storage、Checkout、Admin、啟動與遷移共用同一交易生命週期管理。測試涵蓋 Direct／Shared、不同連線及不同程序；fault injection 驗證失敗仍整筆 rollback。

## 替代方案

- 在 API endpoint 開交易：HTTP 涵蓋 await、等待與事後派送，會放大鎖範圍、跨越 LiteDB 執行緒邊界，也無法涵蓋 CLI，故不採用。
- 每個 helper 各自 BeginTrans：造成重入問題且原子操作擁有者不明，故移除。
- 導入通用 Unit of Work framework：目前 PoC 只需最小 concrete storage 協調元件，不新增 Abstract 合約。

## 後續工作

隨 Phase 3 交付提供跨程序與故障測試結果。若未來換成不同實體 DB，必須重新設計跨庫原子性，不能沿用本次單 LiteDB transaction 的保證。
