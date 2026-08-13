namespace IncidentCopilot.Configuration;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";
    public const string ApiKeyEnvironmentVariable = "LLM_API_KEY";
    public const string BaseUrlEnvironmentVariable = "LLM_BASE_URL";
    public const string ModelEnvironmentVariable = "LLM_MODEL";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
}
