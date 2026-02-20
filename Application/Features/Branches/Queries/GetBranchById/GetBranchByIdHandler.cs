using Application.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches;

public class GetBranchByIdHandler : IRequestHandler<GetBranchByIdQuery, Response<BranchDetailsDTO>>
{
    #region Field(s)
    private readonly IBranchRepository _branchRepository;
    private readonly IMapper _mapper;
    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor(s)
    public GetBranchByIdHandler(IBranchRepository branchRepository, IMapper mapper, ResponseHandler responseHandler)
    {
        _branchRepository = branchRepository;
        _mapper = mapper;
        _responseHandler = responseHandler;
    }
    #endregion

    #region Handler
    public async Task<Response<BranchDetailsDTO>> Handle(GetBranchByIdQuery request, CancellationToken cancellationToken)
    {
        var branch = await _branchRepository
            .GetByIdAsync(request.Id)
            .ProjectTo<BranchDetailsDTO>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
            return _responseHandler.NotFound<BranchDetailsDTO>();

        return _responseHandler.Success(branch);
    }
    #endregion
}