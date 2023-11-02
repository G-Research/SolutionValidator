using Microsoft.Build.Locator;

namespace SolutionValidator.Tests
{
    public abstract class BaseFixture
    {
        static BaseFixture()
        {
            var instance = MSBuildLocator.RegisterDefaults();
        }
    }
}
