using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ProjectTracker.Api.Data;
using ProjectTracker.Api.Models;
using ProjectTracker.Api.Services;

namespace ProjectTracker.Tests;

public class ProjectAndTaskTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TaskId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTime DueDate = new(2030, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private static AppDbContext CreateDb()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("ProjectTrackerTests", databaseRoot)
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<Project> SeedProjectAsync(
        AppDbContext db,
        Guid ownerId,
        Guid? projectId = null)
    {
        var project = new Project
        {
            Id = projectId ?? ProjectId,
            UserId = ownerId,
            Name = "Original project",
            Description = "Original description"
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return project;
    }

    private static async Task<ProjectTask> SeedTaskAsync(AppDbContext db, Guid projectOwnerId)
    {
        var project = new Project
        {
            Id = ProjectId,
            UserId = projectOwnerId,
            Name = "Original project",
            Description = "Original description"
        };
        var task = new ProjectTask
        {
            Id = TaskId,
            ProjectId = project.Id,
            Project = project,
            Title = "Original task",
            Description = "Original task description",
            DueDate = DueDate,
            IsCompleted = false
        };

        db.AddRange(project, task);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return task;
    }

    [Fact]
    public async Task CreateProject_CreatesProjectForUser()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);

        var project = await projectService.CreateProjectAsync(
            OwnerId,
            "  My Project  ",
            "  My Description  ");

        Assert.Equal(OwnerId, project.UserId);
        Assert.Equal("My Project", project.Name);
        Assert.Equal("My Description", project.Description);

        var saved = await db.Projects.SingleAsync(candidate => candidate.Id == project.Id);
        Assert.Equal("My Project", saved.Name);
        Assert.Equal("My Description", saved.Description);
    }

    [Fact]
    public async Task GetOwnedProjectAsync_WhenProjectBelongsToUser_ReturnsProject()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);
        await SeedProjectAsync(db, OwnerId);

        var project = await projectService.GetOwnedProjectAsync(OwnerId, ProjectId);

        Assert.NotNull(project);
        Assert.Equal(ProjectId, project.Id);
        Assert.Equal(OwnerId, project.UserId);
    }

    [Fact]
    public async Task GetOwnedProjectAsync_WhenProjectBelongsToAnotherUser_ReturnsNull()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);
        await SeedProjectAsync(db, OtherOwnerId);

        var project = await projectService.GetOwnedProjectAsync(OwnerId, ProjectId);

        Assert.Null(project);
    }

    [Fact]
    public async Task GetOwnedProjectAsync_WhenProjectIsMissing_ReturnsNull()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);

        var project = await projectService.GetOwnedProjectAsync(OwnerId, ProjectId);

        Assert.Null(project);
    }

    [Fact]
    public async Task ListOwnedProjectsAsync_ReturnsOnlyProjectsForUser()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);
        var ownedProject = new Project
        {
            Id = ProjectId,
            UserId = OwnerId,
            Name = "Owned",
            Description = string.Empty
        };
        var foreignProject = new Project
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            UserId = OtherOwnerId,
            Name = "Foreign",
            Description = string.Empty
        };
        db.Projects.AddRange(ownedProject, foreignProject);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var projects = await projectService.ListOwnedProjectsAsync(OwnerId);

        var project = Assert.Single(projects);
        Assert.Equal(ProjectId, project.Id);
    }

    [Fact]
    public async Task UpdateOwnedProjectAsync_WhenProjectBelongsToAnotherUser_ReturnsNullAndDoesNotModify()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);
        await SeedProjectAsync(db, OtherOwnerId);

        var result = await projectService.UpdateOwnedProjectAsync(
            OwnerId,
            ProjectId,
            "Changed",
            "Changed description");

        Assert.Null(result);
        var persisted = await db.Projects.AsNoTracking().SingleAsync();
        Assert.Equal("Original project", persisted.Name);
        Assert.Equal("Original description", persisted.Description);
    }

    [Fact]
    public async Task DeleteOwnedProjectAsync_WhenProjectBelongsToAnotherUser_ReturnsFalseAndDoesNotDelete()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);
        await SeedProjectAsync(db, OtherOwnerId);

        var result = await projectService.DeleteOwnedProjectAsync(OwnerId, ProjectId);

        Assert.False(result);
        Assert.True(await db.Projects.AnyAsync(candidate => candidate.Id == ProjectId));
    }

    [Fact]
    public async Task AddTask_AddsTaskToOwnedProject()
    {
        await using var db = CreateDb();
        var projectService = new ProjectService(db);
        var taskService = new TaskService(db);

        var project = await projectService.CreateProjectAsync(OwnerId, "Project", "Description");

        var task = await taskService.AddTaskAsync(
            OwnerId,
            project.Id,
            "  Task title  ",
            "  Task description  ",
            DueDate);

        Assert.NotNull(task);
        Assert.Equal(project.Id, task.ProjectId);
        Assert.Equal("Task title", task.Title);
        Assert.Equal("Task description", task.Description);
        Assert.Equal(DueDate, task.DueDate);
        Assert.False(task.IsCompleted);

        var tasks = await db.Tasks.Where(t => t.ProjectId == project.Id).ToListAsync();
        Assert.Single(tasks);
        Assert.Equal("Task title", tasks[0].Title);
    }

    [Fact]
    public async Task AddTaskAsync_WhenProjectBelongsToAnotherUser_ReturnsNullAndDoesNotAddTask()
    {
        await using var db = CreateDb();
        var taskService = new TaskService(db);
        await SeedProjectAsync(db, OtherOwnerId);

        var result = await taskService.AddTaskAsync(
            OwnerId,
            ProjectId,
            "Task",
            "Description",
            DueDate);

        Assert.Null(result);
        Assert.Empty(await db.Tasks.ToListAsync());
    }

    [Fact]
    public async Task AddTaskAsync_WhenProjectIsMissing_ReturnsNullAndDoesNotAddTask()
    {
        await using var db = CreateDb();
        var taskService = new TaskService(db);

        var result = await taskService.AddTaskAsync(
            OwnerId,
            ProjectId,
            "Task",
            "Description",
            DueDate);

        Assert.Null(result);
        Assert.Empty(await db.Tasks.ToListAsync());
    }

    [Fact]
    public async Task ListOwnedProjectTasksAsync_WhenProjectBelongsToAnotherUser_ReturnsNull()
    {
        await using var db = CreateDb();
        var taskService = new TaskService(db);
        await SeedTaskAsync(db, OtherOwnerId);

        var result = await taskService.ListOwnedProjectTasksAsync(OwnerId, ProjectId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateOwnedTaskAsync_WhenTaskBelongsToAnotherUser_ReturnsNullAndDoesNotModify()
    {
        await using var db = CreateDb();
        var taskService = new TaskService(db);
        await SeedTaskAsync(db, OtherOwnerId);
        var changedDueDate = new DateTime(2031, 1, 10, 8, 30, 0, DateTimeKind.Utc);

        var result = await taskService.UpdateOwnedTaskAsync(
            OwnerId,
            TaskId,
            "Changed task",
            "Changed description",
            changedDueDate,
            true);

        Assert.Null(result);
        var persisted = await db.Tasks.AsNoTracking().SingleAsync();
        Assert.Equal("Original task", persisted.Title);
        Assert.Equal("Original task description", persisted.Description);
        Assert.Equal(DueDate, persisted.DueDate);
        Assert.False(persisted.IsCompleted);
    }

    [Fact]
    public async Task UpdateOwnedTaskAsync_WhenTaskIsMissing_ReturnsNull()
    {
        await using var db = CreateDb();
        var taskService = new TaskService(db);

        var result = await taskService.UpdateOwnedTaskAsync(
            OwnerId,
            TaskId,
            "Changed task",
            null,
            DueDate,
            true);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteOwnedTaskAsync_WhenTaskBelongsToAnotherUser_ReturnsFalseAndDoesNotDelete()
    {
        await using var db = CreateDb();
        var taskService = new TaskService(db);
        await SeedTaskAsync(db, OtherOwnerId);

        var result = await taskService.DeleteOwnedTaskAsync(OwnerId, TaskId);

        Assert.False(result);
        Assert.True(await db.Tasks.AnyAsync(candidate => candidate.Id == TaskId));
    }

    [Fact]
    public async Task DeleteOwnedTaskAsync_WhenTaskIsMissing_ReturnsFalse()
    {
        await using var db = CreateDb();
        var taskService = new TaskService(db);

        var result = await taskService.DeleteOwnedTaskAsync(OwnerId, TaskId);

        Assert.False(result);
    }

    [Fact]
    public void ProjectTaskRelationship_IsConfiguredForCascadeDelete()
    {
        using var db = CreateDb();
        var taskEntity = db.Model.FindEntityType(typeof(ProjectTask));
        Assert.NotNull(taskEntity);
        var projectForeignKey = Assert.Single(
            taskEntity.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Project));

        Assert.Equal(DeleteBehavior.Cascade, projectForeignKey.DeleteBehavior);
    }
}
