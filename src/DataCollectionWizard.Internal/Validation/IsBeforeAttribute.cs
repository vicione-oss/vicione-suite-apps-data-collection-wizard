using System.ComponentModel.DataAnnotations;

namespace DataCollectionWizard.Internal.Validation;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class IsBeforeAttribute(string otherDateTimeMemberName) : ValidationAttribute
{
    public string OtherDateTimeMemberName { get; internal set; } = string.IsNullOrWhiteSpace(otherDateTimeMemberName)
            ? throw new ArgumentNullException(nameof(otherDateTimeMemberName))
            : otherDateTimeMemberName;

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateTime dateTimeValue)
            throw new ArgumentException($"The value to validate is not of type {nameof(DateTime)}.");

        var targetProp = validationContext.ObjectType
            .GetProperty(OtherDateTimeMemberName);

        if (targetProp?.PropertyType != typeof(DateTime))
            throw new ArgumentException($"The property \"{OtherDateTimeMemberName}\" to validate against \"{validationContext.MemberName}\" is not of type {nameof(DateTime)}.");

        var comparionDateTimeValue = (DateTime)targetProp.GetValue(validationContext.ObjectInstance)!;

        if (DateTime.Compare(dateTimeValue, comparionDateTimeValue) < 0)
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult(
            string.IsNullOrWhiteSpace(ErrorMessage)
                ? ErrorMessage = $"{validationContext.DisplayName} has to be earlier than {OtherDateTimeMemberName}."
                : ErrorMessageString,
            [validationContext.MemberName ?? string.Empty]
        );
    }
}
