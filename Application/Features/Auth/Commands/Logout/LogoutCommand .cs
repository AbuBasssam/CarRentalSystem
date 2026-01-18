using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

/// <summary>
/// Command for user logout
/// </summary>
public record LogoutCommand : IRequest<Response<bool>>;
