using Application.Models;
using AutoMapper;
using Domain.Entities;
using Interfaces;
using MediatR;
using Serilog;

namespace Application.Features.Branches;

public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, Response<int>>
{
    #region Field(s)
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor(s)
    public CreateBranchHandler(IBranchRepository branchRepository,IUnitOfWork unitOfWork,IMapper mapper,ResponseHandler responseHandler)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _responseHandler = responseHandler;
    }
    #endregion

    #region Handler
    public async Task<Response<int>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var branch = _mapper.Map<Branch>(request.Dto);

            await _branchRepository.AddAsync(branch);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Created(branch.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating branch: {Name}", request.Dto.NameEN);
            return _responseHandler.InternalServerError<int>();
        }
    }
    #endregion
}