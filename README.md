




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
      "description": "Test"
    }
  ],

  "paths": { ... }
}
```