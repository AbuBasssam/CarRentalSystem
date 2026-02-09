
using Application.Models;
using Domain.HelperClasses;
using MediatR;

namespace Application.Features.AuthFeature;

/// <summary>
/// Command to refresh authentication tokens using a valid refresh token
/// </summary>
public class RefreshTokenCommand : IRequest<Response<JwtAuthResult>>
{
    /// <summary>
    /// User's email address for additional verification
    /// Must match the email associated with the refresh token
    /// </summary>
    public string Email { get; set; } = null!;

}
