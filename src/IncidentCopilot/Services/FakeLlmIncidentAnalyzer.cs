using IncidentCopilot.Models;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentCopilot.Services;

public sealed class FakeLlmIncidentAnalyzer : ILlmIncidentAnalyzer
{
    private const string DefaultResponseJson = """
        {
          "Summary": "Análisis sintético para desarrollo y pruebas.",
          "PossibleCauses": [
            "La evidencia disponible podría ser insuficiente para distinguir entre varias hipótesis."
          ],
          "SuggestedChecks": [
            "Confirmar los síntomas y comparar el comportamiento con una ejecución normal."
          ],
          "NextSteps": [
            "Recopilar evidencia adicional antes de realizar cambios."
          ],
          "Warnings": [
            "Este resultado es sintético y no determina una causa raíz."
          ]
        }
        """;

    private readonly string _responseJson;
    private readonly Exception? _failure;

    public FakeLlmIncidentAnalyzer(
        string? responseJson = null,
        Exception? failure = null)
    {
        if (responseJson is not null && failure is not null)
        {
            throw new ArgumentException(
                "El fake no puede configurarse simultáneamente con una respuesta y un fallo.",
                nameof(failure));
        }

        _responseJson = responseJson ?? DefaultResponseJson;
        _failure = failure;
    }

    public Task<string> AnalyzeAsync(
        IncidentRequest incident,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(incident);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(cancellationToken);
        }

        if (_failure is not null)
        {
            return Task.FromException<string>(_failure);
        }

        return Task.FromResult(_responseJson);
    }
}

public static class FakeLlmIncidentAnalyzerServiceCollectionExtensions
{
    public static IServiceCollection AddFakeLlmIncidentAnalyzer(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ILlmIncidentAnalyzer, FakeLlmIncidentAnalyzer>();
        return services;
    }
}
