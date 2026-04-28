# Core-owned OAuth Public Namespace

## 狀態

- accepted
- 日期：2026-04-28
- 回頭修正 Phase 1
- 影響 .Abstract / spec：影響 storefront 與 OAuth URL spec，不影響 `.Abstract` C# contract

## 背景

原本 storefront family 第一版沿用既有 `.API` 的 `/api/login/authorize` 與 `/api/login/token`。這能快速打通 server-side BFF login flow，但 URL 語意會把 OAuth authority 放在 resource API namespace 下，讓 `/api/*` 同時承載「業務 API」與「授權服務」兩種責任。

接下來 demo 的主要展示目標是：維持 source code / binary / container 層級不變，仍能為大型客戶保留高度客製化與 vertical 擴充能力。因此需要把 public URL contract 與部署單位分開看待：

- `/oauth/*` 是 authorization endpoint namespace
- `/api/*` 是 core shop resource API namespace
- 兩者可以由同一個標準 `.API` container 提供
- nginx / Front Door 只負責把 public path 發佈到既有 `.API` container

## 決策

- 將 OAuth canonical public path 改為：
  - `GET /oauth/authorize`
  - `POST /oauth/authorize`
  - `POST /oauth/token`
- `.API` project 繼續擁有 OAuth implementation，不拆新的 `oauth-web` container。
- storefront 不擁有 OAuth authority；storefront 只維持 BFF/session/callback 職責：
  - `/auth/login`
  - `/auth/callback`
  - `/auth/logout`
- ingress 層必須同時發佈：
  - `/oauth/*` -> 標準 `.API` container
  - `/api/*` -> 標準 `.API` container
  - vertical API path -> 對應 vertical API container
  - `/*` -> storefront container
- storefront 的 server-side token exchange 呼叫 internal core API URL，但 endpoint path 改為 `/oauth/token`。
- storefront 對 browser 產生的 authorize URL 改由 `Storefront:CoreApi:PublicOAuthBaseUrl` 控制；未設定時使用目前 request origin。
- `/api/login/*` 不再作為正式長期路徑保留。

## 影響

- `.API` 同時提供 `/oauth/*` 與 `/api/*`，但 binary/container 邊界不變。
- CommonStorefront、AppleBTS Storefront、PetShop Storefront 的 login redirect 會導向 `/oauth/authorize`。
- nginx 設定需要新增 `/oauth/*` route 到標準 `.API`。
- Front Door 若作為 public ingress，也需要新增 `/oauth/*` route 到標準 `.API` origin。
- REST Client `.http`、storefront spec、testcases 與 demo 文件需同步改為 `/oauth/*`。
- 這次變更不調整 token 格式、session 保存方式、會員資料模型與 `.Abstract` contract。

## 替代方案

### 1. 將 `/oauth/*` 合併進 storefront

不採用。storefront 是客戶/vertical 可替換的 BFF 與 UI，若讓 storefront 擁有 OAuth authority，會把標準身份服務綁到客製 host，削弱「標準 core 能力穩定，vertical 透過擴充變化」的 demo 訊息。

### 2. 新增獨立 `oauth-web` container

暫不採用。這會新增 source/binary/container/deployment role，與目前 demo 要展示的「既有 container 邊界不變」相衝突。未來若 login UI、client management、MFA、consent 或跨產品 identity service 成為獨立生命週期，再重新評估。

### 3. 維持 `/api/login/*`

不採用作為正式路徑。這會讓 authorization endpoint 與 resource API namespace 混在一起，部署文件與 demo 說明也較難表達 trust boundary。

## 後續工作

- 更新 `.API` controller route 與 REST Client `.http`。
- 更新 `Storefront.Shared` authorize URL 與 token exchange path。
- 更新 Common / AppleBTS / PetShop 的 compose 與 nginx 設定。
- 更新 storefront family spec、testcases 與部署拓樸文件。
- 發佈到 Azure Container Apps / Front Door 時，需同步新增 `/oauth/*` public route。
