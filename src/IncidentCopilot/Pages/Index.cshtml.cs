using IncidentCopilot.Models;
using IncidentCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IncidentCopilot.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ILlmIncidentAnalyzer _analyzer;
    private readonly IIncidentAnalysisErrorMapper _errorMapper;

    public IndexModel(
        ILlmIncidentAnalyzer analyzer,
        IIncidentAnalysisErrorMapper errorMapper)
    {
        _analyzer = analyzer;
        _errorMapper = errorMapper;
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
}
