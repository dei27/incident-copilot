using System.Net;
using System.Text.RegularExpressions;
using IncidentCopilot.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IncidentCopilot.Tests;

public sealed class WebFlowTests
{
    [Fact]
    public async Task Post_valido_muestra_el_analisis_estructurado_del_fake()
    {
        const string syntheticSecret = "synthetic-browser-secret";
        const string responseJson = """
            {
              "Summary": "Resumen de integración sintético",
              "PossibleCauses": ["Hipótesis de integración sintética"],
              "SuggestedChecks": ["Comprobar el flujo web sintético"],
              "NextSteps": ["Registrar el siguiente paso sintético"],
              "Warnings": ["Resultado sintético: no determina causa raíz"]
            }
            """;

        using var factory = new FakeProviderWebApplicationFactory(
            new FakeLlmIncidentAnalyzer(responseJson: responseJson));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await SubmitIncidentAsync(client, new Dictionary<string, string>
        {
            ["Input.Title"] = "Timeout sintético",
            ["Input.Symptoms"] = "La solicitud sintética tarda demasiado.",
            ["Input.TechnicalContext"] = "Servicio local de pruebas",
            ["Input.Logs"] = $"Authorization: Bearer {syntheticSecret}"
        });
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Resultado del análisis", html);
        Assert.Contains("Resumen de integración sintético", html);
        Assert.Contains("Hipótesis de integración sintética", html);
        Assert.Contains("Comprobar el flujo web sintético", html);
        Assert.Contains("Registrar el siguiente paso sintético", html);
        Assert.Contains("Resultado sintético: no determina causa raíz", html);
        Assert.DoesNotContain(syntheticSecret, html);
    }

    [Fact]
    public async Task Post_con_respuesta_invalida_muestra_error_controlado()
    {
        const string invalidResponse = "{ invalid synthetic response";
        using var factory = new FakeProviderWebApplicationFactory(
            new FakeLlmIncidentAnalyzer(responseJson: invalidResponse));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await SubmitIncidentAsync(client, new Dictionary<string, string>
        {
            ["Input.Title"] = "Respuesta inválida sintética",
            ["Input.Symptoms"] = "El fake devuelve JSON inválido.",
            ["Input.TechnicalContext"] = "Contexto de prueba",
            ["Input.Logs"] = "log sintético"
        });
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No se pudo completar el análisis", html);
        Assert.Contains("no cumple el formato esperado", html);
        Assert.DoesNotContain(invalidResponse, html);
        Assert.DoesNotContain("Resultado del análisis", html);
    }

    [Fact]
    public async Task Post_con_entrada_invalida_muestra_validacion_y_no_analiza()
    {
        using var factory = new FakeProviderWebApplicationFactory(
            new FakeLlmIncidentAnalyzer());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await SubmitIncidentAsync(client, new Dictionary<string, string>
        {
            ["Input.Title"] = "",
            ["Input.Symptoms"] = "",
            ["Input.TechnicalContext"] = "Contexto sintético",
            ["Input.Logs"] = "log sintético"
        });
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("El título es obligatorio.", html);
        Assert.Contains("Los síntomas son obligatorios.", html);
        Assert.DoesNotContain("Resultado del análisis", html);
    }

    private static async Task<HttpResponseMessage> SubmitIncidentAsync(
        HttpClient client,
        IReadOnlyDictionary<string, string> fields)
    {
        var getResponse = await client.GetAsync("/");
        getResponse.EnsureSuccessStatusCode();
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(
            getHtml,
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        Assert.True(tokenMatch.Success, "El formulario no contiene el token antiforgery esperado.");

        var form = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", tokenMatch.Groups[1].Value)
        };
        form.AddRange(fields);

        return await client.PostAsync("/", new FormUrlEncodedContent(form));
    }
}

internal sealed class FakeProviderWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly ILlmIncidentAnalyzer _analyzer;

    public FakeProviderWebApplicationFactory(ILlmIncidentAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            var registrations = services
                .Where(descriptor => descriptor.ServiceType == typeof(ILlmIncidentAnalyzer))
                .ToList();

            foreach (var registration in registrations)
            {
                services.Remove(registration);
            }

            services.AddSingleton(_analyzer);
        });
    }
}
