using System.Text.Json;
using IncidentCopilot.Models;

namespace IncidentCopilot.Services;

public sealed class IncidentAnalysisParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 16
    };

    public IncidentAnalysis Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new IncidentAnalysisParseException("La respuesta del proveedor está vacía.");
        }

        IncidentAnalysis? analysis;

        try
        {
            analysis = JsonSerializer.Deserialize<IncidentAnalysis>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            throw new IncidentAnalysisParseException("La respuesta del proveedor no contiene JSON válido.");
        }
        catch (NotSupportedException)
        {
            throw new IncidentAnalysisParseException("La respuesta del proveedor no tiene un formato compatible.");
        }

        var errors = IncidentAnalysisValidator.GetErrors(analysis);
        if (errors.Count > 0)
        {
            throw new IncidentAnalysisParseException(
                $"La respuesta del proveedor no cumple el contrato de análisis: {string.Join(" ", errors)}");
        }

        return analysis!;
    }
}
