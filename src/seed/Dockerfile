FROM alpine:latest

COPY shop-database.db /seed/shop-database.db

CMD ["sh", "-c", "cp /seed/shop-database.db /data/shop-database.db && chmod 777 /data && chmod 666 /data/shop-database.db"]
