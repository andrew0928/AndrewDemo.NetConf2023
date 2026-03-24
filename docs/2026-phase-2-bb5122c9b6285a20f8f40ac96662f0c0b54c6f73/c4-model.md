# Phase 2 C4 Model

這份 C4 只聚焦在 phase 2 的 checkout 主軸。container 層刻意直接對應到 project，方便和 solution 結構一一比對。

## Context

```mermaid
flowchart LR
    Customer["消費者 / Shopper"]
    Operator["維運或開發者"]
    OpenAI["Azure OpenAI"]
    ShopSystem["AndrewShop Demo Shop\nphase 2"]

    Customer -->|"HTTP API / Console 指令"| ShopSystem
    Operator -->|"初始化資料 / 啟動設定"| ShopSystem
    ShopSystem -->|"聊天補助與 function calling"| OpenAI
```

## Container

```mermaid
flowchart LR
    Customer["消費者 / Shopper"]
    Operator["維運或開發者"]
    OpenAI["Azure OpenAI"]
    LiteDB["LiteDB"]

    subgraph Shop["AndrewShop Demo Shop"]
        API["AndrewDemo.NetConf2023.API\nHTTP Host / Controllers / DI"]
        Console["AndrewDemo.NetConf2023.ConsoleUI\nCLI + Semantic Kernel"]
        Core["AndrewDemo.NetConf2023.Core\nCheckoutService / Domain / DB Context"]
        Abstract["AndrewDemo.NetConf2023.Abstract\nShared Contracts"]
        DbInit["AndrewDemo.NetConf2023.DatabaseInit\nSeed / DB Init Tool"]
        Tests["AndrewDemo.NetConf2023.Core.Tests\nVerification Project"]
    end

    Customer --> API
    Customer --> Console
    Operator --> DbInit
    Console --> OpenAI

    API --> Core
    API --> Abstract
    Console --> Core
    Console --> Abstract
    DbInit --> Core
    Core --> Abstract
    Core --> LiteDB
    DbInit --> LiteDB
    Tests --> Core
    Tests --> Abstract
```

### Container 對應說明

| Container | 對應 project | phase 2 角色 |
| --- | --- | --- |
| API | `src/AndrewDemo.NetConf2023.API` | HTTP boundary、auth middleware、controller mapping |
| Console | `src/AndrewDemo.NetConf2023.ConsoleUI` | CLI 操作介面、Semantic Kernel function calling、共用 `CheckoutService` |
| Core | `src/AndrewDemo.NetConf2023.Core` | checkout orchestration、discount、product lookup、LiteDB context |
| Abstract | `src/AndrewDemo.NetConf2023.Abstract` | 已凍結 contract，phase 2 不任意更動 |
| DbInit | `src/AndrewDemo.NetConf2023.DatabaseInit` | 資料庫初始化 |
| Tests | `tests/AndrewDemo.NetConf2023.Core.Tests` | phase 2 checkout 驗證 |

## Component

phase 2 最重要的 component 變化發生在 checkout path，因此 component 圖以 `API + Core.Checkouts` 為中心。

```mermaid
flowchart LR
    Program["Program.cs\nComposition Root"]
    CheckoutController["CheckoutController\nHTTP Boundary"]
    CheckoutService["CheckoutService"]
    CheckoutModels["CheckoutCreate/Complete\nCommand + Result"]
    WaitingRoom["WaitingRoomTicket"]
    Database["IShopDatabaseContext"]
    DiscountEngine["DiscountEngine"]
    CartContextFactory["CartContextFactory"]
    ProductService["IProductService / DefaultProductService"]
    EventFactory["ProductOrderEventFactory"]
    Manifest["ShopManifest"]

    Program --> CheckoutController
    Program --> CheckoutService
    Program --> DiscountEngine
    Program --> ProductService
    Program --> Manifest
    Program --> Database

    CheckoutController --> CheckoutModels
    CheckoutController --> CheckoutService
    CheckoutController --> Database

    CheckoutService --> CheckoutModels
    CheckoutService --> WaitingRoom
    CheckoutService --> Database
    CheckoutService --> DiscountEngine
    CheckoutService --> CartContextFactory
    CheckoutService --> ProductService
    CheckoutService --> EventFactory
    CheckoutService --> Manifest

    CartContextFactory --> ProductService
    CartContextFactory --> Manifest
    EventFactory --> Manifest
```

## phase 2 解讀重點

- component owner 從 `CheckoutController` 轉移到 `CheckoutService`，這是 phase 1 -> phase 2 的核心差異。
- `CheckoutModels` 讓 `.Core` 不再直接吃 API request / response class，邊界明確化。
- `WaitingRoomTicket` 雖然不是新類別，但 phase 2 起使用決策已屬於 `CheckoutService`，不再由 controller 主導。
- `DiscountEngine`、`CartContextFactory`、`IProductService`、`ProductOrderEventFactory` 現在都由 `CheckoutService` 串接，形成單一 checkout application flow。
