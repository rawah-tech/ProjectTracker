using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Models;

namespace ProjectTracker.Api.Services;

public class ProjectService
{
    private readonly AppDbContext _db;

    public ProjectService(AppDbContext db) => _db = db;

    public async Task<Project> CreateProjectAsync(
        Guid userId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            UserId = userId,
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);
        return project;
    }

    public Task<Project?> GetOwnedProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(
                project => project.Id == projectId && project.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListOwnedProjectsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Projects
            .AsNoTracking()
            .Where(project => project.UserId == userId)
            .OrderByDescending(project => project.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> UpdateOwnedProjectAsync(
        Guid userId,
        Guid projectId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(
            candidate => candidate.Id == projectId && candidate.UserId == userId,
            cancellationToken);

        if (project is null)
        {
            return null;
        }

        project.Name = name.Trim();
        project.Description = description?.Trim() ?? string.Empty;

        await _db.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<bool> DeleteOwnedProjectAsync(
        Guid userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(
            candidate => candidate.Id == projectId && candidate.UserId == userId,
            cancellationToken);

        if (project is null)
        {
            return false;
        }

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
