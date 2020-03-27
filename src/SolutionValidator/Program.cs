using System.Threading.Tasks;

namespace SolutionValidator
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            SolutionValidator solutionValidator = new SolutionValidator();
            return await solutionValidator.Run(args);
        }
    }
}
