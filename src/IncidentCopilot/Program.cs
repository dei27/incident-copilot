using IncidentCopilot.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<LlmOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.ApiKey = configuration[LlmOptions.ApiKeyEnvironmentVariable]
            ?? configuration[$"{LlmOptions.SectionName}:ApiKey"]
            ?? string.Empty;
        options.BaseUrl = configuration[LlmOptions.BaseUrlEnvironmentVariable]
            ?? configuration[$"{LlmOptions.SectionName}:BaseUrl"]
            ?? string.Empty;
        options.Model = configuration[LlmOptions.ModelEnvironmentVariable]
            ?? configuration[$"{LlmOptions.SectionName}:Model"]
            ?? string.Empty;
    })
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<LlmOptions>, LlmOptionsValidator>();
builder.Services.AddRazorPages();

var app = builder.Build();

app.MapRazorPages();

app.Run();
