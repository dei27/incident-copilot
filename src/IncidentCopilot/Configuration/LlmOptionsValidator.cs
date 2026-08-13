using Microsoft.Extensions.Options;

namespace IncidentCopilot.Configuration;

public sealed class LlmOptionsValidator : IValidateOptions<LlmOptions>
{
    public ValidateOptionsResult Validate(string? name, LlmOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add($"Falta la configuración {LlmOptions.ApiKeyEnvironmentVariable}.");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            failures.Add($"Falta la configuración {LlmOptions.BaseUrlEnvironmentVariable}.");
        }
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add($"La configuración {LlmOptions.BaseUrlEnvironmentVariable} debe ser una URL HTTP o HTTPS absoluta.");
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            failures.Add($"Falta la configuración {LlmOptions.ModelEnvironmentVariable}.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
