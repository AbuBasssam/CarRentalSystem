using Application.Models;
using AutoMapper;
using Domain.Entities;
using Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.Cars;

public class CreateCarHandler : IRequestHandler<CreateCarCommand, Response<int>>
{
    #region Field(s)

    private readonly ICarRepository _carRepository;
    private readonly IGenericRepository<Branch, int> _branchRepository;
    private readonly IGenericRepository<CarCategory, int> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public CreateCarHandler(
        ICarRepository carRepository,
        IGenericRepository<Branch, int> branchRepository,
        IGenericRepository<CarCategory, int> categoryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ResponseHandler responseHandler)
    {
        _carRepository = carRepository;
        _branchRepository = branchRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<int>> Handle(CreateCarCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Dto;

            // ── Business validations ──────────────────────────────────────────

            var branch = await _branchRepository
                .GetTableNoTracking()
                .Where(b => b.Id == dto.CurrentBranchId && b.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (branch is null)
                return _responseHandler.BadRequest<int>("Branch not found or is inactive.");

            var categoryExists = await _categoryRepository
                .GetTableNoTracking()
                .AnyAsync(c => c.Id == dto.CategoryId && c.IsActive, cancellationToken);

            if (!categoryExists)
                return _responseHandler.BadRequest<int>("Category not found or is inactive.");

            if (!await _carRepository.IsPlateNumberENUniqueAsync(dto.PlateNumberEN))
                return _responseHandler.UnprocessableEntity<int>("PlateNumberEN is already in use.");

            if (!await _carRepository.IsPlateNumberARUniqueAsync(dto.PlateNumberAR))
                return _responseHandler.UnprocessableEntity<int>("PlateNumberAR is already in use.");

            if (!await _carRepository.IsVINUniqueAsync(dto.VIN))
                return _responseHandler.UnprocessableEntity<int>("VIN is already in use.");

            var car = _mapper.Map<Car>(dto);

            await _carRepository.AddAsync(car);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _responseHandler.Created(car.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating car: {PlateEN}", request.Dto.PlateNumberEN);
            return _responseHandler.InternalServerError<int>();
        }
    }

    #endregion
}