using SlnUtils;

namespace SolutionValidator
{
    // Is this evil?
    public class Solution : FilePath
    {
        public SlnFile SlnFile { get; }

        public Solution(string filePath)
            : base(filePath)
        {
            SlnFile = SlnFile.Read(NormalisedPath);
        }
    }
}
