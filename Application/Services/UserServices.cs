using Domain.Entities;
using Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class UserService : IUserService
{
    #region Fields
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IGenericRepository<User, int> _userRepository;
    private readonly IUserTokenRepository _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    #endregion

    public UserService(IGenericRepository<User, int> userRepository, IUnitOfWork unitOfWork, UserManager<User> userManager, SignInManager<User> signInManager, IUserTokenRepository refreshTokenRepo)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _signInManager = signInManager;
        _refreshTokenRepo = refreshTokenRepo;
    }

    public IQueryable<User> GetUserByUsernameAsync(string username)
    {
        return _userManager.Users.Where(x => x.UserName!.ToLower().Equals(username.ToLower()));
    }

    public IQueryable<User> GetUserByEmailAsync(string email)
    {
        return _userManager.Users.Where(x => x.Email!.ToLower().Equals(email.ToLower()));
    }

    IQueryable<User> IUserService.GetUserByPhoneNumberAsync(string phoneNumber)
    {
        return _userManager.Users.Where(x => x.PhoneNumber!.Equals(phoneNumber));
    }

    public async Task<IdentityResult> CreateUserAsync(User user, string password)
    {
        return await _userManager.CreateAsync(user, password);

    }

    public async Task<IdentityResult> CreateUserAsync(User user)
    {
        return await _userManager.CreateAsync(user);

    }

    public async Task<IdentityResult> UpdateUserAsync(User user)
    {
        return await _userManager.UpdateAsync(user);


    }

    public IQueryable<User> GetUserByIdAsync(int Id)
    {
        return _userManager.Users.Where(x => x.Id == Id);
    }

    public IQueryable<User> GetUsersPage(int pageNumber)
    {
        return _userRepository.GetPage(pageNumber);
    }

    public async Task<bool> IsUserExistsByIdAsync(int Id)
    {
        return await _userManager.Users.AnyAsync(x => x.Id == Id);
    }

    public async Task<bool> IsUserExistsByEmailAsync(string Email)
    {
        return await _userManager.Users.AnyAsync(x => x.Email == Email);
    }

    public async Task<bool> IsUserExistsByUserNameAsync(string UserName)
    {
        return await _userManager.Users.AnyAsync(x => x.UserName == UserName);
    }

    public async Task<bool> IsUserExistsAsync(int Id, string UserName, string Email)
    {
        return await _userManager.Users.AnyAsync(x => x.UserName == UserName);
    }

    public async Task<bool> IsUserExistsAsync(string UserName, string Email)
    {
        return await _userManager.Users.AnyAsync(x => x.Email == Email || x.UserName == UserName);
    }

    public async Task<IdentityResult> ChangeUserPasswordAsync(User User, string CrurentPassword, string NewPassword)
    {
        return await _userManager.ChangePasswordAsync(User, CrurentPassword, NewPassword);

    }

    public async Task<SignInResult> CheckPasswordAsync(User User, string Password)
    {
        return await _signInManager.CheckPasswordSignInAsync(User, Password, false);
    }


    //Role Manger part
    public async Task<List<string>> GetUserRolesAsync(User User)
    {
        return (await _userManager.GetRolesAsync(User)).ToList();
    }

    public async Task<IdentityResult> RemoveFromRolesAsync(User User, IEnumerable<string> Roles)
    {
        return await _userManager.RemoveFromRolesAsync(User, Roles);
    }

    public async Task<IdentityResult> AddToRoleAsync(User User, string Role)
    {
        return await _userManager.AddToRoleAsync(User, Role);

    }

    public async Task<IdentityResult> UpdateUserRolesAsync(User User, List<Role> NewRoles)
    {
        var result = new IdentityResult();

        using (var transaction = await _unitOfWork.BeginTransactionAsync())
        {
            try
            {
                User.UserRoles!.Clear();
                foreach (Role role in NewRoles)
                    User.UserRoles.Add(new UserRole { Role = role });

                result = await UpdateUserAsync(User);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
            }
        }

        return result;
    }


}
