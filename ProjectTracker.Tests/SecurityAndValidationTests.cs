using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectTracker.Api.Dtos;
using ProjectTracker.Api.Models;
using ProjectTracker.Api.Options;
using ProjectTracker.Api.Services;

namespace ProjectTracker.Tests;

public class SecurityAndValidationTests
{
    private static readonly Guid UserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTime DueDate = new(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private static readonly JwtOptions TokenOptions = new()
    {
        Key = "test-only-jwt-signing-key-with-at-least-32-bytes",
        Issuer = "ProjectTracker.Tests",
        Audience = "ProjectTracker.Tests.Client",
        ExpirationMinutes = 45
    };

    private static IReadOnlyList<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateToken_ContainsUserIdentifierAndEmailClaims()
    {
        var service = new JwtService(Options.Create(TokenOptions));
        var user = new User
        {
            Id = UserId,
            Email = "jwt-user@example.invalid",
            PasswordHash = "not-used-by-this-test"
        };

        var encodedToken = service.CreateToken(user);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = true };
        var principal = handler.ValidateToken(
            encodedToken,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = TokenOptions.Issuer,
                ValidAudience = TokenOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TokenOptions.Key))
            },
            out _);

        Assert.Equal(UserId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void CreateToken_UsesConfiguredIssuerAudienceAndExpiration()
    {
        var service = new JwtService(Options.Create(TokenOptions));
        var user = new User
        {
            Id = UserId,
            Email = "jwt-user@example.invalid",
            PasswordHash = "not-used-by-this-test"
        };
        var earliestExpiration = DateTime.UtcNow
            .AddMinutes(TokenOptions.ExpirationMinutes)
            .AddSeconds(-1);

        var encodedToken = service.CreateToken(user);

        var latestExpiration = DateTime.UtcNow
            .AddMinutes(TokenOptions.ExpirationMinutes)
            .AddSeconds(1);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encodedToken);
        Assert.Equal(TokenOptions.Issuer, token.Issuer);
        Assert.Equal(TokenOptions.Audience, Assert.Single(token.Audiences));
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.InRange(token.ValidTo, earliestExpiration, latestExpiration);
    }

    [Fact]
    public void TryGetUserId_WithValidClaim_ReturnsUserId()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())],
                authenticationType: "Test"));

        var result = UserContext.TryGetUserId(principal, out var userId);

        Assert.True(result);
        Assert.Equal(UserId, userId);
    }

    [Fact]
    public void TryGetUserId_WithMissingClaim_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test"));

        var result = UserContext.TryGetUserId(principal, out var userId);

        Assert.False(result);
        Assert.Equal(Guid.Empty, userId);
    }

    [Fact]
    public void TryGetUserId_WithMalformedClaim_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
                authenticationType: "Test"));

        var result = UserContext.TryGetUserId(principal, out var userId);

        Assert.False(result);
        Assert.Equal(Guid.Empty, userId);
    }

    [Fact]
    public void PasswordService_WithMaximumLengthPassword_UsesAllCharacters()
    {
        var service = new PasswordService();
        var password = new string('p', 127) + "A";
        var passwordDifferingAfterBcryptsLegacyLimit = new string('p', 127) + "B";

        var hash = service.HashPassword(password);

        Assert.StartsWith("{bcrypt-sha384}", hash, StringComparison.Ordinal);
        Assert.True(service.VerifyPassword(password, hash));
        Assert.False(service.VerifyPassword(passwordDifferingAfterBcryptsLegacyLimit, hash));
    }

    [Fact]
    public void PasswordService_WithLegacyBcryptHash_RemainsCompatible()
    {
        const string password = "Legacy-test-password-42!";
        var legacyHash = BCrypt.Net.BCrypt.HashPassword(password);
        var service = new PasswordService();

        Assert.True(service.VerifyPassword(password, legacyHash));
        Assert.False(service.VerifyPassword("wrong-password", legacyHash));
    }

    [Fact]
    public void RegisterRequest_WithInvalidEmailAndShortPassword_IsInvalid()
    {
        var results = Validate(new RegisterRequest("not-an-email", "short"));

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(RegisterRequest.Email)));
        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void CreateProjectRequest_WithWhitespaceName_IsInvalid()
    {
        var results = Validate(new CreateProjectRequest("   ", "Description"));

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(CreateProjectRequest.Name)));
    }

    [Fact]
    public void CreateProjectRequest_WithNameOverMaximumLength_IsInvalid()
    {
        var results = Validate(new CreateProjectRequest(new string('p', 201), null));

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(CreateProjectRequest.Name)));
    }

    [Fact]
    public void AddTaskRequest_WithWhitespaceTitle_IsInvalid()
    {
        var results = Validate(new AddTaskRequest("   ", "Description", DueDate));

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(AddTaskRequest.Title)));
    }

    [Fact]
    public void UpdateTaskRequest_WithDescriptionOverMaximumLength_IsInvalid()
    {
        var results = Validate(
            new UpdateTaskRequest("Task", new string('d', 2001), DueDate, false));

        Assert.Contains(
            results,
            result => result.MemberNames.Contains(nameof(UpdateTaskRequest.Description)));
    }

    [Fact]
    public void RequestModels_WithNullOptionalDescriptions_AreValid()
    {
        var projectResults = Validate(new CreateProjectRequest("Project", null));
        var taskResults = Validate(new AddTaskRequest("Task", null, DueDate));

        Assert.Empty(projectResults);
        Assert.Empty(taskResults);
    }
}
