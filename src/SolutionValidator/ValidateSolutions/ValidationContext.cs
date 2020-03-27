using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Construction;

namespace SolutionValidator.ValidateSolutions
{
    public class ValidationContext
    {
        public string SolutionName { get; }
        public string FullPath { get; }
        public SolutionFile Solution { get; }

        private List<string> _errors = new List<string>();

        public IReadOnlyList<string> Errors => _errors;

        public ValidationContext(string absolutePath, SolutionFile solutionFile)
        {
            // TODO: Should I be loading the loading solution in here and 
            // should I be passing in the SolutionValidator options so that they are accessible to the underlying validators?

            SolutionName = Path.GetFileNameWithoutExtension(absolutePath);
            FullPath = absolutePath;
            Solution = solutionFile;
        }

        public void AddError(string error)
        {
            _errors.Add(error);
        }
    }
}
