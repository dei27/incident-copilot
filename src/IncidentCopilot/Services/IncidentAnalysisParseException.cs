namespace IncidentCopilot.Services;

public enum IncidentAnalysisParseFailureKind
{
    EmptyResponse,
    InvalidJson,
    InvalidFormat,
    ContractViolation
}

public sealed class IncidentAnalysisParseException : Exception
{
    public IncidentAnalysisParseFailureKind Kind { get; }

    public IncidentAnalysisParseException(string message)
        : this(IncidentAnalysisParseFailureKind.ContractViolation, message)
    {
    }

    public IncidentAnalysisParseException(
        IncidentAnalysisParseFailureKind kind,
        string message)
        : base(message)
    {
        Kind = kind;
    }
}
