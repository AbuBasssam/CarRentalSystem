using Application.Models;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches;

public class GetBranchesHandler
    : IRequestHandler<GetBranchesQuery, Response<PaginatedResult<BranchSummaryDTO>>>
{
    #region Field(s)
    private readonly IBranchRepository _branchRepository;
    private readonly IRequestContext _requestContext;
    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor(s)
    public GetBranchesHandler(
        IBranchRepository branchRepository,
        IRequestContext requestContext,
        ResponseHandler responseHandler)
    {
        _branchRepository = branchRepository;
        _requestContext = requestContext;
        _responseHandler = responseHandler;
    }
    #endregion

    #region Handler
    public async Task<Response<PaginatedResult<BranchSummaryDTO>>> Handle(
        GetBranchesQuery request, CancellationToken cancellationToken)
    {
        bool isAr = _requestContext.Language == "ar";


        var totalCount = await _branchRepository
            .GetTableNoTracking()
            .OrderBy(b => b.Id)
            .CountAsync(cancellationToken);

        var data = await _branchRepository
            .GetPage(request.PageNumber, request.PageSize)
            .Select(b => new BranchSummaryDTO
            {
                Id = b.Id,
                Name = isAr ? b.NameAR : b.NameEN,
                City = isAr ? b.CityAR : b.CityEN,
                IsActive = b.IsActive
            })
            .ToListAsync(cancellationToken);

        var result = PaginatedResult<BranchSummaryDTO>.Success(
            data,
            totalCount,
            request.PageNumber,
            request.PageSize
        );

        return _responseHandler.Paginated(result);
    }
    #endregion
}