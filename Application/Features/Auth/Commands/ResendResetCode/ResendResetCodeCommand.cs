using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

public record ResendResetCodeCommand() : IRequest<Response<VerificationFlowResponse>>;
