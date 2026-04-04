using System.Net;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Clients;

public sealed class AppleBtsApiRequestException : Exception
{
    public AppleBtsApiRequestException(HttpStatusCode statusCode, string? body)
        : base($"AppleBTS API request failed: {(int)statusCode} {statusCode}")
    {
        StatusCode = statusCode;
        ResponseBody = body;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }
}

public sealed class BtsCatalogItemDto
{
    public string CampaignId { get; set; } = string.Empty;

    public string CampaignName { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public DateTime QueriedAt { get; set; }

    public string MainProductId { get; set; } = string.Empty;

    public string MainProductName { get; set; } = string.Empty;

    public string? MainProductDescription { get; set; }

    public decimal RetailPrice { get; set; }

    public decimal BtsPrice { get; set; }

    public int MaxGiftQuantity { get; set; }

    public decimal? MaxGiftSubsidyAmount { get; set; }

    public List<BtsGiftOptionDto> GiftOptions { get; set; } = new();
}

public sealed class BtsGiftOptionDto
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }
}

public sealed class BtsQualificationResponseDto
{
    public int MemberId { get; set; }

    public string MemberName { get; set; } = string.Empty;

    public bool IsQualified { get; set; }

    public string? Email { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? ExpireAt { get; set; }

    public string? Reason { get; set; }
}
