using Application.Features.AuthFeature;
using Application.Validations;
using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Validators.PasswordReset;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    #region Field(s)
    private readonly IStringLocalizer<SharedResources> _Localizer;
    #endregion

    public ResetPasswordValidator(IStringLocalizer<SharedResources> localizer)
    {
        _Localizer = localizer;

        ApplyValidations();
    }
    private void ApplyValidations()
    {
        byte passwordMinLength = 8;
        byte passwordMaxLength = 16;

        RuleFor(x => x.NewPassword)
            .ApplyNotEmptyRule(_Localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
            .ApplyNotNullableRule(_Localizer[SharedResourcesKeys.PropertyCannotBeNull])
            .ApplyMinLengthRule(passwordMinLength, string.Format(_Localizer[SharedResourcesKeys.MinLength].Value, "Password", passwordMinLength))
            .ApplyMaxLengthRule(passwordMaxLength, string.Format(_Localizer[SharedResourcesKeys.MaxLength].Value, "Password", passwordMaxLength))
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}
