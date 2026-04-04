# AppleBTS Storefront Phase 1 設計稿

## 目的

`AndrewDemo.NetConf2023.AppleBTS.Storefront` 以 `CommonStorefront` 為骨架，追加 Apple BTS 專區需要的頁面與 server-side orchestration。

這一版的設計重點不是重做通用官網流程，而是把以下 vertical-specific 行為補齊：

- BTS 型錄入口
- 教育資格頁
- BTS 商品詳細頁
- 主商品與 gift 的確認加入流程

## 與 CommonStorefront 的差異

### 1. 仍保留通用官網流程

以下頁面與流程維持與 `CommonStorefront` 相同的 grammar：

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

### 2. 額外追加 BTS 專區

新增頁面：

- `/bts`
- `/bts/qualification`
- `/bts/products/{id}`

### 3. 額外整合 `/bts-api`

除了既有 `/api` 外，AppleBTS storefront 還必須在 server side 呼叫：

- `GET /bts-api/catalog`
- `GET /bts-api/catalog/{mainProductId}`
- `GET /bts-api/qualification/me`
- `POST /bts-api/qualification/verify`

### 4. gift 加入流程採 server-rendered 確認步驟

gift 的加入不是直接按下就修改購物車。

流程是：

1. 使用者在 `/bts/products/{id}` 點選某個 gift
2. 頁面先顯示確認區塊
3. 使用者同意後，storefront 才會：
   - 加入主商品 line
   - 取得該主商品 line 的 `LineId`
   - 再加入 gift line，並帶上 `ParentLineId`
4. 使用者取消時，購物車維持原狀

## 不做的事

- 不重刻 login authority UI
- 不在 browser 端直接呼叫 `/api` 或 `/bts-api`
- 不新增 AppleBTS 專屬 `.Core` checkout / discount contract
- 不在第一版加入複雜 client-side interaction

## 本機驗證拓樸

本機以獨立 compose 啟動：

- `applebts-seed`
- `applebts-api`
- `applebts-btsapi`
- `applebts-storefront`
- `applebts-edge`（nginx）

對 browser 來說，單一入口為：

- `http://localhost:5138`
