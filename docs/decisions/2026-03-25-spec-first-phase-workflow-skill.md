# Spec-First Phase Workflow skill 化

## 狀態

- accepted
- 日期：2026-03-25

## 背景

這個 repo 已經反覆確認出一套穩定的合作模式：

- 先做架構與邊界評估，再決定實作
- 先做 Phase 1 規格與 contract freeze，再進 Phase 2 重構
- review 需先列 findings，再談摘要
- 歷史版本分析需依 exact snapshot 建立 phase 文件

這些原則目前分散在：

- `docs/decisions/`
- `AGENTS.md`
- 其他 phase 文件與測試

若只靠每次重新口述，容易遺漏 phase gate、source priority、單線程 msbuild 驗證規則與文件放置原則。

## 決策

### 1. 將這套合作流程整理成可重複使用的通用 software development skill

skill 名稱定為：

- `spec-first-phase-workflow`

用途是讓後續工作的 agent，能直接套用這套 spec-first、phase-gated 的開發流程，而不必每次重新從零歸納。

### 2. skill 的資訊來源優先順序固定如下

1. 當前 user instructions
2. `docs/decisions/`
3. `AGENTS.md`
4. 其他參考資訊

### 3. skill 需明確區分三種工作模式

- Phase 1：確認規格
- Phase 2：依 frozen spec / contract 執行重構
- version analysis：依 commit snapshot 產生 phase 文件

不得把這三種模式混成單一含糊流程。

### 4. skill 必須保留以下不可違反的工作規則

- 先摘要計畫，再 inspect repo，再 implement
- first pass 保持 thin、modular、architecture-first
- canonical 命名一旦變更，需同步更新 source / docs / demo / spec / tests
- 重要決策需寫入 `docs/decisions/`
- build/test 一律單線程 msbuild，使用 `-m:1`
- chat、docs、comments 預設使用繁體中文

### 5. repo 內保留可追溯草稿，確認後再同步到 Codex home

- repo 內保留 skill 草稿與來源文件，方便 review 與後續調整
- 確認後再同步一份到 `~/.codex/skills`，供未來工作輪次自動發現

## 影響

- 後續 agent 更容易遵守這套 phase gate 與 review 節奏
- 新工作輪次較不容易遺漏 source priority 與 decision logging
- 版本分析與 Phase 1 / Phase 2 交付格式可保持一致

## 替代方案

### 替代方案 A：只保留 `AGENTS.md`

優點：

- 維護點較少

缺點：

- 無法涵蓋已反覆確認的細節
- 容易遺失版本分析輸出格式與 review 節奏

結論：

- 不採用

### 替代方案 B：只保留零散文件，不建立 skill

缺點：

- 每次仍要重新人工抽取規則
- 無法直接被 Codex 作為 reusable workflow 載入

結論：

- 不採用

## 後續工作

1. 完成 `spec-first-phase-workflow` skill 內容。
2. 驗證 skill frontmatter 與 metadata。
3. 待 review 確認後再安裝到 `~/.codex/skills`。
4. 後續若這套流程再演進，優先更新 decision 與 skill 草稿，再同步更新已安裝版本。
