using Application.Models;
using AutoMapper;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Branches;

public class UpdateBranchHandler : IRequestHandler<UpdateBranchCommand, Response<bool>>
{
    #region Field(s)
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRequestContext _requestContext;

    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor(s)
    public UpdateBranchHandler(
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRequestContext requestContext,
        ResponseHandler responseHandler)


    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _responseHandler = responseHandler;
        _requestContext = requestContext;
    }
    #endregion

    #region Handler
    public async Task<Response<bool>> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var branch = await _branchRepository
                .GetByIdAsync(request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (branch is null)
                return _responseHandler.NotFound<bool>();

            Log.Information("User {UserId} attempted to update Branch {Id}", _requestContext.UserId, request.Id);

            _mapper.Map(request.Dto, branch);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Success(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating branch: {Id}", request.Id);
            return _responseHandler.InternalServerError<bool>();
        }
    }
    #endregion
}