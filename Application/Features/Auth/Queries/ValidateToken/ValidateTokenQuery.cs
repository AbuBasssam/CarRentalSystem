using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

/// <summary>
/// Query to validate current authentication token and retrieve user data
/// This is executed on app initial load to check if user has valid session
/// </summary>
public class ValidateTokenQuery : IRequest<Response<TokenValidationResponseDTO>>
{
    public ValidateTokenQuery()
    {
    }
}
