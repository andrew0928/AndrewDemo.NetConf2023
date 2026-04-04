using AndrewDemo.NetConf2023.Abstract.Discounts;

namespace AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

public sealed class CartLineViewModel
{
    public string LineId { get; set; } = string.Empty;

    public string? ParentLineId { get; set; }

    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineAmount { get; set; }
}

public sealed class PriceAdjustmentViewModel
{
    public DiscountRecordKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public IReadOnlyList<string> RelatedLineIds { get; set; } = Array.Empty<string>();
}

public sealed class PriceSummaryViewModel
{
    public decimal Subtotal { get; set; }

    public IReadOnlyList<PriceAdjustmentViewModel> Adjustments { get; set; } = Array.Empty<PriceAdjustmentViewModel>();

    public decimal Total { get; set; }
}
