using Bulldog;

namespace SolutionValidator
{
    public interface IValidatorOptions : IToolOptions
    {
        string CommandName { get; }
    }
}
