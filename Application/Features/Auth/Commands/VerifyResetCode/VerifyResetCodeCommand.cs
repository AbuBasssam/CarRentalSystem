using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

public record VerifyResetCodeCommand(VerifyResetCodeDTO DTO) : IRequest<Response<VerificationFlowResponse>>;
