using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

public record VerifyResetCodeCommand(VerificationDTO DTO) : IRequest<Response<VerificationFlowResponse>>;
