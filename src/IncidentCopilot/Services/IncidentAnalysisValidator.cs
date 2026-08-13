using IncidentCopilot.Models;

namespace IncidentCopilot.Services;

public static class IncidentAnalysisValidator
{
    public static IReadOnlyList<string> GetErrors(IncidentAnalysis? analysis)
    {
        if (analysis is null)
        {
            return ["El resultado está vacío."];
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(analysis.Summary))
        {
            errors.Add("Summary es obligatorio.");
        }

        ValidateRequiredItems(analysis.PossibleCauses, "PossibleCauses", errors, requireItems: true);
        ValidateRequiredItems(analysis.SuggestedChecks, "SuggestedChecks", errors, requireItems: true);
        ValidateRequiredItems(analysis.NextSteps, "NextSteps", errors, requireItems: true);
        ValidateRequiredItems(analysis.Warnings, "Warnings", errors, requireItems: false);

        return errors;
    }

    private static void ValidateRequiredItems(
        List<string>? values,
        string fieldName,
        ICollection<string> errors,
        bool requireItems)
    {
        if (values is null)
        {
            errors.Add($"{fieldName} es obligatorio.");
            return;
        }

        if (requireItems && values.Count == 0)
        {
            errors.Add($"{fieldName} debe contener al menos un elemento.");
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"{fieldName} no puede contener elementos vacíos.");
        }
    }
}
