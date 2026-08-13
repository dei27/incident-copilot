using System.Text.Json.Serialization;

namespace IncidentCopilot.Models;

public sealed class IncidentAnalysis
{
    [JsonPropertyName("Summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("PossibleCauses")]
    public List<string>? PossibleCauses { get; init; }

    [JsonPropertyName("SuggestedChecks")]
    public List<string>? SuggestedChecks { get; init; }

    [JsonPropertyName("NextSteps")]
    public List<string>? NextSteps { get; init; }

    [JsonPropertyName("Warnings")]
    public List<string>? Warnings { get; init; }
}
