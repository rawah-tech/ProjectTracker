using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Models;

namespace ProjectTracker.Api.Services;

public class TaskService
{
    private readonly AppDbContext _db;
    public TaskService(AppDbContext db) => _db = db;

    public async Task<ProjectTask> AddTaskAsync(Guid userId, Guid projectId, string title, string description, DateTime dueDate)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) throw new InvalidOperationException("Project not found.");

        var task = new ProjectTask
        {
            ProjectId = projectId,
            Title = title.Trim(),
            Description = description.Trim(),
            DueDate = dueDate,
            IsCompleted = false
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }
}
