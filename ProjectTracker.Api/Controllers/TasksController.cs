using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectTracker.Api.Dtos;
using ProjectTracker.Api.Models;
using ProjectTracker.Api.Services;

namespace ProjectTracker.Api.Controllers;

[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly TaskService _tasks;

    public TasksController(TaskService tasks) => _tasks = tasks;

    [HttpPost("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<TaskResponse>> Add(
        Guid projectId,
        AddTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var task = await _tasks.AddTaskAsync(
            userId,
            projectId,
            request.Title,
            request.Description,
            request.DueDate,
            cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Project not found." });
        }

        return Ok(ToResponse(task));
    }

    [HttpGet("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<List<TaskResponse>>> ListByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var tasks = await _tasks.ListOwnedProjectTasksAsync(userId, projectId, cancellationToken);
        if (tasks is null)
        {
            return NotFound(new { message = "Project not found." });
        }

        return Ok(tasks.Select(ToResponse).ToList());
    }

    [HttpPut("tasks/{taskId:guid}")]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var task = await _tasks.UpdateOwnedTaskAsync(
            userId,
            taskId,
            request.Title,
            request.Description,
            request.DueDate,
            request.IsCompleted,
            cancellationToken);

        if (task is null)
        {
            return NotFound(new { message = "Task not found." });
        }

        return Ok(ToResponse(task));
    }

    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId, CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var deleted = await _tasks.DeleteOwnedTaskAsync(userId, taskId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Task not found." });
        }

        return NoContent();
    }

    private static TaskResponse ToResponse(ProjectTask task) =>
        new(task.Id, task.ProjectId, task.Title, task.Description, task.DueDate, task.IsCompleted);
}
