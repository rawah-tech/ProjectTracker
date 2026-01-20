namespace ProjectTracker.Api.Models;

public class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Link task to a specific project
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
}
