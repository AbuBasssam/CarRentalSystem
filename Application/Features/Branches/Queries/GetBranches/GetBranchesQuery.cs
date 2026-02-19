using Application.Abstracts;
using Application.Models;
using MediatR;

namespace Application.Features.Branches;

public class GetBranchesQuery : LocalizePaginationQuery,
    IRequest<Response<PaginatedResult<BranchSummaryDTO>>>
{
}
