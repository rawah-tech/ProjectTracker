namespace ProjectTracker.Api.Dtos;

public record AddTaskRequest(string Title, string Description, DateTime DueDate);
public record UpdateTaskRequest(string Title, string Description, DateTime DueDate, bool IsCompleted);

public record TaskResponse(Guid Id, Guid ProjectId, string Title, string Description, DateTime DueDate, bool IsCompleted);
