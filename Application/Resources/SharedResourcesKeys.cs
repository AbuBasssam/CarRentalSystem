namespace ApplicationLayer.Resources
{
    public class SharedResourcesKeys
    {
        #region General Response Status Keys
        public const string Success = "Success";
        public const string Created = "Created";
        public const string Deleted = "Deleted";
        public const string Failed = "Failed";
        public const string Forbidden = "Forbidden";

        #endregion

        #region HTTP Status Keys
        public const string BadRequest = "BadRequest";
        public const string Unauthorized = "Unauthorized";
        public const string NotFound = "NotFound";
        public const string UnprocessableEntity = "UnprocessableEntity";
        public const string InternalServerError = "InternalServerError";
        #endregion

        #region General Validation Keys
        public const string Required = "Required";// Not used 
        public const string NotEmpty = "NotEmpty";
        public const string PropertyCannotBeNull = "PropertyCannotBeNull";
        public const string PropertyCannotBeEmpty = "PropertyCannotBeEmpty";
        public const string NameNoSpacesAllowed = "NameNoSpacesAllowed";// Not used 
        public const string NameInvalidCharacters = "NameInvalidCharacters";
        public const string MinLength = "MinLength";
        public const string MaxLength = "MaxLength";
        #endregion

        #region User Input Validation Keys
        public const string EmailRequired = "EmailRequired";
        public const string PasswordRequired = "PasswordRequired";

        public const string CodeRequired = "CodeRequired";
        public const string OtpCodeLength = "OtpCodeLength";

        public const string PhoneNumberRequired = "PhoneNumberRequired";

        public const string RequestPayloadRequired = "RequestPayloadRequired";
        #endregion

        #region Format Validation Keys
        public const string InvalidEmail = "InvalidEmail";
        public const string InvalidCode = "InvalidCode";
        public const string InvalidPhoneNumber = "InvalidPhoneNumber";
        #endregion

        #region Authentication & Authorization Keys
        public const string InValidCredentials = "InValidCredentials";
        public const string TwoFactorRequired = "TwoFactorRequired"; // Not used 
        #endregion

        #region User Management Keys
        public const string EmailAlreadyExists = "EmailAlreadyExists";
        public const string UserCreationFailed = "UserCreationFailed";// Not used
        public const string AccountLockedOut = "AccountLockedOut";// Not used
        #endregion

        #region Token Validation Keys
        public const string ClaimsPrincipleIsNull = "ClaimsPrincipleIsNull";
        public const string InvalidUserId = "InvalidUserId";
        public const string InvalidEmailClaim = "InvalidEmailClaim";
        public const string InvalidPhoneNumberClaim = "InvalidPhoneNumberClaim";// Not used
        public const string NullRefreshToken = "NullRefreshToken";
        public const string RevokedRefreshToken = "RevokedRefreshToken";// may be replaced
        #endregion

        #region Token Extraction Keys
        public const string FailedExtractEmail = "FailedExtractEmail";
        public const string FailedExtractPhoneNumber = "FailedExtractPhoneNumber";// Not used
        #endregion

        #region Cooldown & Rate Limiting Keys
        public const string WaitCooldownPeriod = "WaitCooldownPeriod";// Not used
        public const string CooldownPeriodActive = "CooldownPeriodActive";// Not used
        public const string InvalidExpiredCode = "InvalidExpiredCode";
        public const string ResendCooldown = "ResendCooldown";// Not used
        #endregion


        #region Email Verification Keys
        // all Region not used 
        public const string EmailNotVerified = "EmailNotVerified";
        public const string EmailAlreadyVerified = "EmailAlreadyVerified";
        public const string VerificationCodeResent = "VerificationCodeResent";
        public const string PendingVerification = "PendingVerification";
        #endregion

        #region Token Management Keys
        public const string InvalidToken = "InvalidToken";
        public const string TokenNotFound = "TokenNotFound";
        public const string TokenExpired = "TokenExpired";
        public const string TokenRevoked = "TokenRevoked";
        #endregion

        #region Session Management Keys
        public const string LogoutSuccessful = "LogoutSuccessful";// need it later
        public const string LogoutFailed = "LogoutFailed";
        #endregion

        #region Security Keys
        // all Region not used 

        public const string SuspiciousActivity = "SuspiciousActivity";
        public const string AccountLocked = "AccountLocked";
        public const string InvalidRefreshToken = "InvalidRefreshToken";
        #endregion

        #region Miscellaneous Keys
        public const string Currency = "Currency";// Not used
        public const string UnexpectedError = "UnexpectedError";
        public const string ErrorOccurred = "ErrorOccurred";
        public const string ResetSessionExpired = "ResetSessionExpired";
        public const string AccessDenied = "AccessDenied";
        public const string MaxAttemptsExceeded = "MaxAttemptsExceeded";
        public const string MissingToken = "MissingToken";
        #endregion
    }
}