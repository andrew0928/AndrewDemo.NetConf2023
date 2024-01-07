


## GPT Instructions (Prompt)

Name: 安德魯小舖 v4.1.0

你是安德魯小舖的店長，主要的任務有:
1. 協助客人挑選商品，若有優惠商品則推薦。協助客人加入購物車完成結帳
2. 協助客人註冊會員或是進行身分認證，查詢會員資訊與訂單紀錄
3. 給客人採購建議, 或是預估金額與預算評估的建議
4. 代替客人，呼叫結帳 API 完成交易並成立訂單

注意事項:
1. 採購內容僅限 API 回應的商品資訊
2. 如果你無法處理，或是不理解 API 的使用方式，請輸出下列資訊，請客人直接連絡我:
- 購物車內容
- 會員資訊
- API Request (如果有的話)
- API Response (如果有的話)
3. 若客戶詢問預算範圍內能採購的數量, 商店常有各種優惠活動, API 並不會明確說明, 只有試算 (estimate) 購物車金額時才能知道。用以下的程序來計算:
- 用預算除以單價，預估可購買的數量，並且更新購物車內容，進行試算
- 檢查結帳金額與預算是否足夠買更多的商品? 夠的話計算結帳金額與預算的差額，除上單價判斷還能再多買幾件商品，並更新購物車後再試算結帳金額。重複這步驟直到無法再增加為止
- 回覆購物車內容給客戶確認時，重新呼叫一次 API 試算結果來顯示
4. 要求客戶提供身分驗證資訊，並提供支付代碼 (PaymentId)。確認同意後進行結帳
5. 部分 API 需要通過身分認證才能使用。請詢問客人必要資訊取得 access token, 並保存用於後續 API 呼叫。原則如下:
- 若客人已是會員，詢問帳號名稱 (name) 以及密碼 (password) 代替客人登入取得 token
- 若客人還不是會員，詢問帳號名稱 (name) 代替客人註冊會員取得 token
- 保留登入或是註冊取得的 access token, 並用於後續的 API 呼叫, 直到 access token 失效，或是客人明確表明不再需要協助

## 設定 GPT Action 的步驟

1. Debug Mode, 進入 swagger UI, 點選左上方的 swagger.json (/swagger/v1/swagger.json)
1. 在 json 內容加上 servers 的節點, 並設定實際提供服務的 url
1. 內容貼在 MyGPTs 的 Action 內容


```jsonc
{
  "openapi": "3.0.1",
  "info": { ... },

  // 這裡加上 servers 的節點
  "servers": [
    {
	  "url": "https://gpt-api.azurewebsites.net"
    }
  ],

  "paths": { ... }
}
```



## 對談紀錄

- 2024/01/07 20:00, [安德魯小舖 v4 - 第一次對談紀錄](https://chat.openai.com/share/b07bb31b-ce44-4f9f-9063-ff309c5a6ef7)
- 2024/01/08 02:27, [安德魯小舖 v4.1.0 - 第二次對談紀錄](https://chat.openai.com/share/47d2bfaa-ad39-4086-8a35-6059fee4130a)

```text
2024-01-07T18:10:30.303175095Z: [INFO]  Hosting environment: Production
2024-01-07T18:10:30.304298374Z: [INFO]  Content root path: /app
2024-01-07T18:10:30.311616390Z: [INFO]  Now listening on: http://[::]:8081
2024-01-07T18:10:34.084185555Z: [INFO]     _____
2024-01-07T18:10:34.084221658Z: [INFO]    /  _  \ __________ _________   ____
2024-01-07T18:10:34.084226758Z: [INFO]   /  /_\  \\___   /  |  \_  __ \_/ __ \
2024-01-07T18:10:34.084230858Z: [INFO]  /    |    \/    /|  |  /|  | \/\  ___/
2024-01-07T18:10:34.084234658Z: [INFO]  \____|__  /_____ \____/ |__|    \___  >
2024-01-07T18:10:34.084238959Z: [INFO]          \/      \/                  \/
2024-01-07T18:10:34.084242759Z: [INFO]  A P P   S E R V I C E   O N   L I N U X
2024-01-07T18:10:34.084246459Z: [INFO]
2024-01-07T18:10:34.084250060Z: [INFO]  Documentation: http://aka.ms/webapp-linux
2024-01-07T18:10:34.084253760Z: [INFO]  Dotnet quickstart: https://aka.ms/dotnet-qs
2024-01-07T18:10:34.084257360Z: [INFO]  ASP .NETCore Version: 8.0.0
2024-01-07T18:10:34.084582483Z: [INFO]  Note: Any data outside '/home' is not persisted
2024-01-07T18:10:37.818029753Z: [INFO]  Starting OpenBSD Secure Shell server: sshd.
2024-01-07T18:10:38.211414896Z: [INFO]  Starting periodic command scheduler: cron.
2024-01-07T18:10:38.328362624Z: [INFO]  Running oryx create-script -appPath /home/site/wwwroot -output /opt/startup/startup.sh -defaultAppFilePath /defaulthome/hostingstart/hostingstart.dll     -bindPort 8080 -bindPort2 '' -userStartupCommand 'dotnet AndrewDemo.NetConf2023.API.dll'
2024-01-07T18:10:38.614125584Z: [INFO]  Could not find build manifest file at '/home/site/wwwroot/oryx-manifest.toml'
2024-01-07T18:10:38.614160787Z: [INFO]  Could not find operation ID in manifest. Generating an operation id...
2024-01-07T18:10:38.637230090Z: [INFO]  Build Operation ID: 279bd94c-a90b-4002-b60f-9ba624b6eef5
2024-01-07T18:10:40.640530998Z: [INFO]
2024-01-07T18:10:40.640567900Z: [INFO]  Agent extension
2024-01-07T18:10:40.640575301Z: [INFO]  Before if loop >> DotNet Runtime
2024-01-07T18:10:40.741579553Z: [INFO]  DotNet Runtime 8.0Writing output script to '/opt/startup/startup.sh'
2024-01-07T18:10:40.850431054Z: [INFO]  Running user provided startup command...
2024-01-07T18:10:45.832161960Z: [INFO]  info: Microsoft.Hosting.Lifetime[14]
2024-01-07T18:10:45.832209963Z: [INFO]        Now listening on: http://[::]:8080
2024-01-07T18:10:45.883388734Z: [INFO]  info: Microsoft.Hosting.Lifetime[0]
2024-01-07T18:10:45.883415036Z: [INFO]        Application started. Press Ctrl+C to shut down.
2024-01-07T18:10:45.883421136Z: [INFO]  info: Microsoft.Hosting.Lifetime[0]
2024-01-07T18:10:45.883426137Z: [INFO]        Hosting environment: Production
2024-01-07T18:10:45.883430637Z: [INFO]  info: Microsoft.Hosting.Lifetime[0]
2024-01-07T18:10:45.883435137Z: [INFO]        Content root path: /home/site/wwwroot
2024-01-07T18:12:30  No new trace in the past 1 min(s).
2024-01-07T18:13:30  No new trace in the past 2 min(s).
2024-01-07T18:14:30  No new trace in the past 3 min(s).
2024-01-07T18:15:30  No new trace in the past 4 min(s).
2024-01-07T18:16:15.670785606Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/login.
2024-01-07T18:16:16.028210677Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/login.
2024-01-07T18:16:48.989564323Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/register.
2024-01-07T18:16:56.777975857Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/member/e1f411b95b3149c9abdc35db3cbb4927/orders.
2024-01-07T18:17:32.742469001Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/products.
2024-01-07T18:17:44.002162485Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/create.
2024-01-07T18:17:48.353605277Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:17:51.476112146Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:17:51.487664399Z: [INFO]  - [1] 18天(單價: $65) x 15,     $975
2024-01-07T18:17:51.505279354Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:17:51.506448851Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:17:51.507424531Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:17:51.507440533Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:17:51.514848844Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:17:51.514872046Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:17:51.514878547Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:08.369297298Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:18:13.002355286Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:18:13.008844013Z: [INFO]  - [1] 18天(單價: $65) x 16,     $1040
2024-01-07T18:18:13.009556471Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:13.010100715Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:13.010594755Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:13.016971673Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:13.017287898Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:13.017561921Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:13.017795139Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:18:13.018020058Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:19:30  No new trace in the past 1 min(s).
2024-01-07T18:19:57.646871230Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:20:00.884260293Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:20:00.884312096Z: [INFO]  - [1] 18天(單價: $65) x 17,     $1105
2024-01-07T18:20:00.893921318Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:00.893947920Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:00.893954621Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:00.893959821Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:00.893981423Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:00.893986423Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:00.893991223Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:00.893997324Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:05.883045524Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/items.
2024-01-07T18:20:18.632108373Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/carts/1/estimate.
2024-01-07T18:20:18.632147776Z: [INFO]  - [1] 18天(單價: $65) x 18,     $1170
2024-01-07T18:20:18.632154776Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632159776Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632164277Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632168777Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632173577Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632178078Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632182478Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632186778Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:18.632191179Z: [INFO]  - [優惠] 18天 第二件六折優惠,   $-26.0
2024-01-07T18:20:53.793254153Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/create.
2024-01-07T18:22:09.331076940Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/create.
2024-01-07T18:22:41.218364417Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/complete.
2024-01-07T18:22:57.546838511Z: [INFO]  [system] AndrewShop GTP v4(d069d4eb-6a1f-49c4-a8d0-3e32079e54b5) request api /api/checkout/complete.
2024-01-07T18:22:57.569334746Z: [INFO]  [waiting-room] issue ticket: 1 @ 01/07/2024 18:22:57 (estimate: 01/07/2024 18:22:59)
2024-01-07T18:22:57.570304716Z: [INFO]  [checkout] check system status, please wait ...
2024-01-07T18:22:59.578779829Z: [INFO]  [checkout] checkout process start...
2024-01-07T18:22:59.589819431Z: [INFO]  [checkout] checkout process complete... order created(1)
2024-01-07T18:22:59.598919992Z: [INFO]
2024-01-07T18:24:30  No new trace in the past 1 min(s).
2024-01-07T18:25:30  No new trace in the past 2 min(s).
2024-01-07T18:26:30  No new trace in the past 3 min(s).
2024-01-07T18:27:30  No new trace in the past 4 min(s).
```

- 2024/01/08 02:36, [安德魯小舖 v4.1.0 - 第三次對談紀錄](https://chat.openai.com/share/8abc03ac-28c1-46ec-b928-4e76391a1af0), 會主動建議我超出一點預算的建議了 (預算 1000, 建議 1001 的商品)