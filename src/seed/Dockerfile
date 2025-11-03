# Init container image for seeding shop database into a shared ephemeral volume in Azure Container Apps
# 使用方式: 將此 image 當成 ACA init container，掛載同一個 EmptyDir volume 到 /data

FROM alpine:3.20

# 需要 bash (容錯/可讀性) 以及核心工具
RUN apk add --no-cache bash coreutils

WORKDIR /seed-src

# 複製資料庫檔案
COPY shop-database.db /seed-src/shop-database.db

# Entrypoint script
COPY seed-entrypoint.sh /seed-entrypoint.sh
RUN chmod +x /seed-entrypoint.sh

ENV SEED_DEST=/data \
    SEED_CHOWN_UID=1000 \
    SEED_CHOWN_GID=1000

ENTRYPOINT ["/seed-entrypoint.sh"]
