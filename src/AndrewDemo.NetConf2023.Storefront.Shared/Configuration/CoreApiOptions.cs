namespace AndrewDemo.NetConf2023.Storefront.Shared.Configuration;

public sealed class CoreApiOptions
{
    public const string SectionName = "Storefront:CoreApi";

    public string BaseUrl { get; set; } = "http://localhost:5108";

    public string? PublicOAuthBaseUrl { get; set; }

    public string OAuthClientId { get; set; } = "andrewshop-common-storefront";
}
