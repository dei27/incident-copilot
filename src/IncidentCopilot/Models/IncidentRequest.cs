using System.ComponentModel.DataAnnotations;

namespace IncidentCopilot.Models;

public sealed class IncidentRequest : IValidatableObject
{
    public const int MaxTitleLength = 200;
    public const int MaxSymptomsLength = 4_000;
    public const int MaxTechnicalContextLength = 6_000;
    public const int MaxLogsLength = 12_000;
    public const int MaxTotalLength = 20_000;

    [Required(AllowEmptyStrings = false, ErrorMessage = "El título es obligatorio.")]
    [StringLength(MaxTitleLength, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string? Title { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Los síntomas son obligatorios.")]
    [StringLength(MaxSymptomsLength, ErrorMessage = "Los síntomas no pueden superar los 4000 caracteres.")]
    public string? Symptoms { get; set; }

    [StringLength(MaxTechnicalContextLength, ErrorMessage = "El contexto técnico no puede superar los 6000 caracteres.")]
    public string? TechnicalContext { get; set; }

    [StringLength(MaxLogsLength, ErrorMessage = "Los logs no pueden superar los 12000 caracteres.")]
    public string? Logs { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var fieldsWithNullCharacters = new[]
        {
            (Title, nameof(Title)),
            (Symptoms, nameof(Symptoms)),
            (TechnicalContext, nameof(TechnicalContext)),
            (Logs, nameof(Logs))
        };

        foreach (var (value, fieldName) in fieldsWithNullCharacters)
        {
            if (value?.Contains('\0') == true)
            {
                yield return new ValidationResult(
                    $"El campo {fieldName} contiene un carácter no válido.",
                    new[] { fieldName });
            }
        }

        var totalLength = (Title?.Length ?? 0)
            + (Symptoms?.Length ?? 0)
            + (TechnicalContext?.Length ?? 0)
            + (Logs?.Length ?? 0);

        if (totalLength > MaxTotalLength)
        {
            yield return new ValidationResult(
                "El contenido total del incidente no puede superar los 20000 caracteres.",
                new[] { nameof(Title), nameof(Symptoms), nameof(TechnicalContext), nameof(Logs) });
        }
    }
}
