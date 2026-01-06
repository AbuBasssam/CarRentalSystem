using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;
public record ResendVerificationCodeCommand() : IRequest<Response<string>>;
