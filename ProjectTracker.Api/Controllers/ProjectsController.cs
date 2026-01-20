using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Dtos;
using ProjectTracker.Api.Models;
using ProjectTracker.Api.Services;

namespace ProjectTracker.Api.Controllers;

[ApiController]
[Route("projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProjectsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest req)
    {
        var userId = UserContext.GetUserId(User);

        var project = new Project
        {
            Name = req.Name.Trim(),
            Description = req.Description.Trim(),
            UserId = userId
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return Ok(new ProjectResponse(project.Id, project.Name, project.Description));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> List()
    {
        var userId = UserContext.GetUserId(User);

        var projects = await _db.Projects
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Id)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.Description))
            .ToListAsync();

        return Ok(projects);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(Guid id, UpdateProjectRequest req)
    {
        var userId = UserContext.GetUserId(User);

        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null)
            return NotFound(new { message = "Project not found." });

        project.Name = req.Name.Trim();
        project.Description = req.Description.Trim();

        await _db.SaveChangesAsync();

        return Ok(new ProjectResponse(project.Id, project.Name, project.Description));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = UserContext.GetUserId(User);

        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (project == null)
            return NotFound(new { message = "Project not found." });

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
