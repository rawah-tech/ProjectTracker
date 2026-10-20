using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectTracker.Api.Dtos;
using ProjectTracker.Api.Models;
using ProjectTracker.Api.Services;

namespace ProjectTracker.Api.Controllers;

[ApiController]
[Route("projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ProjectService _projects;

    public ProjectsController(ProjectService projects) => _projects = projects;

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var project = await _projects.CreateProjectAsync(
            userId,
            request.Name,
            request.Description,
            cancellationToken);

        return Ok(ToResponse(project));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> List(CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var projects = await _projects.ListOwnedProjectsAsync(userId, cancellationToken);
        var response = projects.Select(ToResponse).ToList();

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var project = await _projects.UpdateOwnedProjectAsync(
            userId,
            id,
            request.Name,
            request.Description,
            cancellationToken);

        if (project is null)
        {
            return NotFound(new { message = "Project not found." });
        }

        return Ok(ToResponse(project));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!UserContext.TryGetUserId(User, out var userId))
        {
            return Unauthorized(new { message = "Invalid authentication token." });
        }

        var deleted = await _projects.DeleteOwnedProjectAsync(userId, id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Project not found." });
        }

        return NoContent();
    }

    private static ProjectResponse ToResponse(Project project) =>
        new(project.Id, project.Name, project.Description);
}
