using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization.Requirements;

/// <summary>
/// Authorization requirement for password reset flow
/// Validates stage and token validity
/// </summary>
public class ResetPasswordRequirement : IAuthorizationRequirement
{

    public ResetPasswordRequirement() { }
}
