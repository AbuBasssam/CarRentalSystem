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
            const int maxLength = 256;

            // Email Rules
            RuleFor(x => x.Email)
                .ApplyNotEmptyRule(_localizer[SharedResourcesKeys.EmailRequired])
                .ApplyEmailAddressRule(_localizer[SharedResourcesKeys.InvalidEmail])
                .ApplyMaxLengthRule(maxLength, string.Format(_localizer[SharedResourcesKeys.MaxLength].Value, "Email", maxLength));

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .WithMessage(_localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .Length(6)
                .WithMessage("OTP must be 6 digits")
                .Matches(@"^\d{6}$")
                .WithMessage("OTP must contain only digits");
        }
    }
    #endregion
}
