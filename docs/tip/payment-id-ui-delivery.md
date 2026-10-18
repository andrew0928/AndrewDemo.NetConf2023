# PaymentID 輸入 UI 交付

日期：2026-10-18。使用者要求與 AdminCLI 分兩批提交；第一批 AdminCLI commit 為 `cc4aac8`，本文件及 UI 屬第二批。

## 變更

CommonStorefront checkout 以 PaymentID（模擬卡號）必填欄位取代一般／測試付款 radio。預設留空，使用文字輸入與 numeric inputmode；沿用既有 `.field` 寬度、label／focus 樣式及 `.hint-text` 次要色文字，提示預設測試 ID `990001`、測試會員規則及模擬用途。

付款值以 nullable int 綁定後驗證；空白、非整數、小數及溢位顯示繁中摘要／欄位錯誤，保留輸入及購物車，不呼叫 Checkout API。合法值包含 0、一般 ID、測試 ID 與 int 邊界，原值送 create；complete 不再傳付款值。交易用途及略過結果仍取 API 回應，不在 UI 維護第二套分類邏輯。結帳失敗保留付款值，409 引導重新確認 PaymentID。

同步更新 [UI baseline](../../spec/common-storefront-baseline.md)、[案例](../../spec/testcases/common-storefront-baseline.md)、[決策](../decisions/2026-10-18-storefront-payment-id-input.md) 及 roadmap。未修改 Abstract、Core、API 契約或 AdminCLI。

## 驗證

```sh
dotnet test tests/AndrewDemo.NetConf2023.WebContract.Tests --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
docker compose -f compose/common.site-dev.compose.yaml up -d --build --no-deps common-storefront
```

- WebContract **141 項通過**（原 134 項，擴充付款傳遞與錯誤案例後增加 7 項）。包含實際 SimpleTypeModelBinder 對空白／非整數／小數／溢位的處理，以及不產生 checkout 請求、失敗保留輸入、付款值原樣傳遞及 API 用途呈現。
- CommonStorefront 映像建置／重建成功；`--no-deps` 未重建 API、seed 或變更部署憑證，沒有建立新交易。
- 部署後 `/`、`/products`、`/api/products` 均 HTTP 200。既有 Core obsolete／API XML 文件警告仍存在，無新增編譯錯誤。
- `git diff --check`、相對文件連結檢查通過；現行 CommonStorefront 不再保留 PaymentMode 或付款 radio。

本輪 Edge browser connector 不可用；原生 Edge 定位期間視窗狀態改變，未穩定定位到商店，因此未完成新版 400px 畫面／鍵盤人工補驗，也未宣稱舊 radio 的手機驗證可直接代表新 UI。TC-CS-PAY-006 尚待人工確認；欄位沿用既有響應式樣式與 label／aria-describedby。部署已更新，使用者重新開啟結帳頁可檢查新版輸入。

此次未重跑不相關的全 solution 測試，前一批 106 項 CLI 與既有其他專案結果另見 AdminCLI 交付。兩批僅 commit，不 push。
