using Application.Validations;
using ApplicationLayer.Resources;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.AuthFeature;

public class SignUpCommandDTO
{

    #region Field(s)
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

    #endregion

    #region Constructure(s)

    public SignUpCommandDTO(string firstName, string lastName, string email, string password)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;

    }

    #endregion

    #region Mapper(s)
    private class Mapping : Profile
    {
        public Mapping() => CreateMap<SignUpCommandDTO, User>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));

    }
    #endregion

    #region Validation
    public class Validator : AbstractValidator<SignUpCommandDTO>
    {
        #region Field(s)
        private readonly IStringLocalizer<SharedResources> _Localizer;
        #endregion

        #region Constructure(s)
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            _Localizer = localizer;

            ApplyValidations();
        }
        #endregion

        #region Method(s)
        private void ApplyValidations()
        {
            byte minLength = 3;
            byte maxLength = 16;
            byte passwordMinLength = 8;
            byte passwordMaxLength = 16;

            // First Name Rules
            RuleFor(x => x.FirstName)
                .ApplyNotEmptyRule(_Localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .ApplyNotNullableRule(_Localizer[SharedResourcesKeys.PropertyCannotBeNull])
                .ApplyMinLengthRule(minLength, string.Format(_Localizer[SharedResourcesKeys.MinLength].Value, "First Name", minLength))
                .ApplyMaxLengthRule(maxLength, string.Format(_Localizer[SharedResourcesKeys.MaxLength].Value, "First Name", maxLength))

                .Matches(@"^[a-zA-Z0-9\s\-'.,]+$")
                .WithMessage(_Localizer[SharedResourcesKeys.NameInvalidCharacters]);

            // Last Name Rules
            RuleFor(x => x.LastName)
                .ApplyNotEmptyRule(_Localizer[SharedResourcesKeys.PropertyCannotBeEmpty].Value)
                .ApplyNotNullableRule(_Localizer[SharedResourcesKeys.PropertyCannotBeNull].Value)
                .ApplyMinLengthRule(minLength, string.Format(_Localizer[SharedResourcesKeys.MinLength].Value, "Last Name", minLength))
                .ApplyMaxLengthRule(maxLength, string.Format(_Localizer[SharedResourcesKeys.MaxLength].Value, "Last Name", maxLength))

                .Matches(@"^[a-zA-Z0-9\s\-'.,]+$")
                .WithMessage(_Localizer[SharedResourcesKeys.NameInvalidCharacters].Value);


            // Email Rules
            RuleFor(x => x.Email)
                .ApplyNotEmptyRule(_Localizer[SharedResourcesKeys.EmailRequired].Value)
                .ApplyEmailAddressRule(_Localizer[SharedResourcesKeys.InvalidEmail].Value);

            // Password Rules
            RuleFor(x => x.Password)
                .ApplyNotEmptyRule(_Localizer[SharedResourcesKeys.PropertyCannotBeEmpty].Value)
                .ApplyNotNullableRule(_Localizer[SharedResourcesKeys.PropertyCannotBeNull].Value)
                .ApplyMinLengthRule(passwordMinLength, string.Format(_Localizer[SharedResourcesKeys.MinLength].Value, "Password", passwordMinLength))
                .ApplyMaxLengthRule(passwordMaxLength, string.Format(_Localizer[SharedResourcesKeys.MaxLength].Value, "Password", passwordMaxLength));



        }
        #endregion
    }



    #endregion
}
