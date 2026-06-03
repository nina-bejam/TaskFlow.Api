namespace TaskFlow.Api.DTOs;

public record SuggestTasksRequest(string ProjectName, string? Description);

public record SuggestTasksResponse(IReadOnlyList<string> Suggestions);
