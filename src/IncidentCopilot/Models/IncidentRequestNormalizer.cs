namespace IncidentCopilot.Models;

public static class IncidentRequestNormalizer
{
    public static IncidentRequest Normalize(IncidentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new IncidentRequest
        {
            Title = NormalizeText(request.Title),
            Symptoms = NormalizeText(request.Symptoms),
            TechnicalContext = NormalizeOptionalText(request.TechnicalContext),
            Logs = NormalizeOptionalText(request.Logs)
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return value is null ? null : NormalizeText(value);
    }

    private static string NormalizeText(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value.Contains('\0'))
        {
            throw new ArgumentException("El contenido contiene un carácter no válido.");
        }

        var normalizedLineEndings = value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var lines = normalizedLineEndings
            .Split('\n')
            .Select(line => line.TrimEnd())
            .ToArray();

        return string.Join('\n', lines).Trim();
    }
}
