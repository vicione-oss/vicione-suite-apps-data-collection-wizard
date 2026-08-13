using System.ComponentModel.DataAnnotations;

namespace DataCollectionWizard.Internal.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal class ValidatePropertiesAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not null)
        {
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(value, new ValidationContext(value), results, true);

            if (results.Count > 0)
            {
                return new ValidationResult($"{{{string.Join(", ", results.Select(v => $"[{v.MemberNames}]: {v.ErrorMessage}"))}}}",
                    results.SelectMany(v => v.MemberNames));
            }
        }

        return ValidationResult.Success!;
    }
}
