using System.ComponentModel.DataAnnotations;

namespace DataCollectionWizard.Internal.Validation;

internal sealed class IsNotEqualAttribute(string comparisonDateTimeMemberName) : ValidationAttribute
{
    public string ComparisonDateTimeMemberName { get; } = string.IsNullOrWhiteSpace(comparisonDateTimeMemberName)
            ? throw new ArgumentNullException(nameof(comparisonDateTimeMemberName))
            : comparisonDateTimeMemberName;

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime dateTimeValue)
            throw new ArgumentException($"The value to validate is not of type {nameof(DateTime)}.");

        var targetProp = validationContext.ObjectType
            .GetProperty(ComparisonDateTimeMemberName);

        if (targetProp?.PropertyType != typeof(DateTime))
            throw new ArgumentException($"The property \"{ComparisonDateTimeMemberName}\" to validate against \"{validationContext.MemberName}\" is not of type {nameof(DateTime)}.");

        var comparionDateTimeValue = (DateTime)targetProp.GetValue(validationContext.ObjectInstance)!;

        if (DateTime.Compare(dateTimeValue, comparionDateTimeValue) > 0)
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult(
            string.IsNullOrWhiteSpace(ErrorMessage)
                ? ErrorMessage = $"{validationContext.DisplayName} has to be different from {ComparisonDateTimeMemberName}."
                : ErrorMessageString,
            [validationContext.MemberName ?? string.Empty]
        );
    }
}
