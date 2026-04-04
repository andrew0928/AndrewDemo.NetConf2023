namespace AndrewDemo.NetConf2023.Storefront.Shared.Configuration;

public sealed class StorefrontSessionOptions
{
    public const string SectionName = "Storefront:Session";

    public string CookieName { get; set; } = ".AndrewDemo.Storefront.Session";
}
