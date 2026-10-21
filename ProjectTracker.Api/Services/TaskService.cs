using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Models;

namespace ProjectTracker.Api.Services;

public class TaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db) => _db = db;

    public async Task<ProjectTask?> AddTaskAsync(
        Guid userId,
        Guid projectId,
        string title,
        string? description,
        DateTime dueDate,
        CancellationToken cancellationToken = default)
    {
        var ownsProject = await _db.Projects
            .AsNoTracking()
            .AnyAsync(
                project => project.Id == projectId && project.UserId == userId,
                cancellationToken);

        if (!ownsProject)
        {
            return null;
        }

        var task = new ProjectTask
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            DueDate = dueDate,
            IsCompleted = false
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<IReadOnlyList<ProjectTask>?> ListOwnedProjectTasksAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var ownsProject = await _db.Projects
            .AsNoTracking()
            .AnyAsync(
                project => project.Id == projectId && project.UserId == userId,
                cancellationToken);

        if (!ownsProject)
        {
            return null;
        }

        return await _db.Tasks
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .OrderBy(task => task.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectTask?> UpdateOwnedTaskAsync(
        Guid userId,
        Guid taskId,
        string title,
        string? description,
        DateTime dueDate,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(
            candidate => candidate.Id == taskId && candidate.Project!.UserId == userId,
            cancellationToken);

        if (task is null)
        {
            return null;
        }

        task.Title = title.Trim();
        task.Description = description?.Trim() ?? string.Empty;
        task.DueDate = dueDate;
        task.IsCompleted = isCompleted;

        await _db.SaveChangesAsync(cancellationToken);
        return task;
    }

    public async Task<bool> DeleteOwnedTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(
            candidate => candidate.Id == taskId && candidate.Project!.UserId == userId,
            cancellationToken);

        if (task is null)
        {
            return false;
        }

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
