namespace SolutionValidator.ValidateSolutions
{
    public interface ISolutionValidator
    {
        ValidationResult Validate(ValidationContext validationContext);
    }
}
