using System.Security.Cryptography;

namespace Application;
internal static class Helpers
{
    public static string HashString(string Value)
    {
        string hashedString = BCrypt.Net.BCrypt.HashPassword(Value, 10);

        return hashedString;
    }
    public static string GenerateRandomString64Length()
    {
        var randomNumber = new byte[32];
        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            randomNumberGenerator.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber);
    }
    public static string GenerateOtp()
    {
        // 1. توليد رمز عشوائي (6 أرقام)
        string code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        return code;
    }




}
