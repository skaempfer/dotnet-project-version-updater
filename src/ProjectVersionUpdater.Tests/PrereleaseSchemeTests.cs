using NuGet.Versioning;
using Xunit;

namespace ProjectVersionUpdater.Tests;

public class PrereleaseSchemeTests
{
    [Theory]
    [InlineData("1.0.0", "2.0.0-xyz.1")]
    [InlineData("2.0.0-xyz.1", "2.0.0-xyz.2")]
    [InlineData("3.1.0-major.1", "3.0.0-xyz.1")]
    public void Next_CustomSchemeMajorVersion_NextMajor(string initial, string expected)
    {
        // Arrange
        CustomPrereleaseScheme sut = new CustomPrereleaseScheme("xyz");

        // Act
        SemanticVersion actual = sut.Next(SemanticVersion.Parse(initial), VersionPart.Major);

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }

    [Theory]
    [InlineData("1.0.0", "1.1.0-xyz.1")]
    [InlineData("2.1.0-xyz.1", "2.1.0-xyz.2")]
    [InlineData("3.1.0-minor.1", "3.1.0-xyz.1")]
    public void Next_CustomSchemeMinorVersion_NextMinor(string initial, string expected)
    {
        // Arrange
        CustomPrereleaseScheme sut = new CustomPrereleaseScheme("xyz");

        // Act
        SemanticVersion actual = sut.Next(SemanticVersion.Parse(initial), VersionPart.Minor);

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1-xyz.1")]
    [InlineData("2.0.1-xyz.1", "2.0.1-xyz.2")]
    [InlineData("3.0.1-patch.1", "3.0.1-xyz.1")]
    public void Next_CustomSchemePatchVersion_NextPatch(string initial, string expected)
    {
        // Arrange
        CustomPrereleaseScheme sut = new CustomPrereleaseScheme("xyz");

        // Act
        SemanticVersion actual = sut.Next(SemanticVersion.Parse(initial), VersionPart.Patch);

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }
}
