using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Extensions;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Endpoints;

public static class TaskEndpoints
{
    public static RouteGroupBuilder MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        group.MapGet("/", async (Guid projectId, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var projectExists = await db.Projects
                .AnyAsync(p => p.Id == projectId && p.UserId == userId);

            if (!projectExists)
            {
                return Results.NotFound();
            }

            var tasks = await db.Tasks
                .Where(t => t.ProjectId == projectId)
                .OrderBy(t => t.CreatedAt)
                .Select(t => ToResponse(t))
                .ToListAsync();

            return Results.Ok(tasks);
        });

        group.MapGet("/{taskId:guid}", async (Guid projectId, Guid taskId, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var task = await db.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task is null || task.Project.UserId != userId)
            {
                return Results.NotFound();
            }

            return Results.Ok(ToResponse(task));
        });

        group.MapPost("/", async (Guid projectId, CreateTaskRequest request, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest("Title is required.");
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);

            if (project is null)
            {
                return Results.NotFound();
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            db.Tasks.Add(task);
            await db.SaveChangesAsync();

            return Results.Created($"/api/projects/{projectId}/tasks/{task.Id}", ToResponse(task));
        });

        group.MapPut("/{taskId:guid}", async (
            Guid projectId,
            Guid taskId,
            UpdateTaskRequest request,
            ClaimsPrincipal user,
            AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest("Title is required.");
            }

            var task = await db.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task is null || task.Project.UserId != userId)
            {
                return Results.NotFound();
            }

            task.Title = request.Title.Trim();
            task.Description = request.Description?.Trim();
            task.IsCompleted = request.IsCompleted;
            await db.SaveChangesAsync();

            return Results.Ok(ToResponse(task));
        });

        group.MapDelete("/{taskId:guid}", async (Guid projectId, Guid taskId, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var task = await db.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

            if (task is null || task.Project.UserId != userId)
            {
                return Results.NotFound();
            }

            db.Tasks.Remove(task);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return group;
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(task.Id, task.ProjectId, task.Title, task.Description, task.IsCompleted, task.CreatedAt);
}
