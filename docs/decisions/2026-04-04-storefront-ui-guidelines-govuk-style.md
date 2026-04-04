# Storefront UI 指南採 GOV.UK 類型風格

## 狀態

- accepted
- 日期：2026-04-04

## 背景

storefront 的主要目的是展示與驗證業務流程，不是做品牌形象官網。需求已明確要求：

- UI 簡潔清楚即可
- 必須支援無障礙設計
- 必須支援行動裝置友善的 RWD
- 在達成上述目標的前提下，實作技術與架構越簡單越好

## 決策

- storefront UI 參考風格採 GOV.UK / USWDS 類型的任務導向極簡風格
- 第一版 storefront 採 ASP.NET Core Razor Pages 或 MVC server-rendered page
- 第一版不以 Apple 官網式品牌視覺作為目標
- 第一版不導入 Node.js host 或 SPA framework 作為必要前提
- 無障礙基準採 WCAG 2.2 AA
- RWD 採 mobile-first

## 影響

- UI 實作可維持低複雜度
- auth、session、BFF 與頁面 rendering 可維持在同一個 ASP.NET Core host
- CommonStorefront 可作為後續 AppleBTS / PetShop 的基礎 UI grammar
- vertical-specific storefront 不需要各自長出完全不同的視覺語言

## 替代方案

### 1. Apple 官網式品牌導向設計

不採用。這類設計偏重品牌敘事、動態展示與大圖視覺，不符合展示用 PoC 的成本與目標。

### 2. 以 React / SPA 作為第一版必要基礎

不採用。對目前需求來說，這會先引入不必要的前端 runtime 與建置複雜度。

### 3. 純 static HTML + browser 直接呼叫 backend APIs

不採用。這會讓 token、auth flow 與多 backend base URL 的責任回到 browser。

## 後續工作

- 補 storefront UI spec 與 testcases
- `CommonStorefront` 第一版直接以 GOV.UK 類型版型實作
- AppleBTS / PetShop storefront 沿用相同 UI grammar
