using Microsoft.EntityFrameworkCore;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Services;
using Xunit;

namespace ProjectTracker.Tests;

public class ProjectAndTaskTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateProject_CreatesProjectForUser()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);

        var userId = Guid.NewGuid();
        var project = await projectService.CreateProjectAsync(userId, "My Project", "My Desc");

        Assert.Equal(userId, project.UserId);
        Assert.Equal("My Project", project.Name);
        Assert.Equal("My Desc", project.Description);

        var saved = await db.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task AddTask_AddsTaskToOwnedProject()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);
        var taskService = new TaskService(db);

        var userId = Guid.NewGuid();
        var project = await projectService.CreateProjectAsync(userId, "P1", "D1");

        var due = DateTime.UtcNow.AddDays(5);
        var task = await taskService.AddTaskAsync(userId, project.Id, "T1", "TD1", due);

        Assert.Equal(project.Id, task.ProjectId);
        Assert.False(task.IsCompleted);

        var tasks = await db.Tasks.Where(t => t.ProjectId == project.Id).ToListAsync();
        Assert.Single(tasks);
        Assert.Equal("T1", tasks[0].Title);
    }
}
