using Domain.AppMetaData;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Seeder;
public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<Role> _roleManager)
    {
        var rolesCount = await _roleManager.Roles.CountAsync();
        if (rolesCount <= 0)
        {

            await _roleManager.CreateAsync(new Role() { Name = Roles.Admin });
            await _roleManager.CreateAsync(new Role() { Name = Roles.Employee });
            await _roleManager.CreateAsync(new Role() { Name = Roles.Customer });

            // seed permissions as claims for each role
            await SeedRolePermissions(_roleManager);
        }
    }
    private static async Task SeedRolePermissions(RoleManager<Role> _roleManager)
    {
        // Admin
        var adminRole = await _roleManager.FindByNameAsync(Roles.Admin);
        if (adminRole != null)
        {
            await AddMissingClaims(_roleManager, adminRole, Permissions.Admin.All);
        }

        // Employee
        var employeeRole = await _roleManager.FindByNameAsync(Roles.Employee);
        if (employeeRole != null)
        {
            await AddMissingClaims(_roleManager, employeeRole, Permissions.Employee.All);
        }

        // Customer
        var customerRole = await _roleManager.FindByNameAsync(Roles.Customer);
        if (customerRole != null)
        {
            await AddMissingClaims(_roleManager, customerRole, Permissions.Customer.All);
        }
    }
    private static async Task AddMissingClaims(RoleManager<Role> roleManager, Role role, IEnumerable<string> permissions)
    {


        var existingClaims = await roleManager.GetClaimsAsync(role);

        var existingValues = new HashSet<string>(
            existingClaims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
        );

        var newPermissions = permissions.Where(p => !existingValues.Contains(p)).ToList();

        if (!newPermissions.Any()) return;

        foreach (var perm in newPermissions)
        {
            await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, perm));
        }
    }

}
