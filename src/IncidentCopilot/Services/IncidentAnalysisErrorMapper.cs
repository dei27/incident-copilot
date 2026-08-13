using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace IncidentCopilot.Services;

public interface IIncidentAnalysisErrorMapper
{
    IncidentAnalysisError Map(
        Exception exception,
        CancellationToken cancellationToken = default);
}

public sealed class IncidentAnalysisErrorMapper : IIncidentAnalysisErrorMapper
{
    public IncidentAnalysisError Map(
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OptionsValidationException)
        {
            return Create(
                IncidentAnalysisErrorKind.Configuration,
                "No se puede analizar el incidente porque la configuración del proveedor falta o no es válida.",
                canRetry: false);
        }

        if (exception is IncidentAnalysisParseException parseException)
        {
            return parseException.Kind switch
            {
                IncidentAnalysisParseFailureKind.EmptyResponse => Create(
                    IncidentAnalysisErrorKind.EmptyResponse,
                    "El proveedor devolvió una respuesta vacía. Intenta de nuevo más tarde.",
                    canRetry: true),
                IncidentAnalysisParseFailureKind.InvalidJson
                    or IncidentAnalysisParseFailureKind.InvalidFormat
                    or IncidentAnalysisParseFailureKind.ContractViolation => Create(
                        IncidentAnalysisErrorKind.InvalidResponse,
                        "El proveedor devolvió una respuesta que no cumple el formato esperado.",
                        canRetry: false),
                _ => Create(
                    IncidentAnalysisErrorKind.InvalidResponse,
                    "El proveedor devolvió una respuesta no válida.",
                    canRetry: false)
            };
        }

        if (exception is HttpRequestException httpException)
        {
            return MapHttpFailure(httpException.StatusCode);
        }

        if (exception is TimeoutException)
        {
            return Create(
                IncidentAnalysisErrorKind.Timeout,
                "El proveedor tardó demasiado en responder. Intenta de nuevo.",
                canRetry: true);
        }

        if (exception is OperationCanceledException)
        {
            return cancellationToken.IsCancellationRequested
                ? Create(
                    IncidentAnalysisErrorKind.Cancelled,
                    "El análisis fue cancelado.",
                    canRetry: false)
                : Create(
                    IncidentAnalysisErrorKind.Timeout,
                    "El proveedor tardó demasiado en responder. Intenta de nuevo.",
                    canRetry: true);
        }

        return Create(
            IncidentAnalysisErrorKind.Unknown,
            "No se pudo completar el análisis del incidente.",
            canRetry: false);
    }

    private static IncidentAnalysisError MapHttpFailure(HttpStatusCode? statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => Create(
                IncidentAnalysisErrorKind.Unauthorized,
                "El proveedor rechazó la API key configurada. Revisa la configuración local.",
                canRetry: false),
            HttpStatusCode.Forbidden => Create(
                IncidentAnalysisErrorKind.Forbidden,
                "El proveedor no permite esta solicitud con la configuración actual.",
                canRetry: false),
            HttpStatusCode.TooManyRequests => Create(
                IncidentAnalysisErrorKind.RateLimited,
                "El proveedor alcanzó el límite temporal de uso. Intenta más tarde.",
                canRetry: true),
            _ when statusCode.HasValue && (int)statusCode.Value is >= 500 and <= 599 => Create(
                IncidentAnalysisErrorKind.ProviderUnavailable,
                "El proveedor no está disponible temporalmente. Intenta más tarde.",
                canRetry: true),
            _ => Create(
                IncidentAnalysisErrorKind.Transport,
                "No se pudo comunicar con el proveedor LLM.",
                canRetry: true)
        };
    }

    private static IncidentAnalysisError Create(
        IncidentAnalysisErrorKind kind,
        string userMessage,
        bool canRetry)
    {
        return new IncidentAnalysisError(kind, userMessage, canRetry);
    }
}
