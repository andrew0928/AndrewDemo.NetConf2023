namespace AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

public sealed class NotificationBannerViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Tone { get; set; } = "info";
}
