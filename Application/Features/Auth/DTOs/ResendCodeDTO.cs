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

            // Email Rules
            RuleFor(x => x.Email)
                .ApplyEmailValidation(_localizer);


        }
    }
    #endregion
}
