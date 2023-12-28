using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

using Xunit;

using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace ProjectVersionUpdater.Tests
{
    public class ProjectVersionUpdaterMultiTargetTests : MsBuildTest
    {
        private readonly string testDataWorkingDirectory;

        public ProjectVersionUpdaterMultiTargetTests()
        {
            this.testDataWorkingDirectory = Path.GetFullPath("./MultiTargetTestData-WorkingCopy");
            CreateTestDataWorkingCopy(this.testDataWorkingDirectory);
        }

        private void CreateTestDataWorkingCopy(string workingCopyDirectory)
        {
            string testDataDirectory = Path.GetFullPath("./MultiTargetTestData/");

            if (Directory.Exists(workingCopyDirectory))
            {
                Directory.Delete(workingCopyDirectory, recursive: true);
            }
            Directory.CreateDirectory(workingCopyDirectory);

            foreach (string sourceFile in Directory.GetFiles(testDataDirectory))
            {
                File.Copy(sourceFile, Path.Combine(workingCopyDirectory, Path.GetFileName(sourceFile)));
            }
        }

        [Fact]
        public async Task IncreaseVersion_MultiTargetProject_IncreasesVersion()
        {
            ProjectVersionUpdaterFactory updaterFactory = await this.CreateFactoryAsync(Path.Combine(this.testDataWorkingDirectory, "MultiTargetTest.sln"));
            ProjectVersionUpdater updater = updaterFactory.Create(Path.GetFullPath(Path.Combine(this.testDataWorkingDirectory, "A.csproj")));

            updater.IncreaseVersion(VersionPart.Major, prerelease: false);
            updater.IncreaseDependantsVersion();

            ProjectCollection projectCollection = new();
            MsBuildProject projectA = new MsBuildProject(ProjectRootElement.Open(Path.Combine(this.testDataWorkingDirectory, "A.csproj")), null, null, projectCollection);
            MsBuildProject projectB = new MsBuildProject(ProjectRootElement.Open(Path.Combine(this.testDataWorkingDirectory, "B.csproj")), null, null, projectCollection);
            MsBuildProject projectC = new MsBuildProject(ProjectRootElement.Open(Path.Combine(this.testDataWorkingDirectory, "C.csproj")), null, null, projectCollection);

            Assert.Equal("2.0.0", projectA.GetVersion().ToString());
            Assert.Equal("1.0.1", projectB.GetVersion().ToString());
            Assert.Equal("1.0.1", projectC.GetVersion().ToString());
        }

        private async Task<ProjectVersionUpdaterFactory> CreateFactoryAsync(string solutionPath)
        {
            using Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
            Microsoft.CodeAnalysis.Solution solution = await workspace.OpenSolutionAsync(Path.GetFullPath(solutionPath));
            IPrereleaseScheme prereleaseScheme = new NamedPrereleaseScheme("test");
            ProjectVersionUpdaterFactory updaterFactory = new ProjectVersionUpdaterFactory(solution, prereleaseScheme);

            return updaterFactory;
        }
    }
}
