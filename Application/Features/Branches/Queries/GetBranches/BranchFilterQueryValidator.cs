using Application.Validations;
using FluentValidation;

namespace Application.Features.Branches;

public class BranchFilterQueryValidator : FilterQueryValidator<GetBranchesQuery>
{
    // كل ما عليك فعله هو تحديد الـ fields المسموح بها
    protected override IReadOnlyCollection<string> AllowedSortFields { get; }
        = ["id", "name", "city", "active"];

    public BranchFilterQueryValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(75).When(x => x.Name is not null);

        RuleFor(x => x.City)
            .MaximumLength(75).When(x => x.City is not null);
    }
}
