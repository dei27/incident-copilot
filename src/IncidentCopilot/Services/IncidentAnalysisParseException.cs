namespace IncidentCopilot.Services;

public sealed class IncidentAnalysisParseException : Exception
{
    public IncidentAnalysisParseException(string message)
        : base(message)
    {
    }
}
