namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Configuration;

public sealed class AppleBtsApiOptions
{
    public const string SectionName = "Storefront:AppleBtsApi";

    public string BaseUrl { get; set; } = "http://localhost:5118";
}
