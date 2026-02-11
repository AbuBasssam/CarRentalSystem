namespace Domain.Enums;

/// <summary>
/// Authentication Error Codes
/// Used to communicate specific authentication failures to Frontend
/// 
/// IMPORTANT: Frontend uses these codes to determine if token refresh should be attempted
/// - isRecoverable = false → Don't attempt refresh (logout immediately)
/// - isRecoverable = true → Attempt refresh (might succeed)
/// </summary>
public enum enErrorCode
{
    /// <summary>
    /// No authentication token provided
    /// isRecoverable = false
    /// Scenario: New user, never logged in
    /// Frontend Action: Show login page, don't attempt refresh
    /// </summary>
    MissingToken,

    /// <summary>
    /// Token is malformed or corrupted
    /// isRecoverable = false
    /// Scenario: Corrupted cookie, tampered token
    /// Frontend Action: Logout, don't attempt refresh
    /// </summary>
    InvalidToken,

    /// <summary>
    /// Access token has expired but refresh might work
    /// isRecoverable = true
    /// Scenario: User was idle, access token expired naturally
    /// Frontend Action: Attempt token refresh
    /// </summary>
    TokenExpired,

    /// <summary>
    /// Session has fully expired (refresh token also expired)
    /// isRecoverable = false
    /// Scenario: User hasn't used app for extended period
    /// Frontend Action: Logout, show "session expired" message
    /// </summary>
    SessionExpired,

    /// <summary>
    /// User lacks required permissions
    /// isRecoverable = false
    /// Scenario: Trying to access admin endpoint without admin role
    /// Frontend Action: Show "access denied" message
    /// </summary>
    AccessDenied
}
