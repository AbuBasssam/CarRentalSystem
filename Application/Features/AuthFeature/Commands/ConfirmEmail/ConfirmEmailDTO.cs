using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;



public class ConfirmEmailDTO
{
    #region Field(s)
    public string OtpCode { get; set; } = null!;

    #endregion

    #region Constructor(s)
    public ConfirmEmailDTO(string otpCode)
    {
        OtpCode = otpCode;
    }

    #endregion

    #region Validation
    public class Validator : AbstractValidator<ConfirmEmailDTO>
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;


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
