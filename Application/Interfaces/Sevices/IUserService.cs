using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Interfaces;

public interface IUserService : IScopedService
{
    IQueryable<User> GetUserByIdAsync(int Id);
    IQueryable<User> GetUserByUsernameAsync(string username);
    IQueryable<User> GetUserByEmailAsync(string email);
    IQueryable<User> GetUserByPhoneNumberAsync(string phoneNumber);

    IQueryable<User> GetUsersPage(int pageNumber);

    Task<IdentityResult> CreateUserAsync(User user, string password);
    Task<IdentityResult> CreateUserAsync(User user);
    Task<IdentityResult> UpdateUserAsync(User user);

    //Task<bool> DeleteUserAsync(int Id);

    Task<bool> IsUserExistsByIdAsync(int Id);
    Task<bool> IsUserExistsByEmailAsync(string Email);
    Task<bool> IsUserExistsByUserNameAsync(string UserName);
    Task<bool> IsUserExistsAsync(int Id, string UserName, string Email);
    Task<bool> IsUserExistsAsync(string UserName, string Email);
    Task<List<string>> GetUserRolesAsync(User User);
    Task<IdentityResult> ChangeUserPasswordAsync(User User, string CrurentPassword, string NewPassword);
    Task<SignInResult> CheckPasswordAsync(User User, string Password);
    Task<IdentityResult> RemoveFromRolesAsync(User User, IEnumerable<string> Roles);
    Task<IdentityResult> AddToRoleAsync(User User, string Role);
    Task<IdentityResult> UpdateUserRolesAsync(User User, List<Role> NewRoles);
}
