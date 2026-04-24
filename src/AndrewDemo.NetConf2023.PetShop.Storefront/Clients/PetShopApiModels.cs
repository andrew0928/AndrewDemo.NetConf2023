using System.Net;

namespace AndrewDemo.NetConf2023.PetShop.Storefront.Clients;

public sealed class PetShopApiRequestException : Exception
{
    public PetShopApiRequestException(HttpStatusCode statusCode, string? body)
        : base($"PetShop API request failed: {(int)statusCode} {statusCode}")
    {
        StatusCode = statusCode;
        ResponseBody = body;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }
}

public sealed class PetShopServiceDto
{
    public string ServiceId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int DurationMinutes { get; set; }
}

public sealed class PetShopAvailabilitySlotDto
{
    public string ServiceId { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string VenueId { get; set; } = string.Empty;

    public string? VenueName { get; set; }

    public string StaffId { get; set; } = string.Empty;

    public string? StaffName { get; set; }
}

public sealed class CreatePetShopReservationHoldRequestDto
{
    public string ServiceId { get; set; } = string.Empty;

    public DateTime StartAt { get; set; }

    public string VenueId { get; set; } = string.Empty;

    public string StaffId { get; set; } = string.Empty;
}

public sealed class PetShopReservationDto
{
    public string ReservationId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int BuyerMemberId { get; set; }

    public string ServiceId { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string VenueId { get; set; } = string.Empty;

    public string StaffId { get; set; } = string.Empty;

    public DateTime HoldExpiresAt { get; set; }

    public string? CheckoutProductId { get; set; }

    public int? ConfirmedOrderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
