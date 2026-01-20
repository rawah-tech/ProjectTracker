namespace ProjectTracker.Api.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // This links the project to the owner user
    public Guid UserId { get; set; }
    public User? User { get; set; }
}
