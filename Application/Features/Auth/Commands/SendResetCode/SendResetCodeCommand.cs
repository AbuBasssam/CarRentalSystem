using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;
public record SendResetCodeCommand(SendResetCodeDTO DTO) : IRequest<Response<bool>>;
