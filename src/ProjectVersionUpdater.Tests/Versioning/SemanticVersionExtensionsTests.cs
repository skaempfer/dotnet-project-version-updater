using NuGet.Versioning;

using Xunit;

namespace ProjectVersionUpdater.Tests;

public class SemanticVersionExtensionsTests
{
    [Theory]
    [InlineData("2.0.0", "3.0.0")]
    [InlineData("2.1.1", "3.0.0")]
    [InlineData("2.1.1-pre.1", "2.0.0")]
    public void IncreaseMajor_IncreasesToNextMajorVersion(string current, string expected)
    {
        // Arrange
        SemanticVersion currentVersion = SemanticVersion.Parse(current);

        // Act
        SemanticVersion actual = currentVersion.IncreaseMajor();

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }

    [Theory]
    [InlineData("2.0.0", "2.1.0")]
    [InlineData("2.1.1", "2.2.0")]
    [InlineData("2.1.1-pre", "2.1.0")]
    public void IncreaseMinor_IncreasesToNextMinorVersion(string current, string expected)
    {
        // Arrange
        SemanticVersion currentVersion = SemanticVersion.Parse(current);

        // Act
        SemanticVersion actual = currentVersion.IncreaseMinor();

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }

    [Theory]
    [InlineData("2.0.0", "2.0.1")]
    [InlineData("2.1.1", "2.1.2")]
    [InlineData("2.1.1-pre", "2.1.1")]
    public void IncreasePatch_IncreasesToNextPatchVersion(string current, string expected)
    {
        // Arrange
        SemanticVersion currentVersion = SemanticVersion.Parse(current);

        // Act
        SemanticVersion actual = currentVersion.IncreasePatch();

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }

    [Theory]
    [InlineData("1.0.0", "xyz", "1.0.0-xyz")]
    [InlineData("2.0.0-xyz", "abc", "2.0.0-abc")]
    public void SetReleaseLabel(string current, string label, string expected)
    {
        // Arrange
        SemanticVersion currentVersion = SemanticVersion.Parse(current);

        // Act
        SemanticVersion actual = currentVersion.SetReleaseLabel(label);

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("2.0.0-pre.1", "2.0.0")]
    public void RemoveReleaseLabel(string current, string expected)
    {
        // Arrange
        SemanticVersion currentVersion = SemanticVersion.Parse(current);

        // Act
        SemanticVersion actual = currentVersion.RemoveReleaseLabel();

        // Assert
        Assert.Equal(SemanticVersion.Parse(expected), actual);
    }
}
