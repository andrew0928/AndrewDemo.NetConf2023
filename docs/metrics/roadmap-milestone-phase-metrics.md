# Roadmap Commit Metrics

## 統計口徑

- 來源資料：`docs/metrics/git-commit-metrics.csv`，一列一個 git commit。
- 本報告只保留 `2026-03-01` 之後的 commit；原始序號 1-64 不列入。
- 依 commit 順序完整列出，每列代表單一 commit 的異動量，不是 snapshot 累計量。
- `Commit SHA / Date` 使用完整 commit SHA 與 `MM/DD`。
- `Related Milestone / Phase` 依目前 roadmap completion group 歸屬計算，並載入 `docs/project-roadmap.md` 的標題；同一段 commit range 會對應到同一組 milestone / phase。
- `Docs Files / Lines` 統計 `/docs + /spec` 的異動檔案事件數與異動行數。
- `Decision Files` 只統計 `/docs/decisions/`，並排除 `README.md`。
- `Major/Core Decisions` 統計 decision 內容含 `重大決策` 或 `影響 .Core` 的異動檔案事件數。
- `Backtracking Decisions` 統計 decision 內容含 `回頭修正 Phase 1` 或 `影響 .Abstract / spec` 的異動檔案事件數。
- `Core/Abstract Src Files / Lines` 只統計 `.Core` 與 `.Abstract` 專案下的 `/src/*.cs` 異動。
- `Src Files / Lines` 只統計 `/src/*.cs` 的異動檔案事件數與異動行數。
- `Tests Files / Lines` 只統計 `/tests/*.cs` 的異動檔案事件數與異動行數。
- `Public Contract Δ I/M/T` 代表 public interface / method / type 的 snapshot delta；`Contract Churn` 是 public contract signature added + removed。
- `Test Δ Fixtures/Cases` 代表 test fixture / `[Fact]` + `[Theory]` 的 snapshot delta；`Test/Contract Ratio` 是 test case delta 除以 public contract surface delta。
- `Core Touch %` 是 Core/Abstract src changed lines 除以所有 src changed lines。
- milestone / phase 統計表的數字一律用 `Sum()`；若某 commit 同時關聯多個 milestone / phase，會分別計入各自的 group。
- `序號區間` 會壓縮成連續區間列表，例如 `65-81, 91, 94`；`日期區間` 只保留 `MM/DD`。

## Commit Details

| 序號 | Commit SHA / Date | Related Milestone / Phase | Docs Files / Lines | Decision Files | Major/Core Decisions | Backtracking Decisions | Core/Abstract Src Files / Lines | Src Files / Lines | Tests Files / Lines | Public Contract Δ I/M/T | Contract Churn | Test Δ Fixtures/Cases | Core Touch % | Test/Contract Ratio |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 65 | `d0401c1e8cc0ec0af71ba6af48bf2f649ce0aa9f` / 03/23 | M1 - 基礎建設 / M1-P1 - Shop Runtime / DiscountRule Phase 1 | 0 / 0 | 0 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 66 | `97e58760e31c4b9dd18dafb543172df39eed0b63` / 03/23 | M1 - 基礎建設 / M1-P1 - Shop Runtime / DiscountRule Phase 1 | 4 / 459 | 1 | 0 | 0 | 9 / 323 | 16 / 526 | 2 / 78 | 4 / 6 / 9 | 29 | 1 / 2 | 61% | 0.11 |
| 67 | `37470db0771504cb10c2f8fc232410f1bab72314` / 03/23 | M1 - 基礎建設 / M1-P1 - Shop Runtime / DiscountRule Phase 1 | 8 / 1,016 | 2 | 0 | 0 | 10 / 381 | 15 / 514 | 2 / 50 | -2 / -3 / -3 | 28 | 0 / 0 | 74% | - |
| 68 | `dfa7fbbc0cdecabec4583d2ffd9869d8c437758f` / 03/23 | M1 - 基礎建設 / M1-P2 - ProductService / Order Event Phase 1 | 5 / 788 | 2 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 69 | `af175a0c7c56b1ff7add6ac089d1f35032a7a80b` / 03/24 | M1 - 基礎建設 / M1-P2 - ProductService / Order Event Phase 1 | 0 / 0 | 0 | 0 | 0 | 8 / 79 | 11 / 104 | 4 / 30 | 1 / 4 / 3 | 12 | 0 / 0 | 76% | 0.00 |
| 70 | `37ae0a2ed76cbea448f668226a90f6ed312643a8` / 03/24 | M1 - 基礎建設 / M1-P2 - ProductService / Order Event Phase 1 | 0 / 0 | 0 | 0 | 0 | 4 / 145 | 9 / 235 | 2 / 15 | 0 / 6 / 4 | 14 | 0 / 0 | 62% | 0.00 |
| 71 | `48ae67b275be57aca1a11dd3dc5a1be4a2492ac5` / 03/24 | M1 - 基礎建設 / M1-P3 - CheckoutService Phase 2 | 4 / 503 | 1 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 72 | `15f2b31feacd578c5ecb7ccf7d4378bd8adfdb75` / 03/24 | M1 - 基礎建設 / M1-P3 - CheckoutService Phase 2 | 8 / 226 | 2 | 0 | 0 | 2 / 286 | 4 / 482 | 1 / 180 | 0 / 10 / 7 | 17 | 1 / 4 | 59% | 0.24 |
| 73 | `5e05eb86782c384cd9e13edc1a7cbc8425e0abfd` / 03/24 | M1 - 基礎建設 / M1-P3 - CheckoutService Phase 2 | 0 / 0 | 0 | 0 | 0 | 0 / 0 | 3 / 246 | 0 / 0 | 0 / 0 / 0 | 2 | 0 / 0 | 0% | - |
| 74 | `bb5122c9b6285a20f8f40ac96662f0c0b54c6f73` / 03/24 | M1 - 基礎建設 / M1-P3 - CheckoutService Phase 2 | 0 / 0 | 0 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 75 | `6e7249bcab3ed4f0b2f4326536f9000f9d3446c2` / 03/24 | M1 - 基礎建設 / M1-P4 - Cart / SKU / Inventory Core 回補 | 24 / 2,720 | 0 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 76 | `0d192b9ecead3c698d3cf12b2ba936010d34a640` / 03/30 | M1 - 基礎建設 / M1-P4 - Cart / SKU / Inventory Core 回補 | 8 / 905 | 1 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 77 | `0997fe30ed04175af79f6ad4caf2b82b3b4215fc` / 03/31 | M1 - 基礎建設 / M1-P4 - Cart / SKU / Inventory Core 回補 | 1 / 11 | 0 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 78 | `b739fdb8185cc95ad4f463b7f061a2e3c9b9099a` / 04/01 | M1 - 基礎建設 / M1-P4 - Cart / SKU / Inventory Core 回補 | 5 / 300 | 1 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 79 | `aa8663663b8c8fbb292a26a74e21a4906f3a0061` / 04/01 | M1 - 基礎建設 / M1-P4 - Cart / SKU / Inventory Core 回補 | 7 / 1,839 | 4 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 80 | `a80777d1c1a455a0295db78e4c6dbccf1d2ac0e0` / 04/01 | M1 - 基礎建設 / M1-P4 - Cart / SKU / Inventory Core 回補 | 10 / 2,257 | 5 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 81 | `c7237396cd8fecbeaef938d1b9c59cc7c846a657` / 04/01 | M1 - 基礎建設 / M1-P4 - Cart / SKU / Inventory Core 回補 | 12 / 1,120 | 3 | 0 | 0 | 11 / 277 | 13 / 317 | 5 / 184 | 0 / 2 / 2 | 4 | 0 / 5 | 87% | 1.25 |
| 82 | `36cfb35df5a64c8ea85b9555e9ae47908258160f` / 04/01 | M3 - AppleBTS 擴充設計 / M3-P1 - Campaign 技術邊界與 `.Core` 回補方向 | 9 / 251 | 4 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 83 | `73778c654e1bfd085dbd3a0802e8b60013020da6` / 04/01 | M3 - AppleBTS 擴充設計 / M3-P1 - Campaign 技術邊界與 `.Core` 回補方向 | 3 / 15 | 1 | 0 | 0 | 4 / 37 | 7 / 72 | 2 / 206 | 0 / 0 / 1 | 3 | 3 / 3 | 51% | 3.00 |
| 84 | `c9f4e0ec0a6c502c523d3b0f0cf24c6e5b8e9fdd` / 04/02 | M3 - AppleBTS 擴充設計 / M3-P2 - AppleBTS Phase 1 Spec / Skeleton | 9 / 997 | 3 | 0 | 0 | 0 / 0 | 15 / 346 | 4 / 221 | 0 / 16 / 15 | 31 | 2 / 2 | 0% | 0.06 |
| 85 | `c26c55d9b682786798e22ba11a5ef8e692879072` / 04/02 | M3 - AppleBTS 擴充設計 / M3-P2 - AppleBTS Phase 1 Spec / Skeleton | 0 / 0 | 0 | 0 | 0 | 0 / 0 | 0 / 0 | 3 / 521 | 0 / 0 / 0 | 0 | 1 / 17 | - | - |
| 86 | `de2c975a7430004b210871b4bd51dd34cb6de020` / 04/02 | M3 - AppleBTS 擴充設計 / M3-P3 - AppleBTS API / Seed / Local Topology | 0 / 0 | 0 | 0 | 0 | 0 / 0 | 6 / 432 | 0 / 0 | 0 / 1 / 0 | 1 | 0 / 0 | 0% | 0.00 |
| 87 | `2826795b53fa3b35733acc8074d67eb29e432913` / 04/04 | M3 - AppleBTS 擴充設計 / M3-P3 - AppleBTS API / Seed / Local Topology | 6 / 361 | 3 | 0 | 0 | 1 / 3 | 25 / 820 | 6 / 61 | 0 / 5 / 6 | 13 | 0 / 1 | 0% | 0.09 |
| 88 | `10b74b9b642a4edd5e814da8d239a78a15b0e73c` / 04/04 | M2 - 標準系統建立 / M2-P1 - Storefront Family / CommonStorefront Phase 1 | 4 / 64 | 1 | 0 | 1 | 9 / 161 | 19 / 255 | 8 / 203 | 0 / 6 / 5 | 15 | 3 / 3 | 63% | 0.27 |
| 89 | `7bad526860af54c42139d2f98bf99238534ad613` / 04/04 | M2 - 標準系統建立 / M2-P1 - Storefront Family / CommonStorefront Phase 1 | 11 / 1,511 | 3 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 90 | `d33096045acef2f128402bb04e10644eca067d0a` / 04/05 | M2 - 標準系統建立 / M2-P2 - CommonStorefront 實作與本機驗證拓樸 | 8 / 428 | 3 | 1 | 0 | 2 / 4 | 28 / 1,243 | 1 / 48 | 0 / 36 / 34 | 70 | 0 / 1 | 0% | 0.01 |
| 91 | `c363365d90be9a503449359dfda0d57375f31d88` / 04/05 | M1 - 基礎建設, M3 - AppleBTS 擴充設計 / M1-P5 - 可測試時間與通用語意回補, M3-P4 - AppleBTS Storefront Phase 1 | 9 / 392 | 2 | 1 | 1 | 1 / 41 | 23 / 1,234 | 1 / 39 | 0 / 11 / 8 | 19 | 0 / 2 | 3% | 0.11 |
| 92 | `1d5c7c2717f39623ef8702ec1f8b3c88c0bed2a1` / 04/06 | M3 - AppleBTS 擴充設計 / M3-P5 - 折扣拆分與部署文件收斂 | 1 / 246 | 0 | 0 | 0 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 / 0 | 0 | 0 / 0 | - | - |
| 93 | `64343cdf7d3ea6f9c76176c786cb591006798f55` / 04/09 | M3 - AppleBTS 擴充設計 / M3-P5 - 折扣拆分與部署文件收斂 | 7 / 417 | 1 | 0 | 0 | 0 / 0 | 2 / 54 | 2 / 74 | 0 / 0 / 0 | 0 | 0 / 0 | 0% | - |
| 94 | `9a4ba58a50687550eac78985bb4d2df946b01d95` / 04/23 | M1 - 基礎建設 / M1-P6 - OrderEventDispatcher 邊界修正 | 12 / 458 | 3 | 1 | 1 | 7 / 138 | 9 / 157 | 1 / 4 | 1 / 0 / 0 | 21 | 0 / 0 | 88% | 0.00 |
| 95 | `6cd651f580e623e99444bd020b6aefbc2285d9a1` / 04/23 | M4 - PetShop 擴充設計 / M4-P1A - Reservation / Hidden Product Projection 核心模型, M4-P2A - PetShop Extension Implementation | 10 / 1,500 | 3 | 0 | 0 | 0 / 0 | 8 / 488 | 2 / 303 | 0 / 12 / 9 | 21 | 3 / 7 | 0% | 0.33 |
| 96 | `9a3fc9a702fd5f41c0dce66d5d432878bf1d8c9b` / 04/23 | M4 - PetShop 擴充設計 / M4-P4 - PetShop Discount / Promotion | 6 / 211 | 1 | 0 | 0 | 0 / 0 | 5 / 162 | 2 / 311 | 0 / 3 / 2 | 7 | 2 / 6 | 0% | 1.20 |
| 97 | `8354791d3ec833793890f024a51422ca75a8ba5d` / 04/24 | M4 - PetShop 擴充設計 / M4-P1B - PetShop Lifecycle / API Spec, M4-P2B - PetShop API, M4-P2C - PetShop Host / Seed / Config | 6 / 818 | 2 | 0 | 0 | 0 / 0 | 17 / 1,268 | 2 / 334 | 0 / 25 / 18 | 45 | 3 / 7 | 0% | 0.16 |
| 98 | `05b2195df6869b4612e4cfaad30665a684ddc9df` / 04/24 | M4 - PetShop 擴充設計 / M4-P3A - PetShop Storefront Spec / Skeleton | 5 / 608 | 1 | 0 | 0 | 0 / 0 | 6 / 317 | 0 / 0 | 0 / 6 / 7 | 13 | 0 / 0 | 0% | 0.00 |
| 99 | `7dffc9b061778fd3a12d9c212d20500fa3de223b` / 04/24 | M4 - PetShop 擴充設計 / M4-P3 - PetShop Storefront, M4-P3B - Reservation Flow Pages, M4-P3C - Member / Order Integration 與 Browser Smoke | 0 / 0 | 0 | 0 | 0 | 0 / 0 | 1 / 1 | 1 / 30 | 0 / 0 / 0 | 0 | 1 / 1 | 0% | - |
| 100 | `299f32f456da239147f731e78658a643096243dc` / 04/24 | M4 - PetShop 擴充設計 / M4-P3 - PetShop Storefront, M4-P3B - Reservation Flow Pages, M4-P3C - Member / Order Integration 與 Browser Smoke | 5 / 51 | 1 | 0 | 0 | 0 / 0 | 13 / 957 | 0 / 0 | 0 / 5 / 1 | 6 | 0 / 0 | 0% | 0.00 |

## Group By Milestone

| Milestone / Phase | 序號區間 | 日期區間 | Docs Files / Lines | Decision Files | Major/Core Decisions | Backtracking Decisions | Core/Abstract Src Files / Lines | Src Files / Lines | Tests Files / Lines | Public Contract Δ I/M/T | Contract Churn | Test Δ Fixtures/Cases | Core Touch % | Test/Contract Ratio |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| M1 - 基礎建設 | 65-81, 91, 94 | 03/23 ~ 04/23 | 117 / 12,994 | 27 | 2 | 2 | 52 / 1,670 | 103 / 3,815 | 18 / 580 | 4 / 36 / 30 | 146 | 2 / 13 | 44% | 0.19 |
| M3 - AppleBTS 擴充設計 | 82-87, 91-93 | 04/01 ~ 04/09 | 44 / 2,679 | 14 | 1 | 1 | 6 / 81 | 78 / 2,958 | 18 / 1,122 | 0 / 33 / 30 | 67 | 6 / 25 | 3% | 0.40 |
| M2 - 標準系統建立 | 88-90 | 04/04 ~ 04/05 | 23 / 2,003 | 7 | 1 | 1 | 11 / 165 | 47 / 1,498 | 9 / 251 | 0 / 42 / 39 | 85 | 3 / 4 | 11% | 0.05 |
| M4 - PetShop 擴充設計 | 95-100 | 04/23 ~ 04/24 | 32 / 3,188 | 8 | 0 | 0 | 0 / 0 | 50 / 3,193 | 7 / 978 | 0 / 51 / 37 | 92 | 9 / 21 | 0% | 0.24 |

## Group By Phase

| Milestone / Phase | 序號區間 | 日期區間 | Docs Files / Lines | Decision Files | Major/Core Decisions | Backtracking Decisions | Core/Abstract Src Files / Lines | Src Files / Lines | Tests Files / Lines | Public Contract Δ I/M/T | Contract Churn | Test Δ Fixtures/Cases | Core Touch % | Test/Contract Ratio |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| M1-P1 - Shop Runtime / DiscountRule Phase 1 | 65-67 | 03/23 ~ 03/23 | 12 / 1,475 | 3 | 0 | 0 | 19 / 704 | 31 / 1,040 | 4 / 128 | 2 / 3 / 6 | 57 | 1 / 2 | 68% | 0.18 |
| M1-P2 - ProductService / Order Event Phase 1 | 68-70 | 03/23 ~ 03/24 | 5 / 788 | 2 | 0 | 0 | 12 / 224 | 20 / 339 | 6 / 45 | 1 / 10 / 7 | 26 | 0 / 0 | 66% | 0.00 |
| M1-P3 - CheckoutService Phase 2 | 71-74 | 03/24 ~ 03/24 | 12 / 729 | 3 | 0 | 0 | 2 / 286 | 7 / 728 | 1 / 180 | 0 / 10 / 7 | 19 | 1 / 4 | 39% | 0.24 |
| M1-P4 - Cart / SKU / Inventory Core 回補 | 75-81 | 03/24 ~ 04/01 | 67 / 9,152 | 14 | 0 | 0 | 11 / 277 | 13 / 317 | 5 / 184 | 0 / 2 / 2 | 4 | 0 / 5 | 87% | 1.25 |
| M3-P1 - Campaign 技術邊界與 `.Core` 回補方向 | 82-83 | 04/01 ~ 04/01 | 12 / 266 | 5 | 0 | 0 | 4 / 37 | 7 / 72 | 2 / 206 | 0 / 0 / 1 | 3 | 3 / 3 | 51% | 3.00 |
| M3-P2 - AppleBTS Phase 1 Spec / Skeleton | 84-85 | 04/02 ~ 04/02 | 9 / 997 | 3 | 0 | 0 | 0 / 0 | 15 / 346 | 7 / 742 | 0 / 16 / 15 | 31 | 3 / 19 | 0% | 0.61 |
| M3-P3 - AppleBTS API / Seed / Local Topology | 86-87 | 04/02 ~ 04/04 | 6 / 361 | 3 | 0 | 0 | 1 / 3 | 31 / 1,252 | 6 / 61 | 0 / 6 / 6 | 14 | 0 / 1 | 0% | 0.08 |
| M2-P1 - Storefront Family / CommonStorefront Phase 1 | 88-89 | 04/04 ~ 04/04 | 15 / 1,575 | 4 | 0 | 1 | 9 / 161 | 19 / 255 | 8 / 203 | 0 / 6 / 5 | 15 | 3 / 3 | 63% | 0.27 |
| M2-P2 - CommonStorefront 實作與本機驗證拓樸 | 90 | 04/05 | 8 / 428 | 3 | 1 | 0 | 2 / 4 | 28 / 1,243 | 1 / 48 | 0 / 36 / 34 | 70 | 0 / 1 | 0% | 0.01 |
| M1-P5 - 可測試時間與通用語意回補 | 91 | 04/05 | 9 / 392 | 2 | 1 | 1 | 1 / 41 | 23 / 1,234 | 1 / 39 | 0 / 11 / 8 | 19 | 0 / 2 | 3% | 0.11 |
| M3-P4 - AppleBTS Storefront Phase 1 | 91 | 04/05 | 9 / 392 | 2 | 1 | 1 | 1 / 41 | 23 / 1,234 | 1 / 39 | 0 / 11 / 8 | 19 | 0 / 2 | 3% | 0.11 |
| M3-P5 - 折扣拆分與部署文件收斂 | 92-93 | 04/06 ~ 04/09 | 8 / 663 | 1 | 0 | 0 | 0 / 0 | 2 / 54 | 2 / 74 | 0 / 0 / 0 | 0 | 0 / 0 | 0% | - |
| M1-P6 - OrderEventDispatcher 邊界修正 | 94 | 04/23 | 12 / 458 | 3 | 1 | 1 | 7 / 138 | 9 / 157 | 1 / 4 | 1 / 0 / 0 | 21 | 0 / 0 | 88% | 0.00 |
| M4-P1A - Reservation / Hidden Product Projection 核心模型 | 95 | 04/23 | 10 / 1,500 | 3 | 0 | 0 | 0 / 0 | 8 / 488 | 2 / 303 | 0 / 12 / 9 | 21 | 3 / 7 | 0% | 0.33 |
| M4-P2A - PetShop Extension Implementation | 95 | 04/23 | 10 / 1,500 | 3 | 0 | 0 | 0 / 0 | 8 / 488 | 2 / 303 | 0 / 12 / 9 | 21 | 3 / 7 | 0% | 0.33 |
| M4-P4 - PetShop Discount / Promotion | 96 | 04/23 | 6 / 211 | 1 | 0 | 0 | 0 / 0 | 5 / 162 | 2 / 311 | 0 / 3 / 2 | 7 | 2 / 6 | 0% | 1.20 |
| M4-P1B - PetShop Lifecycle / API Spec | 97 | 04/24 | 6 / 818 | 2 | 0 | 0 | 0 / 0 | 17 / 1,268 | 2 / 334 | 0 / 25 / 18 | 45 | 3 / 7 | 0% | 0.16 |
| M4-P2B - PetShop API | 97 | 04/24 | 6 / 818 | 2 | 0 | 0 | 0 / 0 | 17 / 1,268 | 2 / 334 | 0 / 25 / 18 | 45 | 3 / 7 | 0% | 0.16 |
| M4-P2C - PetShop Host / Seed / Config | 97 | 04/24 | 6 / 818 | 2 | 0 | 0 | 0 / 0 | 17 / 1,268 | 2 / 334 | 0 / 25 / 18 | 45 | 3 / 7 | 0% | 0.16 |
| M4-P3A - PetShop Storefront Spec / Skeleton | 98 | 04/24 | 5 / 608 | 1 | 0 | 0 | 0 / 0 | 6 / 317 | 0 / 0 | 0 / 6 / 7 | 13 | 0 / 0 | 0% | 0.00 |
| M4-P3 - PetShop Storefront | 99-100 | 04/24 ~ 04/24 | 5 / 51 | 1 | 0 | 0 | 0 / 0 | 14 / 958 | 1 / 30 | 0 / 5 / 1 | 6 | 1 / 1 | 0% | 0.17 |
| M4-P3B - Reservation Flow Pages | 99-100 | 04/24 ~ 04/24 | 5 / 51 | 1 | 0 | 0 | 0 / 0 | 14 / 958 | 1 / 30 | 0 / 5 / 1 | 6 | 1 / 1 | 0% | 0.17 |
| M4-P3C - Member / Order Integration 與 Browser Smoke | 99-100 | 04/24 ~ 04/24 | 5 / 51 | 1 | 0 | 0 | 0 / 0 | 14 / 958 | 1 / 30 | 0 / 5 / 1 | 6 | 1 / 1 | 0% | 0.17 |

## Interpretation Summary

這個 side project 的設計目標，不只是做出幾個 demo shop，而是驗證一種架構工作方法：先把主流程、系統層級邊界、contract 與 `.Core` orchestration 設計到穩定，讓大型客戶的高度差異化需求可以透過 extension、sidecar data、vertical API 與 storefront 擴充，而不是修改既有 binary code 或重寫主流程。

本報告的解讀前提是：此 side project 的一般 coding 主要由 AI coding agent 完成；人為介入集中在 `.Core` / `.Abstract` 的 interface code review、架構邊界判斷與 decision 收斂。因此，src / tests 異動量可視為 AI coding agent 承接的實作 workload proxy；Major/Core Decisions、Backtracking Decisions 與 Core/Abstract 異動則反映架構師需要介入主流程或核心邊界的程度。

較精準的結論不是「後期沒有 decision」，而是「後期仍有局部 decision 記錄，但 Major/Core Decisions 與 Backtracking Decisions 下降到 0，且 Core/Abstract 異動下降到 0」。這表示後期仍保留設計紀錄與 review trace，但不再需要回頭修改系統主幹。

## Milestone Summary

### M1 - 基礎建設

M1 的目的，是把原本由 API controller、固定 product collection 與單一折扣邏輯主導的系統，整理成可支援模組化商店的共同基礎。成果包含 `.Abstract` contract、`.Core` orchestration、shop runtime、discount rule、product service、checkout service、line-based cart、SKU / inventory、TimeProvider 與 order event dispatcher。統計上，M1 有 decisions `27`、Major/Core `2`、Backtracking `2`、Core/Abstract `52 / 1,670`，Core Touch `44%`。這是架構投入最集中的階段，反映前期確實把不穩定性集中在核心邊界與主流程設計上處理。

### M2 - 標準系統建立

M2 的目的，是建立標準商店 baseline，讓後續 vertical extension 可以重用 storefront family、BFF、auth/session、cart、checkout、member order 與本機驗證拓樸。統計上，M2 有 decisions `7`、Major/Core `1`、Backtracking `1`、Core/Abstract `11 / 165`，src `47 / 1,498`。這一段的重點是把 M1 的 Core 能力落地成可複製的標準應用骨架，讓後續 AI coding agent 可以沿用固定的 API / Storefront pattern。

### M3 - AppleBTS 擴充設計

M3 的目的，是以 AppleBTS 驗證第一個大型客戶式 vertical extension：campaign、offer、qualification、gift subsidy、BTS discount rule、AppleBTS API、AppleBTS Storefront 與 deployment topology。統計上，M3 有 decisions `14`、Major/Core `1`、Backtracking `1`，但 Core/Abstract 只剩 `6 / 81`，Core Touch `3%`。這表示第一個 vertical case 仍會暴露少量通用缺口，需要架構師判斷哪些能力應回補 Core；但主要 workload 已經轉向 extension API、storefront、seed、tests 與部署。

### M4 - PetShop 擴充設計

M4 的目的，是以 PetShop 驗證第二個、且結構差異更大的 vertical extension：服務預約不是一般商品，但可透過 hidden standard `Product` 接入既有 cart / checkout / order event flow。統計上，M4 有 decisions `8`、src `50 / 3,193`、tests `7 / 978`，但 Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`，Core Touch `0%`。這是最強的證明點：後期仍有大量程式碼與測試產出，但不需要重新打開 `.Core` / `.Abstract`，代表前期架構設計已足以支撐 AI coding agent 在穩定邊界內完成高 workload 擴充。

## Phase Summary

### M1 Phase Pattern

- `M1-P1 - Shop Runtime / DiscountRule Phase 1`：建立 shop runtime 與 discount rule contract，先把折扣擴充點從既有實作抽出來，讓後續 shop 差異可以透過 manifest 與 rule composition 接入。本階段統計為 docs `12 / 1,475`、decisions `3`、Major/Core `0`、Backtracking `0`、Core/Abstract `19 / 704`、src `31 / 1,040`、tests `4 / 128`。

- `M1-P2 - ProductService / Order Event Phase 1`：建立 product service 與 order event 的第一版邊界，把商品查詢、hidden product 與 checkout 後副作用從固定資料結構中拆開。本階段統計為 docs `5 / 788`、decisions `2`、Major/Core `0`、Backtracking `0`、Core/Abstract `12 / 224`、src `20 / 339`、tests `6 / 45`。

- `M1-P3 - CheckoutService Phase 2`：將 checkout orchestration 從 API controller 移入 `.Core`，讓交易一致性、buyer authorization 與 checkout lifecycle 成為可測試的核心能力。本階段統計為 docs `12 / 729`、decisions `3`、Major/Core `0`、Backtracking `0`、Core/Abstract `2 / 286`、src `7 / 728`、tests `1 / 180`。

- `M1-P4 - Cart / SKU / Inventory Core 回補`：針對後續 AppleBTS 暴露出的 cart line、SKU 與 inventory correctness 缺口，回補為通用 Core 能力，而不是讓第一個客戶案例自行承擔。本階段統計為 docs `67 / 9,152`、decisions `14`、Major/Core `0`、Backtracking `0`、Core/Abstract `11 / 277`、src `13 / 317`、tests `5 / 184`。

- `M1-P5 - 可測試時間與通用語意回補`：把時間來源、buyer satisfaction 語意與 cart line 操作收斂為可測試的共同能力，避免 storefront 或 vertical flow 以臨時方式繞過主流程。本階段統計為 docs `9 / 392`、decisions `2`、Major/Core `1`、Backtracking `1`、Core/Abstract `1 / 41`、src `23 / 1,234`、tests `1 / 39`。

- `M1-P6 - OrderEventDispatcher 邊界修正`：將 checkout 後副作用從 product service callback 拆成 order event dispatcher，讓 `IProductService` 回歸商品查詢邊界，並為 PetShop reservation confirmed transition 預留穩定接點。本階段統計為 docs `12 / 458`、decisions `3`、Major/Core `1`、Backtracking `1`、Core/Abstract `7 / 138`、src `9 / 157`、tests `1 / 4`。

M1 的 phase pattern 顯示，架構工作不是一次性畫圖，而是在具體案例暴露缺口時，持續判斷哪些能力應回到 `.Core` / `.Abstract`，哪些應留在 extension。這也是後續能交給 AI coding agent 大量實作的前提：agent 需要穩定邊界，否則只會把案例需求直接寫進主流程。

### M2 Phase Pattern

- `M2-P1 - Storefront Family / CommonStorefront Phase 1`：建立 storefront family、BFF、auth/session 與 browser 驗收原則，讓後續 vertical storefront 不必重新發明 UI / BFF 架構。本階段統計為 docs `15 / 1,575`、decisions `4`、Major/Core `0`、Backtracking `1`、Core/Abstract `9 / 161`、src `19 / 255`、tests `8 / 203`。

- `M2-P2 - CommonStorefront 實作與本機驗證拓樸`：實作 CommonStorefront 與本機驗證拓樸，把 M1 的 Core 能力落成可被重用的標準商店 flow。本階段統計為 docs `8 / 428`、decisions `3`、Major/Core `1`、Backtracking `0`、Core/Abstract `2 / 4`、src `28 / 1,243`、tests `1 / 48`。

M2 的 phase pattern 顯示，當 shared storefront grammar 與本機驗證拓樸定義完成後，實作工作可以明顯轉向可交付的 API / UI code。這一層把後續 AI coding agent 的工作從「理解整個系統」降低為「沿用既有 family pattern 實作新的 vertical」。

### M3 Phase Pattern

- `M3-P1 - Campaign 技術邊界與 `.Core` 回補方向`：先用 AppleBTS campaign 檢查 business / technical boundary，決定 BTS 是同一 shop 的 campaign，而不是另一個 shop 或另一套 checkout。本階段統計為 docs `12 / 266`、decisions `5`、Major/Core `0`、Backtracking `0`、Core/Abstract `4 / 37`、src `7 / 72`、tests `2 / 206`。

- `M3-P2 - AppleBTS Phase 1 Spec / Skeleton`：凍結 AppleBTS spec、testcase 與 extension skeleton，讓 campaign、offer、qualification 與 gift subsidy 成為 extension 內的明確模型。本階段統計為 docs `9 / 997`、decisions `3`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `15 / 346`、tests `7 / 742`。

- `M3-P3 - AppleBTS API / Seed / Local Topology`：實作 AppleBTS API、seed、module registration 與 local topology，驗證 extension 能被獨立啟動並透過標準 cart / checkout 接入主流程。本階段統計為 docs `6 / 361`、decisions `3`、Major/Core `0`、Backtracking `0`、Core/Abstract `1 / 3`、src `31 / 1,252`、tests `6 / 61`。

- `M3-P4 - AppleBTS Storefront Phase 1`：建立 AppleBTS Storefront，沿用 CommonStorefront 的 auth/session/BFF/UI grammar，只補 BTS catalog、qualification 與 gift flow orchestration。本階段統計為 docs `9 / 392`、decisions `2`、Major/Core `1`、Backtracking `1`、Core/Abstract `1 / 41`、src `23 / 1,234`、tests `1 / 39`。

- `M3-P5 - 折扣拆分與部署文件收斂`：把折扣輸出與部署文件收斂在 AppleBTS extension / topology 層，證明業務語意修正可以留在 vertical 層，不必回頭改 checkout 主流程。本階段統計為 docs `8 / 663`、decisions `1`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `2 / 54`、tests `2 / 74`。

M3 的 phase pattern 是第一個擴充案例的驗證：初期仍會暴露少量通用缺口，需要架構師判斷是否回補 Core；但大部分產出已轉向 extension API、storefront、seed、tests 與部署。這代表設計方法開始把人為介入集中在邊界判斷，而不是日常 coding。

### M4 Phase Pattern

- `M4-P1A - Reservation / Hidden Product Projection 核心模型`：先固定 reservation 與 hidden standard `Product` projection 的核心模型，讓 PetShop 這種服務預約案例也能接入既有 cart / checkout / order event flow。本階段統計為 docs `10 / 1,500`、decisions `3`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `8 / 488`、tests `2 / 303`。

- `M4-P2A - PetShop Extension Implementation`：實作 PetShop extension domain、repository、product service decorator 與 order event dispatcher，直接在 extension boundary 內完成 domain 行為。本階段統計為 docs `10 / 1,500`、decisions `3`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `8 / 488`、tests `2 / 303`。

- `M4-P4 - PetShop Discount / Promotion`：補上 PetShop reservation 搭配一般商品滿額折扣，將促銷規則保留在 extension discount rule，而不是放進 checkout 主流程。本階段統計為 docs `6 / 211`、decisions `1`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `5 / 162`、tests `2 / 311`。

- `M4-P1B - PetShop Lifecycle / API Spec`：定義 reservation lifecycle 與 API contract，讓 storefront 或測試流程能透過 PetShop API 建立 hold、查詢狀態與取得 checkout product id。本階段統計為 docs `6 / 818`、decisions `2`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `17 / 1,268`、tests `2 / 334`。

- `M4-P2B - PetShop API`：實作 `/petshop-api/*`，涵蓋 service catalog、availability、reservation hold、owner isolation、cancel hold 與 reservation status。本階段統計為 docs `6 / 818`、decisions `2`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `17 / 1,268`、tests `2 / 334`。

- `M4-P2C - PetShop Host / Seed / Config`：完成 PetShop host、seed、config 與 compose topology，讓 PetShop 可以用獨立 shop runtime 做 API-level E2E 驗證。本階段統計為 docs `6 / 818`、decisions `2`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `17 / 1,268`、tests `2 / 334`。

- `M4-P3A - PetShop Storefront Spec / Skeleton`：固定 PetShop Storefront 的 route、BFF client、testcase 與最小 skeleton，讓後續頁面實作遵循已定義的 storefront family pattern。本階段統計為 docs `5 / 608`、decisions `1`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `6 / 317`、tests `0 / 0`。

- `M4-P3 - PetShop Storefront`：完成 PetShop consumer-facing reservation、cart、checkout、member reservation/order flow 與 browser smoke，讓完整 vertical storefront 在既有主流程上運作。本階段統計為 docs `5 / 51`、decisions `1`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `14 / 958`、tests `1 / 30`。

- `M4-P3B - Reservation Flow Pages`：實作 reservation flow pages，讓使用者可以建立 hold、加入標準 cart，並在 checkout 前取消 hold。本階段統計為 docs `5 / 51`、decisions `1`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `14 / 958`、tests `1 / 30`。

- `M4-P3C - Member / Order Integration 與 Browser Smoke`：補齊 member/order integration 與 browser smoke，確認 checkout completed 後 reservation 可以轉為 confirmed，並可在 storefront 看到狀態。本階段統計為 docs `5 / 51`、decisions `1`、Major/Core `0`、Backtracking `0`、Core/Abstract `0 / 0`、src `14 / 958`、tests `1 / 30`。

M4 的 phase pattern 是整份報告最強的成效訊號：PetShop 是與 AppleBTS 結構差異很大的第二個 vertical case，但所有 phase 的 Major/Core Decisions、Backtracking Decisions 與 Core/Abstract 異動都維持為 0。這代表架構師不需要重新打開主流程，AI coding agent 可以直接在既有 extension、API、storefront 與 test pattern 中完成高工作量輸出。
