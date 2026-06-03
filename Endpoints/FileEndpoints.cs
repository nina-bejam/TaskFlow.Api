using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Extensions;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Endpoints;

public static class FileEndpoints
{
    public static RouteGroupBuilder MapFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Files")
            .RequireAuthorization();

        group.MapPost("/projects/{projectId:guid}/tasks/{taskId:guid}/files", async (
            Guid projectId,
            Guid taskId,
            IFormFile file,
            ClaimsPrincipal user,
            AppDbContext db,
            LocalFileStorage storage,
            CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (file.Length == 0)
            {
                return Results.BadRequest("File is empty.");
            }

            var task = await db.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId, cancellationToken);

            if (task is null || task.Project.UserId != userId)
            {
                return Results.NotFound();
            }

            var stored = await storage.SaveAsync(taskId, file, cancellationToken);
            db.Files.Add(stored);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created(
                $"/api/files/{stored.Id}",
                ToResponse(stored));
        });

        group.MapGet("/files/{fileId:guid}", async (
            Guid fileId,
            ClaimsPrincipal user,
            AppDbContext db,
            LocalFileStorage storage,
            CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var file = await db.Files
                .Include(f => f.Task)
                .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            if (file is null || file.Task.Project.UserId != userId)
            {
                return Results.NotFound();
            }

            var path = storage.GetFullPath(file);
            if (!System.IO.File.Exists(path))
            {
                return Results.NotFound();
            }

            return Results.File(path, file.ContentType, file.FileName);
        });

        group.MapDelete("/files/{fileId:guid}", async (
            Guid fileId,
            ClaimsPrincipal user,
            AppDbContext db,
            LocalFileStorage storage,
            CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var file = await db.Files
                .Include(f => f.Task)
                .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);

            if (file is null || file.Task.Project.UserId != userId)
            {
                return Results.NotFound();
            }

            storage.Delete(file);
            db.Files.Remove(file);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        return group;
    }

    private static FileResponse ToResponse(Models.StoredFile file) =>
        new(file.Id, file.TaskId, file.FileName, file.ContentType, file.SizeBytes, file.CreatedAt);
}
