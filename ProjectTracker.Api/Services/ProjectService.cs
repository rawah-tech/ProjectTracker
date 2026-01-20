using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Models;

namespace ProjectTracker.Api.Services;

public class ProjectService
{
    private readonly AppDbContext _db;
    public ProjectService(AppDbContext db) => _db = db;

    public async Task<Project> CreateProjectAsync(Guid userId, string name, string description)
    {
        var project = new Project
        {
            UserId = userId,
            Name = name.Trim(),
            Description = description.Trim()
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<Project?> GetOwnedProjectAsync(Guid userId, Guid projectId)
    {
        return await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
    }
}
