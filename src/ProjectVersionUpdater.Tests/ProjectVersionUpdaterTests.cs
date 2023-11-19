using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Microsoft.Build.Locator;

using Xunit;

namespace ProjectVersionUpdater.Tests;

public class ProjectVersionUpdaterTests
{
    static ProjectVersionUpdaterTests()
    {
        VisualStudioInstance net6Instance = MSBuildLocator.QueryVisualStudioInstances().First(i => i.Version.Major == 6);
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterInstance(net6Instance);
        }
    }

    private Microsoft.CodeAnalysis.Solution solution;

    private IMsbuildProjectAdapter projectAdapter;

    private Microsoft.CodeAnalysis.Project dependantSolutionProject;

    private Microsoft.Build.Evaluation.Project dependant;

    private readonly string dependantTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">
<PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <Version>{0}</Version>
</PropertyGroup >
</Project>
";

    private Microsoft.CodeAnalysis.Project dependency1SolutionProject;

    private Microsoft.Build.Evaluation.Project dependency1;

    private readonly string dependencyTemplate1 = @"
<Project Sdk=""Microsoft.NET.Sdk"">
<PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <Version>{0}</Version>
</PropertyGroup >
</Project>
";

    private Microsoft.CodeAnalysis.Project dependency2SolutionProject;

    private Microsoft.Build.Evaluation.Project dependency2;

    private readonly string dependencyTemplate2 = @"
<Project Sdk=""Microsoft.NET.Sdk"">
<PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <Version>{0}</Version>
</PropertyGroup >
</Project>
";

    [Theory]
    [InlineData("2.0.0", VersionPart.Major, false, "3.0.0")]
    [InlineData("2.0.0", VersionPart.Minor, false, "2.1.0")]
    [InlineData("2.0.0", VersionPart.Patch, false, "2.0.1")]
    [InlineData("2.0.0", VersionPart.Major, true, "3.0.0-pre.1")]
    [InlineData("2.0.0", VersionPart.Minor, true, "2.1.0-pre.1")]
    [InlineData("2.0.0", VersionPart.Patch, true, "2.0.1-pre.1")]
    [InlineData("3.0.0-pre", VersionPart.Major, true, "3.0.0-pre.1")]
    [InlineData("2.1.0-pre", VersionPart.Minor, true, "2.1.0-pre.1")]
    [InlineData("2.0.1-pre", VersionPart.Patch, true, "2.0.1-pre.1")]
    [InlineData("2.0.0-otherscheme", VersionPart.Major, true, "2.0.0-pre.1")]
    [InlineData("2.1.0-otherscheme", VersionPart.Minor, true, "2.1.0-pre.1")]
    [InlineData("2.0.1-otherscheme", VersionPart.Patch, true, "2.0.1-pre.1")]
    public void IncreaseVersion_DefaultScheme(string version, VersionPart update, bool prerelease, string expectedVersion)
    {
        // Arrange
        this.SetupTestData(version);
        ProjectVersionUpdater sut = new ProjectVersionUpdater(this.dependency1SolutionProject, this.solution, new CustomPrereleaseScheme("pre"), this.projectAdapter);

        // Act
        sut.IncreaseVersion(update, prerelease);

        // Assert
        Assert.Equal(expectedVersion, this.dependency1.GetVersion().ToString());
    }

    [Theory]
    [InlineData("2.0.0", "1.0.0", "1.0.1")]
    [InlineData("2.0.0-pre", "1.0.0", "1.0.1-pre.1")]
    [InlineData("2.0.0-pre", "1.0.1-pre", "1.0.1-pre.1")]
    [InlineData("3.0.0", "2.0.1-pre", "2.0.1")]
    public void IncreaseDependant_SingleProjectDefaultScheme(string dependencyVersion, string dependantVersion, string expectedVersion)
    {
        // Arrange
        this.SetupTestData(dependencyVersion, dependantVersion);
        ProjectVersionUpdater sut = new ProjectVersionUpdater(this.dependency1SolutionProject, this.solution, new CustomPrereleaseScheme("pre"), this.projectAdapter);

        // Act
        sut.IncreaseDependantsVersion();

        // Assert
        Assert.Equal(expectedVersion, this.dependant.GetVersion().ToString());
    }

    [Theory]
    [InlineData("2.0.0", "2.0.0", "1.0.0", "1.0.1")]
    [InlineData("2.0.0-pre", "2.0.0-pre", "1.0.0", "1.0.1-pre.1")]
    [InlineData("3.0.0-pre", "3.0.0-pre", "1.0.1-pre", "1.0.1-pre.1")]
    [InlineData("4.0.0-pre", "4.0.0", "2.0.0", "2.0.1-pre.1")]
    public void IncreaseDependant_MultipleProjectsDefaultScheme(string dependency1Version, string dependency2Version, string dependantVersion, string expectedVersion)
    {
        // Arrange
        this.SetupTestData(dependency1Version, dependency2Version, dependantVersion);
        ProjectVersionUpdater sut = new ProjectVersionUpdater(new[] { this.dependency1SolutionProject, this.dependency2SolutionProject }, this.solution, new CustomPrereleaseScheme("pre"), this.projectAdapter);

        // Act
        sut.IncreaseDependantsVersion();

        // Assert
        Assert.Equal(expectedVersion, this.dependant.GetVersion().ToString());
    }

    private void SetupTestData(string dependencyVersion) => this.SetupTestData(dependencyVersion, dependencyVersion, "1.0.0");

    private void SetupTestData(string dependencyVersion, string dependantVersion) => this.SetupTestData(dependencyVersion, dependencyVersion, dependantVersion);

    private void SetupTestData(string dependency1Version, string dependency2Version, string dependantVersion)
    {
        Microsoft.CodeAnalysis.ProjectId dependantSolutionProjectId = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
        Microsoft.CodeAnalysis.ProjectId dependencySolution1ProjectId = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
        Microsoft.CodeAnalysis.ProjectId dependencySolution2ProjectId = Microsoft.CodeAnalysis.ProjectId.CreateNewId();

        this.solution = new Microsoft.CodeAnalysis.AdhocWorkspace().CurrentSolution
            .AddProject(dependantSolutionProjectId, "Dependant", "Dependant", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddProject(dependencySolution1ProjectId, "Dependency1", "Dependency1", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddProject(dependencySolution2ProjectId, "Dependency2", "Dependency2", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddProjectReference(dependantSolutionProjectId, new Microsoft.CodeAnalysis.ProjectReference(dependencySolution1ProjectId))
            .AddProjectReference(dependantSolutionProjectId, new Microsoft.CodeAnalysis.ProjectReference(dependencySolution2ProjectId));

        this.dependantSolutionProject = this.solution.GetProject(dependantSolutionProjectId);
        this.dependency1SolutionProject = this.solution.GetProject(dependencySolution1ProjectId);
        this.dependency2SolutionProject = this.solution.GetProject(dependencySolution2ProjectId);

        this.dependency1 = new(XmlReader.Create(new StringReader(string.Format(this.dependencyTemplate1, dependency1Version))));
        this.dependency2 = new(XmlReader.Create(new StringReader(string.Format(this.dependencyTemplate2, dependency2Version))));
        this.dependant = new(XmlReader.Create(new StringReader(string.Format(this.dependantTemplate, dependantVersion))));

        this.projectAdapter = new InMemoryProjectAdapter(new Dictionary<Microsoft.CodeAnalysis.Project, Microsoft.Build.Evaluation.Project>
        {
            { this.dependency1SolutionProject, this.dependency1 },
            { this.dependency2SolutionProject, this.dependency2 },
            { this.dependantSolutionProject, this.dependant },
        });
    }
}

internal class InMemoryProjectAdapter : IMsbuildProjectAdapter
{
    private readonly Dictionary<Microsoft.CodeAnalysis.Project, Microsoft.Build.Evaluation.Project> mapping;

    public InMemoryProjectAdapter(Dictionary<Microsoft.CodeAnalysis.Project, Microsoft.Build.Evaluation.Project> mapping)
    {
        this.mapping = mapping;
    }

    public Microsoft.Build.Evaluation.Project LoadProject(Microsoft.CodeAnalysis.Project solutionProject)
        => this.mapping[solutionProject];

    public void SaveProject(Microsoft.Build.Evaluation.Project msbuildProject)
    {
    }
}
