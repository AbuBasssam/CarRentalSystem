using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

public record ResendResetCodeCommand(ResendCodeDTO dto) : IRequest<Response<bool>>;
