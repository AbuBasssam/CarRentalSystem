using Application.Models;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Branches;

public class DeleteBranchHandler : IRequestHandler<DeleteBranchCommand, Response<bool>>
{
    #region Field(s)
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;
    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor(s)
    public DeleteBranchHandler(
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
    public async Task<Response<bool>> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var branch = await _branchRepository
                .GetByIdAsync(request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (branch is null)
            {
                //Log.Warning("Branch with Id:{Id} not found for deletion.", request.Id);
                Log.Error("Trying to delete a non-existent branch with Id:{Id} by {userAgent}.", request.Id, _requestContext.UserAgent);
                return _responseHandler.NotFound<bool>();
            }
            // Business Rule: Cannot delete a branch that has cars assigned to it.
            var hasCars = await _branchRepository.HasCarsAsync(request.Id, cancellationToken);

            if (hasCars)
            {
                Log.Warning("Attempt to delete branch with Id:{Id} with assigned cars.", request.Id);

                return _responseHandler.BadRequest<bool>(
                    "Cannot delete a branch that has cars assigned to it."
                );

            }


            _branchRepository.Delete(branch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Deleted<bool>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error deleting branch: {Id}", request.Id);
            return _responseHandler.InternalServerError<bool>();
        }
    }
    #endregion
}