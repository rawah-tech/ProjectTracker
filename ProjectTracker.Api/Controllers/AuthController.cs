using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Dtos;
using ProjectTracker.Api.Models;
using ProjectTracker.Api.Services;

namespace ProjectTracker.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly PasswordService _passwords;

    public AuthController(AppDbContext db, JwtService jwt, PasswordService passwords)
    {
        _db = db;
        _jwt = jwt;
        _passwords = passwords;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (exists)
        {
            return BadRequest(new { message = "Email already registered." });
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwords.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwt.CreateToken(user);
        return Ok(new AuthResponse(token));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var passwordIsValid = _passwords.VerifyPassword(request.Password, user.PasswordHash);
        if (!passwordIsValid)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = _jwt.CreateToken(user);
        return Ok(new AuthResponse(token));
    }
}
