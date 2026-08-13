using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IncidentCopilot.Configuration;
using IncidentCopilot.Models;
using IncidentCopilot.Security;
using Microsoft.Extensions.Options;

namespace IncidentCopilot.Services;

public sealed class OpenAiLlmIncidentAnalyzer : ILlmIncidentAnalyzer
{
    private const string SystemInstructions = """
        Analiza únicamente la evidencia suministrada de un incidente técnico sintético.
        Presenta posibles causas como hipótesis, nunca como una causa raíz confirmada.
        Diferencia evidencia de hipótesis, propone comprobaciones antes de cambios destructivos
        y devuelve únicamente el objeto JSON solicitado. No incluyas secretos detectados.
        """;

    private static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private static readonly object ResponseSchema = new
    {
        type = "object",
        properties = new
        {
            Summary = new { type = "string", minLength = 1 },
            PossibleCauses = new
            {
                type = "array",
                minItems = 1,
                items = new { type = "string", minLength = 1 }
            },
            SuggestedChecks = new
            {
                type = "array",
                minItems = 1,
                items = new { type = "string", minLength = 1 }
            },
            NextSteps = new
            {
                type = "array",
                minItems = 1,
                items = new { type = "string", minLength = 1 }
            },
            Warnings = new
            {
                type = "array",
                items = new { type = "string", minLength = 1 }
            }
        },
        required = new[] { "Summary", "PossibleCauses", "SuggestedChecks", "NextSteps", "Warnings" },
        additionalProperties = false
    };

    private readonly HttpClient _httpClient;
    private readonly IOptions<LlmOptions> _options;
    private readonly ISecretRedactor _secretRedactor;
    private readonly IncidentAnalysisParser _parser;

    public OpenAiLlmIncidentAnalyzer(
        HttpClient httpClient,
        IOptions<LlmOptions> options,
        ISecretRedactor secretRedactor,
        IncidentAnalysisParser parser)
    {
        _httpClient = httpClient;
        _options = options;
        _secretRedactor = secretRedactor;
        _parser = parser;
    }

    public async Task<IncidentAnalysis> AnalyzeAsync(
        IncidentRequest incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        var normalizedIncident = IncidentRequestNormalizer.Normalize(incident);
        var evidence = BuildEvidence(normalizedIncident);
        var sanitizedEvidence = _secretRedactor.Redact(evidence).SanitizedText;
        var options = _options.Value;
        var endpoint = BuildEndpoint(options.BaseUrl);

        var requestBody = new
        {
            model = options.Model,
            instructions = SystemInstructions,
            input = new[]
            {
                new
                {
                    role = "user",
                    content = new[]
                    {
                        new { type = "input_text", text = sanitizedEvidence }
                    }
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "incident_analysis",
                    schema = ResponseSchema,
                    strict = true
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(requestBody, options: RequestSerializerOptions);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"El proveedor LLM respondió con HTTP {(int)response.StatusCode}.",
                inner: null,
                response.StatusCode);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var outputJson = ExtractOutputText(responseBody);
        return _parser.Parse(outputJson);
    }

    private static Uri BuildEndpoint(string baseUrl)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), "responses");
    }

    private static string BuildEvidence(IncidentRequest incident)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Título:");
        builder.AppendLine(incident.Title);
        builder.AppendLine("Síntomas:");
        builder.AppendLine(incident.Symptoms);
        builder.AppendLine("Contexto técnico:");
        builder.AppendLine(incident.TechnicalContext);
        builder.AppendLine("Logs:");
        builder.AppendLine(incident.Logs);
        return builder.ToString();
    }

    private static string ExtractOutputText(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("output", out var output)
                || output.ValueKind != JsonValueKind.Array)
            {
                throw new IncidentAnalysisParseException("La respuesta del proveedor no contiene output.");
            }

            foreach (var outputItem in output.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var content)
                    || content.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("type", out var type)
                        && type.GetString() == "output_text"
                        && contentItem.TryGetProperty("text", out var text)
                        && text.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(text.GetString()))
                    {
                        return text.GetString()!;
                    }
                }
            }
        }
        catch (JsonException)
        {
            throw new IncidentAnalysisParseException("La respuesta del proveedor no contiene JSON válido.");
        }

        throw new IncidentAnalysisParseException("La respuesta del proveedor no contiene texto estructurado.");
    }
}
