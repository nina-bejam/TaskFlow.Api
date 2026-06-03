namespace TaskFlow.Api.DTOs;

public record CreateTaskRequest(string Title, string? Description);

public record UpdateTaskRequest(string Title, string? Description, bool IsCompleted);

public record TaskResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt);
