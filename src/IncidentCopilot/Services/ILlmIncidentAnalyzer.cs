using IncidentCopilot.Models;

namespace IncidentCopilot.Services;

public interface ILlmIncidentAnalyzer
{
    Task<string> AnalyzeAsync(
        IncidentRequest incident,
        CancellationToken cancellationToken = default);
}
