using System.Security.Cryptography;
using System.Text;

namespace Application;
internal static class Helpers
{
    // ============================================================
    // SHA-256 
    // ============================================================
    public static string HashString(string input)
    {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = sha.ComputeHash(bytes);

        // Convert bytes : hex string for readable display
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));

        return sb.ToString();

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
