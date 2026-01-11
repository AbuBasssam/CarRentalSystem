namespace Domain.AppMetaData;
public static class Router
{
    private const string _root = "api";
    private const string _version = "v1";

    // api/v1
    private const string _rule = _root + "/" + _version;

    // sub routes
    // api/v1/Student/{Id}
    private const string _ById = "/{Id}";

    // api/v1/<Controller>/query? key=value & key=value
    private const string _Query = "/query";

    #region Authentication Routes
    public class AuthenticationRouter
    {
        public const string BASE = _rule + "/authentication";

        public const string SignIn = BASE + "/signin";
        public const string SignUp = BASE + "/signup";


        // Email verification endpoints

        public const string EmailVerification = BASE + "/email-verification";

        public const string EmailConfirmation = BASE + "/email-confirmation";

        public const string ResendVerification = BASE + "re-verification";



        // Password reset endpoints

        public const string PasswordReset = BASE + "/password-reset";

        public const string PasswordResetVerification = BASE + "/password-reset-verification";

        public const string Password = BASE + "/password";

        public const string ResendPasswordReset = BASE + "/password-reset/resend";

        // Token management endpoints
        public const string Token = BASE + "/token";

        public const string TokenValidation = BASE + "/token/validation";


        public const string Logout = BASE + "/logout";

    }

    #endregion

}
