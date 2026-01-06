using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

public record ResetPasswordCommand(string NewPassword, string ConfirmPassword) : IRequest<Response<bool>>;