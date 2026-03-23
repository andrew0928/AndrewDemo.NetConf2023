# 處理原則

- Keep the first pass thin, modular, and architecture-first.
- Summarize plan first, then inspect repo, then implement.
- Consistency first: canonical 路徑、欄位名、術語一旦變更，應同步更新 source code / docs / demo / spec；除非明確要求相容，否則不要長期保留雙軌命名。
- Decisions, 討論過程中我回覆的重要決策, 按照 ./docs/decisions/README.md 的規範記錄

## 階段化開發流程

- Phase 1 是確認規格階段：
  - 產出 `/spec` 規格文件
  - 產出 `/spec/testcases` 測試案例文件
  - 產出 `/src/AndrewDemo.NetConf2023.Abstract` contract project，放 C# interfaces 與 models
  - 此階段 `/spec` 與 `Abstract` 可以異動
  - 此階段完成後，`/spec` 與 `Abstract` 視為定案，不應在後續重構階段任意變更
- Phase 2 是執行重構階段：
  - 依定案後的 `/spec` 與 `Abstract` 重構 `.Core` 與 host 專案
  - 此階段應高度交給 agent 自主開發、自我驗證，直到交付 review
  - 此階段不可異動 `AndrewDemo.NetConf2023.Abstract`，除非我明確重新開啟規格階段
- 若需要配合階段交付驗證：
  - 優先補 `/spec/testcases`
  - 必要時搭配 `AndrewDemo.NetConf2023.Core.Tests` 作為交付內容的一部分

## 文件放置原則

- `/docs`:
  - 放與我溝通、幫助我理解設計、用來評估是否採用的說明文件
  - 這些文件預設是給我閱讀與討論用
- `/spec`:
  - 放正式規格、公開合約、測試案例與會影響後續開發協作的資訊
  - 這些文件是未來所有開發與協作者都應依循的基準
- `/src/AndrewDemo.NetConf2023.Abstract`:
  - 放正式 contract
  - Phase 1 定案後，Phase 2 不得隨意更動



# Git commit message format

1. (第一行) 說明這次變更的目標
2. milestones: 列出已完成的里程碑
3. changes: 列出已完成的具體變更
4. fixes: 修正的問題清單 (如果有的話)
5. comments: 其他注意事項 (如果有的話)

Commit message 請包含以上三段，依序撰寫。

# General Rules

- Write docs / comments / answer (in chat) in Traditional Chinese (zh-TW) please.


# Repo Structures

- /docs:    說明文件
- /spec:    規格文件
- /src:     source code, root of dotnet solution file, use dotnet 10 as default runtime
- README.md 

# Long-term Project Direction

- 後續所有討論、設計與實作，預設都應對齊 `/docs/project-roadmap.md`
- 若新需求與上述主線關聯很弱，應先明確說明它為何值得插隊。
- 發展主線 roadmap
    - 模組化, 依據不同客戶設定, 載入不同的模組
    - DiscountRule 模組化
    - ProductService 模組化
