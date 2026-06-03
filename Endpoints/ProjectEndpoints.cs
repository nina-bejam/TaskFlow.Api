using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Extensions;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Endpoints;

public static class ProjectEndpoints
{
    public static RouteGroupBuilder MapProjectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var projects = await db.Projects
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.Name)
                .Select(p => ToResponse(p))
                .ToListAsync();

            return Results.Ok(projects);
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            return project is null ? Results.NotFound() : Results.Ok(ToResponse(project));
        });

        group.MapPost("/", async (CreateProjectRequest request, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Name is required.");
            }

            var project = new Project
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync();

            return Results.Created($"/api/projects/{project.Id}", ToResponse(project));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateProjectRequest request, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Name is required.");
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project is null)
            {
                return Results.NotFound();
            }

            project.Name = request.Name.Trim();
            project.Description = request.Description?.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(ToResponse(project));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var userId = user.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var project = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project is null)
            {
                return Results.NotFound();
            }

            db.Projects.Remove(project);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return group;
    }

    private static ProjectResponse ToResponse(Project project) =>
        new(project.Id, project.Name, project.Description, project.CreatedAt);
}
