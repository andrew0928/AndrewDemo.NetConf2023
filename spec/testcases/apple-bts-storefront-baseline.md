# AppleBTS Storefront Baseline Testcases

## 驗收原則

- 本文件是 `AndrewDemo.NetConf2023.AppleBTS.Storefront` 的正式驗收清單
- 驗收方式以 `agent-browser` 操作實際啟動中的 storefront 為主
- 若某 testcase 因環境限制無法驗收，必須標成 blocked

## TC-BTS-UI-01 BTS 首頁顯示活動型錄

- Given: 使用者開啟 `AppleBTS.Storefront`
- When: 進入 `/bts`
- Then: 可看到 BTS 主商品型錄
- And: 每筆商品都顯示一般售價、BTS 活動價與 gift 清單摘要
- And: gift 清單會顯示原價與最高可折金額

## TC-BTS-UI-02 教育資格頁要求登入

- Given: 使用者尚未登入
- When: 開啟 `/bts/qualification`
- Then: storefront 先導向自身 `/auth/login`
- And: 再導向 `/api/login/authorize`

## TC-BTS-UI-03 教育資格頁可完成驗證

- Given: 使用者已登入
- When: 在 `/bts/qualification` 送出 `.edu.xxx` 信箱
- Then: storefront 在 server side 呼叫 `/bts-api/qualification/verify`
- And: 頁面可看到更新後的資格狀態

## TC-BTS-UI-04 BTS 商品詳細頁顯示價格與 gift

- Given: 使用者進入 `/bts/products/{id}`
- When: 頁面載入完成
- Then: 可看到一般售價、BTS 活動價、gift 補貼上限與 gift 清單

## TC-BTS-UI-05 可只加入主商品

- Given: 使用者已登入並進入 `/bts/products/{id}`
- When: 點選「只加入主商品」
- Then: storefront 在 server side 呼叫 `/api/carts/*`
- And: 購物車新增主商品 line

## TC-BTS-UI-06 gift 加入前需先確認

- Given: 使用者在 `/bts/products/{id}` 選擇某個 gift
- When: 頁面切到確認狀態
- Then: 可看到「是否將主商品與 gift 一併加入購物車」的確認區塊
- And: 取消時不應修改購物車

## TC-BTS-UI-07 同意後加入主商品與 gift

- Given: 使用者已進入 gift 確認狀態
- When: 點選同意
- Then: storefront 先加入主商品，再加入 gift
- And: gift line 必須帶 `ParentLineId`
- And: 最終導向 `/cart`

## TC-BTS-UI-08 AppleBTS 與 Common 使用相同 UI grammar

- Given: 使用者分別瀏覽 `CommonStorefront` 與 `AppleBTS.Storefront`
- When: 比較版面與主要元件
- Then: 兩者沿用相同 layout、navigation、form 與 notification grammar
- And: AppleBTS 只在內容區塊上追加 BTS 專屬 UI

## TC-BTS-UI-09 會員資料頁顯示教育資格摘要

- Given: 使用者已登入 AppleBTS storefront
- When: 開啟 `/member`
- Then: 頁面可同時看到 member profile 與 Apple BTS 教育資格摘要
- And: 可前往 `/bts/qualification`

## TC-BTS-UI-10 訂單頁顯示 BTS 折扣內容

- Given: 使用者已完成一筆含 BTS 折扣的訂單
- When: 開啟 `/member/orders`
- Then: 該訂單會列出折扣明細
- And: 至少包含折扣名稱、說明與金額
