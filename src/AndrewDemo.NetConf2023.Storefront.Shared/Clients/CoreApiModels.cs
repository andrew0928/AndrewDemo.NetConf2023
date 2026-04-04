using System.Net;
using System.Text.Json.Serialization;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;

namespace AndrewDemo.NetConf2023.Storefront.Shared.Clients;

public sealed class CoreApiRequestException : Exception
{
    public CoreApiRequestException(HttpStatusCode statusCode, string? body)
        : base($"Core API request failed: {(int)statusCode} {statusCode}")
    {
        StatusCode = statusCode;
        ResponseBody = body;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }
}

public sealed class TokenExchangeResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public sealed class CartEstimateResponseDto
{
    public decimal TotalPrice { get; set; }

    public List<CartDiscountRecordDto> Discounts { get; set; } = new();
}

public sealed class CartDiscountRecordDto
{
    public DiscountRecordKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public List<string> RelatedLineIds { get; set; } = new();
}

public sealed class MemberOrdersResponseDto
{
    public int TotalOrders { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderDto> Orders { get; set; } = new();
}

public sealed class OrderDto
{
    public int Id { get; set; }

    public Member Buyer { get; set; } = new();

    public List<OrderProductLineDto> ProductLines { get; set; } = new();

    public List<OrderDiscountLineDto> DiscountLines { get; set; } = new();

    public decimal TotalPrice { get; set; }

    public OrderFulfillmentStatus FulfillmentStatus { get; set; }
}

public sealed class OrderProductLineDto
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineAmount { get; set; }
}

public sealed class OrderDiscountLineDto
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

public sealed class CheckoutCreateResponseDto
{
    public int TransactionId { get; set; }

    public DateTime TransactionStartAt { get; set; }

    public int ConsumerId { get; set; }

    public string ConsumerName { get; set; } = string.Empty;
}

public sealed class CheckoutCompleteResponseDto
{
    public int TransactionId { get; set; }

    public int PaymentId { get; set; }

    public DateTime TransactionCompleteAt { get; set; }

    public int ConsumerId { get; set; }

    public string ConsumerName { get; set; } = string.Empty;

    public OrderDto OrderDetail { get; set; } = new();
}

public sealed class AddCartItemRequestDto
{
    public string ProductId { get; set; } = string.Empty;

    public int Qty { get; set; }

    public string? ParentLineId { get; set; }
}

public sealed class CreateCheckoutRequestDto
{
    public int CartId { get; set; }
}

public sealed class CompleteCheckoutRequestDto
{
    public int TransactionId { get; set; }

    public int PaymentId { get; set; }

    public int? Satisfaction { get; set; }

    public string? ShopComments { get; set; }
}
