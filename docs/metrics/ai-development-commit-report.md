# AI 開發前後 Commit 生產速度與顆粒度分析

- 產生時間: 2026/04/28 12:07
- Commit 範圍: `d8f6948` - `f802bb1`
- Commit 數: 101
- AI 開發切分日: 2026/03/01
- Raw CSV: `docs/metrics/ai-development-commit-report.csv`
- 統計口徑: `lines change = added + deleted`；binary 檔案行數以 0 計。
- 表格中的 `docs` / `src` / `tests` / `spec` 欄位格式為 `異動檔案數/異動行數`。
- 日期與區間彙總的檔案數為 commit file change 加總；同一檔案同日多次提交會重複計入。
- Commit 清單的 title 取自 git subject；若 subject 同行包含 `milestones` / `changes` / `fixes` / `comments` 區段，清單只顯示區段前的目標文字。

## AI 前後速度摘要

| 區間 | 日期範圍 | commits | 有 commit 天數 | calendar days | commits/有 commit 日 | commits/calendar day | total | avg files/commit | avg lines/commit | 顆粒度分布 |
|---|---|---:|---:|---:|---:|---:|---|---:|---:|---|
| AI 前 | 2023/12/04 - 2025/11/03 | 64 | 28 | 701 | 2.29 | 0.09 | 574/161190 | 8.97 | 2518.59 | 小 19 / 中 31 / 大 14 / 混合 0 |
| AI 後 | 2026/03/23 - 2026/04/27 | 37 | 13 | 36 | 2.85 | 1.03 | 672/39940 | 18.16 | 1079.46 | 小 4 / 中 4 / 大 12 / 混合 17 |

## 日期統計表

| 日期 | commits | total | docs | src | tests | spec | 顆粒度分布 |
|---|---:|---|---|---|---|---|---|
| 2023/12/04 | 3 | 8/709 | 0/0 | 0/0 | 0/0 | 0/0 | 小 1 / 中 1 / 大 1 / 混合 0 |
| 2023/12/05 | 1 | 7/152 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2023/12/07 | 4 | 5/93 | 0/0 | 0/0 | 0/0 | 0/0 | 小 4 / 中 0 / 大 0 / 混合 0 |
| 2023/12/08 | 1 | 2/55 | 0/0 | 0/0 | 0/0 | 0/0 | 小 1 / 中 0 / 大 0 / 混合 0 |
| 2023/12/27 | 1 | 11/204 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2023/12/28 | 1 | 5/154 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2023/12/29 | 1 | 9/372 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2024/01/01 | 1 | 17/475 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 0 / 大 1 / 混合 0 |
| 2024/01/07 | 2 | 9/444 | 0/0 | 0/0 | 0/0 | 0/0 | 小 1 / 中 0 / 大 1 / 混合 0 |
| 2024/01/08 | 2 | 6/220 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 2 / 大 0 / 混合 0 |
| 2024/01/15 | 2 | 90/75200 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 1 / 混合 0 |
| 2024/01/16 | 1 | 9/590 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 0 / 大 1 / 混合 0 |
| 2024/01/17 | 3 | 10/504 | 0/0 | 0/0 | 0/0 | 0/0 | 小 1 / 中 2 / 大 0 / 混合 0 |
| 2024/01/18 | 1 | 9/893 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 0 / 大 1 / 混合 0 |
| 2024/02/08 | 4 | 13/981 | 0/0 | 0/0 | 0/0 | 0/0 | 小 2 / 中 0 / 大 2 / 混合 0 |
| 2024/02/09 | 6 | 23/1094 | 0/0 | 0/0 | 0/0 | 0/0 | 小 2 / 中 3 / 大 1 / 混合 0 |
| 2024/02/11 | 1 | 4/22 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2024/02/16 | 3 | 7/44 | 0/0 | 0/0 | 0/0 | 0/0 | 小 3 / 中 0 / 大 0 / 混合 0 |
| 2024/02/18 | 1 | 1/19 | 0/0 | 0/0 | 0/0 | 0/0 | 小 1 / 中 0 / 大 0 / 混合 0 |
| 2024/06/26 | 1 | 8/118 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2024/06/30 | 1 | 8/118 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2024/07/01 | 1 | 1/12 | 0/0 | 0/0 | 0/0 | 0/0 | 小 1 / 中 0 / 大 0 / 混合 0 |
| 2025/07/23 | 1 | 3/82 | 0/0 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2025/10/20 | 2 | 115/106 | 0/0 | 113/55 | 0/0 | 0/0 | 小 0 / 中 1 / 大 1 / 混合 0 |
| 2025/10/28 | 2 | 84/75491 | 1/423 | 81/75032 | 0/0 | 0/0 | 小 0 / 中 0 / 大 2 / 混合 0 |
| 2025/11/01 | 7 | 48/1401 | 0/0 | 34/1067 | 13/326 | 0/0 | 小 1 / 中 5 / 大 1 / 混合 0 |
| 2025/11/02 | 8 | 42/1179 | 0/0 | 35/1085 | 6/92 | 0/0 | 小 1 / 中 7 / 大 0 / 混合 0 |
| 2025/11/03 | 2 | 20/458 | 0/0 | 16/275 | 0/0 | 0/0 | 小 0 / 中 1 / 大 1 / 混合 0 |
| 2026/03/23 | 4 | 67/3685 | 10/1441 | 43/1222 | 5/130 | 7/822 | 小 1 / 中 0 / 大 1 / 混合 2 |
| 2026/03/24 | 7 | 73/4745 | 30/3158 | 30/1071 | 7/225 | 6/291 | 小 1 / 中 1 / 大 4 / 混合 1 |
| 2026/03/30 | 1 | 9/905 | 8/905 | 0/0 | 0/0 | 0/0 | 小 0 / 中 0 / 大 1 / 混合 0 |
| 2026/03/31 | 1 | 2/15 | 1/11 | 0/0 | 0/0 | 0/0 | 小 1 / 中 0 / 大 0 / 混合 0 |
| 2026/04/01 | 6 | 73/6561 | 28/4714 | 20/389 | 7/390 | 18/1068 | 小 0 / 中 2 / 大 2 / 混合 2 |
| 2026/04/02 | 3 | 40/2565 | 7/883 | 23/799 | 8/769 | 2/114 | 小 0 / 中 0 / 大 2 / 混合 1 |
| 2026/04/04 | 3 | 102/4108 | 17/1442 | 60/1292 | 15/266 | 4/494 | 小 0 / 中 0 / 大 1 / 混合 2 |
| 2026/04/05 | 2 | 131/5214 | 11/637 | 107/3933 | 2/87 | 6/183 | 小 0 / 中 0 / 大 0 / 混合 2 |
| 2026/04/06 | 1 | 1/246 | 1/246 | 0/0 | 0/0 | 0/0 | 小 0 / 中 1 / 大 0 / 混合 0 |
| 2026/04/09 | 1 | 12/549 | 3/388 | 2/54 | 2/74 | 4/29 | 小 0 / 中 0 / 大 0 / 混合 1 |
| 2026/04/23 | 3 | 62/3644 | 18/1744 | 28/831 | 6/644 | 10/425 | 小 0 / 中 0 / 大 0 / 混合 3 |
| 2026/04/24 | 4 | 96/5839 | 10/418 | 69/3524 | 5/392 | 6/1059 | 小 1 / 中 0 / 大 0 / 混合 3 |
| 2026/04/27 | 1 | 4/1864 | 2/290 | 0/0 | 0/0 | 0/0 | 小 0 / 中 0 / 大 1 / 混合 0 |

## 日期清單

### 2023/12/04

> 摘要: commits 3, total 8/709, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `d8f6948` - Add .gitignore and .gitattributes. (total 2/426, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
- `ea06300` - Add project files. (total 4/264, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)
- `487ec61` - fix (total 2/19, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2023/12/05

> 摘要: commits 1, total 7/152, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `2fecb6a` - round2 (total 7/152, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2023/12/07

> 摘要: commits 4, total 5/93, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `728f35c` - add: event (total 1/34, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `d9a9497` - add: checkout message (total 1/11, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `ff5aa63` - add: waiting room demo (total 2/43, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `3da367f` - clean code (total 1/5, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2023/12/08

> 摘要: commits 1, total 2/55, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `2be4b4a` - final code for .net conf 2023 (total 2/55, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2023/12/27

> 摘要: commits 1, total 11/204, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `166f527` - WIP - add api project (total 11/204, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2023/12/28

> 摘要: commits 1, total 5/154, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `75bb677` - WIP add: products, carts api controller add: http client (total 5/154, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2023/12/29

> 摘要: commits 1, total 9/372, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `c6ddfb2` - commit: api ready (total 9/372, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2024/01/01

> 摘要: commits 1, total 17/475, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `63d0af3` - add: docker support add: publish settings add: readme add: enable & write xml comments remove: not necessary files - weatherforecast.cs (total 17/475, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)

### 2024/01/07

> 摘要: commits 2, total 9/444, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `66ed58b` - v4 - add: access token - many changes (total 8/427, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
- `29fda60` - version: 4.0.1 prompt: change prompt (total 1/17, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2024/01/08

> 摘要: commits 2, total 6/220, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `a62118e` - v4.1.0 - /api/carts: estimate support report discounts - model refactor: hide Cart.ProdQtyMap - model ext: add Cart.EstimateDiscounts() - readme.md: update prompt for v4.1.0 (total 4/74, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)
- `4f3c83d` - update prompt (total 2/146, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2024/01/15

> 摘要: commits 2, total 90/75200, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `85cd2f4` - WIP (total 84/75097, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
- `f478c29` - WIP (total 6/103, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2024/01/16

> 摘要: commits 1, total 9/590, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `778acf4` - WIP (total 9/590, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)

### 2024/01/17

> 摘要: commits 3, total 10/504, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `de5d8c8` - - change: http script - change: products content - change: products model - update: my gpt settings (total 5/265, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)
- `f18a312` - - 拆分 signin form / process authorize request - clean code (total 4/233, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)
- `5a2fac2` - fix: 修正目前是忽略密碼，但是 Member.Login() 沒有密碼會登入失敗 (total 1/6, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2024/01/18

> 摘要: commits 1, total 9/893, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `c8e0182` - clean code, move files (total 9/893, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)

### 2024/02/08

> 摘要: commits 4, total 13/981, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `561248b` - fix: discount estimate bug (total 1/6, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `281eeef` - add: console ui application demo (total 7/415, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
- `ab3cb14` - release: console-ui 1.0 (total 1/1, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `2b05151` - refec: seperate to partial class (total 4/559, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)

### 2024/02/09

> 摘要: commits 6, total 23/1094, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `96f364d` - release: copilot v1 (total 6/408, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
- `c1b108a` - WIP (total 5/259, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)
- `bcf040a` - clean code (total 3/166, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)
- `9a7e80b` - done (total 3/43, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `848cde7` - release (total 3/186, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)
- `9fe4dd0` - release (total 3/32, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2024/02/11

> 摘要: commits 1, total 4/22, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `2e774dc` - release v1.0 - for article (total 4/22, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2024/02/16

> 摘要: commits 3, total 7/44, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `8138243` - add console trace fix message (total 3/8, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `d84fa62` - fix (total 2/32, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `2b9226a` - fix (total 2/4, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2024/02/18

> 摘要: commits 1, total 1/19, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `0e9f0ef` - add: feature toggle (total 1/19, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2024/06/26

> 摘要: commits 1, total 8/118, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `03a0e73` - changes (5.0.0 -> 5.2.0) (total 8/118, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2024/06/30

> 摘要: commits 1, total 8/118, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `64a9683` - Merge branch 'devopsdays2024' (total 8/118, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2024/07/01

> 摘要: commits 1, total 1/12, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `beb0bf8` - - change model deployment to GPT4o - fix system prompt (total 1/12, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2025/07/23

> 摘要: commits 1, total 3/82, docs 0/0, src 0/0, tests 0/0, spec 0/0

- `0652ad1` - Update .gitignore, Dockerfile, and README.md (total 3/82, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2025/10/20

> 摘要: commits 2, total 115/106, docs 0/0, src 113/55, tests 0/0, spec 0/0

- `32501c5` - folder structure refactory (total 111/36, docs 0/0, src 110/33, tests 0/0, spec 0/0, 顆粒度 大)
- `f324b42` - 目標：整合 OAuth 認證機制並建立容器化建置流程 (total 4/70, docs 0/0, src 3/22, tests 0/0, spec 0/0, 顆粒度 中)

### 2025/10/28

> 摘要: commits 2, total 84/75491, docs 1/423, src 81/75032, tests 0/0, spec 0/0

- `fa63240` - add oauth2 documents (total 1/423, docs 1/423, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
- `c7e8690` - clean up solutions (total 83/75068, docs 0/0, src 81/75032, tests 0/0, spec 0/0, 顆粒度 大)

### 2025/11/01

> 摘要: commits 7, total 48/1401, docs 0/0, src 34/1067, tests 13/326, spec 0/0

- `c74d9c6` - LiteDB 核心儲存遷移 (total 8/366, docs 0/0, src 8/366, tests 0/0, spec 0/0, 顆粒度 中)
- `4d41ef3` - 新增 Git 提交訊息生成指導文件 (total 1/8, docs 0/0, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)
- `ac5450d` - 新增 Core LiteDB 持久化基本測試 (total 6/192, docs 0/0, src 0/0, tests 6/192, spec 0/0, 顆粒度 中)
- `a67da58` - LiteDB 整合 API 與 Console (total 6/110, docs 0/0, src 6/110, tests 0/0, spec 0/0, 顆粒度 中)
- `11bd1df` - LiteDB 整合下游專案調整 (total 5/178, docs 0/0, src 5/178, tests 0/0, spec 0/0, 顆粒度 中)
- `451e284` - feat: 引入 ShopDatabaseContext 取代 LiteDbContext (total 8/349, docs 0/0, src 8/349, tests 0/0, spec 0/0, 顆粒度 中)
- `7ee611b` - refactor: 引入 ShopDatabase.Create 並淘汰靜態 CRUD (total 14/198, docs 0/0, src 7/64, tests 7/134, spec 0/0, 顆粒度 大)

### 2025/11/02

> 摘要: commits 8, total 42/1179, docs 0/0, src 35/1085, tests 6/92, spec 0/0

- `c60d37f` - feat(core): 開放 ShopDatabase 供跨專案使用 (total 4/25, docs 0/0, src 4/25, tests 0/0, spec 0/0, 顆粒度 中)
- `6c53052` - feat(console): 改用 ShopDatabase 流程取代舊版靜態 API (total 4/227, docs 0/0, src 4/227, tests 0/0, spec 0/0, 顆粒度 中)
- `fd36e15` - feat(console): 調整 Azure OpenAI 環境載入流程 (total 4/89, docs 0/0, src 3/87, tests 0/0, spec 0/0, 顆粒度 中)
- `61ea2f5` - refactor(api): 改用 ShopDatabase 取代舊版靜態 API (total 6/176, docs 0/0, src 6/176, tests 0/0, spec 0/0, 顆粒度 中)
- `cc7daff` - 重構 Core 專案移除 ShopDatabase.Current 靜態依賴 (total 12/385, docs 0/0, src 7/295, tests 5/90, spec 0/0, 顆粒度 中)
- `398de72` - 重構 Core 與 Tests: 移除 ShopDatabase.Current 靜態依賴 (total 1/2, docs 0/0, src 0/0, tests 1/2, spec 0/0, 顆粒度 小)
- `b3d251f` - 重構 API 專案: 實作依賴注入移除靜態依賴 (total 7/248, docs 0/0, src 7/248, tests 0/0, spec 0/0, 顆粒度 中)
- `5cec411` - 重構 ConsoleUI 專案: 使用靜態 Database 屬性取代 ShopDatabase.Current (total 4/27, docs 0/0, src 4/27, tests 0/0, spec 0/0, 顆粒度 中)

### 2025/11/03

> 摘要: commits 2, total 20/458, docs 0/0, src 16/275, tests 0/0, spec 0/0

- `988c16b` - feat(deploy): 新增資料庫初始化工具與 Docker Compose 部署環境 (total 16/358, docs 0/0, src 13/205, tests 0/0, spec 0/0, 顆粒度 大)
- `cb466d4` - 修正與強化 seed init container 的建置與啟動腳本 (total 4/100, docs 0/0, src 3/70, tests 0/0, spec 0/0, 顆粒度 中)

### 2026/03/23

> 摘要: commits 4, total 67/3685, docs 10/1441, src 43/1222, tests 5/130, spec 7/822

- `d0401c1` - fix(oauth2): comment out local baseURL and update access token code feat(vscode): add settings to disable cloud sync (total 2/10, docs 0/0, src 1/7, tests 0/0, spec 0/0, 顆粒度 小)
- `97e5876` - 建立 Phase 1 規格與 shop runtime / discount plugin 前置骨架 (total 34/1255, docs 2/240, src 26/649, tests 3/80, spec 2/219, 顆粒度 混合)
- `37470db` - 收斂 Phase 1 折扣與商店 runtime 契約並補齊開發模式 OAuth 流程 (total 26/1632, docs 5/747, src 16/566, tests 2/50, spec 3/269, 顆粒度 混合)
- `dfa7fbb` - 補 Product Phase 1 決策與規格文件 (total 5/788, docs 3/454, src 0/0, tests 0/0, spec 2/334, 顆粒度 大)

### 2026/03/24

> 摘要: commits 7, total 73/4745, docs 30/3158, src 30/1071, tests 7/225, spec 6/291

- `af175a0` - 對齊 Product Phase 1 抽象契約與字串商品識別碼 (total 15/134, docs 0/0, src 11/104, tests 4/30, spec 0/0, 顆粒度 大)
- `37ae0a2` - 建立 DefaultProductService 並讓訂單支援 fulfillment 狀態 (total 13/252, docs 0/0, src 11/237, tests 2/15, spec 0/0, 顆粒度 大)
- `48ae67b` - 補 Checkout Phase 2 搬移決策與規格文件 (total 4/503, docs 2/315, src 0/0, tests 0/0, spec 2/188, 顆粒度 大)
- `15f2b31` - 完成 CheckoutService 搬移並修正交易一致性與 buyer 驗證 (total 13/888, docs 4/123, src 4/482, tests 1/180, spec 4/103, 顆粒度 混合)
- `5e05eb8` - 修正 ConsoleUI 與現行 Core 契約的相容性 (total 3/246, docs 0/0, src 3/246, tests 0/0, spec 0/0, 顆粒度 中)
- `bb5122c` - 升級 ConsoleUI 的 SemanticKernel 套件版本 (total 1/2, docs 0/0, src 1/2, tests 0/0, spec 0/0, 顆粒度 小)
- `6e7249b` - 整理 phase0 到 phase2 的版本分析文件 (total 24/2720, docs 24/2720, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)

### 2026/03/30

> 摘要: commits 1, total 9/905, docs 8/905, src 0/0, tests 0/0, spec 0/0

- `0d192b9` - 整理 spec-first workflow skill 與 review 摘要 (total 9/905, docs 8/905, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)

### 2026/03/31

> 摘要: commits 1, total 2/15, docs 1/11, src 0/0, tests 0/0, spec 0/0

- `0997fe3` - Update .gitignore to include .env and worktree directories; enhance decision index in README with new entries (total 2/15, docs 1/11, src 0/0, tests 0/0, spec 0/0, 顆粒度 小)

### 2026/04/01

> 摘要: commits 6, total 73/6561, docs 28/4714, src 20/389, tests 7/390, spec 18/1068

- `b739fdb` - 釐清 shop runtime 的 tenant isolation mode 基本原則 (total 5/300, docs 2/99, src 0/0, tests 0/0, spec 3/201, 顆粒度 中)
- `aa86636` - 暫存 Apple BTS 設計探索文件 (total 7/1839, docs 7/1839, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
- `a80777d` - 收斂 Apple BTS campaign 商業規格並移除多 ShopId 探索文件 (total 10/2257, docs 8/1945, src 0/0, tests 0/0, spec 2/312, 顆粒度 大)
- `c723739` - 完成 Core 購物車與 SKU 庫存主線重構 (total 30/1621, docs 5/698, src 13/317, tests 5/184, spec 7/422, 顆粒度 混合)
- `36cfb35` - 收斂 DiscountRecord 與 Apple BTS 簡化規格 (total 9/251, docs 5/130, src 0/0, tests 0/0, spec 4/121, 顆粒度 中)
- `73778c6` - 擴充 DiscountRecord 的 hint 與 line 關聯語意 (total 12/293, docs 1/3, src 7/72, tests 2/206, spec 2/12, 顆粒度 混合)

### 2026/04/02

> 摘要: commits 3, total 40/2565, docs 7/883, src 23/799, tests 8/769, spec 2/114

- `c9f4e0e` - 確認 AppleBTS Phase 1 規格與 skeleton 基線 (total 31/1612, docs 7/883, src 17/367, tests 5/248, spec 2/114, 顆粒度 混合)
- `c26c55d` - 確認 AppleBTS Phase 2 測試基線已 ready (total 3/521, docs 0/0, src 0/0, tests 3/521, spec 0/0, 顆粒度 大)
- `de2c975` - 完成 AppleBTS 最小閉環實作 (total 6/432, docs 0/0, src 6/432, tests 0/0, spec 0/0, 顆粒度 大)

### 2026/04/04

> 摘要: commits 3, total 102/4108, docs 17/1442, src 60/1292, tests 15/266, spec 4/494

- `2826795` - 完成 AppleBTS 本機測試環境與時間模擬規劃 (total 54/1895, docs 6/361, src 38/1025, tests 7/63, spec 0/0, 顆粒度 混合)
- `10b74b9` - 以 TimeProvider 重構系統時間來源並補 time-shift 測試環境 (total 37/702, docs 4/64, src 22/267, tests 8/203, spec 0/0, 顆粒度 混合)
- `7bad526` - 確認 storefront family 與 CommonStorefront 的 phase1 規格 (total 11/1511, docs 7/1017, src 0/0, tests 0/0, spec 4/494, 顆粒度 大)

### 2026/04/05

> 摘要: commits 2, total 131/5214, docs 11/637, src 107/3933, tests 2/87, spec 6/183

- `d330960` - 完成 CommonStorefront 實作與本機 compose 驗證拓樸 (total 69/2538, docs 6/426, src 58/1972, tests 1/48, spec 2/2, 顆粒度 混合)
- `c363365` - 建置 AppleBTS Storefront Phase 1 與補齊 storefront 訂單折扣顯示 (total 62/2676, docs 5/211, src 49/1961, tests 1/39, spec 4/181, 顆粒度 混合)

### 2026/04/06

> 摘要: commits 1, total 1/246, docs 1/246, src 0/0, tests 0/0, spec 0/0

- `1d5c7c2` - 補 AppleBTS DatabaseInit 資料說明文件 (total 1/246, docs 1/246, src 0/0, tests 0/0, spec 0/0, 顆粒度 中)

### 2026/04/09

> 摘要: commits 1, total 12/549, docs 3/388, src 2/54, tests 2/74, spec 4/29

- `64343cd` - 修正 AppleBTS 折扣拆分與補齊部署說明文件 (total 12/549, docs 3/388, src 2/54, tests 2/74, spec 4/29, 顆粒度 混合)

### 2026/04/23

> 摘要: commits 3, total 62/3644, docs 18/1744, src 28/831, tests 6/644, spec 10/425

- `9a4ba58` - 拆分訂單事件邊界為 OrderEventDispatcher (total 26/623, docs 6/337, src 13/161, tests 1/4, spec 6/121, 顆粒度 混合)
- `6cd651f` - PetShop reservation hidden product projection (total 23/2337, docs 8/1300, src 10/508, tests 3/329, spec 2/200, 顆粒度 混合)
- `9a3fc9a` - Implement PetShop reservation purchase discount (total 13/684, docs 4/107, src 5/162, tests 2/311, spec 2/104, 顆粒度 混合)

### 2026/04/24

> 摘要: commits 4, total 96/5839, docs 10/418, src 69/3524, tests 5/392, spec 6/1059

- `8354791` - 完成 PetShop API 與本機驗證拓樸 (total 42/3169, docs 4/239, src 28/1550, tests 3/361, spec 2/579, 顆粒度 混合)
- `05b2195` - 建立 PetShop Storefront P3A 規格與骨架 (total 20/1064, docs 3/143, src 15/456, tests 0/0, spec 2/465, 顆粒度 混合)
- `7dffc9b` - 修正 PetShop checkout manifest dispatcher 設定 (total 3/32, docs 0/0, src 1/1, tests 2/31, spec 0/0, 顆粒度 小)
- `299f32f` - 完成 PetShop Storefront 預約流程與 compose 驗收 (total 31/1574, docs 3/36, src 25/1517, tests 0/0, spec 2/15, 顆粒度 混合)

### 2026/04/27

> 摘要: commits 1, total 4/1864, docs 2/290, src 0/0, tests 0/0, spec 0/0

- `f802bb1` - 建立 roadmap git history metrics 與 AI coding 成效報告 (total 4/1864, docs 2/290, src 0/0, tests 0/0, spec 0/0, 顆粒度 大)
