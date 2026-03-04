using Application.Abstracts;
using FluentValidation;

namespace Application.Validations;

public abstract class CursorPaginationValidator<T> : AbstractValidator<T> where T : CursorPaginationQuery
{
    protected CursorPaginationValidator()
    {


        

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 30)
            .WithMessage("Page size must be between 1 and 30.");
    }
}
