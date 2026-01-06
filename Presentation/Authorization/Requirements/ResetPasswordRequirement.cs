using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Presentation.Authorization.Requirements;
public class ResetPasswordRequirement : IAuthorizationRequirement
{
    public enResetPasswordStage RequiredStage { get; }
    public ResetPasswordRequirement(enResetPasswordStage stage) => RequiredStage = stage;
}
