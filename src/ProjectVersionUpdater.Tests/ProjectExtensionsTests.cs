using System.IO;
using System.Linq;
using System.Xml;

using Microsoft.Build.Locator;

using NuGet.Versioning;

using Xunit;

namespace ProjectVersionUpdater.Tests;

public class ProjectExtensionsTests
{
    static ProjectExtensionsTests()
    {
        VisualStudioInstance net6Instance = MSBuildLocator.QueryVisualStudioInstances().First(i => i.Version.Major == 6);
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterInstance(net6Instance);
        }
    }

    private readonly string nonVersionedProject = @"
<Project Sdk=""Microsoft.NET.Sdk"">
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
</PropertyGroup >
</Project>
";

    private readonly string versionedProjectTemplate = @"
<Project Sdk=""Microsoft.NET.Sdk"">
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Version>{0}</Version>
</PropertyGroup >
</Project>
";

    [Fact]
    public void GetVersion_NoVersionProperty_ReturnsNull()
    {
        // Arrange
        using XmlReader nonVersionedProject = XmlReader.Create(new StringReader(this.nonVersionedProject));
        Microsoft.Build.Evaluation.Project sut = new Microsoft.Build.Evaluation.Project(nonVersionedProject);

        // Act
        SemanticVersion projectVersion = sut.GetVersion();

        // Assert
        Assert.Null(projectVersion);
    }

    [Fact]
    public void GetVersion_VersionPropertySet_ReturnsVersion()
    {
        // Arrange
        using XmlReader versionedProject = XmlReader.Create(new StringReader(string.Format(this.versionedProjectTemplate, "3.1.0")));
        Microsoft.Build.Evaluation.Project sut = new Microsoft.Build.Evaluation.Project(versionedProject);

        // Act
        SemanticVersion projectVersion = sut.GetVersion();

        // Assert
        Assert.Equal("3.1.0", projectVersion.ToFullString());
    }

    [Fact]
    public void SetVersion_NoVersionProperty_AddsVersion()
    {
        // Arrange
        using XmlReader versionedProject = XmlReader.Create(new StringReader(this.nonVersionedProject));
        Microsoft.Build.Evaluation.Project sut = new Microsoft.Build.Evaluation.Project(versionedProject);

        // Act
        SemanticVersion initialVersion = sut.GetVersion();
        sut.SetVersion("3.2.1");

        // Assert
        Assert.Equal("3.2.1", sut.GetVersion().ToString());
    }

    [Fact]
    public void SetVersion_VersionPropertySet_UpdatesVersion()
    {
        // Arrange
        using XmlReader versionedProject = XmlReader.Create(new StringReader(string.Format(this.versionedProjectTemplate, "2.0.0")));
        Microsoft.Build.Evaluation.Project sut = new Microsoft.Build.Evaluation.Project(versionedProject);

        // Act
        SemanticVersion initialVersion = sut.GetVersion();
        sut.SetVersion("3.0.0");

        // Assert
        Assert.Equal("3.0.0", sut.GetVersion().ToString());
    }
}
