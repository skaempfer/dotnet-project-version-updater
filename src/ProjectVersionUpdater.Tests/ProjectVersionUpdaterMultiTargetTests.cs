using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Locator;

using Xunit;

namespace ProjectVersionUpdater.Tests
{
    public class ProjectVersionUpdaterMultiTargetTests
    {
        static ProjectVersionUpdaterMultiTargetTests()
        {
            VisualStudioInstance net6Instance = MSBuildLocator.QueryVisualStudioInstances().First(i => i.Version.Major == 6);
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterInstance(net6Instance);
            }
        }

        [Fact]
        public async Task IncreaseVersion_MultiTargetProject_IncreasesVersion()
        {
            using Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
            Microsoft.CodeAnalysis.Solution solution = await workspace.OpenSolutionAsync(Path.GetFullPath("./MultiTargetTestData/MultiTargetTest.sln"));
            IPrereleaseScheme prereleaseScheme = new CustomPrereleaseScheme("test");
            ProjectVersionUpdaterFactory updaterFactory = new ProjectVersionUpdaterFactory(solution, prereleaseScheme);
            
            ProjectVersionUpdater updater = updaterFactory.Create(Path.GetFullPath("./MultiTargetTestData/A.csproj"));

            updater.IncreaseVersion(VersionPart.Major, prerelease: false);
        }
    }
}
