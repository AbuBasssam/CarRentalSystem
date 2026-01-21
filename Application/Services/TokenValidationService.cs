using Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;


namespace Application.Services;

/// <summary>
/// Implementation of token validation service
/// </summary>
/// <remarks>
/// This service is invoked during the OnTokenValidated event in JWT authentication
/// to ensure tokens that are syntactically valid haven't been revoked or used.
/// Performance considerations:
/// - Uses AsNoTracking for read-only operations
/// - Each authenticated request triggers this validation
/// </remarks>
public class TokenValidationService : ITokenValidationService
{
    #region Fields

    private readonly IUserTokenRepository _refreshTokenRepo;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of TokenValidationService
    /// </summary>
    /// <param name="refreshTokenRepo">Repository for accessing token data</param>
    public TokenValidationService(IUserTokenRepository refreshTokenRepo)
    {
        _refreshTokenRepo = refreshTokenRepo;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Validates a token by checking its database state
    /// </summary>
    /// <param name="jti">JWT ID from the token's JTI claim</param>
    /// <returns>
    /// False if token is revoked, used, expired, not found, or validation fails otherwise true
    /// </returns>
    /// <exception cref="Exception">
    /// Exceptions are caught and logged, method returns false on error
    /// </exception>
    public async Task<bool> ValidateTokenAsync(string jti)
    {
        try
        {
            var tokenEntity = await _refreshTokenRepo
                .GetTableNoTracking()
                .Where(x => x.JwtId == jti)
                .FirstOrDefaultAsync();

            return !(tokenEntity == null || !tokenEntity.IsValid());

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating token {Jti}", jti);
            return false;
        }
    }
    #endregion

}