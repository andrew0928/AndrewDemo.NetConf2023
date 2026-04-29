# Azure Container Apps site-prod 部署

本目錄是 `compose/*.site-prod.compose.yaml` 的 Azure Container Apps 對應部署檔。Compose 檔案仍負責本機端驗證 registry image，ACA YAML 才是 Azure 上的部署拓撲。

## 對應關係

| site-prod compose | ACA YAML | Container App |
|---|---|---|
| `compose/common.site-prod.compose.yaml` | `deploy/aca/common.site-prod.aca.yaml` | `andrewshop-common-site` |
| `compose/applebts.site-prod.compose.yaml` | `deploy/aca/applebts.site-prod.aca.yaml` | `andrewshop-applebts-site` |
| `compose/petshop.site-prod.compose.yaml` | `deploy/aca/petshop.site-prod.aca.yaml` | `andrewshop-petshop-site` |

## Cloud Shell 部署

先確認本機已把 image push 到 `andrew0928.azurecr.io`：

```bash
./build.sh --push
```

ACA 部署檔使用與 `compose/*.site-prod.compose.yaml` 相同的 site-prefixed image repository 命名：

| role | image repository |
|---|---|
| common core API | `andrewdemo-shop-common-api` |
| common seed | `andrewdemo-shop-common-seed` |
| common storefront | `andrewdemo-shop-common-storefront` |
| AppleBTS seed | `andrewdemo-shop-applebts-seed` |
| AppleBTS vertical API | `andrewdemo-shop-applebts-btsapi` |
| AppleBTS storefront | `andrewdemo-shop-applebts-storefront` |
| PetShop seed | `andrewdemo-shop-petshop-seed` |
| PetShop vertical API | `andrewdemo-shop-petshop-reservationapi` |
| PetShop storefront | `andrewdemo-shop-petshop-storefront` |

在 Azure Portal Cloud Shell 進入本 repo 後執行：

```bash
az account show
az extension add --name containerapp --upgrade

export RESOURCE_GROUP=<resource-group>
export CONTAINERAPPS_ENV=<container-apps-environment>
export LOCATION=<azure-region>

bash deploy/aca/deploy-site-prod.sh
```

只部署單一站台：

```bash
SITES=common bash deploy/aca/deploy-site-prod.sh
SITES=applebts bash deploy/aca/deploy-site-prod.sh
SITES=petshop bash deploy/aca/deploy-site-prod.sh
```

若 ACR 不在同一個 resource group，補上：

```bash
export ACR_RESOURCE_GROUP=<acr-resource-group>
```

## Cloud Shell 診斷

如果新 revision 長時間停在 `Activating`，先檢查 ACR image tag、revision、replica 與 system logs：

```bash
RESOURCE_GROUP=<resource-group> APP_NAME=andrewshop-common-site bash deploy/aca/diagnose-site-prod.sh
```

針對特定 container 看 console logs：

```bash
RESOURCE_GROUP=<resource-group> APP_NAME=andrewshop-common-site CONTAINER=common-seed bash deploy/aca/diagnose-site-prod.sh
RESOURCE_GROUP=<resource-group> APP_NAME=andrewshop-common-site CONTAINER=common-api bash deploy/aca/diagnose-site-prod.sh
RESOURCE_GROUP=<resource-group> APP_NAME=andrewshop-common-site CONTAINER=common-storefront bash deploy/aca/diagnose-site-prod.sh
RESOURCE_GROUP=<resource-group> APP_NAME=andrewshop-common-site CONTAINER=common-edge bash deploy/aca/diagnose-site-prod.sh
```

## 部署模型

- 每個 site-prod compose 對應一個 Container App。
- nginx 是對外 ingress target，ACA ingress 只打到 nginx `targetPort: 80`。
- backend API 與 storefront 不直接對外開放，只透過同一個 Container App 內的 loopback routing 存取。
- seed 以 `initContainers` 執行，完成後才啟動 app containers。
- `/data` 使用 `EmptyDir`，降低 demo 部署前置步驟；若需要跨 revision/replica 保留資料，改用 Azure Files。
- `minReplicas` 與 `maxReplicas` 固定為 `1`，避免 LiteDB shared file 與 init seed 在多 replica 下產生一致性問題。

## site-prod 內部 port

prod topology 統一使用 role-based internal port。ACA 同一個 Container App 內的 containers 共用 network namespace，因此 app containers 必須使用不同 port；compose site-prod 也採同一套分配，讓本機驗證與 ACA 部署一致。

| site | container | port |
|---|---|---:|
| common | common-api | 5108 |
| common | common-storefront | 8080 |
| applebts | applebts-api | 5108 |
| applebts | applebts-btsapi | 5109 |
| applebts | applebts-storefront | 8080 |
| petshop | petshop-api | 5108 |
| petshop | petshop-reservationapi | 5109 |
| petshop | petshop-storefront | 8080 |
