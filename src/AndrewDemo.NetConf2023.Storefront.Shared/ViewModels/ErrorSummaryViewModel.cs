namespace AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

public sealed class ErrorSummaryViewModel
{
    public string Title { get; set; } = "請檢查以下欄位";

    public List<string> Errors { get; set; } = new();
}
