using System.ComponentModel.DataAnnotations;

namespace ProjectTracker.Api.Dtos;

public sealed class CreateProjectRequest
{
    public CreateProjectRequest()
    {
    }

    public CreateProjectRequest(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }
}

public sealed class UpdateProjectRequest
{
    public UpdateProjectRequest()
    {
    }

    public UpdateProjectRequest(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }
}

public record ProjectResponse(Guid Id, string Name, string Description);
