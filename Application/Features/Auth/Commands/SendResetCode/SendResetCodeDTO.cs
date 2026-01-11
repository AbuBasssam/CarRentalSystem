using Application.Validations;
using ApplicationLayer.Resources;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public record SendResetCodeDTO
{
    #region Field(s)
    public string Email { get; init; } = null!;

    #endregion

    #region Constructor(s)
    public SendResetCodeDTO(string email)
    {
        Email = email;
    }

    #endregion

    #region Validation
    public class Validator : AbstractValidator<SendResetCodeDTO>
    {
        private readonly IStringLocalizer<SharedResources> _localizer;

        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
            RuleFor(x => x.Email)
                .ApplyNotEmptyRule(_localizer[SharedResourcesKeys.EmailRequired])
                .ApplyEmailAddressRule(_localizer[SharedResourcesKeys.InvalidEmail]);


        }
    }
    #endregion
}
