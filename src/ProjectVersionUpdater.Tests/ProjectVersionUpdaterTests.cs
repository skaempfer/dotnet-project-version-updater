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
        ProjectVersionUpdater sut = new ProjectVersionUpdater(this.solutionProjectB, this.solution, new CustomPrereleaseScheme("pre"), this.projectAdapter);

        // Act
        sut.IncreaseVersion(update, prerelease);

        // Assert
        Assert.Equal(expectedVersion, this.projectB.GetVersion().ToString());
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
        ProjectVersionUpdater sut = new ProjectVersionUpdater(this.solutionProjectB, this.solution, new CustomPrereleaseScheme("pre"), this.projectAdapter);

        // Act
        sut.IncreaseDependantsVersion();

        // Assert
        Assert.Equal(expectedVersion, this.projectA.GetVersion().ToString());
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
        ProjectVersionUpdater sut = new ProjectVersionUpdater(new[] { this.solutionProjectB, this.solutionProjectC }, this.solution, new CustomPrereleaseScheme("pre"), this.projectAdapter);

        // Act
        sut.IncreaseDependantsVersion();

        // Assert
        Assert.Equal(expectedVersion, this.projectA.GetVersion().ToString());
    }

    /***
     * Test projects with following structure: A ==> B,C
     * where A has a dependency on B and C
     ***/

    private Microsoft.CodeAnalysis.Project solutionProjectA;
    private Microsoft.Build.Evaluation.Project projectA;

    private Microsoft.CodeAnalysis.Project solutionProjectB;
    private Microsoft.Build.Evaluation.Project projectB;

    private Microsoft.CodeAnalysis.Project solutionProjectC;
    private Microsoft.Build.Evaluation.Project projectC;

    private readonly string projectTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">
<PropertyGroup>
    <TargetFrameworks>net6;net7</TargetFrameworks>
    <Version>{0}</Version>
</PropertyGroup >
</Project>
";

    private void SetupTestData(string dependencyVersion) => this.SetupTestData(dependencyVersion, dependencyVersion, "1.0.0");

    private void SetupTestData(string dependencyVersion, string dependantVersion) => this.SetupTestData(dependencyVersion, dependencyVersion, dependantVersion);

    private void SetupTestData(string dependency1Version, string dependency2Version, string dependantVersion)
    {
        Microsoft.CodeAnalysis.ProjectId solutionProjectAId = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
        Microsoft.CodeAnalysis.ProjectId solutionProjectBId = Microsoft.CodeAnalysis.ProjectId.CreateNewId();
        Microsoft.CodeAnalysis.ProjectId solutionProjectCId = Microsoft.CodeAnalysis.ProjectId.CreateNewId();

        this.solution = new Microsoft.CodeAnalysis.AdhocWorkspace().CurrentSolution
            .AddProject(solutionProjectAId, "A", "A", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddProject(solutionProjectBId, "B", "B", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddProject(solutionProjectCId, "C", "C", Microsoft.CodeAnalysis.LanguageNames.CSharp)
            .AddProjectReference(solutionProjectAId, new Microsoft.CodeAnalysis.ProjectReference(solutionProjectBId))
            .AddProjectReference(solutionProjectAId, new Microsoft.CodeAnalysis.ProjectReference(solutionProjectCId));

        this.solutionProjectA = this.solution.GetProject(solutionProjectAId);
        this.solutionProjectB = this.solution.GetProject(solutionProjectBId);
        this.solutionProjectC = this.solution.GetProject(solutionProjectCId);

        this.projectB = new(XmlReader.Create(new StringReader(string.Format(this.projectTemplate, dependency1Version))));
        this.projectC = new(XmlReader.Create(new StringReader(string.Format(this.projectTemplate, dependency2Version))));
        this.projectA = new(XmlReader.Create(new StringReader(string.Format(this.projectTemplate, dependantVersion))));

        this.projectAdapter = new InMemoryProjectAdapter(new Dictionary<Microsoft.CodeAnalysis.Project, Microsoft.Build.Evaluation.Project>
        {
            { this.solutionProjectB, this.projectB },
            { this.solutionProjectC, this.projectC },
            { this.solutionProjectA, this.projectA },
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

    public Microsoft.Build.Evaluation.Project LoadBuildProject(Microsoft.CodeAnalysis.Project solutionProject)
        => this.mapping[solutionProject];

    public IReadOnlyCollection<Microsoft.Build.Evaluation.Project> LoadBuildProjects(IEnumerable<Microsoft.CodeAnalysis.Project> solutionProjects)
    {
        List<Microsoft.Build.Evaluation.Project> msBuildProjects = new();
        foreach (Microsoft.CodeAnalysis.Project solutionProject in solutionProjects)
        {
            msBuildProjects.Add(this.mapping[solutionProject]);
        }
        return msBuildProjects;
    }

    public void SaveProject(Microsoft.Build.Evaluation.Project msbuildProject)
    {
    }
}
