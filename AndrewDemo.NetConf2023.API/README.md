# build container

dotnet publish 內建 OCI build image 的能力, 不需要依賴 docker 生態系的工具
( docker build, dockerfile ) 就能完成 docker image 的建置

最小化依賴 dotnet 指令, 只要 build 好後再 tag image, push image 即可

可沿用 dotnet 的 build 體系, 不需要額外撰寫複雜的 dockerfile (尤其是跨專案 build 的過程)

以下是執行的範例 (WSL 下執行)

```
docker login andrew0928.azurecr.io
docker image list

dotnet publish /t:PublishContainer
docker tag andrewdemo-netconf2023-api:latest andrew0928.azurecr.io/andrew-shop-api:20250723
docker push andrew0928.azurecr.io/andrew-shop-api:20250723

```





# My GPT setup

## create action

paste swagger, add this line:

```json

{
  "openapi": "3.1.0",

  "servers": [
    { "url": "https://shop.chicken-house.net/" }
  ],
  
  // ...
}

```

set authentication to: oauth

- client id: 0000
- client secret: 0000
- authorization url: https://shop.chicken-house.net/api/login/authorize
- token url: https://shop.chicken-house.net/api/login/token
- score: 
- token exchange method: default(post request)