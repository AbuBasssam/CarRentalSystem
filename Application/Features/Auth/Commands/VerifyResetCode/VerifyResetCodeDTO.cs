using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

/// <summary>
/// DTO to verify password reset code
/// </summary>
public record VerifyResetCodeDTO
{
    #region Field(s)

    public string Code { get; init; } = null!;

    #endregion

    #region Validation

    public class Validator : AbstractValidator<VerifyResetCodeDTO>
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;


            RuleFor(x => x.Code)
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
