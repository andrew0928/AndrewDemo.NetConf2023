using AndrewDemo.NetConf2023.AppleBTS.Storefront.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Pages.Bts;

public sealed class IndexModel : StorefrontPageModel
{
    private readonly AppleBtsApiClient _appleBtsApiClient;

    public IndexModel(StorefrontSessionAccessor sessionAccessor, AppleBtsApiClient appleBtsApiClient)
        : base(sessionAccessor)
    {
        _appleBtsApiClient = appleBtsApiClient;
    }

    public List<BtsCatalogItemDto> Offers { get; private set; } = new();

    public BtsQualificationResponseDto? Qualification { get; private set; }

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Offers = (await _appleBtsApiClient.GetPublishedOffersAsync(cancellationToken)).ToList();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("BTS 型錄目前無法載入，請稍後再試。");
        }

        var accessToken = SessionAccessor.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        try
        {
            Qualification = await _appleBtsApiClient.GetCurrentQualificationAsync(accessToken, cancellationToken);
        }
        catch (AppleBtsApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法載入教育資格狀態。");
        }
    }
}
