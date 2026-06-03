using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string?> SuggestTasksAsync(string projectName, string? description, CancellationToken cancellationToken)
    {
        var model = _configuration["Ollama:Model"] ?? "llama3.2";
        var prompt =
            $"""
            You are helping plan work for a project called "{projectName}".
            Project description: {description ?? "No description provided."}

            Suggest 5 short, actionable task titles for this project.
            Reply with one task per line. No numbering or extra text.
            """;

        var request = new OllamaGenerateRequest(model, prompt);
        var response = await _httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);
        return result?.Response?.Trim();
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream = false);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response);
}
