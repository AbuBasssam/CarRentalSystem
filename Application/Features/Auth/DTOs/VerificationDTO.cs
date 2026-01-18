using Application.Validations;
using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;



public class VerificationDTO
{
    #region Field(s)
    public string Email { get; set; } = null!;
    public string OtpCode { get; set; } = null!;

    #endregion

    #region Constructor(s)
    public VerificationDTO(string otpCode)
    {
        OtpCode = otpCode;
    }

    #endregion

    #region Validation
    public class Validator : AbstractValidator<VerificationDTO>
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;

            // Email Rules
            RuleFor(x => x.Email).ApplyEmailValidation(_localizer);

            RuleFor(x => x.OtpCode).ApplyOtpCodeRules(6, _localizer);
        }
    }
    #endregion
}
