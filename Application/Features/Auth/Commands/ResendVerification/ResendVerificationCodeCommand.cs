using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;
public record ResendVerificationCodeCommand(ResendCodeDTO DTO) : IRequest<Response<VerificationFlowResponse>>;
