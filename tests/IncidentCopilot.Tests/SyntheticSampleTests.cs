using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using IncidentCopilot.Models;
using IncidentCopilot.Services;
using Xunit;

namespace IncidentCopilot.Tests;

public sealed class SyntheticSampleTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] ExpectedSampleNames =
    [
        "configuracion-invalida.json",
        "http-429.json",
        "null-reference.json",
        "sql-lento.json",
        "timeout-api.json"
    ];

    private static readonly string[] SecretPatterns =
    [
        "Bearer ",
        "api_key=",
        "password=",
        "connection string",
        "sk-",
        "ghp_",
        "glpat-",
        "AIza"
    ];

    [Fact]
    public void Samples_contienen_los_cinco_casos_sinteticos_esperados()
    {
        var files = GetSampleFiles();

        Assert.Equal(ExpectedSampleNames, files.Select(Path.GetFileName).Order());
    }

    [Fact]
    public void Samples_cumplen_el_contrato_de_entrada_y_sus_limites()
    {
        foreach (var file in GetSampleFiles())
        {
            var json = File.ReadAllText(file);
            var request = JsonSerializer.Deserialize<IncidentRequest>(json, SerializerOptions);
            var errors = new List<ValidationResult>();

            Assert.NotNull(request);
            Assert.True(
                Validator.TryValidateObject(
                    request!,
                    new ValidationContext(request!),
                    errors,
                    validateAllProperties: true),
                $"El sample {Path.GetFileName(file)} no cumple el contrato: {string.Join(" ", errors.Select(error => error.ErrorMessage))}");

            var normalized = IncidentRequestNormalizer.Normalize(request!);
            Assert.NotNull(normalized.Title);
            Assert.NotNull(normalized.Symptoms);
        }
    }

    [Fact]
    public void Samples_no_contienen_patrones_de_secretos_conocidos()
    {
        foreach (var file in GetSampleFiles())
        {
            var json = File.ReadAllText(file);

            foreach (var pattern in SecretPatterns)
            {
                Assert.False(
                    json.Contains(pattern, StringComparison.OrdinalIgnoreCase),
                    $"El sample {Path.GetFileName(file)} contiene el patrón sensible {pattern}.");
            }
        }
    }

    [Fact]
    public void Evaluacion_rechaza_json_de_analisis_invalido()
    {
        var exception = Assert.Throws<IncidentAnalysisParseException>(
            () => new IncidentAnalysisParser().Parse("{ \"Summary\": \"incompleto\""));

        Assert.Equal(IncidentAnalysisParseFailureKind.InvalidJson, exception.Kind);
    }

    private static string[] GetSampleFiles()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "samples", "incidents");
        return Directory.GetFiles(directory, "*.json")
            .Select(Path.GetFullPath)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
