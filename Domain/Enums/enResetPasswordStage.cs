namespace Domain.Enums;

/// <summary>
/// Defines the stages of password reset flow
/// </summary>
public enum enResetPasswordStage
{
    /// <summary>
    /// Stage 1: Code sent, waiting for verification
    /// </summary>
    AwaitingVerification=1,

    /// <summary>
    /// Stage 2: Code verified, can now reset password
    /// </summary>
    Verified,

    /// <summary>
    /// Stage 3: Password successfully reset (final stage)
    /// </summary>
    Completed
}
