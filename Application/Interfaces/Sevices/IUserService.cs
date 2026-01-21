using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Interfaces;

/// <summary>
/// Service interface for managing user-related business logic and Identity operations.
/// </summary>
public interface IUserService : IScopedService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    IQueryable<User> GetUserByIdAsync(int Id);

    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    IQueryable<User> GetUserByUsernameAsync(string username);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    IQueryable<User> GetUserByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by their phone number.
    /// </summary>
    IQueryable<User> GetUserByPhoneNumberAsync(string phoneNumber);

    /// <summary>
    /// Facilitates paginated retrieval of users.
    /// </summary>
    IQueryable<User> GetUsersPage(int pageNumber);

    /// <summary>
    /// Creates a new user with the specified password.
    /// </summary>
    Task<IdentityResult> CreateUserAsync(User user, string password);

    /// <summary>
    /// Creates a new user with hased password inside user parameter.
    /// </summary>
    Task<IdentityResult> CreateUserAsync(User user);

    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    Task<IdentityResult> UpdateUserAsync(User user);

    /// <summary>
    /// Checks if a user exists based on Id.
    /// </summary>
    Task<bool> IsUserExistsByIdAsync(int Id);

    /// <summary>
    /// Checks if a user exists based on email.
    /// </summary>
    Task<bool> IsUserExistsByEmailAsync(string Email);

    /// <summary>
    /// Checks if a user exists based on username.
    /// </summary>
    Task<bool> IsUserExistsByUserNameAsync(string UserName);

    /// <summary>
    /// Checks if a user exists based on Id, username and email.
    /// </summary>
    Task<bool> IsUserExistsAsync(int Id, string UserName, string Email);

    /// <summary>
    /// Checks if a user exists based on username and email.
    /// </summary>
    Task<bool> IsUserExistsAsync(string UserName, string Email);

    /// <summary>
    /// Retrieves a list of roles assigned to a specific user.
    /// </summary>
    Task<List<string>> GetUserRolesAsync(User User);

    /// <summary>
    /// Changes a user's password after validating the current password.
    /// </summary>
    /// <param name="User">The user entity whose password will be changed.</param>
    /// <param name="CurrentPassword">The user's existing password for verification.</param>
    /// <param name="NewPassword">The new password to be set for the user.</param>
    /// <returns>An IdentityResult indicating whether the password change was successful.</returns>
    Task<IdentityResult> ChangeUserPasswordAsync(User User, string CurrentPassword, string NewPassword);

    /// <summary>
    /// Authenticates a user's password.
    /// </summary>
    Task<SignInResult> CheckPasswordAsync(User User, string Password);

    /// <summary>
    /// Removes a user from multiple assigned roles simultaneously.
    /// </summary>
    /// <param name="User">The user entity to remove roles from.</param>
    /// <param name="Roles">A collection of role names to be removed.</param>
    /// <returns>An IdentityResult indicating whether the operation succeeded.</returns>
    Task<IdentityResult> RemoveFromRolesAsync(User User, IEnumerable<string> Roles);

    /// <summary>
    /// Assigns a specific role to a user.
    /// </summary>
    /// <param name="User">The user entity to receive the new role.</param>
    /// <param name="Role">The name of the role to assign.</param>
    /// <returns>An IdentityResult indicating whether the operation succeeded.</returns>
    Task<IdentityResult> AddToRoleAsync(User User, string Role);

    /// <summary>
    /// Updates and synchronizes the user's roles by replacing current roles with a new set.
    /// Usually involves removing old roles and adding the specified new ones.
    /// </summary>
    /// <param name="User">The user entity whose roles need updating.</param>
    /// <param name="NewRoles">The new list of Role entities to be assigned to the user.</param>
    /// <returns>An IdentityResult indicating whether the roles were successfully updated.</returns>
    Task<IdentityResult> UpdateUserRolesAsync(User User, List<Role> NewRoles);
}