namespace AndrewDemo.NetConf2023.PetShop.Storefront.Configuration;

public sealed class PetShopApiOptions
{
    public const string SectionName = "Storefront:PetShopApi";

    public string BaseUrl { get; set; } = "http://localhost:5218";
}
