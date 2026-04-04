# CommonStorefront Baseline 規格

## 狀態

- phase: 1
- status: proposed-for-review
- 日期：2026-04-04

## 範圍

本規格只涵蓋：

1. `AndrewDemo.NetConf2023.CommonStorefront` 的 page routes
2. auth / session flow
3. server-side BFF client 邊界
4. page model 與 layout / partial boundary
5. Phase 1 驗收基準

本規格暫不涵蓋：

- AppleBTS 專屬頁面
- PetShop 專屬頁面
- 品牌化視覺細節
- 複雜 client-side interaction

## 目標

- 定義 storefront family 的 baseline 實作
- 讓 `CommonStorefront` 可作為 `AppleBTS.Storefront` 與 `PetShop.Storefront` 的共同參考
- 讓後續實作能在最小複雜度下完成 BFF、auth/session 與通用 UI

## Canonical 術語

- `CommonStorefront`: 只整合標準 `.API` 的 baseline storefront
- `Storefront.Shared`: storefront family 共用的 auth/session、typed clients 與共用 UI shell
- `BFF`: storefront server side 代 browser 呼叫 backend APIs 的模式
- `AuthenticatedPage`: 需要 access token 才能進入的頁面
- `AnonymousPage`: 不要求先登入即可瀏覽的頁面

## 正式規格

### 1. 技術基準

- `CommonStorefront` 必須採 ASP.NET Core Razor Pages 或 MVC server-rendered page
- 第一版不應引入 Node.js host
- 第一版不應以 SPA framework 作為必要依賴

### 2. Backend 呼叫方式

- `CommonStorefront` 必須由 server side 呼叫 `.API`
- browser 不得直接持 bearer token 呼叫 `/api`
- `CommonStorefront` server side 應直接呼叫內部 backend service URL
- 不應再繞經 Azure Front Door 呼叫 `/api`

### 3. OAuth / Login

- login authority UI 第一版沿用 `/api/login/authorize`
- `CommonStorefront` 必須提供：
  - `/auth/login`
  - `/auth/callback`
  - `/auth/logout`
- `/auth/callback` 必須在 server side 呼叫 `/api/login/token`
- access token 必須由 storefront server side 保存，不直接暴露給 browser JavaScript

### 4. Page Routes

`CommonStorefront` 必須至少提供：

- `/`
- `/products`
- `/products/{id}`
- `/cart`
- `/checkout`
- `/member`
- `/member/orders`
- `/auth/login`
- `/auth/callback`
- `/auth/logout`

### 5. Page Auth Boundary

以下頁面可匿名瀏覽：

- `/`
- `/products`
- `/products/{id}`
- `/auth/login`
- `/auth/callback`

以下頁面必須要求登入：

- `/cart`
- `/checkout`
- `/member`
- `/member/orders`
- `/auth/logout`

### 6. Shared Project Boundary

`Storefront.Shared` 應擁有：

- auth/session service
- access token 讀寫 abstraction
- `CoreApiClient`
- 共用 UI shell 與 partials
- 共用 view models

`CommonStorefront` 自身應擁有：

- 各頁 page model
- Common-specific view composition

### 7. Typed Client Boundary

第一版 `CoreApiClient` 必須至少支援：

- 產品列表
- 產品詳細頁
- member profile
- member orders
- cart create / get / add item / estimate
- checkout create / complete

### 8. UI 基準

- UI 風格沿用 storefront family 已確認的 GOV.UK 類型極簡風格
- 必須符合 storefront family 的 accessibility / RWD 基準
- `/checkout` 第一版應為「確認送單」頁，不應要求消費者填寫滿意度、備註或其他非必要欄位

### 9. 驗收方式

- `CommonStorefront` 完成條件不得只以 build 或 unit test 通過判定
- 必須依 `/spec/testcases/common-storefront-baseline.md` 逐條驗收
- 驗收方式以 `agent-browser` 操作實際啟動中的 `CommonStorefront` 為主
- 若某 testcase 因環境限制無法驗收，必須明確列為 blocked item

## 非目標

- 第一版不包含 AppleBTS flow
- 第一版不包含 PetShop reservation flow
- 第一版不重刻 login authority UI
- 第一版不導入 client-side SPA state management
