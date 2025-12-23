using System.Security.Cryptography;
using System.Text;

namespace Backend.Api.Security;

public static class PasswordHasher
{
    private const string Salt = "Helpdesk_Salt_2025";

    public static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + Salt);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public static bool Verify(string password, string storedHash)
        => Hash(password) == storedHash;
}
