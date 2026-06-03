namespace TaskFlow.Api.DTOs;

public record FileResponse(
    Guid Id,
    Guid TaskId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);
