using ApplicationLayer.Resources;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.Branches;

public class UpdateBranchDTO
{
    public string NameEN { get; set; } = null!;
    public string NameAR { get; set; } = null!;
    public string CityEN { get; set; } = null!;
    public string CityAR { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    #region Mapper
    private class Mapping : Profile
    {
        public Mapping()
        {

            CreateMap<UpdateBranchDTO, Branch>()
                .ForMember(dest => dest.NameEN,
                opt => opt.MapFrom(src => src.NameEN.Trim()))

            .ForMember(dest => dest.NameAR,
                opt => opt.MapFrom(src => src.NameAR.Trim()))

            .ForMember(dest => dest.CityEN,
                opt => opt.MapFrom(src => src.CityEN.Trim()))

            .ForMember(dest => dest.CityAR,
                opt => opt.MapFrom(src => src.CityAR.Trim()))
            .ForAllMembers(opt =>
        opt.Condition((src, dest, srcValue, destValue) => !Equals(srcValue, destValue))

        );
        }
    }
    #endregion

    #region Validator
    public class Validator : AbstractValidator<UpdateBranchDTO>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            byte maxLength = 75;

            RuleFor(x => x.NameEN)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .MaximumLength(maxLength)
                .WithMessage(string.Format(localizer[SharedResourcesKeys.MaxLength].Value, "NameEN", maxLength));

            RuleFor(x => x.NameAR)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .MaximumLength(maxLength)
                .WithMessage(string.Format(localizer[SharedResourcesKeys.MaxLength].Value, "NameAR", maxLength));

            RuleFor(x => x.CityEN)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .MaximumLength(maxLength)
                .WithMessage(string.Format(localizer[SharedResourcesKeys.MaxLength].Value, "CityEN", maxLength));

            RuleFor(x => x.CityAR)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .MaximumLength(maxLength)
                .WithMessage(string.Format(localizer[SharedResourcesKeys.MaxLength].Value, "CityAR", maxLength));

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage(localizer[SharedResourcesKeys.InvalidLatitude]);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage(localizer[SharedResourcesKeys.InvalidLongitude]);
        }
    }
    #endregion
}