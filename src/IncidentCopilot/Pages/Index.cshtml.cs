using IncidentCopilot.Models;
using IncidentCopilot.Security;
using IncidentCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IncidentCopilot.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ILlmIncidentAnalyzer _analyzer;
    private readonly IIncidentAnalysisErrorMapper _errorMapper;
    private readonly ISecretRedactor _secretRedactor;

    public IndexModel(
        ILlmIncidentAnalyzer analyzer,
        IIncidentAnalysisErrorMapper errorMapper,
        ISecretRedactor secretRedactor)
    {
        _analyzer = analyzer;
        _errorMapper = errorMapper;
        _secretRedactor = secretRedactor;
    }

    [BindProperty]
    public IncidentRequest Input { get; set; } = new();

    public bool IsSubmitted { get; private set; }
    public IncidentAnalysis? Analysis { get; private set; }
    public IncidentAnalysisError? AnalysisError { get; private set; }

    public void OnGet()
    {
    }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return;
        }

        try
        {
            Input = IncidentRequestNormalizer.Normalize(Input);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return;
        }

        ModelState.Clear();
        if (!TryValidateModel(Input, nameof(Input)))
        {
            return;
        }

        Input = RedactInput(Input);
        IsSubmitted = true;

        try
        {
            Analysis = await _analyzer.AnalyzeAsync(Input, cancellationToken);
        }
        catch (Exception exception)
        {
            AnalysisError = _errorMapper.Map(exception, cancellationToken);
        }
    }

    private IncidentRequest RedactInput(IncidentRequest request)
    {
        return new IncidentRequest
        {
            Title = _secretRedactor.Redact(request.Title ?? string.Empty).SanitizedText,
            Symptoms = _secretRedactor.Redact(request.Symptoms ?? string.Empty).SanitizedText,
            TechnicalContext = RedactOptional(request.TechnicalContext),
            Logs = RedactOptional(request.Logs)
        };
    }

    private string? RedactOptional(string? value)
    {
        return value is null ? null : _secretRedactor.Redact(value).SanitizedText;
    }
}
