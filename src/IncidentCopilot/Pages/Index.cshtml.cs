using IncidentCopilot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IncidentCopilot.Pages;

public sealed class IndexModel : PageModel
{
    [BindProperty]
    public IncidentRequest Input { get; set; } = new();

    public bool IsSubmitted { get; private set; }

    public void OnGet()
    {
    }

    public void OnPost()
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
    }
}
