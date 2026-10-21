namespace ProjectTracker.Api.Services;

public sealed class PasswordService
{
    private const string EnhancedHashPrefix = "{bcrypt-sha384}";

    public string HashPassword(string password) =>
        EnhancedHashPrefix + BCrypt.Net.BCrypt.EnhancedHashPassword(password);

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (passwordHash.StartsWith(EnhancedHashPrefix, StringComparison.Ordinal))
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(
                password,
                passwordHash[EnhancedHashPrefix.Length..]);
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
