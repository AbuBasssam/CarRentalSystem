using Application.Models;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Branches;

public class ToggleBranchStatusHandler : IRequestHandler<ToggleBranchStatusCommand, Response<bool>>
{
    #region Field(s)
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;
    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor(s)
    public ToggleBranchStatusHandler(
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        ResponseHandler responseHandler,
        IRequestContext requestContext)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _responseHandler = responseHandler;
        _requestContext = requestContext;
    }
    #endregion

    #region Handler
    public async Task<Response<bool>> Handle(ToggleBranchStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var branch = await _branchRepository
                .GetByIdAsync(request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (branch is null)
            {
                Log.Error(messageTemplate: "Trying to change non-existent branch status with Id:{Id} by {userAgent}.", request.Id, _requestContext.UserAgent);
                return _responseHandler.NotFound<bool>();
            }
            branch.IsActive = !branch.IsActive;

            _branchRepository.Update(branch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Success(branch.IsActive);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error toggling branch status: {Id}", request.Id);
            return _responseHandler.InternalServerError<bool>();
        }
    }
    #endregion
}