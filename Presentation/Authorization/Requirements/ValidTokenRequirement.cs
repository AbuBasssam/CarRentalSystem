using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization.Requirements;

/// <summary>
/// Generic requirement for operations requiring valid authentication token
/// </summary>
public class ValidTokenRequirement : IAuthorizationRequirement
{
}