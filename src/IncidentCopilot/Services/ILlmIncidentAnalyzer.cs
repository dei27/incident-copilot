using IncidentCopilot.Models;

namespace IncidentCopilot.Services;

public interface ILlmIncidentAnalyzer
{
    Task<IncidentAnalysis> AnalyzeAsync(
        IncidentRequest incident,
        CancellationToken cancellationToken = default);
}
