using System.Text.RegularExpressions;

namespace IncidentCopilot.Security;

public sealed class SecretRedactor : ISecretRedactor
{
    private const string RedactedValue = "[REDACTED]";

    private static readonly Regex AuthorizationHeaderPattern = new(
        @"(?im)(?<prefix>\bAuthorization\s*:\s*)[^\s\r\n]+(?:\s+[^\s\r\n]+)?",
        RegexOptions.CultureInvariant);

    private static readonly Regex BearerTokenPattern = new(
        @"\bBearer\s+[A-Za-z0-9._~+/=-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex ConnectionStringPattern = new(
        @"(?i)(?<prefix>\bconnection\s*string\s*[:=]\s*)(?:""[^""\r\n]*""|'[^'\r\n]*'|[^\r\n]+)",
        RegexOptions.CultureInvariant);

    private static readonly Regex ConnectionUriCredentialsPattern = new(
        @"(?i)(?<prefix>\b(?:mongodb(?:\+srv)?|postgres(?:ql)?|mysql|redis)://)[^@\s]+@",
        RegexOptions.CultureInvariant);

    private static readonly Regex SensitiveAssignmentPattern = new(
        @"(?i)(?<prefix>(?<![\w-])(?:api[_-]?key|x-api-key|password|passwd|pwd|secret|access[_-]?token|refresh[_-]?token|token)\s*[:=]\s*)(?:""[^""]*""|'[^']*'|[^\s,;]+)",
        RegexOptions.CultureInvariant);

    private static readonly Regex CommonApiKeyPattern = new(
        @"\b(?:sk|pk|ghp|glpat|AIza)[-_A-Za-z0-9]{16,}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public RedactionResult Redact(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var redactionCount = 0;
        var sanitizedText = text;

        sanitizedText = ReplaceWithRedactedValue(
            sanitizedText,
            AuthorizationHeaderPattern,
            ref redactionCount);
        sanitizedText = ReplaceWithRedactedValue(
            sanitizedText,
            BearerTokenPattern,
            ref redactionCount);
        sanitizedText = ReplaceWithRedactedValue(
            sanitizedText,
            ConnectionStringPattern,
            ref redactionCount);
        sanitizedText = ReplaceWithRedactedValue(
            sanitizedText,
            ConnectionUriCredentialsPattern,
            ref redactionCount);
        sanitizedText = ReplaceWithRedactedValue(
            sanitizedText,
            SensitiveAssignmentPattern,
            ref redactionCount);
        sanitizedText = ReplaceWithRedactedValue(
            sanitizedText,
            CommonApiKeyPattern,
            ref redactionCount);

        return new RedactionResult(
            sanitizedText,
            redactionCount > 0,
            redactionCount);
    }

    private static string ReplaceWithRedactedValue(
        string text,
        Regex pattern,
        ref int redactionCount)
    {
        redactionCount += pattern.Matches(text).Count;

        return pattern.Replace(
            text,
            match => match.Groups["prefix"].Success
                ? match.Groups["prefix"].Value + RedactedValue
                : RedactedValue);
    }
}
