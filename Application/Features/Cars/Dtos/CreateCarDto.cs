using ApplicationLayer.Resources;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.HelperClasses;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace Application.Features.Cars;

public class CreateCarDto
{
    public string PlateNumberEN { get; set; } = null!;
    public string PlateNumberAR { get; set; } = null!;
    public string VIN { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public byte NumberOfSeats { get; set; }
    public byte NumberOfBags { get; set; }
    public short EngineCapacity { get; set; }
    public byte FuelType { get; set; }
    public enTransmissionType TransmissionType { get; set; }
    public int CurrentBranchId { get; set; }
    public int CategoryId { get; set; }
    public int? PolicyOverrideId { get; set; }
    public decimal? CustomDailyRate { get; set; }
    public decimal? CustomWeeklyRate { get; set; }
    public decimal? CustomMonthlyRate { get; set; }


    #region Mapper

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<CreateCarDto, Car>()
                .ForMember(dest => dest.FuelTypeObject, opt => opt.MapFrom(src => FuelTypeConverter(src.FuelType)))

                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.FleetConditionStatus, opt => opt.MapFrom(_ => enFleetConditionStatus.Ready))
                .ForMember(dest => dest.KmMileage, opt => opt.MapFrom(_ => 0));
        }
        private FuelType FuelTypeConverter(byte type)
        {
            var fuelType = Domain.HelperClasses.FuelType.FromId(type);
            if (fuelType == null)
            {
                throw new InvalidOperationException($"Invalid FuelType: {type}");
            }
            return fuelType;
        }
    }

    #endregion

    #region Validator

    public class Validator : AbstractValidator<CreateCarDto>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            string strTransmissionType = Helpers.FormatEnumComment<enTransmissionType>();
            string strFuelType = Domain.HelperClasses.FuelType.FormatFuelTypeComments();
            RuleFor(x => x.PlateNumberEN)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .Matches(@"^[A-Z]{3} \d{4}$")
                .WithMessage("PlateNumberEN format must be 'ABC 1234'.");

            RuleFor(x => x.PlateNumberAR)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .MaximumLength(20);

            RuleFor(x => x.VIN)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .Length(17).WithMessage("VIN must be exactly 17 characters.")
                .Matches(@"^[A-HJ-NPR-Z0-9]{17}$")
                .WithMessage("VIN contains invalid characters (I, O, Q are not allowed).");

            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .MaximumLength(50).WithMessage(string.Format(localizer[SharedResourcesKeys.MaxLength].Value, "Brand", 50));

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage(localizer[SharedResourcesKeys.PropertyCannotBeEmpty])
                .MaximumLength(50).WithMessage(string.Format(localizer[SharedResourcesKeys.MaxLength].Value, "Model", 50));

            RuleFor(x => x.Year)
                .InclusiveBetween(1990, DateTime.UtcNow.Year + 1)
                .WithMessage($"Year must be between 1990 and {DateTime.UtcNow.Year + 1}.");

            RuleFor(x => x.NumberOfSeats)
                .InclusiveBetween((byte)1, (byte)15)
                .WithMessage("NumberOfSeats must be between 1 and 15.");

            RuleFor(x => x.NumberOfBags)
                .InclusiveBetween((byte)0, (byte)10)
                .WithMessage("NumberOfBags must be between 0 and 10.");

            RuleFor(x => x.EngineCapacity)
                .GreaterThanOrEqualTo((short)0)
                .WithMessage("EngineCapacity must be >= 0.");

            RuleFor(x => x.FuelType)

                .InclusiveBetween((byte)1, (byte)4)
                .WithMessage($"FuelType must be {strFuelType}.");

            RuleFor(x => x.TransmissionType)
                .IsInEnum()
                .WithMessage($"TransmissionType must be {strTransmissionType}.");

            RuleFor(x => x.CurrentBranchId)
                .GreaterThan(0)
                .WithMessage("CurrentBranchId is required.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0)
                .WithMessage("CategoryId is required.");

            RuleFor(x => x.CustomDailyRate)
           .GreaterThanOrEqualTo(0)
           .WithMessage("custom daily rate cannot be negative.")
           .When(x => x.CustomDailyRate.HasValue);

            RuleFor(x => x.CustomWeeklyRate)
                .GreaterThanOrEqualTo(0)
                .WithMessage("custom weekly rate cannot be negative.")
                .When(x => x.CustomWeeklyRate.HasValue);

            RuleFor(x => x.CustomMonthlyRate)
                .GreaterThanOrEqualTo(0)
                .WithMessage("custom monthly rate cannot be negative.")
                .When(x => x.CustomMonthlyRate.HasValue);
        }
    }

    #endregion
}