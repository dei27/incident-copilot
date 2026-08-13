namespace IncidentCopilot.Services;

public enum IncidentAnalysisErrorKind
{
    Configuration,
    Unauthorized,
    Forbidden,
    RateLimited,
    ProviderUnavailable,
    Timeout,
    Cancelled,
    EmptyResponse,
    InvalidResponse,
    Transport,
    Unknown
}

public sealed record IncidentAnalysisError(
    IncidentAnalysisErrorKind Kind,
    string UserMessage,
    bool CanRetry);
