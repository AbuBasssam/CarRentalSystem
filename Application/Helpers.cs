using System.Security.Cryptography;
using System.Text;

namespace Application;
internal static class Helpers
{
    public static string HashString(string Value)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(Value)).ToArray();
            return Convert.ToBase64String(hashedBytes);
        }
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
        Random generator = new Random();
        return generator.Next(100000, 1000000).ToString("D6");
    }




}
