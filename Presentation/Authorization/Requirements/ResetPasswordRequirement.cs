using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization.Requirements;

/// <summary>
/// Authorization requirement for password reset flow
/// Validates stage and token validity
/// </summary>
public class ResetPasswordRequirement : IAuthorizationRequirement
{
    public enResetPasswordStage RequiredStage { get; }
    public ResetPasswordRequirement(enResetPasswordStage stage) => RequiredStage = stage;
}
