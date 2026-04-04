using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.CommonStorefront.Pages.Member;

public sealed class IndexModel : AuthenticatedPageModel
{
    private readonly CoreApiClient _coreApiClient;

    public IndexModel(StorefrontSessionAccessor sessionAccessor, CoreApiClient coreApiClient)
        : base(sessionAccessor)
    {
        _coreApiClient = coreApiClient;
    }

    public AndrewDemo.NetConf2023.Core.Member? Member { get; private set; }

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        try
        {
            Member = await _coreApiClient.GetMemberProfileAsync(AccessToken!, cancellationToken);
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("會員資料目前無法載入。");
        }

        return Page();
    }
}
