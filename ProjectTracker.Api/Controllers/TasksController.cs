using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Dtos;
using ProjectTracker.Api.Models;
using ProjectTracker.Api.Services;

namespace ProjectTracker.Api.Controllers;

[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    // Add a task to a specific project (owned by the current user)
    [HttpPost("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<TaskResponse>> Add(Guid projectId, AddTaskRequest req)
    {
        var userId = UserContext.GetUserId(User);

        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

        if (project == null)
            return NotFound(new { message = "Project not found." });

        var task = new ProjectTask
        {
            ProjectId = projectId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            DueDate = req.DueDate,
            IsCompleted = false
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return Ok(new TaskResponse(task.Id, task.ProjectId, task.Title, task.Description, task.DueDate, task.IsCompleted));
    }

    // Retrieve tasks for a specific project (owned by the current user)
    [HttpGet("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<List<TaskResponse>>> ListByProject(Guid projectId)
    {
        var userId = UserContext.GetUserId(User);

        var projectExists = await _db.Projects
            .AnyAsync(p => p.Id == projectId && p.UserId == userId);

        if (!projectExists)
            return NotFound(new { message = "Project not found." });

        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.DueDate)
            .Select(t => new TaskResponse(t.Id, t.ProjectId, t.Title, t.Description, t.DueDate, t.IsCompleted))
            .ToListAsync();

        return Ok(tasks);
    }

    // Update a task (only if it belongs to a project owned by current user)
    [HttpPut("tasks/{taskId:guid}")]
    public async Task<ActionResult<TaskResponse>> Update(Guid taskId, UpdateTaskRequest req)
    {
        var userId = UserContext.GetUserId(User);

        var task = await _db.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound(new { message = "Task not found." });

        if (task.Project == null || task.Project.UserId != userId)
            return NotFound(new { message = "Task not found." }); // hide existence

        task.Title = req.Title.Trim();
        task.Description = req.Description.Trim();
        task.DueDate = req.DueDate;
        task.IsCompleted = req.IsCompleted;

        await _db.SaveChangesAsync();

        return Ok(new TaskResponse(task.Id, task.ProjectId, task.Title, task.Description, task.DueDate, task.IsCompleted));
    }

    // Delete a task (only if it belongs to a project owned by current user)
    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId)
    {
        var userId = UserContext.GetUserId(User);

        var task = await _db.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return NotFound(new { message = "Task not found." });

        if (task.Project == null || task.Project.UserId != userId)
            return NotFound(new { message = "Task not found." });

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
