using System.ComponentModel.DataAnnotations;

namespace ProjectTracker.Api.Dtos;

public sealed class RegisterRequest
{
    public RegisterRequest()
    {
    }

    public RegisterRequest(string email, string password)
    {
        Email = email;
        Password = password;
    }

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    public LoginRequest()
    {
    }

    public LoginRequest(string email, string password)
    {
        Email = email;
        Password = password;
    }

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public record AuthResponse(string Token);
