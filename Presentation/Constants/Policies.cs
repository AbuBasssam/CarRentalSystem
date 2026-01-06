namespace Presentation.Constants;
public static class Policies
{
    // المرحلة الأولى: توكن بانتظار التحقق من الكود
    public const string AwaitVerification = "AwaitVerification";

    // المرحلة الثانية: توكن تم التحقق منه وبانتظار تغيير الباسورد
    public const string ResetPasswordVerified = "ResetPasswordVerified";


    // سياسة توثيق الإيميل العادية
    public const string VerificationOnly = "VerificationOnly";
}
