# TC-04 建立 Checkout Transaction、完成結帳、查詢訂單

## 目的

驗證結帳主流程是否能：

1. 先建立 checkout transaction。
2. 在完成付款後產生正式訂單。
3. 讓會員查詢自己的訂單歷史。

## 主要來源

- `src/AndrewDemo.NetConf2023.API/Controllers/CheckoutController.cs`
- `src/AndrewDemo.NetConf2023.API/Controllers/MemberController.cs`
- `src/AndrewDemo.NetConf2023.Core/Order.cs`
- `src/AndrewDemo.NetConf2023.Core/Checkout.cs`
- `src/AndrewDemo.NetConf2023.Core/DiscountEngine.cs`
- `src/AndrewDemo.NetConf2023.API/AndrewDemo.NetConf2023.API.http`

## 前置條件

- Bearer token 有效。
- 指定 cart 已存在，且 cart 內已有商品。
- 外部支付流程已先完成，能提供 `paymentId`。

## 主流程

1. Client 呼叫 `POST /api/checkout/create`，送入 `cartId`。
2. `CheckoutController` 驗證 token、member、cart，建立 `CheckoutTransactionRecord`。
3. Client 呼叫 `POST /api/checkout/complete`，送入 `transactionId`、`paymentId`、滿意度與評論。
4. Controller 建立 `WaitingRoomTicket`，等待可執行時機。
5. Controller 讀出 transaction，之後立即刪除該 transaction record。
6. Controller 載入 cart、member、products，建立 `Order` 與商品明細。
7. Controller 呼叫 `DiscountEngine.Calculate(...)`，把折扣轉成 `OrderLineItem`。
8. Controller 寫入 `Orders` collection，回傳 `CheckoutCompleteResponse`。
9. Client 之後可呼叫 `GET /api/member/orders` 查詢這位會員的所有訂單。

## 預期結果

- `checkout_transactions` 只在 create 到 complete 之間短暫存在。
- `orders` collection 會新增一筆 `Order.Id = transactionId` 的資料。
- `GET /api/member/orders` 會依 `Buyer.Id` 聚合這位會員的訂單數與金額。
- 這版實作實際上不會清空 cart；這點已記錄在 review notes。

## Class Diagram

```mermaid
classDiagram
    class CheckoutController {
        +Create(request) ActionResult~CheckoutCreateResponse~
        +CompleteAsync(request) Task
    }

    class MemberController {
        +GetOrders() ActionResult~MemberOrdersResponse~
    }

    class CheckoutCreateRequest {
        +int CartId
    }

    class CheckoutCreateResponse {
        +int TransactionId
        +DateTime TransactionStartAt
        +int ConsumerId
        +string ConsumerName
    }

    class CheckoutCompleteRequest {
        +int TransactionId
        +int PaymentId
        +int Satisfaction
        +string ShopComments
    }

    class CheckoutCompleteResponse {
        +int TransactionId
        +int PaymentId
        +DateTime TransactionCompleteAt
        +int ConsumerId
        +string ConsumerName
        +Order OrderDetail
    }

    class MemberOrdersResponse {
        +int TotalOrders
        +decimal TotalAmount
        +List~Order~ Orders
    }

    class CheckoutTransactionRecord {
        +int TransactionId
        +int CartId
        +int MemberId
        +DateTime CreatedAt
    }

    class WaitingRoomTicket {
        +int Id
        +WaitUntilCanRunAsync() Task
    }

    class Order {
        +int Id
        +Member Buyer
        +List~OrderLineItem~ LineItems
        +decimal TotalPrice
        +OrderShopNotes ShopNotes
    }

    class OrderLineItem {
        +string Title
        +decimal Price
    }

    class OrderShopNotes {
        +int BuyerSatisfaction
        +string Comments
    }

    class Cart {
        +int Id
        +IEnumerable~CartLineItem~ LineItems
    }

    class Product {
        +int Id
        +string Name
        +decimal Price
    }

    class Member {
        +int Id
        +string Name
    }

    class DiscountEngine {
        <<static>>
        +Calculate(cart, consumer, context) IEnumerable~DiscountRecord~
    }

    class IShopDatabaseContext {
        <<interface>>
        +CheckoutTransactions
        +Orders
        +Carts
        +Members
        +Products
        +MemberTokens
    }

    CheckoutController --> CheckoutCreateRequest : accepts
    CheckoutController --> CheckoutCompleteRequest : accepts
    CheckoutController --> CheckoutCreateResponse : returns
    CheckoutController --> CheckoutCompleteResponse : returns
    CheckoutController --> IShopDatabaseContext : uses
    CheckoutController --> WaitingRoomTicket : uses
    CheckoutController --> DiscountEngine : uses
    MemberController --> MemberOrdersResponse : returns
    IShopDatabaseContext --> CheckoutTransactionRecord : persists
    IShopDatabaseContext --> Order : persists
    IShopDatabaseContext --> Cart : loads
    IShopDatabaseContext --> Product : loads
    IShopDatabaseContext --> Member : loads
    Order --> OrderLineItem : contains
    Order --> OrderShopNotes : contains
```

## Sequence Diagram

```mermaid
sequenceDiagram
    actor Client as Authenticated Client
    participant CheckoutApi as CheckoutController
    participant MemberApi as MemberController
    participant DB as IShopDatabaseContext
    participant Waiting as WaitingRoomTicket
    participant Discount as DiscountEngine

    Client->>CheckoutApi: POST /api/checkout/create(cartId)
    CheckoutApi->>DB: MemberTokens.FindById(token)
    CheckoutApi->>DB: Members.FindById(memberId)
    CheckoutApi->>DB: Carts.FindById(cartId)
    CheckoutApi->>DB: CheckoutTransactions.Insert(transaction)
    CheckoutApi-->>Client: 201 TransactionId

    Client->>CheckoutApi: POST /api/checkout/complete(transactionId, paymentId)
    CheckoutApi->>DB: MemberTokens.FindById(token)
    CheckoutApi->>DB: Members.FindById(memberId)
    CheckoutApi->>Waiting: WaitUntilCanRunAsync()
    Waiting-->>CheckoutApi: ready
    CheckoutApi->>DB: CheckoutTransactions.FindById(transactionId)
    DB-->>CheckoutApi: transaction
    CheckoutApi->>DB: CheckoutTransactions.Delete(transactionId)
    CheckoutApi->>DB: Carts.FindById(transaction.CartId)
    CheckoutApi->>DB: Members.FindById(transaction.MemberId)
    loop 每一筆 cart item
        CheckoutApi->>DB: Products.FindById(productId)
        DB-->>CheckoutApi: product
        CheckoutApi->>CheckoutApi: build order line
    end
    CheckoutApi->>Discount: Calculate(cart, consumer, DB)
    Discount-->>CheckoutApi: discount records
    CheckoutApi->>CheckoutApi: build discount lines and total
    CheckoutApi->>DB: Orders.Upsert(order)
    CheckoutApi-->>Client: 200 CheckoutCompleteResponse

    Client->>MemberApi: GET /api/member/orders
    MemberApi->>DB: MemberTokens.FindById(token)
    MemberApi->>DB: Members.FindById(memberId)
    MemberApi->>DB: Orders.Find(o => o.Buyer.Id == member.Id)
    MemberApi-->>Client: 200 TotalOrders + TotalAmount + Orders
```

## 與這版設計相關的重點

- `paymentId` 只被帶入 response 與 order completion context，沒有真正的 payment integration。
- 訂單編號直接沿用 `transactionId`。
- 這版 controller 註解聲稱完成結帳後會清空購物車，但實作沒有做這件事。
