using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Dtos;

namespace ProjectTracker.Tests;

public sealed class ApiValidationTests : IClassFixture<ProjectTrackerApplicationFactory>
{
    private readonly ProjectTrackerApplicationFactory _factory;

    public ApiValidationTests(ProjectTrackerApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_WithInvalidRequest_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest("not-an-email", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(RegisterRequest.Email), problem.Errors.Keys);
        Assert.Contains(nameof(RegisterRequest.Password), problem.Errors.Keys);
    }
}

public sealed class ProjectTrackerApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=(local);Database=ProjectTrackerTests;Integrated Security=True;TrustServerCertificate=True;");
        builder.UseSetting(
            "Jwt:Key",
            "test-only-api-signing-key-with-at-least-32-bytes");
        builder.UseSetting("Jwt:Issuer", "ProjectTracker.Tests");
        builder.UseSetting("Jwt:Audience", "ProjectTracker.Tests.Client");
        builder.UseSetting("Jwt:ExpirationMinutes", "30");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("ProjectTrackerApiValidationTests"));
        });
    }
}
