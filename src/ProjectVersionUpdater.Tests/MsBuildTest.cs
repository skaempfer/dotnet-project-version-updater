using System.Linq;

using Microsoft.Build.Locator;

namespace ProjectVersionUpdater.Tests;

/// <summary>
/// Base class for tests that require MsBuild specific initialization
/// </summary>
public abstract class MsBuildTest
{
    static MsBuildTest()
    {
        VisualStudioInstance net6Instance = MSBuildLocator.QueryVisualStudioInstances().First(i => i.Version.Major == 6);
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterInstance(net6Instance);
        }
    }
}
