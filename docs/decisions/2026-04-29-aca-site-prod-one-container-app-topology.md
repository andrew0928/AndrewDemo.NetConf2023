# ACA site-prod 一對一 Container App 部署拓撲

## 狀態

accepted

## 背景

本專案需要同時支援三個 demo site：common、AppleBTS、PetShop。`site-prod` compose 的用途是本機端驗證 `build.sh` 已經建置並推送到 registry 的正式部署 image，不再於 `docker compose up` 時 on-the-fly build。

後續 Azure 部署希望降低管理複雜度，採用「一份 site-prod compose 對應一個 Azure Container App」的操作模型。這代表一個 demo site 內的 nginx、storefront、core API、vertical API 與 seed lifecycle 會被收斂到同一個 Container App 內，而不是拆成多個 Container Apps 再用 Front Door 或外部 router 串接。

## 決策

- `compose/*.site-prod.compose.yaml` 保留為本機端 registry image 驗證用途。
- Azure 上新增 `deploy/aca/*.site-prod.aca.yaml`，作為 ACA 實際部署定義。
- 每個 site-prod compose 一對一對應一個 Container App：
  - `common.site-prod` -> `andrewshop-common-site`
  - `applebts.site-prod` -> `andrewshop-applebts-site`
  - `petshop.site-prod` -> `andrewshop-petshop-site`
- nginx 保留為 Container App 內的 edge sidecar，ACA ingress 只對外打到 nginx `targetPort: 80`。
- storefront、core API、vertical API 只在同一個 Container App 內透過 loopback 存取，不直接對外開放。
- seed 改為 ACA `initContainers`，先初始化 `/data/shop-database.db`，成功後才啟動 app containers。
- `/data` 第一版採 ACA `EmptyDir`，用最少前置步驟支援 demo site 啟動與重建；若需要跨 revision/replica 保留資料，再改成 Azure Files。
- `minReplicas` 與 `maxReplicas` 固定為 `1`，避免 LiteDB shared file、seed 初始化與多 replica 寫入一致性問題。
- Azure Container Apps 同一個 Container App 內的 containers 共用 network namespace，因此 ASP.NET containers 必須改用不同內部 port，nginx upstream 使用 `127.0.0.1:<port>`。
- prod topology 統一採 role-based internal port：common/core API 使用 `5108`，vertical API 使用 `5109`，storefront 使用 `8080`。`site-prod` compose 與 ACA YAML 都遵守同一套分配。

## 影響

- 新增 `deploy/aca/common.site-prod.aca.yaml`。
- 新增 `deploy/aca/applebts.site-prod.aca.yaml`。
- 新增 `deploy/aca/petshop.site-prod.aca.yaml`。
- 新增 `deploy/aca/deploy-site-prod.sh`，提供 Azure Portal Cloud Shell 可直接執行的部署流程。
- 新增 `deploy/aca/README.md`，記錄 Cloud Shell 操作與 compose 對應關係。
- 不影響 `.Core`、`.Abstract` 或正式 API contract。
- 不改變 `compose/*.site-prod.compose.yaml` 的本機驗證定位。

## 替代方案

- 每個 API / storefront / nginx 各自建立 Container App：架構分離較清楚，但 demo 管理成本與 routing 設定較高。
- 完全依賴 Azure Front Door，不在 ACA 內保留 nginx：更接近 cloud-native edge，但無法與本機 reverse proxy demo 拓撲一對一對應。
- 使用 Azure Files 作為第一版 `/data` volume：可保留資料，但需要額外建立 storage account、file share 與 Container Apps environment storage mount，初次 demo 部署步驟較多。

## 後續工作

- 若 demo 需要長期保留操作資料，將 `deploy/aca/*.yaml` 的 `EmptyDir` volume 改成 Azure Files。
- 若未來 API 需要由 BFF 以外的 client 存取，再另外評估是否讓 nginx 對外發佈 `/api/*` 或拆出獨立 API Container App。
- 若導入 Front Door，Front Door 可只對接三個 Container App 的 nginx ingress，不需要理解內部 API / storefront 拓撲。
