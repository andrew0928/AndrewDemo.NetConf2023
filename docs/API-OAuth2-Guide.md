# AndrewShop API - OAuth2 Authentication Guide

**API Base URL**: `https://shop.chicken-house.net`  
**Last Updated**: 2025-10-28  
**OAuth2 Flow**: Authorization Code Flow

---

## Overview

AndrewShop API 使用 OAuth2 Authorization Code Flow 進行身份驗證。所有 API 端點都需要有效的 access token。

## Quick Start

### 1. 註冊您的應用程式

使用以下憑證（或向 API 提供者申請新的）：

- **Client ID**: `andrewshop-webapp`
- **Client Secret**: `andrewshop-webapp-secret`
- **Redirect URI**: 您的應用程式 callback URL（例如：`http://localhost:3000/auth/callback`）

### 2. OAuth2 流程概覽

```
1. 使用者訪問您的應用 → 檢測到未授權 (401)
   ↓
2. 重導向到授權頁面（帶上 client_id, redirect_uri 等參數）
   ↓
3. 使用者在授權頁面登入並同意授權
   ↓
4. 重導向回您的 redirect_uri（帶上 authorization code）
   ↓
5. 您的後端用 code 換取 access_token
   ↓
6. 使用 access_token 呼叫受保護的 API
```

---

## OAuth2 端點

### 授權端點（Authorization Endpoint）

**URL**: `GET https://shop.chicken-house.net/api/login/authorize`

使用者將被重導向到此頁面進行登入和授權。

#### 請求參數

| 參數 | 必填 | 說明 |
|------|------|------|
| `client_id` | ✅ | 您的應用程式 Client ID |
| `redirect_uri` | ✅ | 授權完成後的重導向 URL |
| `response_type` | ✅ | 必須為 `code` |
| `scope` | ❌ | 可選，例如 `read write` |
| `state` | ❌ | 防 CSRF 攻擊的隨機字串（建議使用） |

#### 範例請求

```
https://shop.chicken-house.net/api/login/authorize?
  client_id=andrewshop-webapp&
  redirect_uri=http://localhost:3000/auth/callback&
  response_type=code&
  scope=read%20write&
  state=random_string_12345
```

#### 成功回應

使用者授權後，會重導向到您的 `redirect_uri`：

```
http://localhost:3000/auth/callback?
  code=3d43909de43148fa91019c0e43dd670c&
  state=random_string_12345
```

---

### Token 端點（Token Endpoint）

**URL**: `POST https://shop.chicken-house.net/api/login/token`  
**Content-Type**: `application/x-www-form-urlencoded`

用 authorization code 換取 access token。

#### 請求參數

| 參數 | 必填 | 說明 |
|------|------|------|
| `code` | ✅ | 從授權端點獲得的 authorization code |

**注意**: 此 API 實作較簡化，只需要 `code` 參數。不需要 `grant_type`、`client_id`、`client_secret`、`redirect_uri`。

#### 範例請求

```bash
curl -X POST https://shop.chicken-house.net/api/login/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "code=3d43909de43148fa91019c0e43dd670c"
```

或使用 JavaScript (Node.js):

```javascript
const response = await axios.post(
  'https://shop.chicken-house.net/api/login/token',
  new URLSearchParams({ code: authorizationCode }),
  {
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
  }
);
```

#### 成功回應

```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

#### 錯誤回應

| 狀態碼 | 說明 |
|--------|------|
| `400` | Bad Request - code 無效或已使用 |
| `401` | Unauthorized - 認證失敗 |

**重要**: Authorization code 只能使用一次，使用後會失效。

---

## 使用 Access Token

獲得 access token 後，在所有 API 請求中加入 `Authorization` header：

```
Authorization: Bearer {access_token}
```

### 範例：取得商品列表

```bash
curl https://shop.chicken-house.net/api/products \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

```javascript
const response = await axios.get('https://shop.chicken-house.net/api/products', {
  headers: {
    'Authorization': `Bearer ${accessToken}`,
  },
});
```

---

## 實作建議

### 前端實作

#### 1. 檢測未授權並重導向

```javascript
// 當收到 401 時，重導向到授權頁面
axios.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      // 儲存當前頁面，授權後可以返回
      sessionStorage.setItem('auth_redirect', window.location.pathname);
      
      // 重導向到授權頁面
      const params = new URLSearchParams({
        client_id: 'andrewshop-webapp',
        redirect_uri: `${window.location.origin}/auth/callback`,
        response_type: 'code',
        scope: 'read write',
        state: generateRandomState(), // 實作 CSRF 保護
      });
      
      window.location.href = `https://shop.chicken-house.net/api/login/authorize?${params}`;
    }
    return Promise.reject(error);
  }
);
```

#### 2. 處理 Callback

```javascript
// /auth/callback 頁面
const urlParams = new URLSearchParams(window.location.search);
const code = urlParams.get('code');
const state = urlParams.get('state');

// 驗證 state（防 CSRF）
if (state !== sessionStorage.getItem('oauth_state')) {
  console.error('Invalid state parameter');
  return;
}

// 用 code 換取 token（透過後端）
const response = await fetch('/api/auth/token', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ code }),
});

const { access_token } = await response.json();

// 儲存 token
localStorage.setItem('access_token', access_token);

// 重導向回原始頁面
const redirectPath = sessionStorage.getItem('auth_redirect') || '/';
window.location.href = redirectPath;
```

### 後端實作（Token Exchange）

**⚠️ 重要**: 雖然此 API 不強制要求 client_secret，但建議在後端處理 token exchange，避免暴露敏感邏輯。

```javascript
// Node.js + Express 範例
app.post('/api/auth/token', async (req, res) => {
  const { code } = req.body;
  
  try {
    const response = await axios.post(
      'https://shop.chicken-house.net/api/login/token',
      new URLSearchParams({ code }),
      {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
      }
    );
    
    res.json({
      access_token: response.data.access_token,
      token_type: response.data.token_type,
      expires_in: response.data.expires_in,
    });
  } catch (error) {
    res.status(500).json({ error: 'Token exchange failed' });
  }
});
```

---

## Token 管理

### Token 儲存

```javascript
// 儲存 token
localStorage.setItem('access_token', accessToken);

// 讀取 token
const token = localStorage.getItem('access_token');

// 移除 token（登出）
localStorage.removeItem('access_token');
```

### 自動加入 Token 到請求

```javascript
// Axios interceptor
axios.interceptors.request.use(config => {
  const token = localStorage.getItem('access_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
```

### Token 過期處理

Access token 有效期為 3600 秒（1 小時）。過期後需要重新授權。

```javascript
// 收到 401 時重新授權
axios.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      localStorage.removeItem('access_token');
      // 重導向到授權頁面
      redirectToAuthorize();
    }
    return Promise.reject(error);
  }
);
```

---

## 安全建議

### 1. State 參數（防 CSRF）

```javascript
// 產生隨機 state
function generateRandomState() {
  const state = Math.random().toString(36).substring(2, 15);
  sessionStorage.setItem('oauth_state', state);
  return state;
}

// 在 callback 中驗證
const receivedState = urlParams.get('state');
const storedState = sessionStorage.getItem('oauth_state');
if (receivedState !== storedState) {
  throw new Error('CSRF attack detected');
}
sessionStorage.removeItem('oauth_state');
```

### 2. HTTPS Only

生產環境必須使用 HTTPS，確保 token 傳輸安全。

### 3. Token 儲存

- ✅ 使用 `localStorage` 或 `sessionStorage`
- ❌ 不要將 token 儲存在 cookie 中（除非有適當的安全設定）
- ❌ 不要將 token 暴露在 URL 中

### 4. Redirect URI 驗證

確保 redirect_uri 與註冊的 URI 完全一致。

---

## 完整範例程式碼

### React + Axios 完整實作

參考專案：`andrewshop.webdemo`

#### 關鍵檔案

1. **src/services/authService.js** - OAuth2 流程管理
2. **src/services/api.js** - Axios 設定與攔截器
3. **src/components/AuthCallback.jsx** - Callback 處理
4. **server/auth-server.js** - 後端 token exchange

#### 執行方式

```bash
# 安裝依賴
npm install

# 同時啟動前端和後端
npm run dev:all

# 或分別啟動
npm run dev        # 前端: http://localhost:5173
npm run dev:auth   # 後端: http://localhost:3001
```

---

## 常見問題

### Q1: Authorization code 已使用過怎麼辦？

**A**: Code 只能使用一次。如果重複使用會收到 400 錯誤。需要重新導向使用者到授權頁面獲取新的 code。

### Q2: 為什麼我的 token exchange 失敗？

**A**: 檢查：
- Code 是否正確從 URL 取得
- Code 是否已經使用過
- 請求的 Content-Type 是否為 `application/x-www-form-urlencoded`
- Code 參數名稱是否正確（`code`）

### Q3: Token 過期後如何處理？

**A**: 此 API 不提供 refresh token。Token 過期後需要重新授權（重導向到授權頁面）。

### Q4: 可以在前端直接呼叫 token endpoint 嗎？

**A**: 可以，因為此 API 不強制要求 client_secret。但建議透過後端處理以提高安全性。

### Q5: State 參數是必須的嗎？

**A**: 雖然不是必填，但強烈建議使用以防止 CSRF 攻擊。

---

## API 端點總覽

| 端點 | 方法 | 用途 | 需要授權 |
|------|------|------|----------|
| `/api/login/authorize` | GET | 授權頁面 | ❌ |
| `/api/login/token` | POST | Token exchange | ❌ |
| `/api/products` | GET | 取得商品列表 | ✅ |
| `/api/products/{id}` | GET | 取得商品詳情 | ✅ |
| `/api/carts/create` | POST | 建立購物車 | ✅ |
| `/api/carts/{id}/items` | POST | 加入購物車項目 | ✅ |
| `/api/carts/{id}` | GET | 取得購物車內容 | ✅ |

---

## 聯絡與支援

如有問題或需要協助，請聯絡 API 維護者。

**API Base URL**: https://shop.chicken-house.net  
**Documentation Version**: 1.0.0  
**Last Updated**: 2025-10-28
