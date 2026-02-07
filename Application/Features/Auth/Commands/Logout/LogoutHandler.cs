using Application.Models;
using ApplicationLayer.Resources;
using Interfaces;
using MediatR;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class LogoutHandler : IRequestHandler<LogoutCommand, Response<bool>>
{
    #region Fields
    private readonly IAuthService _authService;


    private readonly IRequestContext _requestContext;
    private readonly IUnitOfWork _unitOfWork;

    private readonly IStringLocalizer<SharedResources> _localizer;

    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor
    public LogoutHandler(
        IAuthService authService,
        ITokenValidationService tokenValidation,
        IRequestContext requestContext,
        IUnitOfWork unitOfWork,
        IStringLocalizer<SharedResources> localizer,
        ResponseHandler responseHandler)
    {
        _authService = authService;

        _requestContext = requestContext;

        _unitOfWork = unitOfWork;

        _localizer = localizer;

        _responseHandler = responseHandler;
    }
    #endregion

    #region Handler
    public async Task<Response<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var contextResult = _ExtractLogoutContext();

        if (!contextResult.IsSuccess)
        {
            return _responseHandler.Unauthorized<bool>();
        }

        var context = contextResult.Data;

        // Step 2: Perform logout through AuthService
        var logoutResult = await _authService.Logout(context.UserId, context.JwtId);

        if (!logoutResult.IsSuccess)
        {
            return _responseHandler.BadRequest<bool>(
                string.Join('\n', logoutResult.Errors)
            );
        }

        // Step 3: Log successful logout activity
        _LogLogoutActivity(context);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _responseHandler.Success(true);
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts logout context from the current request
    /// Should only be called after authorization succeeds
    /// </summary>
    public Result<LogoutContext> _ExtractLogoutContext()
    {
        var userId = _requestContext.UserId!;
        var jwtId = _requestContext.TokenJti!;
        var ipAddress = _requestContext.ClientIP;
        var userAgent = _requestContext.UserAgent;



        // Create logout context
        var logoutContext = new LogoutContext
        {
            UserId = userId.Value,
            JwtId = jwtId,
            IpAddress = ipAddress ?? "Unknown",
            UserAgent = userAgent ?? "Unknown"
            ,
            LogoutTimestamp = DateTime.UtcNow
        };

        return Result<LogoutContext>.Success(logoutContext);
    }

    /// <summary>
    /// Logs logout activity with context information
    /// </summary>
    private void _LogLogoutActivity(LogoutContext context)
    {
        Log.Information(
            "User {UserId} logged out successfully at {Timestamp} from IP {IpAddress} using {UserAgent}",
            context.UserId,
            context.LogoutTimestamp,
            context.IpAddress,
            context.UserAgent
        );
    }

    #endregion

}
/// <summary>
/// Contains validated logout context information
/// </summary>
public class LogoutContext
{

    public int UserId { get; set; }
    public string JwtId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime LogoutTimestamp { get; set; }
}