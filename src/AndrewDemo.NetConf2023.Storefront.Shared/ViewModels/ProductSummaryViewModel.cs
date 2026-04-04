namespace AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

public sealed class ProductSummaryViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }
}
