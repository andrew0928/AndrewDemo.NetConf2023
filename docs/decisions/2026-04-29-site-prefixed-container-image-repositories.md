# Site-prefixed container image repository 命名

## 狀態

accepted

## 背景

container image 需要同時支援 common、AppleBTS、PetShop 三組 demo site。先前 common baseline 的 core API image 使用 `andrewdemo-shop-api`，但其他 image 已經使用 site-prefixed repository，例如 `andrewdemo-shop-common-storefront`、`andrewdemo-shop-applebts-seed`、`andrewdemo-shop-petshop-reservationapi`。

這造成 build、compose、ACA deploy 與 ACR image 檢查時，common 站台缺少一致的 site namespace，與 vertical domain application 的命名規則不對齊。

## 決策

- production image repository 一律採用 `andrewdemo-shop-{site}-{role}`。
- common baseline 也必須顯式帶上 `common` site prefix。
- `andrewdemo-shop-api` 改名為 `andrewdemo-shop-common-api`。
- 不保留長期雙軌命名；部署設定、build script 與診斷腳本均改用新 repository 名稱。
- service/container runtime role 名稱不因 image repository 改名而更動，例如 AppleBTS 仍可用 `applebts-api` 作為 container/service 名稱，但其 image 來源是共用的 `andrewdemo-shop-common-api`。

## 影響

- `build.sh` 發佈的 common core API image 改為 `andrewdemo-shop-common-api`。
- `compose/*.compose.yaml` 中所有 core API image reference 改為 `andrewdemo-shop-common-api`。
- `deploy/aca/*.site-prod.aca.yaml` 中所有 core API image reference 改為 `andrewdemo-shop-common-api`。
- ACA deployment / diagnose scripts 的 ACR repository preflight 改為檢查 `andrewdemo-shop-common-api`。
- 已在 ACR 內存在的舊 `andrewdemo-shop-api:*` 不再作為部署來源；重新部署前需要 push 新 image。

## 替代方案

- 保留 `andrewdemo-shop-api` 作為 alias：短期相容性較好，但會延續 common 與 vertical image repository 命名不一致。
- 將 AppleBTS / PetShop 的 core API image 也改成各自 site-specific repository：部署視覺上更獨立，但會重複發佈同一個 common API binary，反而模糊「runtime env 決定 vertical behavior」的設計。

## 後續工作

- 重新執行 `./build.sh --push --image common-api`，將 `andrewdemo-shop-common-api` 推送到 ACR。
- 若要清理 ACR，可在確認所有部署不再引用舊 repository 後移除 `andrewdemo-shop-api`。
