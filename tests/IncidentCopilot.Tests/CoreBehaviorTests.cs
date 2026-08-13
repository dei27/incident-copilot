using System.ComponentModel.DataAnnotations;
using System.Net;
using IncidentCopilot.Configuration;
using IncidentCopilot.Models;
using IncidentCopilot.Security;
using IncidentCopilot.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace IncidentCopilot.Tests;

public sealed class IncidentRequestTests
{
    [Fact]
    public void Normalize_conserva_el_contenido_y_normaliza_saltos_de_linea()
    {
        var request = new IncidentRequest
        {
            Title = "  Timeout  ",
            Symptoms = "Primera línea\r\nSegunda línea  ",
            TechnicalContext = null,
            Logs = "  log sintético\r"
        };

        var normalized = IncidentRequestNormalizer.Normalize(request);

        Assert.Equal("Timeout", normalized.Title);
        Assert.Equal("Primera línea\nSegunda línea", normalized.Symptoms);
        Assert.Null(normalized.TechnicalContext);
        Assert.Equal("log sintético", normalized.Logs);
    }

    [Fact]
    public void Normalize_rechaza_caracteres_nulos()
    {
        var request = new IncidentRequest
        {
            Title = "Incidente\0sintético",
            Symptoms = "Síntomas"
        };

        var exception = Assert.Throws<ArgumentException>(
            () => IncidentRequestNormalizer.Normalize(request));

        Assert.Contains("carácter no válido", exception.Message);
    }

    [Fact]
    public void Validation_rechaza_campos_obligatorios_y_payload_excesivo()
    {
        var requiredFieldsRequest = new IncidentRequest
        {
            Title = "",
            Symptoms = ""
        };
        var requiredFieldErrors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            requiredFieldsRequest,
            new ValidationContext(requiredFieldsRequest),
            requiredFieldErrors,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(requiredFieldErrors, error => error.ErrorMessage == "El título es obligatorio.");
        Assert.Contains(requiredFieldErrors, error => error.ErrorMessage == "Los síntomas son obligatorios.");

        var oversizedRequest = new IncidentRequest
        {
            Title = "Incidente",
            Symptoms = new string('s', IncidentRequest.MaxSymptomsLength),
            TechnicalContext = new string('c', IncidentRequest.MaxTechnicalContextLength),
            Logs = new string('l', IncidentRequest.MaxLogsLength)
        };
        var oversizedErrors = new List<ValidationResult>();

        Validator.TryValidateObject(
            oversizedRequest,
            new ValidationContext(oversizedRequest),
            oversizedErrors,
            validateAllProperties: true);

        Assert.Contains(oversizedErrors, error => error.ErrorMessage == "El contenido total del incidente no puede superar los 20000 caracteres.");
    }

    [Fact]
    public void Validation_rechaza_un_campo_que_supera_su_limite()
    {
        var request = new IncidentRequest
        {
            Title = "Incidente sintético",
            Symptoms = new string('s', IncidentRequest.MaxSymptomsLength + 1)
        };
        var errors = new List<ValidationResult>();

        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            errors,
            validateAllProperties: true);

        Assert.Contains(errors, error => error.ErrorMessage == "Los síntomas no pueden superar los 4000 caracteres.");
    }
}

public sealed class SecretRedactorTests
{
    [Fact]
    public void Redact_reemplaza_patrones_de_secretos_sinteticos()
    {
        const string syntheticSecret = "synthetic-token-1234567890";
        var text = $"Authorization: Bearer {syntheticSecret}\napi_key={syntheticSecret}\nconnection string: \"Server=synthetic;Password={syntheticSecret}\"";
        var redactor = new SecretRedactor();

        var result = redactor.Redact(text);

        Assert.True(result.WasRedacted);
        Assert.True(result.RedactionCount >= 3);
        Assert.DoesNotContain(syntheticSecret, result.SanitizedText);
        Assert.Contains("[REDACTED]", result.SanitizedText);
    }

    [Fact]
    public void Redact_no_modifica_texto_sin_secretos()
    {
        var text = "error sintético: timeout al llamar a servicio local";

        var result = new SecretRedactor().Redact(text);

        Assert.False(result.WasRedacted);
        Assert.Equal(0, result.RedactionCount);
        Assert.Equal(text, result.SanitizedText);
    }
}

public sealed class IncidentAnalysisParserTests
{
    private const string ValidJson = """
        {
          "Summary": "Resumen sintético",
          "PossibleCauses": ["Hipótesis sintética"],
          "SuggestedChecks": ["Comprobación sintética"],
          "NextSteps": ["Paso sintético"],
          "Warnings": ["No determina causa raíz"]
        }
        """;

    [Fact]
    public void Parse_devuelve_el_contrato_completo()
    {
        var analysis = new IncidentAnalysisParser().Parse(ValidJson);

        Assert.Equal("Resumen sintético", analysis.Summary);
        Assert.Equal(["Hipótesis sintética"], analysis.PossibleCauses);
        Assert.Equal(["Comprobación sintética"], analysis.SuggestedChecks);
        Assert.Equal(["Paso sintético"], analysis.NextSteps);
        Assert.Equal(["No determina causa raíz"], analysis.Warnings);
    }

    [Fact]
    public void Parse_rechaza_json_invalido()
    {
        var exception = Assert.Throws<IncidentAnalysisParseException>(
            () => new IncidentAnalysisParser().Parse("{ invalid"));

        Assert.Equal(IncidentAnalysisParseFailureKind.InvalidJson, exception.Kind);
    }

    [Fact]
    public void Parse_rechaza_respuesta_vacia()
    {
        var exception = Assert.Throws<IncidentAnalysisParseException>(
            () => new IncidentAnalysisParser().Parse("  "));

        Assert.Equal(IncidentAnalysisParseFailureKind.EmptyResponse, exception.Kind);
    }

    [Fact]
    public void Parse_rechaza_contrato_incompleto()
    {
        var exception = Assert.Throws<IncidentAnalysisParseException>(
            () => new IncidentAnalysisParser().Parse("""{"Summary":"Solo resumen"}"""));

        Assert.Equal(IncidentAnalysisParseFailureKind.ContractViolation, exception.Kind);
        Assert.Contains("PossibleCauses", exception.Message);
    }
}

public sealed class ConfigurationAndErrorTests
{
    [Fact]
    public void OptionsValidator_acepta_configuracion_http_completa()
    {
        var options = new LlmOptions
        {
            ApiKey = "synthetic-api-key",
            BaseUrl = "https://example.invalid/api/v1",
            Model = "synthetic-model"
        };

        var result = new LlmOptionsValidator().Validate(Options.DefaultName, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void OptionsValidator_rechaza_configuracion_incompleta_y_url_invalida()
    {
        var options = new LlmOptions
        {
            BaseUrl = "file:///synthetic",
            Model = "synthetic-model"
        };

        var result = new LlmOptionsValidator().Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("LLM_API_KEY"));
        Assert.Contains(result.Failures!, failure => failure.Contains("HTTP o HTTPS"));
    }

    [Theory]
    [InlineData(401, IncidentAnalysisErrorKind.Unauthorized)]
    [InlineData(403, IncidentAnalysisErrorKind.Forbidden)]
    [InlineData(429, IncidentAnalysisErrorKind.RateLimited)]
    [InlineData(500, IncidentAnalysisErrorKind.ProviderUnavailable)]
    [InlineData(503, IncidentAnalysisErrorKind.ProviderUnavailable)]
    public void ErrorMapper_clasifica_respuestas_http_sin_exponer_detalles(
        int statusCode,
        IncidentAnalysisErrorKind expectedKind)
    {
        var exception = new HttpRequestException(
            "synthetic provider body must not escape",
            inner: null,
            (HttpStatusCode)statusCode);

        var error = new IncidentAnalysisErrorMapper().Map(exception);

        Assert.Equal(expectedKind, error.Kind);
        Assert.DoesNotContain("synthetic provider body", error.UserMessage);
    }

    [Fact]
    public void ErrorMapper_distingue_cancelacion_de_timeout()
    {
        var mapper = new IncidentAnalysisErrorMapper();

        var cancelled = mapper.Map(
            new OperationCanceledException(),
            new CancellationToken(canceled: true));
        var timeout = mapper.Map(new OperationCanceledException());

        Assert.Equal(IncidentAnalysisErrorKind.Cancelled, cancelled.Kind);
        Assert.Equal(IncidentAnalysisErrorKind.Timeout, timeout.Kind);
    }

    [Fact]
    public void ErrorMapper_clasifica_configuracion_y_respuesta_invalida()
    {
        var mapper = new IncidentAnalysisErrorMapper();
        var configurationException = new OptionsValidationException(
            "Llm",
            typeof(LlmOptions),
            ["synthetic configuration failure"]);
        var parseException = new IncidentAnalysisParseException(
            IncidentAnalysisParseFailureKind.InvalidJson,
            "synthetic parser detail");

        var configurationError = mapper.Map(configurationException);
        var responseError = mapper.Map(parseException);

        Assert.Equal(IncidentAnalysisErrorKind.Configuration, configurationError.Kind);
        Assert.Equal(IncidentAnalysisErrorKind.InvalidResponse, responseError.Kind);
        Assert.DoesNotContain("synthetic", configurationError.UserMessage);
        Assert.DoesNotContain("synthetic", responseError.UserMessage);
    }
}

public sealed class FakeLlmIncidentAnalyzerTests
{
    private static readonly IncidentRequest SyntheticRequest = new()
    {
        Title = "Incidente sintético",
        Symptoms = "Timeout sintético"
    };

    [Fact]
    public async Task Fake_devuelve_resultado_determinista()
    {
        var analysis = await new FakeLlmIncidentAnalyzer().AnalyzeAsync(SyntheticRequest);

        Assert.Equal("Análisis sintético para desarrollo y pruebas.", analysis.Summary);
        Assert.NotEmpty(analysis.PossibleCauses!);
        Assert.NotEmpty(analysis.SuggestedChecks!);
        Assert.NotEmpty(analysis.NextSteps!);
        Assert.NotNull(analysis.Warnings);
    }

    [Fact]
    public async Task Fake_propaga_fallo_configurado_sin_red()
    {
        var fake = new FakeLlmIncidentAnalyzer(
            failure: new InvalidOperationException("synthetic failure"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fake.AnalyzeAsync(SyntheticRequest));

        Assert.Equal("synthetic failure", exception.Message);
    }

    [Fact]
    public async Task Fake_rechaza_respuesta_invalida()
    {
        var fake = new FakeLlmIncidentAnalyzer(responseJson: "{ invalid");

        var exception = await Assert.ThrowsAsync<IncidentAnalysisParseException>(
            () => fake.AnalyzeAsync(SyntheticRequest));

        Assert.Equal(IncidentAnalysisParseFailureKind.InvalidJson, exception.Kind);
    }

    [Fact]
    public async Task Fake_respeta_cancelacion()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new FakeLlmIncidentAnalyzer().AnalyzeAsync(SyntheticRequest, cancellation.Token));
    }
}
