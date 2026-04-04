using System.Text.RegularExpressions;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Services;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Time;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.AppleBTS.API.Controllers
{
    [Route("bts-api/qualification")]
    [ApiController]
    public class QualificationController : ControllerBase
    {
        private static readonly Regex EduMailPattern = new(
            @"^[^@\s]+@[^@\s]+\.edu\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IShopDatabaseContext _database;
        private readonly AppleBtsAdminService _adminService;
        private readonly MemberEducationQualificationService _qualificationService;
        private readonly BtsOfferRepository _offerRepository;
        private readonly TimeProvider _timeProvider;

        public QualificationController(
            IShopDatabaseContext database,
            AppleBtsAdminService adminService,
            MemberEducationQualificationService qualificationService,
            BtsOfferRepository offerRepository,
            TimeProvider timeProvider)
        {
            _database = database;
            _adminService = adminService;
            _qualificationService = qualificationService;
            _offerRepository = offerRepository;
            _timeProvider = timeProvider;
        }

        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<QualificationResponse> GetCurrentQualification()
        {
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized();
            }

            return ToResponse(member, _qualificationService.Evaluate(member.Id, _timeProvider.GetUtcDateTime()));
        }

        [HttpPost("verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<QualificationResponse> Verify([FromBody] VerifyEducationRequest request)
        {
            var member = GetAuthenticatedMember();
            if (member == null)
            {
                return Unauthorized();
            }

            var now = _timeProvider.GetUtcDateTime();
            var activeCampaign = _offerRepository.GetActiveCampaign(now);
            var expireAt = activeCampaign?.EndAt ?? now.AddDays(30);
            var isQualified = IsEducationEmail(request.Email);

            _adminService.UpsertMemberEducationVerification(new MemberEducationVerificationRecord
            {
                VerificationId = $"{member.Id}-{now:yyyyMMddHHmmssfff}",
                MemberId = member.Id,
                Email = request.Email?.Trim() ?? string.Empty,
                Status = isQualified ? EducationVerificationStatus.Verified : EducationVerificationStatus.Rejected,
                VerifiedAt = now,
                ExpireAt = expireAt,
                Source = "bts-api"
            });

            return ToResponse(member, _qualificationService.Evaluate(member.Id, now));
        }

        private static bool IsEducationEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return EduMailPattern.IsMatch(email.Trim());
        }

        private QualificationResponse ToResponse(Member member, EducationQualificationResult result)
        {
            return new QualificationResponse
            {
                MemberId = member.Id,
                MemberName = member.Name,
                IsQualified = result.IsQualified,
                Email = result.Email,
                VerifiedAt = result.VerifiedAt,
                ExpireAt = result.ExpireAt,
                Reason = result.Reason
            };
        }

        private Member? GetAuthenticatedMember()
        {
            var accessToken = HttpContext.Items["access-token"] as string;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var tokenRecord = _database.MemberTokens.FindById(accessToken);
            if (tokenRecord == null || tokenRecord.Expire <= _timeProvider.GetLocalDateTime())
            {
                return null;
            }

            return _database.Members.FindById(tokenRecord.MemberId);
        }

        public sealed class VerifyEducationRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        public sealed class QualificationResponse
        {
            public int MemberId { get; set; }
            public string MemberName { get; set; } = string.Empty;
            public bool IsQualified { get; set; }
            public string? Email { get; set; }
            public DateTime? VerifiedAt { get; set; }
            public DateTime? ExpireAt { get; set; }
            public string? Reason { get; set; }
        }
    }
}
