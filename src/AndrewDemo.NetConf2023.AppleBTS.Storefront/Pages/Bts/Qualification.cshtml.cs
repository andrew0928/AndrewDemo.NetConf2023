using AndrewDemo.NetConf2023.AppleBTS.Storefront.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront.Pages.Bts;

public sealed class QualificationModel : AuthenticatedPageModel
{
    private readonly AppleBtsApiClient _appleBtsApiClient;

    public QualificationModel(StorefrontSessionAccessor sessionAccessor, AppleBtsApiClient appleBtsApiClient)
        : base(sessionAccessor)
    {
        _appleBtsApiClient = appleBtsApiClient;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public BtsQualificationResponseDto? Qualification { get; private set; }

    public ErrorSummaryViewModel ErrorSummary { get; } = new();

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        return await LoadQualificationPageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var redirect = EnsureAuthenticated();
        if (redirect != null)
        {
            return redirect;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorSummary.Errors.Add("請輸入教育信箱。");
            return await LoadQualificationPageAsync(cancellationToken);
        }

        try
        {
            Qualification = await _appleBtsApiClient.VerifyQualificationAsync(AccessToken!, Email.Trim(), cancellationToken);
            TempData["NotificationTitle"] = Qualification.IsQualified ? "驗證完成" : "驗證未通過";
            TempData["NotificationMessage"] = Qualification.IsQualified
                ? "教育資格已更新，可回到 BTS 專區確認活動商品。"
                : Qualification.Reason ?? "目前未符合 BTS 資格。";
            TempData["NotificationTone"] = Qualification.IsQualified ? "success" : "warning";
            return Page();
        }
        catch (AppleBtsApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
            return RedirectToLogin();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("教育資格目前無法驗證，請稍後再試。");
            return await LoadQualificationPageAsync(cancellationToken);
        }
    }

    private async Task<IActionResult> LoadQualificationPageAsync(CancellationToken cancellationToken)
    {
        try
        {
            Qualification = await _appleBtsApiClient.GetCurrentQualificationAsync(AccessToken!, cancellationToken);
            Email = Qualification.Email ?? Email;
            return Page();
        }
        catch (AppleBtsApiRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            SessionAccessor.ClearAccessToken();
            return RedirectToLogin();
        }
        catch (Exception)
        {
            ErrorSummary.Errors.Add("目前無法讀取教育資格。");
            return Page();
        }
    }
}
