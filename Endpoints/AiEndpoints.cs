using TaskFlow.Api.DTOs;
using TaskFlow.Api.Extensions;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Endpoints;

public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ai")
            .WithTags("AI")
            .RequireAuthorization();

        group.MapPost("/suggest-tasks", async (
            SuggestTasksRequest request,
            OllamaService ollama,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.ProjectName))
            {
                return Results.BadRequest("ProjectName is required.");
            }

            var response = await ollama.SuggestTasksAsync(
                request.ProjectName.Trim(),
                request.Description?.Trim(),
                cancellationToken);

            if (response is null)
            {
                return Results.Problem(
                    "Could not reach Ollama. Make sure it is running locally and the model is pulled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var suggestions = response
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Take(10)
                .ToList();

            return Results.Ok(new SuggestTasksResponse(suggestions));
        });

        return group;
    }
}
