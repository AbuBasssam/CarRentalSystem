using Microsoft.AspNetCore.Authorization;

namespace Domain.Security;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public  PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}