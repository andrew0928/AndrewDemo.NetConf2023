# Review 原則摘要

這份文件是給使用這個 skill 的人閱讀的摘要說明。

它的用途是幫助 reviewer 或協作者快速理解這套 workflow 背後的 review 習慣與判定方式，不是 agent 實際執行流程的規則來源。

內容依據 repo 內先前整理的 `session-summary-2026-03-23-to-2026-03-24.md`。

## 1. 實際案例

以下不是抽象口號，而是從實際工作時序整理出的案例脈絡。

### 案例 A：從架構評估一路走到 Phase 1 與 Phase 2

這類任務的典型節奏是：

1. 先做高層架構評估，確認需求是否值得做、會影響哪些邊界。
2. 很早就先切出 Phase 1 與 Phase 2。
3. 先產出 `spec`、`spec/testcases`、`.Abstract` contract，再進入重構。
4. 在命名、邊界、責任仍不穩定時，先用 class diagram、C4、sequence diagram 幫助 review。
5. contract 與 scenario 一旦定案，就用 build 與 test 當作 agent 自主驗證的最低門檻。
6. 同一主題如果同時有「責任搬移」與「correctness 修正」，通常會拆成兩輪，不混在一起。

實際上，這種節奏曾經出現在：

- shop runtime / discount plugin 邊界確認
- ProductService 與 order event 邊界確認
- CheckoutService 的搬移
- checkout correctness 修正

### 案例 B：版本分析與 phase 文件

另一種常見任務不是直接寫 code，而是對特定 commit 做 reverse engineering 與交付文件。

這類任務的節奏是：

1. 先固定 exact snapshot。
2. 以該 snapshot 的實際 source / spec / tests 為準，不拿現在的 `HEAD` 亂補。
3. 產出 phase 文件集：
   - `README.md`
   - `c4-model.md`
   - `testcases/README.md`
   - `review-notes.md`
   - 各 testcase 文件
4. 每個 testcase 都要附：
   - class diagram
   - sequence diagram
5. 如果是 phase 之間的比較，還要補：
   - `phase0 -> phase1`
   - `phase1 -> phase2`
   這類差異摘要與對照圖

這代表 review 的重點不只是「這版做了什麼」，而是：

- 邊界如何改變
- testcase 代表了哪些使用情境
- 目前文件、規格、實作之間還有哪些殘留落差

### 案例 C：驗證不是附屬品，而是每個階段的交付條件

在這套 workflow 裡，驗證不是最後才補的尾巴。

每個階段都要有：

- 對應的 contract
- 對應的 tests
- 對應的 review 材料

review-ready 的前提至少是：

1. contract 能被 code 對齊，build 通過
2. scenario 能被 test 對齊，test 通過

也就是說，review 並不是拿「尚未可驗證的想法」來討論，而是拿：

- 已能被 build 驗證的 contract
- 已能被 test 驗證的 scenario
- 已經整理好的邊界圖與流程圖

## 2. 我的原則

以下是從多次互動中反覆出現的判定方式。

### 原則 A：先確認架構，再做實作

- 先看這件事的邊界是否正確
- 先看責任是否放對層
- 先看 naming 是否可作為 canonical baseline
- 還沒確認前，不急著直接鋪大量 code

### 原則 B：spec 要精確到 code contract

- `spec` 不是抽象需求描述而已
- 它應該精確到足以映射成 code contract
- `.Abstract` 或等價 contract project 應與 spec 對齊
- contract 若不符合，build 應該會失敗

### 原則 C：scenario 要精確到可執行驗證

- `spec/testcases` 不是展示用途而已
- 它應該精確到可以落成 unit test 或其他可執行驗證
- scenario 若不符合，test 應該會失敗

### 原則 D：每個階段都要有自己的 contract / tests

- Phase 1 不是只出文件
- Phase 1 需要有對應 contract 與 tests 的基準
- Phase 2 不是只改 code
- Phase 2 需要在 frozen contract 上持續維持 build/test 可驗證

換句話說，階段邊界本身就要可驗證，而不是只有最終系統才驗證。

### 原則 E：review 分三層

1. review contract / boundary
   - 用 class diagram
   - 用 C4 model
2. review scenario / flow
   - 用 sequence diagram
3. review coverage / scope
   - 用 decision table

這三層缺一不可。

### 原則 F：coverage 不追求盲目的 100%，但要有全貌

重點不是為了數字追求無意義的 100% coverage，而是讓 reviewer 看得見：

- 完整 decision space 有多大
- 目前已知 scenario 有哪些
- 哪些 scenario 已納入
- 哪些 scenario 刻意 defer
- 哪些風險仍未覆蓋

因此應該把 scenario 展開成 decision table，逐列標記。

這樣 reviewer 才能有意識地決定：

- 要不要接受目前 coverage
- 略過了哪些情境
- 哪些缺口是這個階段可接受的

### 原則 G：命名與術語一旦定案，就要同步

- canonical 路徑、欄位名、術語一旦改變
- source / docs / demo / spec / tests 都要同步更新
- 如果沒有明確要求相容，不要長期保留雙軌命名

### 原則 H：把不同目的的變更拆開

若同一主題同時包含：

- 責任搬移
- correctness 修正
- 新功能擴張

通常應拆成多輪。

這樣做的好處是：

- spec 比較容易 review
- decision 比較容易記錄
- code 變更目的比較單純
- 測試失敗時比較容易定位原因

### 原則 I：agent 可以高度自主，但前提是可自我驗證

這套 workflow 並不是要阻止 agent 自主工作。

相反地，它要求 agent 在交付 review 前先做到：

- contract 已能被 build 驗證
- scenario 已能被 test 驗證
- boundary 已有 class diagram / C4
- flow 已有 sequence diagram
- coverage 已有 decision table

當這些條件成立時，review 才真正有意義。

## 總結

這套 review 原則的核心不是「先寫文件」而已，而是：

- 先把邊界說清楚
- 再把 contract 定清楚
- 再把 scenario 轉成可驗證的 tests
- 再用圖與 decision table 讓 reviewer 看見整體結構、流程與 coverage

真正要交給 reviewer 的，不只是想法，而是一個已經可以被 build 與 test 驗證的階段性成果。
