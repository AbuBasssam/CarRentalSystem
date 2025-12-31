using Domain.AppMetaData;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class UserSeeder
{
    public static async Task SeedAsync(UserManager<User> _userManager)
    {
        var usersCount = await _userManager.Users.CountAsync();
        if (usersCount <= 0)
        {
            await _CreateAdmins(_userManager);
            await _CreateEmployee(_userManager);
            await _CreateCustomer(_userManager);


        }

    }

    private static async Task _CreateAdmins(UserManager<User> _userManager)
    {
        var defaultuser = new User()
        {
            UserName = "AbuBassam",
            FirstName = "Abdul Rahman",
            LastName = "Hajjar",
            Email = "bassan258@gmail.com",
            EmailConfirmed = true,

        };
        var result = await _userManager.CreateAsync(defaultuser, "Test123456.");

        await _userManager.AddToRoleAsync(defaultuser, Roles.Admin);
        await _userManager.AddToRoleAsync(defaultuser, Roles.Employee);
    }
    private static async Task _CreateEmployee(UserManager<User> _userManager)
    {
        var email = "employee@carrental.com";
        if (await Exists(_userManager, email)) return;

        var employee = new User()
        {
            UserName = email,
            FirstName = "Default",
            LastName = "Employee",
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(employee, "Test123456.");
        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(employee, Roles.Employee);
        }
    }

    private static async Task _CreateCustomer(UserManager<User> _userManager)
    {
        var email = "customer@carrental.com";
        if (await Exists(_userManager, email)) return;

        var customer = new User()
        {
            UserName = email,
            FirstName = "Default",
            LastName = "Customer",
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(customer, "Test123456.");
        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(customer, Roles.Customer);
        }
    }

    // Generic existence helper used by both employee/customer creation flows.
    private static async Task<bool> Exists(UserManager<User> _userManager, string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

}