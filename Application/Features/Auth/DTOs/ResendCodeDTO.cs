using Application.Validations;
using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class ResendCodeDTO
{
    #region Field(s)
    public string Email { get; set; } = null!;

    #endregion

    #region Constructor(s)
    public ResendCodeDTO(string email)
    {
        Email = email;
    }

    #endregion

    #region Validation
    public class Validator : AbstractValidator<ResendCodeDTO>
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
            ApplyValidations();



        }
        public void ApplyValidations()
        {
            const int maxLength = 256;

            // Email Rules
            RuleFor(x => x.Email)
                .ApplyNotEmptyRule(_localizer[SharedResourcesKeys.EmailRequired])
                .ApplyEmailAddressRule(_localizer[SharedResourcesKeys.InvalidEmail])
                .ApplyMaxLengthRule(maxLength, string.Format(_localizer[SharedResourcesKeys.MaxLength].Value, "Email", maxLength));


        }
    }
    #endregion
}
