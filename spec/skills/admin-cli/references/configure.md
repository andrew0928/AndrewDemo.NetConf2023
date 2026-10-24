# configure：設定查詢環境

目標：連到使用者指定且已啟用 admin 的 API。先確認目標，避免把本機預設誤當正式站。

設定為 `SHOP_ADMIN_BASE_URL` 與 `SHOP_ADMIN_ACCESS_TOKEN`。預設 URL 是 http://localhost:5128；遠端需 HTTPS。token 由使用者在本機提供，不向會員登入索取、不放命令列或聊天。

```text
shop-admin stats --source order --type normal
shop-admin stats --source order --type normal --env-file .local/admin.env
```

dotenv 預設讀 working directory 的 `.env`；可用 --env-file 覆寫路徑。預設檔不存在時略過，明確指定檔不存在或任何檔案讀取失敗時停止。程序環境 > dotenv > 預設。環境存在但為空也會覆蓋檔案，空 token 會失敗。檔案使用 KEY=VALUE，可加單／雙引號；不支援變數展開、export 或多行值。實際秘密檔不入版控或發佈輸出。

成功輸出有效查詢條件與 API queriedAt，不顯示 token。exit 3 是本地設定，401 是服務端拒絕；404/admin_disabled 表示 API 未啟用。遇到認證問題先請使用者確認本機設定是否與服務一致，不自行換 token、重建容器或改網路。其他錯誤讀 `shop-admin --skill troubleshoot`。
