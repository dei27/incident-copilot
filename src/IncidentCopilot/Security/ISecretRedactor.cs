namespace IncidentCopilot.Security;

public interface ISecretRedactor
{
    RedactionResult Redact(string text);
}

public sealed record RedactionResult(
    string SanitizedText,
    bool WasRedacted,
    int RedactionCount);
