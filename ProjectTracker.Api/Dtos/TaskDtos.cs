using System.ComponentModel.DataAnnotations;

namespace ProjectTracker.Api.Dtos;

public sealed class AddTaskRequest
{
    public AddTaskRequest()
    {
    }

    public AddTaskRequest(string title, string? description, DateTime dueDate)
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
    }

    [Required, StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    public DateTime DueDate { get; init; }
}

public sealed class UpdateTaskRequest
{
    public UpdateTaskRequest()
    {
    }

    public UpdateTaskRequest(
        string title,
        string? description,
        DateTime dueDate,
        bool isCompleted)
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
        IsCompleted = isCompleted;
    }

    [Required, StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    public DateTime DueDate { get; init; }
    public bool IsCompleted { get; init; }
}

public record TaskResponse(Guid Id, Guid ProjectId, string Title, string Description, DateTime DueDate, bool IsCompleted);
