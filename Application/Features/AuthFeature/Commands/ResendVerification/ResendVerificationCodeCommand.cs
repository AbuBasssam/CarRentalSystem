using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;
public record ResendVerificationCodeCommand(string VerificationToken) : IRequest<Response<string>>;
